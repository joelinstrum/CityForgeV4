using UnityEngine;

namespace CityForge.World
{
    public sealed partial class MeadowDistrictLandscape
    {
        private void ApplySoftMeadowBlend(ref float[,,] controlMaps, bool sharesCachedPaint)
        {
            if (IsRegionOverview || View.Climate != RegionClimate.Temperate ||
                View.VisualSeason != RegionSeason.Summer || View.MountainTest)
                return;

            var textures = new[]
            {
                Resources.Load<Texture2D>("LocalMeadowBlend/1"),
                Resources.Load<Texture2D>("LocalMeadowBlend/2"),
                Resources.Load<Texture2D>("LocalMeadowBlend/3")
            };
            if (!textures[0] || !textures[1] || !textures[2]) return;

            var layers = terrainData.terrainLayers;
            int[] grassSlots = { 1, 2, 4 };
            float[] repeats = { 8f, 12f, 17f };
            for (int i = 0; i < grassSlots.Length; i++)
            {
                int slot = grassSlots[i];
                var layer = Own(Instantiate(layers[slot]));
                layer.diffuseTexture = textures[i];
                layer.tileSize = Vector2.one * repeats[i];
                layers[slot] = layer;
            }
            terrainData.terrainLayers = layers;

            // The cached paint may be shared by the loaded region. Preserve every non-grass weight.
            if (sharesCachedPaint) controlMaps = (float[,,])controlMaps.Clone();
            int height = controlMaps.GetLength(0);
            int width = controlMaps.GetLength(1);
            float stepX = terrainData.size.x / (width - 1);
            float stepZ = terrainData.size.z / (height - 1);
            for (int z = 0; z < height; z++)
            for (int x = 0; x < width; x++)
            {
                float grass = controlMaps[z, x, 1] + controlMaps[z, x, 2] + controlMaps[z, x, 4];
                if (grass < .0001f) continue;

                float px = Origin.x + x * stepX;
                float pz = Origin.y + z * stepZ;
                float soil = Mathf.Lerp(.07f, .21f, Mathf.PerlinNoise(px / 19f + 88, pz / 19f + 112));
                float detail = Mathf.Lerp(.40f, .67f, Mathf.PerlinNoise(px / 12f + 32, pz / 12f + 55));
                controlMaps[z, x, 1] = grass * (1f - soil) * detail;
                controlMaps[z, x, 2] = grass * (1f - soil) * (1f - detail);
                controlMaps[z, x, 4] = grass * soil;
            }
        }
    }
}
