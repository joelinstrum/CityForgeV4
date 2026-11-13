using System;
using CityForge.World;
using UnityEngine;

namespace CityForge.Lots
{
    [Serializable]
    public sealed class LotAssetCatalogEntry
    {
        public string id;
        public string label;
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
            (string.IsNullOrEmpty(era) || eraId == era);

        public bool IsCharacter => category == "Characters";
        internal float DefaultPlacementScale => id == LotCharacterDeck.HarborHouse ? 1.5f : 1f;
        public GameObject prefab;
        public GameObject autumnPrefab, winterPrefab;
        internal bool IsSeasonalTree => category == "Trees" && autumnPrefab && winterPrefab;

        internal GameObject PrefabFor(RegionSeason season) => season switch
        {
            RegionSeason.Fall when autumnPrefab => autumnPrefab,
            RegionSeason.Winter when winterPrefab => winterPrefab,
            _ => prefab
        };
        public Vector2 footprint;
        public bool playerPlaceable;
        public int fromYear, toYear;
        public bool fitModel;
        public Material materialOverride;
        public float fenceSpan, fenceDepth;
        public bool fenceAlongZ;
        public Vector3 fenceCenter;
        public GameObject fencePost;
        public Vector3 fencePostCenter;
        public float fencePostWidth, fenceRailLift;
    }

    public sealed class LotAssetCatalog : ScriptableObject
    {
        public const string StreetFurniture = "Street Furniture";

        public LotAssetCatalogEntry[] entries = Array.Empty<LotAssetCatalogEntry>();

        public static LotAssetCatalog Load()
        {
            var catalog = Resources.Load<LotAssetCatalog>("Lots/LocalCatalog");

            return catalog;
        }

        public LotAssetCatalogEntry Find(string id) => Array.Find(entries, item => item != null && item.id == id);
    }

    public static class LotRuntimeLoader
    {
        private static Mesh gardenSoilMesh;
        private static Material gardenSoilMaterial;
        private static readonly int LeafColor = Shader.PropertyToID("_BaseColor");

        public static GameObject InstantiateObject(LotObjectDefinition item, LotAssetCatalogEntry asset, Transform parent, RegionSeason season = RegionSeason.Summer)
        {
            if (asset == null || !asset.prefab) throw new InvalidOperationException("Missing lot asset: " + item.assetId);
            var wrapper = new GameObject(asset.label);
            wrapper.transform.SetParent(parent, false);
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
            wrapper.transform.localPosition = new Vector3(item.position.x, 0f, item.position.y);
            wrapper.transform.localRotation = Quaternion.Euler(0f, item.yaw, 0f);
            wrapper.transform.localScale = Vector3.one * (item.scale * PlaceableTreeScale(asset));
            if (asset.category == "Trees")
                TintPlaceableTree(wrapper, asset.id, season);
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
            if (asset.category != "Trees" || !asset.prefab) return 1f;
            if (!TreeDimensions.TryGetValue(asset.prefab, out var dimensions))
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
                TreeDimensions[asset.prefab] = dimensions;
            }
            float span = Mathf.Max(dimensions.x, dimensions.z);
            float height = asset.fitModel && span > .001f
                ? dimensions.y * Mathf.Max(asset.footprint.x, asset.footprint.y) / span : dimensions.y;
            return height > 12f ? .75f : 1f;
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
                        name.Contains("leaves", StringComparison.OrdinalIgnoreCase);
                    bool wood = name.Contains("bark", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("trunk", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("branch", StringComparison.OrdinalIgnoreCase);
                    Color tint = leaf ? foliage : wood ? bark : Color.white;
                    if (tint == Color.white) continue;

                    renderer.GetPropertyBlock(properties, slot);
                    if (properties.isEmpty) renderer.GetPropertyBlock(properties);
                    var color = properties.HasColor(LeafColor)
                        ? properties.GetColor(LeafColor) : material.GetColor(LeafColor);
                    properties.SetColor(LeafColor, color * tint);
                    renderer.SetPropertyBlock(properties, slot);
                }
            }
        }

        public static bool Fits(LotDefinition lot, LotObjectDefinition item, LotAssetCatalogEntry asset)
        {
            var bounds = Footprint(item, asset);
            var half = lot.Size * .5f;
            const float tolerance = .001f;
            return bounds.xMin >= -half.x - tolerance && bounds.xMax <= half.x + tolerance &&
                bounds.yMin >= -half.y - tolerance && bounds.yMax <= half.y + tolerance;
        }

        public static Rect Footprint(LotObjectDefinition item, LotAssetCatalogEntry asset, Vector2? footprintOverride = null)
        {
            var size = (item.fenceLength > 0 && asset.fenceSpan > 0
                ? new Vector2(item.fenceLength + asset.fencePostWidth, Mathf.Max(asset.fenceDepth, asset.fencePostWidth)) : (footprintOverride ?? PlacementFootprint(asset))) * (item.scale * PlaceableTreeScale(asset));
            var angle = item.yaw * Mathf.Deg2Rad;
            var c = Mathf.Abs(Mathf.Cos(angle));
            var s = Mathf.Abs(Mathf.Sin(angle));
            var extent = new Vector2(size.x * c + size.y * s, size.x * s + size.y * c);
            return new Rect(item.position - extent * .5f, extent);
        }
    }
}
