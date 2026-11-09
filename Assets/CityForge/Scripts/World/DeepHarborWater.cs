using System;
using UnityEngine;
using WaveHarmonic.Crest;

namespace CityForge.World
{
    public sealed partial class MeadowDistrictLandscape
    {
        private bool EntireDistrictHasHarborWater =>
            hasDeepHarbor && Array.TrueForAll(View.ActiveDistrictMembers, View.HasDeepHarbor);

        internal float HarborWaterWeight(Vector2 point)
        {
            if (!hasDeepHarbor) return 0;
            int cell = View.DistrictAtWorld(new Vector3(point.x, 0, point.y));
            if (cell < 0 || !View.HasDeepHarbor(cell) || !View.InActiveDistrict(point)) return 0;
            if (!View.IsCombinedDistrict) return 1;

            const float transition = 24f;
            int grid = View.GridSide;
            var members = View.ActiveDistrictMembers;
            var local = point - View.DistrictOrigin(cell);
            float side = View.DistrictSide;
            float edge = transition;
            if (cell % grid > 0 && IsBeachMember(cell - 1)) edge = Mathf.Min(edge, local.x);
            if (cell % grid < grid - 1 && IsBeachMember(cell + 1)) edge = Mathf.Min(edge, side - local.x);
            if (cell >= grid && IsBeachMember(cell - grid)) edge = Mathf.Min(edge, local.y);
            if (cell < grid * (grid - 1) && IsBeachMember(cell + grid)) edge = Mathf.Min(edge, side - local.y);
            return Mathf.SmoothStep(0, 1, edge / transition);

            bool IsBeachMember(int neighbor) => Array.IndexOf(members, neighbor) >= 0 && !View.HasDeepHarbor(neighbor);
        }

        private ScatteringTextureLodInputData BuildHarborWaterColors(Transform holder, bool groundFollowing)
        {
            if (!hasDeepHarbor) return null;
            const int resolution = 256;
            float inputSide = Side * 1.04f;
            float pad = (inputSide - Side) * .5f;
            var scatter = new Color[resolution * resolution];
            var absorption = new Color[scatter.Length];
            for (int z = 0; z < resolution; z++)
            for (int x = 0; x < resolution; x++)
            {
                var local = new Vector2((x + .5f) * inputSide / resolution - pad,
                    (z + .5f) * inputSide / resolution - pad);
                // Extend edge texels past the clipped surface to avoid a strip of base ocean color.
                var point = Origin + new Vector2(Mathf.Clamp(local.x, .01f, Side - .01f),
                    Mathf.Clamp(local.y, .01f, Side - .01f));
                float weight = HarborWaterWeight(point);
                // The ocean renderer clips its own footprint. Use its water level here
                // because the saved shoreline outline can omit sea-level water near an estuary.
                if (weight > 0 && groundFollowing)
                {
                    var uv = point / View.MapSide + Vector2.one * .5f;
                    float level = RegionalRiverNetwork.GroundWaterLevel(View.Data, uv.x, uv.y);
                    weight *= 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(View.SeaLevel + .5f, View.SeaLevel + 2f, level));
                }
                int index = z * resolution + x;
                // Scale both coefficients to obscure the seabed faster while keeping
                // their ratio, which controls the color of deep water.
                const float density = 3f;
                scatter[index] = new Color(.031f * density, .024f * density, .017f * density, weight);
                absorption[index] = new Color(.132f * density, .114f * density, .129f * density, weight);
            }

            Water.ScatteringLod.Enabled = true;
            Water.ScatteringLod.Resolution = resolution;
            Water.AbsorptionLod.Enabled = true;
            Water.AbsorptionLod.Resolution = resolution;
            var scatteringObject = InputObject("Deep Harbor blue-green scattering");
            var scatterInput = WaveHarmonic.Crest._Extensions.AddComponent<ScatteringLodInput>(scatteringObject, LodInputMode.Texture);
            scatterInput.Queue = 10;
            scatterInput.FeatherWidth = 0;
            var data = scatterInput.GetData<ScatteringTextureLodInputData>();
            data.Texture = InputTexture("Deep Harbor scattering", scatter);

            var absorptionObject = InputObject("Deep Harbor absorption");
            var absorptionInput = WaveHarmonic.Crest._Extensions.AddComponent<AbsorptionLodInput>(absorptionObject, LodInputMode.Texture);
            absorptionInput.Queue = 10;
            absorptionInput.FeatherWidth = 0;
            absorptionInput.GetData<AbsorptionTextureLodInputData>().Texture = InputTexture("Deep Harbor absorption", absorption);
            return data;

            GameObject InputObject(string name)
            {
                var input = new GameObject(name);
                input.transform.SetParent(holder, false);
                input.transform.position = new Vector3(Origin.x + Side * .5f, View.SeaLevel, Origin.y + Side * .5f);
                input.transform.localScale = new Vector3(inputSide, 1, inputSide);
                return input;
            }

            Texture2D InputTexture(string name, Color[] pixels)
            {
                var texture = Own(new Texture2D(resolution, resolution, TextureFormat.RGBAHalf, false, true)
                {
                    name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
                });
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                return texture;
            }
        }
    }
}
