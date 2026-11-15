using System.Linq;
using CityForge.CameraSystem;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    internal sealed class DistrictBuildingDetail : MonoBehaviour
    {
        private const int DistantZoom = 3;

        private CityForgeCameraController cameraRig;
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

            var detail = instance.gameObject.AddComponent<DistrictBuildingDetail>();
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
                else renderer.sharedMaterials = original.sharedMaterials;
            }
            detail.Bind(rig);
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
            ApplyZoom(rig.ZoomLevel);
        }

        private void ApplyZoom(int zoom)
        {
            bool distant = zoom >= DistantZoom;
            if (showingDistant == distant) return;
            showingDistant = distant;
            if (nativeLevels != null)
            {
                foreach (var (group, tier) in nativeLevels) group.ForceLOD(distant ? tier : -1);
                return;
            }
            foreach (var renderer in closeRenderers) renderer.enabled = !distant;
            foreach (var renderer in distantRenderers) renderer.enabled = distant;
        }

        private void OnDestroy()
        {
            if (cameraRig) cameraRig.ZoomChanged -= ApplyZoom;
        }
    }
}
