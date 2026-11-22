using UnityEngine;

namespace CityForge.World
{
    internal sealed class ResidentialWindowLights : MonoBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly Color WarmLight = new(2.2f, .92f, .29f, 1f);
        private Renderer[] renderers;
        private bool highWealth;
        private Renderer glazing;
        private static Material glazingMaterial;

        internal static void Attach(GameObject instance, CityForge.Lots.LotAssetCatalogEntry asset, RegionTimeOfDay time)
        {
            if (asset.category != "Buildings" || !asset.materialOverride ||
                !asset.materialOverride.IsKeywordEnabled("_EMISSION")) return;
            var lights = instance.AddComponent<ResidentialWindowLights>();
            if (asset.nightWindowGlass)
            {
                var shader = Resources.Load<Shader>("Region/NightWindowGlass");
                if (shader)
                {
                    if (!glazingMaterial) glazingMaterial = new Material(shader)
                        { name = "Night window glazing", enableInstancing = true, hideFlags = HideFlags.DontSave };
                    var panes = new GameObject("Night window panes", typeof(MeshFilter), typeof(MeshRenderer));
                    panes.layer = instance.layer;
                    panes.transform.SetParent(instance.transform, false);
                    // Meshes use the source model's normalized bounds. Resolve the fitted
                    // near model in object space so edited footprints and lot rotations
                    // do not detach the glazing, and distant-body bounds cannot stretch it.
                    var model = instance.transform.childCount > 1 ? instance.transform.GetChild(0) : instance.transform;
                    var localBounds = new Bounds();
                    bool hasBounds = false;
                    foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
                    {
                        if (!filter.sharedMesh) continue;
                        var bounds = filter.sharedMesh.bounds;
                        var matrix = instance.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                        for (int corner = 0; corner < 8; corner++)
                        {
                            var point = matrix.MultiplyPoint3x4(bounds.center + Vector3.Scale(bounds.extents,
                                new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1,
                                    (corner & 4) == 0 ? -1 : 1)));
                            if (hasBounds) localBounds.Encapsulate(point);
                            else { localBounds = new Bounds(point, Vector3.zero); hasBounds = true; }
                        }
                    }
                    if (hasBounds)
                    {
                        panes.transform.localPosition = localBounds.min;
                        panes.transform.localScale = localBounds.size;
                    }
                    panes.GetComponent<MeshFilter>().sharedMesh = asset.nightWindowGlass;
                    lights.glazing = panes.GetComponent<MeshRenderer>();
                    lights.glazing.sharedMaterial = glazingMaterial;
                    lights.glazing.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    lights.glazing.receiveShadows = false;
                }
            }
            lights.highWealth = asset.growableWealth == CityForge.Lots.LotWealth.High;
            lights.SetTime(time);
        }

        internal void SetTime(RegionTimeOfDay time)
        {
            if (glazing) glazing.enabled = time is RegionTimeOfDay.Evening or RegionTimeOfDay.Night;
            var emission = time switch
            {
                RegionTimeOfDay.Night => WarmLight,
                RegionTimeOfDay.Evening => new Color(1.21f, .83f, .25f, 1f),
                _ => Color.black
            };
            if (highWealth && time is RegionTimeOfDay.Night or RegionTimeOfDay.Evening)
                emission = time == RegionTimeOfDay.Night ? new Color(1.8f, 1.05f, .49f, 1f)
                    : new Color(1.08f, .64f, .31f, 1f);
            emission.r *= .72f; emission.g *= .72f; emission.b *= .72f;
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
