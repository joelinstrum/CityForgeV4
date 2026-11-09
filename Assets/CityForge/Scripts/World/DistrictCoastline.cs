using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        internal int[] DeepHarborDistricts { get; private set; } = Array.Empty<int>();

        internal bool HasDeepHarbor(int district) => Array.IndexOf(DeepHarborDistricts, district) >= 0;

        internal bool CanOpenDistrictContext => CanEditTowns && Relief && Relief.SurfaceReady;

        internal bool CanDesignCoastline(int district)
        {
            if (!CanOpenDistrictContext || district < 0 || district >= DistrictCount || Waterless || Coastline || !wantsMeadow)
                return false;
            var area = new Rect(DistrictOrigin(district) / MapSide + Vector2.one * .5f, Vector2.one / GridSide);
            return Array.Exists(Data.WaterBodies, body => body.kind == WaterBodyKind.Ocean && body.HasShorelineInside(area));
        }

        internal static void ValidateDeepHarbors(int[] districts, int districtCount)
        {
            if (districts == null) return;
            var unique = new HashSet<int>();
            foreach (int district in districts)
                if (district < 0 || district >= districtCount || !unique.Add(district))
                    throw new InvalidDataException("Invalid district coastline settings.");
        }

        internal string SetCoastlineDesign(int district, bool deep)
        {
            if (!CanDesignCoastline(district)) return "Choose a coastal district on the region map.";
            if (HasDeepHarbor(district) == deep) return null;
            DeepHarborDistricts = deep ? DeepHarborDistricts.Append(district).OrderBy(cell => cell).ToArray() :
                DeepHarborDistricts.Where(cell => cell != district).ToArray();
            return null;
        }
    }

    public sealed partial class MeadowDistrictLandscape
    {
        private bool hasDeepHarbor;

        private IEnumerator ShapeDeepHarborStages(float[,] heights)
        {
            hasDeepHarbor = coastalOceans.Length > 0 && View.ActiveDistrictMembers.Any(View.HasDeepHarbor);
            if (!hasDeepHarbor) yield break;

            yield return BuildCoastalBeachDistances(heights);
            int count = heights.GetLength(0);
            float step = Side / (count - 1);
            float sea = View.SeaLevel;
            float vertical = terrainData.size.y;
            var origin = Origin;
            float harborDepth = Mathf.Min(10f, sea - .25f);
            float side = Side;
            yield return RegionBuildSteps.ComputeRows(count, z =>
            {
                for (int x = 0; x < count; x++)
                {
                    float original = heights[z, x] * vertical;
                    if (original >= sea + 1f || original <= sea - harborDepth) continue;
                    // Sample just inside the rendered bounds so the final row stays on the harbor profile.
                    var point = origin + new Vector2(Mathf.Min(x * step, side - .01f),
                        Mathf.Min(z * step, side - .01f));
                    float distance = CoastalBeachDistance(point);
                    if (distance < -side || distance > 5f) continue;
                    float weight = HarborWaterWeight(point);
                    if (weight <= 0) continue;

                    // Steepen both sides of the crossing by the same factor so interpolation
                    // retains the waterline instead of pulling it toward individual dry vertices.
                    float relative = original - sea;
                    float shoreGrade = relative * 2.5f;
                    float shaped;
                    if (relative >= 0)
                    {
                        float dryBlend = 1 - Mathf.SmoothStep(0, 1, Mathf.Max(relative, distance / 5f));
                        shaped = Mathf.Lerp(relative, shoreGrade, dryBlend);
                    }
                    else
                    {
                        float offshore = Mathf.Max(0, -distance);
                        float deepBlend = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(step * 1.5f, Mathf.Max(7f, step * 3f), offshore));
                        shaped = Mathf.Max(-harborDepth, Mathf.Lerp(shoreGrade, -harborDepth, deepBlend));
                    }
                    heights[z, x] = Mathf.Lerp(original, sea + shaped, weight) / vertical;
                }
            });
        }

        private float DeepHarborWeight(Vector2 point)
        {
            if (!hasDeepHarbor) return 0;
            int cell = View.DistrictAtWorld(new Vector3(point.x, 0, point.y));
            if (cell < 0 || !View.HasDeepHarbor(cell)) return 0;

            var local = point - View.DistrictOrigin(cell);
            float edge = Mathf.Min(Mathf.Min(local.x, local.y), View.DistrictSide - Mathf.Max(local.x, local.y));
            // Every original district edge keeps its canonical profile, including combined districts.
            return Mathf.SmoothStep(0, 1, edge / 20f);
        }
    }
}
