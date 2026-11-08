using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    /// <summary>Seeded tidal river and a sheltered, south-facing bay in region coordinates.</summary>
    internal sealed class RiverBayLayout
    {
        internal const int RemakeVariantCount = 6;

        internal static bool BayOnLeft(int variant) => variant is 3 or 4 or 5;

        internal static int ChooseRemakeVariant(int previousVariant, int seed)
        {
            int shape = new System.Random(seed).Next(3);
            // Move the bay to the other side every time. Varying its width
            // and depth alone can still read as the same map from overhead.
            return BayOnLeft(previousVariant) ? shape switch { 0 => 1, 1 => 2, _ => 6 }
                : shape switch { 0 => 4, 1 => 5, _ => 3 };
        }
        readonly float bayHead, widthScale;
        readonly bool mirrored, longBends;
        const int ShoreSegments = 112;
        const int IslandSegments = 64;
        readonly float phase, lateralShift;
        readonly Vector2[] islands;
        readonly Vector2[] radii =
        {
            new Vector2(.064f, .047f), new Vector2(.041f, .035f), new Vector2(.025f, .020f)
        };

        public RiverBayLayout(int seed, int variant, bool longBends)
        {
            // Each remake plan changes the water footprint at the scale of districts.
            this.longBends = longBends;
            mirrored = BayOnLeft(variant);
            (bayHead, widthScale) = variant switch
            {
                1 or 4 => (.60f, 1.14f),
                2 or 5 => (.80f, .84f),
                3 or 6 => (.69f, .98f),
                _ => (.72f, 1f)
            };
            var random = new System.Random(seed ^ 0x3a51b);
            phase = (float)random.NextDouble() * Mathf.PI * 2;
            lateralShift = ((float)random.NextDouble() - .5f) * .035f - (variant is 1 or 4 ? .035f : 0);
            islands = new[] { Island(.17f, .085f), Island(.37f, .06f), Island(.52f, -.036f) };

            Vector2 Island(float z, float offset)
            {
                z += ((float)random.NextDouble() - .5f) * .030f;
                z *= bayHead / .72f;
                return new Vector2(Center(z) + offset * widthScale + ((float)random.NextDouble() - .5f) * .026f, z);
            }
        }

        float LayoutX(float x) => mirrored ? 1 - x : x;

        float Center(float z) => .67f + lateralShift + .041f * Mathf.Sin(z * 8 + phase);

        float HalfWidth(float z)
        {
            float taper = Mathf.Sqrt(Mathf.Clamp01(1 - Mathf.Pow(Mathf.InverseLerp(.54f * (bayHead / .72f), bayHead, z), 2)));
            float width = .155f + .092f * Mathf.Exp(-Mathf.Pow((z - .21f) / .175f, 2)) -
                .027f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.35f, .55f, z));
            return widthScale * taper * (width + .013f * Mathf.Sin(z * 34 + phase) + .008f * Mathf.Sin(z * 61 - phase));
        }

        float IslandRadius(int i, float angle) =>
            1 + .13f * Mathf.Sin(3 * angle + phase + i) + .06f * Mathf.Cos(5 * angle - phase);

        public float LandDistance(Vector2 point)
        {
            point.x = LayoutX(point.x);
            float z = Mathf.Min(point.y, bayHead);
            float shore = Mathf.Abs(point.x - Center(z)) - HalfWidth(z);
            if (point.y >= bayHead) shore = new Vector2(shore, point.y - bayHead).magnitude;
            for (int i = 0; i < islands.Length; i++)
            {
                var d = point - islands[i];
                var scaled = new Vector2(d.x / radii[i].x, d.y / radii[i].y);
                shore = Mathf.Max(shore, (IslandRadius(i, Mathf.Atan2(scaled.y, scaled.x)) - scaled.magnitude) * radii[i].y);
            }
            return shore;
        }

        public RegionWaterBody Ocean()
        {
            var outline = new Vector2[ShoreSegments * 2 + 1];
            for (int i = 0; i <= ShoreSegments; i++)
            {
                // Denser samples near the rounded head.
                float t = i / (float)ShoreSegments;
                float z = bayHead * (1 - (1 - t) * (1 - t));
                outline[i] = new Vector2(LayoutX(Center(z) - HalfWidth(z)), z);
                outline[outline.Length - 1 - i] = new Vector2(LayoutX(Center(z) + HalfWidth(z)), z);
            }
            var holes = new RegionWaterContour[islands.Length];
            for (int i = 0; i < holes.Length; i++)
            {
                var points = new Vector2[IslandSegments];
                for (int j = 0; j < points.Length; j++)
                {
                    float angle = j * (2 * Mathf.PI / points.Length);
                    points[j] = islands[i] + Vector2.Scale(radii[i], new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))) *
                        IslandRadius(i, angle);
                    points[j].x = LayoutX(points[j].x);
                }
                holes[i] = new RegionWaterContour { points = points };
            }
            return new RegionWaterBody
            {
                id = "regional-bay", kind = WaterBodyKind.Ocean, outline = outline, holes = holes,
                appearanceProfile = "coastal-ocean", hasSurfaceLevel = true,
                surfaceLevelNormalized = 12f / PrototypeRegionData.Height
            };
        }

        internal const float MouthRadius=.040f;

        internal static float RoundedMouthDistance(float coast,float river)
        {
            float blend=Mathf.Max(0,1-Mathf.Abs(coast-river)/MouthRadius);
            return Mathf.Min(coast,river)-MouthRadius*.25f*blend*blend;
        }

        public float RiverCenter(float z) => LayoutX(RiverCenterInPlan(z));

        float RiverCenterInPlan(float z)
        {
            float upstream = .24f + lateralShift + .082f * Mathf.Sin(z * 16 + phase)
                + .021f * Mathf.Sin(z * 30 - phase);
            if (longBends)
                upstream = .25f + lateralShift + .077f * Mathf.Sin(z * 10.5f + phase);
            float mouth = Center(z) - .085f - .17f * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.10f, .43f, z));
            return Mathf.Lerp(mouth, upstream, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(longBends ? .22f : .26f, longBends ? .59f : .51f, z)));
        }

        public float RiverHalfWidth => .024f + .003f * Mathf.Sin(phase);

        public void AddWaterways(RegionCreationSettings recipe, float side, List<RegionWaterBody> bodies)
        {
            var main = new Vector2[193];
            for (int i = 0; i < main.Length; i++)
            {
                float z = i / (float)(main.Length - 1);
                main[i] = new Vector2(RiverCenter(z), z);
            }
            bodies.Add(Channel("tidal-river", main, RiverHalfWidth, .038f, false, false));

            int streams = recipe.streams == 0 ? 0 : recipe.streams == 1 ? 2 : 5;
            int total = recipe.rivers + streams;
            for (int i = 0; i < total; i++)
            {
                bool stream = i >= recipe.rivers;
                float z = Mathf.Lerp(.50f, .95f, (i + .5f) / total);
                float end = RiverCenterInPlan(z);
                var points = new Vector2[65];
                for (int j = 0; j < points.Length; j++)
                {
                    float t = j / (float)(points.Length - 1);
                    float bend = Mathf.Sin(t * Mathf.PI) * (.012f * Mathf.Sin(t * 7 + phase + i) + .013f);
                    if (longBends && !stream)
                    {
                        // Sweep across the valley before turning downstream into the trunk.
                        float back = 1 - t;
                        bend = 3 * back * t * (-.044f * back + .064f * t);
                    }
                    points[points.Length - 1 - j] = new Vector2(LayoutX(t * end), z + bend);
                }
                float half = stream ? Mathf.Lerp(1.83f, 6.1f, .5f + .5f * Mathf.Sin(phase + i)) / (2 * side) : .0065f;
                bodies.Add(Channel((stream ? "bay-stream-" : "bay-tributary-") + i, points,
                    half, stream ? 2 / side : .017f, stream, true));
            }
        }

        static RegionWaterBody Channel(string id, Vector2[] points, float half, float bank, bool stream, bool alongX) =>
            new RegionWaterBody
            {
                id = id, kind = WaterBodyKind.River, centerline = points, halfWidthNormalized = half,
                bankWidthNormalized = bank, channelVersion = 1, isStream = stream, channelAlongX = alongX,
                flowSpeed = .12f, appearanceProfile = "calm-river", hasSurfaceLevel = true,
                surfaceLevelNormalized = 12f / PrototypeRegionData.Height
            };
    }
}
