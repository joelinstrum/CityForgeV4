using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityForge.World
{
    internal static class ResourceSurvey
    {
        const int Resolution = 64;

        internal static RegionalResourceData Generate(PrototypeRegionData data, RegionCreationSettings recipe, int size, RegionTerrainLayout layout)
        {
            string[] types = { "timber", "stone", "fish", "fertile-soil", "pasture", "fresh-water",
                "natural-harbor", "scenic-beauty" };
            var fields = types.Select(_ => new byte[Resolution * Resolution]).ToArray();
            var deposits = new List<MineralDeposit>();
            var random = new System.Random(data.Seed);
            float offset = random.Next(100, 9000);
            float side = NativeCampPlanner.MapSide(size, recipe.GridSide);
            float legacyScale = side / (recipe.GridSide / 2f) / PrototypeRegionData.Side;
            float heightScale = PrototypeRegionData.Height * (recipe.version >= 5 ? 1 : legacyScale);
            float sea = TropicalOceanDepth.DeepRecipe(recipe) ? TropicalOceanDepth.Sea : 12;
            if (recipe.version < 5) sea *= legacyScale;
            var waters = data.WaterBodies.Where(body => body.kind != WaterBodyKind.Ocean)
                .Select(body => (body, bounds: body.Bounds)).ToArray();

            float Height(Vector2 p) => SmoothRiverSurface.Sample(data.Heights, Mathf.Clamp01(p.x), Mathf.Clamp01(p.y)) * heightScale;

            (float fresh, float coast) WaterDistances(Vector2 p)
            {
                float fresh = 700;
                foreach (var (body, bounds) in waters)
                {
                    float pad = 700 / side;
                    if (p.x < bounds.xMin - pad || p.x > bounds.xMax + pad ||
                        p.y < bounds.yMin - pad || p.y > bounds.yMax + pad) continue;
                    float distance = body.centerline?.Length > 1
                        ? (body.IndexedCenterlineDistance(p) - body.halfWidthNormalized) * side
                        : ShoreDistance(body, p) * side;
                    if (body.kind == WaterBodyKind.River || body.kind == WaterBodyKind.Lake || body.kind == WaterBodyKind.Pond)
                        fresh = Mathf.Min(fresh, distance);
                }
                return (fresh, CoastDistance(p));
            }

            float CoastDistance(Vector2 point) => layout != null && layout.HasOcean ? layout.LandDistance(point) * side : 700;

            float abundanceScale = recipe.resourceAbundance switch
            {
                ResourceAbundance.Sparse => .72f,
                ResourceAbundance.Abundant => 1.22f,
                ResourceAbundance.VeryAbundant => 1.48f,
                _ => 1f
            };
            float contrast = recipe.resourceDistribution switch
            {
                ResourceDistribution.Balanced => .12f,
                ResourceDistribution.HighlySpecialized => .72f,
                _ => .38f
            };

            float Adjust(float strength, Vector2 point, int channel)
            {
                if (strength <= 0) return 0;
                // Seeded regional patches vary by resource and cross district borders smoothly.
                float patch = Mathf.PerlinNoise(offset + channel * 31 + point.x * recipe.GridSide * .65f,
                    channel * 13 + point.y * recipe.GridSide * .65f);
                float emphasis = Mathf.Lerp(.45f, 1.55f, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.25f, .75f, patch)));
                return Mathf.Clamp01(strength * abundanceScale * Mathf.Lerp(1, emphasis, contrast));
            }

            void Set(int field, int index, Vector2 point, float strength) =>
                fields[field][index] = (byte)Mathf.RoundToInt(Adjust(strength, point, field) * 255);


            for (int z = 0; z < Resolution; z++)
            for (int x = 0; x < Resolution; x++)
            {
                int index = z * Resolution + x;
                var point = new Vector2(x, z) / (Resolution - 1);
                var (fresh, coast) = WaterDistances(point);
                float water = Mathf.Min(fresh, coast);
                float fish = 1 - Mathf.InverseLerp(0, 420, Mathf.Abs(water));
                Set(2, index, point, fish);
                if (water < 0) continue;

                float height = Height(point);
                float probe = 40 / side;
                var dx = new Vector2(probe, 0);
                var dz = new Vector2(0, probe);
                float slope = new Vector2(Height(point + dx) - Height(point - dx),
                    Height(point + dz) - Height(point - dz)).magnitude / 80;
                float rock = Mathf.Max(Mathf.InverseLerp(.12f, .65f, slope),
                    Mathf.InverseLerp(120, 390, height - sea));
                float lowland = (1 - Mathf.InverseLerp(35, 225, height - sea)) *
                    (1 - Mathf.InverseLerp(.035f, .28f, slope));
                float freshwater = 1 - Mathf.InverseLerp(0, 650, fresh);
                var biome = data.Biomes?.At(point) ?? new RegionBiomeSample(.5f, .35f, .2f, RegionBiome.Meadow);
                float woodland = biome.woodland * (1 - rock * .65f);
                Set(0, index, point, woodland);
                Set(1, index, point, rock);
                Set(3, index, point, lowland * (.35f + biome.moisture * .45f + freshwater * .3f));
                Set(4, index, point, (1 - woodland) * (1 - rock) * (1 - biome.dryness * .6f));
                Set(5, index, point, freshwater);
                Set(7, index, point, .12f + rock * .5f + woodland * .18f + fish * .30f);

                if (coast < 320 && slope < .35f)
                {
                    int wet = 0;
                    for (int angle = 0; angle < 8; angle++)
                    {
                        float radians = angle * Mathf.PI / 4;
                        var nearby = point + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * (340 / side);
                        if (ResourceMap.Contains(nearby) && CoastDistance(nearby) < 0) wet++;
                    }
                    // Open shores have about half a ring of water. Enclosed bays have less.
                    if (wet >= 2 && wet <= 3) Set(6, index, point, (1 - coast / 320) * (wet == 2 ? 1 : .75f));
                }

                if (x % 4 != 2 || z % 4 != 2) continue;
                float iron = rock * Mathf.PerlinNoise(offset + point.x * 13, point.y * 13);
                float coal = lowland * Mathf.PerlinNoise(point.x * 17, offset + point.y * 17);
                if (iron > .32f) AddDeposit("iron", iron);
                if (coal > .48f) AddDeposit("coal", coal);

                void AddDeposit(string type, float abundance) => deposits.Add(new MineralDeposit
                {
                    id = $"survey1-{type}-{index}", type = type, position = point,
                    abundance = Adjust(.30f + abundance, point, type == "iron" ? 8 : 9),
                    radius = Mathf.Clamp(360 / side, .018f, .12f)
                });
            }

            return new RegionalResourceData
            {
                resolution = Resolution, deposits = deposits.ToArray(), discoveries = recipe.resourceDiscovery == ResourceDiscovery.RevealedAtStart
                    ? deposits.Select(deposit => deposit.id).ToArray() : Array.Empty<string>(),
                layers = types.Select((type, i) => new ResourceLayer
                    { type = type, samples = Convert.ToBase64String(fields[i]) }).ToArray()
            };
        }

        static float ShoreDistance(RegionWaterBody body, Vector2 point)
        {
            float nearest = float.PositiveInfinity;
            foreach (var contour in body.Contours)
                for (int i = 0, j = contour.Length - 1; i < contour.Length; j = i++)
                {
                    Vector2 start = contour[j], edge = contour[i] - start;
                    float t = edge.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point - start, edge) / edge.sqrMagnitude) : 0;
                    nearest = Mathf.Min(nearest, (point - start - edge * t).sqrMagnitude);
                }
            return Mathf.Sqrt(nearest) * (body.Contains(point) ? -1 : 1);
        }
    }
}
