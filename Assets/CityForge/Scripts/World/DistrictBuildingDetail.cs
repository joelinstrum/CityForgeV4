using System.Linq;
using CityForge.CameraSystem;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    internal sealed class DistrictBuildingDetail : MonoBehaviour
    {
        private const int DistantZoom = 2;
        private int distantZoom = DistantZoom;

        private CityForgeCameraController cameraRig;
        private CameraMotionQuality motionQuality;
        private Transform closeRoot,distantRoot;
        private Renderer[] closeRenderers;
        private Renderer[] distantRenderers;
        private (LODGroup group, int tier)[] nativeLevels;
        private bool? showingDistant;

        internal static void Attach(Transform instance, Transform closeModel, LotAssetCatalogEntry asset,
            CityForgeCameraController rig, RegionSeason season)
        {
            if (!asset.distantPrefab)
            {
                var levels = instance.GetComponentsInChildren<LODGroup>()
                    .Select(group => (group, tier: ShadowCompatibleTier(group)))
                    .Where(level => level.tier > 0).ToArray();
                if (levels.Length == 0) return;
                var nativeDetail = instance.gameObject.AddComponent<DistrictBuildingDetail>();
                nativeDetail.nativeLevels = levels;
                nativeDetail.distantZoom = asset.category == "Fences" || asset.preserveTreeMaterials || ChildrenAtPlay.IsChild(asset.id) ? 3 : DistantZoom;
                nativeDetail.Bind(rig);
                return;
            }

            var closeRenderers = closeModel.GetComponentsInChildren<Renderer>();
            if (closeRenderers.Length == 0) return;

            // The models share the same authored origin. Parent the alternate to the
            // fitted visual's parent so fence spans and crop height scaling apply to both.
            var source = asset.PrefabFor(season).transform;
            var distantModel = Instantiate(asset.distantPrefab, closeModel.parent, false).transform;
            float fit = closeModel.localScale.x / source.localScale.x;
            distantModel.localPosition = closeModel.localPosition +
                (distantModel.localPosition - source.localPosition) * fit;
            distantModel.localRotation = closeModel.localRotation *
                Quaternion.Inverse(source.localRotation) * distantModel.localRotation;
            distantModel.localScale *= fit;

            if (asset.category == "Fences" && asset.fenceSpan > 0 && closeModel.parent != instance)
                AlignFenceDistant(closeModel, distantModel);

            var detail = instance.gameObject.AddComponent<DistrictBuildingDetail>();
            detail.distantZoom = asset.category == "Fences" || asset.id.StartsWith("v3-building-r3-", System.StringComparison.Ordinal) ||
                asset.id.StartsWith("v3-building-r2-art-deco-", System.StringComparison.Ordinal) ||
                asset.id.StartsWith("v3-building-c2-", System.StringComparison.Ordinal) ||
                asset.id is LotAssetCatalog.ArtDecoCityCenter or "v3-building-new-england-church" or "v3-building-chapel" or "v3-building-constable-station" or
                    "v3-building-founders-academy" or "v3-building-founders-courthouse" or
                    "v3-building-founders-infirmary" or "v3-building-founders-schoolhouse" or
                    "v3-building-founders-college" ? 3 : DistantZoom;
            bool authoredMaterials = distantModel.GetComponent<AuthoredBuildingLighting>();
            if (authoredMaterials) detail.distantZoom = 3;
            detail.closeRoot=closeModel;detail.distantRoot=distantModel;
            detail.closeRenderers = closeRenderers;
            detail.distantRenderers = distantModel.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < detail.distantRenderers.Length; i++)
            {
                var renderer = detail.distantRenderers[i];
                var original = detail.closeRenderers[Mathf.Min(i, detail.closeRenderers.Length - 1)];
                renderer.shadowCastingMode = original.shadowCastingMode;
                renderer.receiveShadows = original.receiveShadows;
                if (asset.materialOverride)
                {
                    var materials = renderer.sharedMaterials;
                    for (int slot = 0; slot < materials.Length; slot++) materials[slot] = asset.materialOverride;
                    renderer.sharedMaterials = materials;
                }
                else if (!authoredMaterials)
                    renderer.sharedMaterials = original.sharedMaterials;
            }
            detail.Bind(rig);
        }

        private static void AlignFenceDistant(Transform closeModel, Transform distantModel)
        {
            var space = closeModel.parent;
            var near = LocalBounds(closeModel, space);
            var far = LocalBounds(distantModel, space);
            if (near.size.x < .001f || far.size.x < .001f) return;
            // Reduced meshes can lose a few end vertices. Keep their span endpoints
            // and ground seating identical without stretching their height or posts.
            var alignment = new GameObject("Distant fence alignment").transform;
            alignment.SetParent(space, false);
            distantModel.SetParent(alignment, false);
            float stretch = near.size.x / far.size.x;
            alignment.localScale = new Vector3(stretch, 1, 1);
            alignment.localPosition = new Vector3(near.center.x - far.center.x * stretch,
                near.min.y - far.min.y, near.center.z - far.center.z);
        }

        private static Bounds LocalBounds(Transform model, Transform space)
        {
            var bounds = new Bounds(); bool first = true;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                var local = renderer.localBounds;
                var matrix = space.worldToLocalMatrix * renderer.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    var point = matrix.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents,
                        new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1)));
                    if (first) { bounds = new Bounds(point, Vector3.zero); first = false; }
                    else bounds.Encapsulate(point);
                }
            }
            return bounds;
        }

        private static int ShadowCompatibleTier(LODGroup group)
        {
            var levels = group.GetLODs();
            if (levels.Length < 2) return 0;
            bool castsShadows = levels[0].renderers.Any(CastsShadows);
            for (int tier = Mathf.Min(2, levels.Length - 1); tier > 0; tier--)
                if (levels[tier].renderers.Any(renderer => renderer) &&
                    (!castsShadows || levels[tier].renderers.Any(CastsShadows)))
                    return tier;
            return 0;

            static bool CastsShadows(Renderer renderer) =>
                renderer && renderer.shadowCastingMode != ShadowCastingMode.Off;
        }

        private void Bind(CityForgeCameraController rig)
        {
            cameraRig = rig;
            rig.ZoomChanged += ApplyZoom;
            motionQuality = rig.GetComponent<CameraMotionQuality>();
            if (motionQuality) motionQuality.QualityChanged += ApplyMotionQuality;
            ApplyZoom(rig.ZoomLevel);
        }

        private void ApplyMotionQuality(bool reduced) => ApplyZoom(cameraRig.ZoomLevel);

        private void ApplyZoom(int zoom)
        {
            bool distant = zoom >= distantZoom || (motionQuality && motionQuality.ReducedQuality && motionQuality.ReduceAuthoredDetailDuringNavigation);
            if (showingDistant == distant) return;
            showingDistant = distant;
            if (nativeLevels != null)
            {
                foreach (var (group, tier) in nativeLevels) if (group) group.ForceLOD(distant ? tier : -1);
                return;
            }
            foreach (var renderer in closeRenderers) if (renderer) renderer.enabled = !distant;
            foreach (var renderer in distantRenderers) if (renderer) renderer.enabled = distant;
        }

        internal void RefreshSignRenderers()
        {
            if(!closeRoot||!distantRoot)return;
            closeRenderers=closeRoot.GetComponentsInChildren<Renderer>(true);distantRenderers=distantRoot.GetComponentsInChildren<Renderer>(true);
            showingDistant=null;ApplyZoom(cameraRig.ZoomLevel);
        }
        private void OnDestroy()
        {
            if (cameraRig) cameraRig.ZoomChanged -= ApplyZoom;
            if (motionQuality) motionQuality.QualityChanged -= ApplyMotionQuality;
        }
    }
}
