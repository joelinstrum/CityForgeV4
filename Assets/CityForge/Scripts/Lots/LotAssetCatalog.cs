using System;
using CityForge.CameraSystem;
using CityForge.World;
using UnityEngine;

namespace CityForge.Lots
{
    [Serializable]
    public sealed class LotAssetCatalogEntry
    {
        public LotRowGeometry rowGeometry;
        public string id;
        public string label;
        public string description;
        public float defaultBuildingScale;
        public string category;
        public string group;
        public string subcategory;
        public string eraId;
        public TownZone growableZone;
        public LotWealth growableWealth;
        public string GrowableLabel => new LotZoningLayout { zone = growableZone, wealth = growableWealth }.Label;
        public string idleState;
        internal bool CanPreviewGrowable(TownZone zone, LotWealth wealth, string era) =>
            prefab && category == "Buildings" && growableZone == zone && growableWealth == wealth &&
            ResidentialWealthPolicy.Allows(zone, wealth, era) &&
            (string.IsNullOrEmpty(era) || eraId == era);

        public bool IsCharacter => category == "Characters";
        internal float DefaultPlacementScale => category == "Buildings" && float.IsFinite(defaultBuildingScale) && defaultBuildingScale > 0
            ? defaultBuildingScale : id == LotCharacterDeck.HarborHouse ? 1.5f : 1f;
        public GameObject prefab;
        public GameObject distantPrefab;
        public GameObject autumnPrefab, winterPrefab;
        public Mesh nightWindowGlass;
        internal bool IsSeasonalTree => category == "Trees" && autumnPrefab && winterPrefab;

        internal GameObject PrefabFor(RegionSeason season) => season switch
        {
            RegionSeason.Fall when autumnPrefab => autumnPrefab,
            RegionSeason.Winter when winterPrefab => winterPrefab,
            _ => prefab
        };
        public Vector2 footprint;
        // Prepared model bounds at placement scale: width (X), height (Y), depth (Z), in meters.
        public Vector3 buildingDimensionsMeters;
        public bool playerPlaceable;
        // Prepared third-party trees keep their authored palette and model scale.
        public bool preserveTreeMaterials;
        public int fromYear, toYear;
        public bool fitModel;
        public Material materialOverride;
        public float fenceSpan, fenceDepth;
        public float FencePlacementLength => LotPropStyles.Find(id) is { Straight: true } style ? style.Length : fenceSpan;
        // Editor catalog preparation calls this without depending on internal style types.
        public void PrepareFenceSpan()
        {
            if (LotPropStyles.Find(id) is not { Straight: true } || fenceSpan > 0) return;
            fenceAlongZ = footprint.y > footprint.x;
            fenceSpan = Mathf.Max(footprint.x, footprint.y);
            fenceDepth = Mathf.Min(footprint.x, footprint.y);
        }
        public bool fenceAlongZ;
        public Vector3 fenceCenter;
        public GameObject fencePost;
        public Vector3 fencePostCenter;
        public float fencePostWidth, fenceRailLift;
    }

    public sealed class LotAssetCatalog : ScriptableObject
    {
        // Tree review, 2026-11-21: Remove choices are hidden across planting and lot rendering.
        public static bool IsRetiredTree(string id) =>
            id is "animated-tree-beech-2" or "animated-tree-birch-1" or "animated-tree-birch-2" or "animated-tree-birch-3" or "animated-tree-fir-1" or "animated-tree-juniper-1" or "animated-tree-juniper-2" or "animated-tree-pine-1" or "animated-tree-pine-2" or "animated-tree-pine-3" or "animated-tree-pine-4" or "animated-tree-pine-5" or "animated-tree-sycamore" or "oak-pilot-a" or "oak-pilot-b" or "plane-pack-01" or "plane-pack-02" or "plane-pack-03" or "plane-pack-04" or "plane-pack-05" or "plane-pack-06" or "silver-maple" or "silver-maple-b";
        public const string Transport = "Transport";
        public const string StreetFurniture = "Street Furniture";

        public LotAssetCatalogEntry[] entries = Array.Empty<LotAssetCatalogEntry>();

