using UnityEngine;

namespace CityForge.World
{
    internal sealed class ResidentialWindowLights : MonoBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly Color WarmLight = new(2.2f, .92f, .29f, 1f);
        private Renderer[] renderers;
        private bool highWealth;

        internal static void Attach(GameObject instance, CityForge.Lots.LotAssetCatalogEntry asset, RegionTimeOfDay time)
        {
            if (asset.growableZone is not (TownZone.Residential1 or TownZone.Commercial1) || !asset.materialOverride ||
                !asset.materialOverride.IsKeywordEnabled("_EMISSION")) return;
            var lights = instance.AddComponent<ResidentialWindowLights>();
            lights.highWealth = asset.growableWealth == CityForge.Lots.LotWealth.High;
            lights.SetTime(time);
        }

        internal void SetTime(RegionTimeOfDay time)
        {
            var emission = time switch
            {
                RegionTimeOfDay.Night => WarmLight,
                RegionTimeOfDay.Evening => new Color(1.21f, .83f, .25f, 1f),
                _ => Color.black
            };
            if (highWealth && time is RegionTimeOfDay.Night or RegionTimeOfDay.Evening)
                emission = time == RegionTimeOfDay.Night ? new Color(1.8f, 1.05f, .49f, 1f)
                    : new Color(1.08f, .64f, .31f, 1f);
            renderers ??= GetComponentsInChildren<Renderer>(true);
            var properties = new MaterialPropertyBlock();
            foreach (var renderer in renderers)
            {
                if (!renderer) continue;
                renderer.GetPropertyBlock(properties);
                properties.SetColor(EmissionColor, emission);
                renderer.SetPropertyBlock(properties);
                var materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    if (!materials[slot] || !materials[slot].IsKeywordEnabled("_EMISSION")) continue;
                    renderer.GetPropertyBlock(properties, slot);
                    if (properties.isEmpty) continue;
                    properties.SetColor(EmissionColor, emission);
                    renderer.SetPropertyBlock(properties, slot);
                }
            }
        }
    }
}
