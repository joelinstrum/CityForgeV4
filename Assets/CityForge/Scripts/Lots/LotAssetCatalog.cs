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
            wrapper.transform.localPosition = new Vector3(item.position.x, 0f, item.position.y);
            wrapper.transform.localRotation = Quaternion.Euler(0f, item.yaw, 0f);
            wrapper.transform.localScale = Vector3.one * item.scale;
            return wrapper;
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
                ? new Vector2(item.fenceLength + asset.fencePostWidth, Mathf.Max(asset.fenceDepth, asset.fencePostWidth)) : (footprintOverride ?? asset.footprint)) * item.scale;
            var angle = item.yaw * Mathf.Deg2Rad;
            var c = Mathf.Abs(Mathf.Cos(angle));
            var s = Mathf.Abs(Mathf.Sin(angle));
            var extent = new Vector2(size.x * c + size.y * s, size.x * s + size.y * c);
            return new Rect(item.position - extent * .5f, extent);
        }
    }
}