        private static LotAssetCatalog sourceCatalog, sessionCatalog;
        private static LotAssetCatalogEntry[] sourceEntries;
        public static LotAssetCatalog Load()
        {
            var catalog = Resources.Load<LotAssetCatalog>("Lots/LocalCatalog");

            if (!catalog) return null;
            if (!sessionCatalog || sourceCatalog != catalog || !ReferenceEquals(sourceEntries, catalog.entries))
            {
                sourceCatalog = catalog; sourceEntries = catalog.entries;
                sessionCatalog = Instantiate(catalog); sessionCatalog.name = "Local catalog session";
                sessionCatalog.hideFlags = HideFlags.DontSave;
            }
            LotBuildingPropertiesStore.Apply(sessionCatalog);
            foreach(var entry in sessionCatalog.entries)if(entry!=null&&IsRetiredTree(entry.id))entry.playerPlaceable=false;
            return sessionCatalog;
        }

        private LotAssetCatalogEntry[] lookupSource;
        private readonly System.Collections.Generic.Dictionary<string, LotAssetCatalogEntry> lookup = new(StringComparer.Ordinal);
        private void OnValidate() => lookupSource = null;
        public LotAssetCatalogEntry Find(string id)
        {
            if (id == null) return null;
            if (!ReferenceEquals(lookupSource, entries))
            {
                lookup.Clear();
                if (entries != null)
                    foreach (var entry in entries)
                        if (entry != null && entry.id != null && !lookup.ContainsKey(entry.id)) lookup.Add(entry.id, entry);
                lookupSource = entries;
            }
            return lookup.TryGetValue(id, out var found) ? found : null;
        }
    }

    public static class LotRuntimeLoader
    {
        private static Mesh gardenSoilMesh;
        private static Material gardenSoilMaterial;
        private static readonly int LeafColor = Shader.PropertyToID("_BaseColor");

