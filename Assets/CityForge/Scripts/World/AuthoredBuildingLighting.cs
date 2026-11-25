using UnityEngine;

namespace CityForge.World
{
    // Authored recessed rooms retain their geometry; only their emission changes with the district clock.
    [ExecuteAlways]
    public sealed class AuthoredBuildingLighting : MonoBehaviour
    {
        public Light[] fixtureLights;
        Renderer[] surfaces;
        PrototypeRegionView region;
        RegionTimeOfDay? applied;
        bool previewOverride;
        RegionTimeOfDay previewTime;
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        void OnEnable() { surfaces = GetComponentsInChildren<Renderer>(true); region = GetComponentInParent<PrototypeRegionView>(); applied = null; SetTime(RegionTimeOfDay.Noon); }
        void LateUpdate()
        {
            if (!region) region = GetComponentInParent<PrototypeRegionView>();
            if (previewOverride) SetTime(previewTime);
            else if (region) SetTime(region.TimeOfDay);
            // The district switches mesh renderers, not whole prefab roots. Avoid duplicate pools from the hidden tier.
            bool visible = false;
            if (surfaces != null) foreach (var surface in surfaces) if (surface && surface.enabled) { visible = true; break; }
            bool lit = applied is RegionTimeOfDay.Evening or RegionTimeOfDay.Night;
            if (fixtureLights != null) foreach (var light in fixtureLights) if (light) light.enabled = lit && visible;
        }
        public void SetPreviewTime(RegionTimeOfDay time, int cullingMask)
        {
            previewOverride = true; previewTime = time;
            if (fixtureLights != null) foreach (var light in fixtureLights) if (light) light.cullingMask = cullingMask;
            SetTime(time);
        }
        public void SetTime(RegionTimeOfDay time)
        {
            if (applied == time) return;
            applied = time;
            surfaces ??= GetComponentsInChildren<Renderer>(true);
            float gain = time == RegionTimeOfDay.Night ? 1.8f : time == RegionTimeOfDay.Evening ? 1.1f : 0;
            var block = new MaterialPropertyBlock();
            foreach (var surface in surfaces)
            {
                var materials = surface.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    if (!materials[slot] || !materials[slot].IsKeywordEnabled("_EMISSION")) continue;
                    block.Clear(); surface.GetPropertyBlock(block, slot);
                    block.SetColor(EmissionColor, Color.white * gain);
                    surface.SetPropertyBlock(block, slot);
                }
            }
            if (fixtureLights != null)
                foreach (var light in fixtureLights) if (light) light.enabled = gain > 0;
        }
    }
}
