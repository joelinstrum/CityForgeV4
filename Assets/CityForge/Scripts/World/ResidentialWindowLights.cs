using UnityEngine;

namespace CityForge.World
{
    internal sealed class ResidentialWindowLights : MonoBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly Color WarmLight = new(2.2f, .92f, .29f, 1f);
        private Renderer[] renderers;

        internal static void Attach(GameObject instance, CityForge.Lots.LotAssetCatalogEntry asset, RegionTimeOfDay time)
        {
            if (asset.growableWealth != CityForge.Lots.LotWealth.Medium || !asset.materialOverride ||
                !asset.materialOverride.IsKeywordEnabled("_EMISSION")) return;
            instance.AddComponent<ResidentialWindowLights>().SetTime(time);
        }

        internal void SetTime(RegionTimeOfDay time)
        {
            var emission = time switch
            {
                RegionTimeOfDay.Night => WarmLight,
                RegionTimeOfDay.Evening => new Color(1.21f, .83f, .25f, 1f),
                _ => Color.black
            };
            renderers ??= GetComponentsInChildren<Renderer>(true);
            var properties = new MaterialPropertyBlock();
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    if (!materials[slot] || !materials[slot].IsKeywordEnabled("_EMISSION")) continue;
                    renderer.GetPropertyBlock(properties, slot);
                    if (properties.isEmpty) renderer.GetPropertyBlock(properties);
                    properties.SetColor(EmissionColor, emission);
                    renderer.SetPropertyBlock(properties, slot);
                }
            }
        }
    }
}