        public static GameObject InstantiateObject(LotObjectDefinition item, LotAssetCatalogEntry asset,
            Transform parent, RegionSeason season = RegionSeason.Summer, CityForgeCameraController districtCamera = null)
        {
            if (asset == null || !asset.prefab) throw new InvalidOperationException("Missing lot asset: " + item.assetId);
            var wrapper = new GameObject(asset.label);
            wrapper.transform.SetParent(parent, false);
            if(LotAssetCatalog.IsRetiredTree(asset.id))return wrapper;
            var model = UnityEngine.Object.Instantiate(asset.PrefabFor(season), wrapper.transform, false);
            if (asset.materialOverride)
                foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                {
                    var slots = renderer.sharedMaterials;
                    for (int i = 0; i < slots.Length; i++) slots[i] = asset.materialOverride;
                    renderer.sharedMaterials = slots;
                }
            if (asset.fitModel)
            {
                var renderers = model.GetComponentsInChildren<Renderer>();
                if (renderers.Length != 0)
                {
                    var bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    var span = Mathf.Max(bounds.size.x, bounds.size.z);
                    if (span > .001f)
                    {
                        float fit = Mathf.Max(asset.footprint.x, asset.footprint.y) / span;
                        model.transform.localScale *= fit;
                        var offset = wrapper.transform.InverseTransformPoint(
                            new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
                        model.transform.localPosition -= offset * fit;
                        if (asset.id == "v3-three-lantern-lamppost")
                            model.transform.localPosition -= Vector3.up * .035f;
                        if (asset.id == "corn-garden" && bounds.size.y > .001f)
                        {
                            // The upper canopy reaches about 1.29 m, with a few tassels above it.
                            const float tallestTassel = 1.45f;
                            float stretch = tallestTassel / (bounds.size.y * fit);
                            var crop = new GameObject("Corn height").transform;
                            crop.SetParent(wrapper.transform, false);
                            model.transform.SetParent(crop, false);
                            crop.localScale = new Vector3(1, stretch, 1);
                        }
                    }
                }
            }
            if (asset.id == "silver-maple" && !asset.preserveTreeMaterials && model.TryGetComponent<LODGroup>(out var mapleLods))
            {
                var tiers = mapleLods.GetLODs();
                if (tiers.Length > 0 && tiers[0].renderers.Length > 0)
                {
                    float baseY = float.PositiveInfinity;
                    foreach (var renderer in tiers[0].renderers)
                        if (renderer) baseY = Mathf.Min(baseY, renderer.bounds.min.y);
                    if (float.IsFinite(baseY)) model.transform.localPosition -= Vector3.up * (baseY - wrapper.transform.position.y);
                }
            }
            if (item.fenceLength > 0 && asset.fenceSpan > 0)
            {
                var span = new GameObject("Fence span").transform;
                span.SetParent(wrapper.transform, false);
                model.transform.SetParent(span, false);
                if (asset.fenceAlongZ)
                    model.transform.localRotation = Quaternion.Euler(0, 90, 0) * model.transform.localRotation;
                model.transform.localPosition = (asset.fenceAlongZ
                    ? Quaternion.Euler(0, 90, 0) * (model.transform.localPosition - asset.fenceCenter)
                    : model.transform.localPosition - asset.fenceCenter);
                span.localScale = new Vector3(item.fenceLength / asset.fenceSpan, 1, 1);
                span.localPosition = Vector3.up * asset.fenceRailLift;
                if (asset.fencePost)
                {
                    AddPost(-item.fenceLength / 2);
                    if (item.fenceEndPost) AddPost(item.fenceLength / 2);
                }

                void AddPost(float x)
                {
                    var post = UnityEngine.Object.Instantiate(asset.fencePost, wrapper.transform, false);
                    post.transform.localPosition += Vector3.right * x - asset.fencePostCenter;
                }
            }
            if (asset.IsCharacter)
            {
                foreach (var animator in model.GetComponentsInChildren<Animator>())
                {
                    animator.applyRootMotion = false;
                    var idle = string.IsNullOrEmpty(asset.idleState) ? "Idle" : asset.idleState;
                    if (!animator.runtimeAnimatorController || animator.layerCount == 0) continue;
                    int state = Animator.StringToHash(animator.GetLayerName(0) + "." + idle);
                    if (animator.HasState(0, state))
                    {
                        animator.Play(state, 0, 0);
                        animator.Update(0);
                    }
                }
            }
            if (asset.id == "corn-garden" || asset.id == "wheat-garden")
                AddGardenSoil(wrapper.transform);
            wrapper.transform.localPosition = new Vector3(item.position.x, item.elevation, item.position.y);
            wrapper.transform.localRotation = Quaternion.Euler(0f, item.yaw, 0f);
            wrapper.transform.localScale = Vector3.one * (item.scale * PlaceableTreeScale(asset));
            if (districtCamera && (!asset.IsCharacter || ChildrenAtPlay.IsChild(asset.id)))
                DistrictBuildingDetail.Attach(wrapper.transform, model.transform, asset, districtCamera, season);
            if(asset.id=="v3-building-lumber-mill")WaterMillWheel.Attach(wrapper);
            if (asset.category == "Trees" && !asset.preserveTreeMaterials)
                TintPlaceableTree(wrapper, asset.id, season);
            if (item.vehicleScript.behavior != LotVehicleBehavior.None && item.vehicleScript.ResourceTask && VehicleProfile.Find(asset.id) is { } cargoProfile)
                VehicleCargoPresentation.Attach(wrapper.transform, cargoProfile)?.SetLoad(item.vehicleScript.resource, 0, item.vehicleScript.Capacity);
            if (ChildrenAtPlay.IsChild(asset.id) && (!item.characterScript.configured ||
                item.characterScript.scriptedBehavior == LotScriptedBehavior.ChildrenAtPlay))
                ChildrenAtPlay.Attach(wrapper);
            DistrictAudioEmitter.Attach(wrapper, asset);
            return wrapper;
        }

        private static void AddGardenSoil(Transform parent)
        {
            if (!gardenSoilMesh)
            {
                const float half = LotDefinition.TileMeters * .5f;
                gardenSoilMesh = new Mesh { name = "Garden soil patch" };
                gardenSoilMesh.vertices = new[]
                {
                    new Vector3(-half, 0, -half), new Vector3(-half, 0, half),
                    new Vector3(half, 0, half), new Vector3(half, 0, -half)
                };
                gardenSoilMesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
                gardenSoilMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                gardenSoilMesh.RecalculateNormals();
            }
            if (!gardenSoilMaterial)
            {
                var texture = Resources.Load<Texture2D>("Lots/PathMaterials/tilled-garden-soil");
                gardenSoilMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    name = "Tilled garden soil",
                    mainTexture = texture,
                    color = texture ? Color.white : new Color(.32f, .22f, .14f)
                };
                gardenSoilMaterial.SetFloat("_Smoothness", .08f);
            }
            var patch = new GameObject("Tilled garden soil", typeof(MeshFilter), typeof(MeshRenderer));
            patch.transform.SetParent(parent, false);
            // Clear the terrain and the Lot Editor grid without adding a visible slab.
            patch.transform.localPosition = Vector3.up * .06f;
            patch.GetComponent<MeshFilter>().sharedMesh = gardenSoilMesh;
            patch.GetComponent<MeshRenderer>().sharedMaterial = gardenSoilMaterial;
        }

        internal static Vector2 PlacementFootprint(LotAssetCatalogEntry asset) =>
            asset.id == "corn-garden" || asset.id == "wheat-garden" ? Vector2.one * LotDefinition.TileMeters : asset.footprint;

        private static readonly System.Collections.Generic.Dictionary<GameObject, Vector3> TreeDimensions = new();

        private static float PlaceableTreeScale(LotAssetCatalogEntry asset)
        {
            if(asset.id=="v3-building-lumber-mill")return 1.3f;
            if (asset.category != "Trees" || !asset.prefab || asset.preserveTreeMaterials) return 1f;
            var dimensions = TreeModelDimensions(asset);
            float span = Mathf.Max(dimensions.x, dimensions.z);
            float height = asset.fitModel && span > .001f
                ? dimensions.y * Mathf.Max(asset.footprint.x, asset.footprint.y) / span : dimensions.y;
            float scale = height > 12f ? .75f : 1f;
            if (asset.id is "balsam-fir" or "london-plane-c") scale *= .75f;
            return scale;
        }

        private static Vector3 TreeModelDimensions(LotAssetCatalogEntry asset)
        {
            var cache = TreeDimensions;
            if (!cache.TryGetValue(asset.prefab, out var dimensions))
            {
                var root = asset.prefab.transform;
                var lod = root.GetComponentInChildren<LODGroup>();
                var renderers = lod && lod.lodCount > 0 ? lod.GetLODs()[0].renderers :
                    root.GetComponentsInChildren<Renderer>();
                var bounds = new Bounds();
                bool first = true;
                foreach (var renderer in renderers)
                {
                    if (!renderer) continue;
                    var local = renderer.localBounds;
                    var matrix = root.worldToLocalMatrix * renderer.localToWorldMatrix;
                    for (int i = 0; i < 8; i++)
                    {
                        var corner = local.center + Vector3.Scale(local.extents,
                            new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                        var point = matrix.MultiplyPoint3x4(corner);
                        if (first) { bounds = new Bounds(point, Vector3.zero); first = false; }
                        else bounds.Encapsulate(point);
                    }
                }
                dimensions = Vector3.Scale(bounds.size, root.localScale);
                cache[asset.prefab] = dimensions;
            }
            return dimensions;
        }
        private static readonly System.Collections.Generic.Dictionary<GameObject,float> PlacedTreeHeights = new();
        internal static float TreePlacementHeight(LotAssetCatalogEntry asset)
        {
            if (asset == null || !asset.prefab || LotAssetCatalog.IsRetiredTree(asset.id)) return 0;
            if (PlacedTreeHeights.TryGetValue(asset.prefab,out float cached)) return cached;
            // Authored root rotations matter: measure upright world bounds, not root-local Y.
            var root=asset.prefab;
            var all=root.GetComponentsInChildren<Renderer>();
            var lod=root.GetComponentInChildren<LODGroup>();
            var near=lod&&lod.lodCount>0?lod.GetLODs()[0].renderers:all;
            if(all.Length==0||near.Length==0)return 0;
            var full=all[0].bounds;foreach(var r in all)full.Encapsulate(r.bounds);
            var close=near[0].bounds;foreach(var r in near)if(r)close.Encapsulate(r.bounds);
            float span=Mathf.Max(full.size.x,full.size.z);
            float fit=asset.fitModel&&span>.001f?Mathf.Max(asset.footprint.x,asset.footprint.y)/span:1;
            return PlacedTreeHeights[asset.prefab]=close.size.y*fit*PlaceableTreeScale(asset);
        }

        private static void TintPlaceableTree(GameObject tree, string assetId, RegionSeason season)
        {
            Color foliage = Color.white;
            if (season == RegionSeason.Summer)
            {
                if (assetId.StartsWith("london-plane-", StringComparison.Ordinal))
                    foliage = new Color(.61f, .58f, .55f, 1f);
                else if (assetId.StartsWith("siberian-elm", StringComparison.Ordinal))
                    foliage = new Color(.66f, .56f, .69f, 1f);
            }
            var bark = new Color(.63f, .50f, .40f, 1f);
            var properties = new MaterialPropertyBlock();
            foreach (var renderer in tree.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    var material = materials[slot];
                    if (!material || !material.HasProperty(LeafColor)) continue;
                    string name = material.name;
                    bool leaf = name.Contains("leaf", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("leaves", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("needle", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("foliage", StringComparison.OrdinalIgnoreCase);
                    bool wood = name.Contains("bark", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("trunk", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("branch", StringComparison.OrdinalIgnoreCase);
                    Color tint = leaf ? foliage : wood ? bark : Color.white;
                    bool oliveMaple = leaf && assetId == "silver-maple-b" && season == RegionSeason.Summer;
                    bool brighten = leaf && (assetId == "balsam-fir" ||
                        (assetId == "silver-maple" && season == RegionSeason.Summer));
                    if (tint == Color.white && !oliveMaple && !brighten) continue;

                    renderer.GetPropertyBlock(properties, slot);
                    if (properties.isEmpty) renderer.GetPropertyBlock(properties);
                    var color = properties.HasColor(LeafColor)
                        ? properties.GetColor(LeafColor) : material.GetColor(LeafColor);
                    // Replace the dark teal multiplier with a lighter olive while retaining the leaf texture.
                    if (oliveMaple) color = new Color(.80f, .85f, .53f, color.a);
                    else if (brighten)
                    {
                        color.r = Mathf.Max(color.r * 1.2f, .68f);
                        color.g = Mathf.Max(color.g * 1.2f, .77f);
                        color.b = Mathf.Max(color.b * 1.2f, .57f);
                    }
                    else color *= tint;
                    properties.SetColor(LeafColor, color);
                    renderer.SetPropertyBlock(properties, slot);
                }
            }
        }

        // Allow each side to extend by 5% of the lot dimension on that axis.
        internal static Vector2 PlacementHalfExtents(LotDefinition lot) => lot.Size * .55f;

        internal static bool FitsPlacementBounds(LotDefinition lot, Rect bounds)
        {
            var half = PlacementHalfExtents(lot);
            const float tolerance = .001f;
            return bounds.xMin >= -half.x - tolerance && bounds.xMax <= half.x + tolerance &&
                bounds.yMin >= -half.y - tolerance && bounds.yMax <= half.y + tolerance;
        }

        public static bool Fits(LotDefinition lot, LotObjectDefinition item, LotAssetCatalogEntry asset)
        {
            if (asset.category == "Trees")
            {
                var half = lot.Size * .5f;
                return Mathf.Abs(item.position.x) <= half.x + .001f && Mathf.Abs(item.position.y) <= half.y + .001f;
            }
            return FitsPlacementBounds(lot, Footprint(item, asset));
        }

        internal static Vector2 ObjectFootprintSize(LotObjectDefinition item, LotAssetCatalogEntry asset, Vector2? footprintOverride = null)
        {
            return (item.fenceLength > 0 && asset.fenceSpan > 0
                ? new Vector2(item.fenceLength + asset.fencePostWidth, Mathf.Max(asset.fenceDepth, asset.fencePostWidth)) : (footprintOverride ?? PlacementFootprint(asset))) * (item.scale * PlaceableTreeScale(asset));
        }

        public static Rect Footprint(LotObjectDefinition item, LotAssetCatalogEntry asset, Vector2? footprintOverride = null)
        {
            var size = ObjectFootprintSize(item, asset, footprintOverride);
            var angle = item.yaw * Mathf.Deg2Rad;
            var c = Mathf.Abs(Mathf.Cos(angle));
            var s = Mathf.Abs(Mathf.Sin(angle));
            var extent = new Vector2(size.x * c + size.y * s, size.x * s + size.y * c);
            return new Rect(item.position - extent * .5f, extent);
        }
    }
}
