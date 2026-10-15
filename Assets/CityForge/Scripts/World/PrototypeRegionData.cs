using UnityEngine;

namespace CityForge.World
{
    /// <summary>One continuous, deterministic region. Districts are views of this same heightfield.</summary>
    public sealed class PrototypeRegionData
    {
        public const int Resolution = 513;
        // A square approximately 25 miles across its diagonal.
        public const float Side = 28450f;
        public const float Height = 1200f;
        public const int DefaultSeed = 730421;
        public readonly int Seed;
        public RegionWaterBody[] WaterBodies;
        public RegionWaterBody WaterBodyAt(Vector2 point)
        {
            if(WaterBodies==null)return null;
            // Centerlines take precedence at confluences; polygon bodies cover open water.
            foreach(var body in WaterBodies)if(body.centerline!=null)for(int i=1;i<body.centerline.Length;i++){
                var a=body.centerline[i-1];var delta=body.centerline[i]-a;float t=delta.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude):0;
                if(Vector2.Distance(point,a+delta*t)<=body.halfWidthNormalized)return body;
            }
            foreach(var body in WaterBodies)if(body.outline!=null){bool inside=false;var points=body.outline;
                for(int i=0,j=points.Length-1;i<points.Length;j=i++)if((points[i].y>point.y)!=(points[j].y>point.y)&&point.x<(points[j].x-points[i].x)*(point.y-points[i].y)/(points[j].y-points[i].y)+points[i].x)inside=!inside;
                if(inside)return body;
            }
            return null;
        }
        public bool SmoothRiverHeights;
        float[,] riverGrade;float riverSide,riverVertical;
        public void ConfigureContinuousRiver(float side)
        {
            riverSide=side;riverVertical=Height*side/Side;riverGrade=new float[513,513];
            for(int z=0;z<513;z++)for(int x=0;x<513;x++)riverGrade[z,x]=Heights[z,x]+RiverIncision(x/512f,z/512f)/riverVertical;
        }
        float RiverIncision(float u,float v)
        {
            DryRiverBedMigration.Channel(this,riverSide,u,v,out float d,out float width,out float depth,2);
            return depth*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(width*.28f,width,d)));
        }
        public float SmoothHeight(float u,float v)=>riverGrade!=null?SmoothRiverSurface.Sample(riverGrade,u,v)-RiverIncision(u,v)/riverVertical:SmoothRiverSurface.Sample(Heights,u,v);
        public readonly float[,] Heights;
        public readonly float[,] WaterDistance;

        public PrototypeRegionData(int seed,float[,] heights,float[,] waterDistance)
        {
            Seed=seed;
            if(heights.GetLength(0)!=Resolution||heights.GetLength(1)!=Resolution||waterDistance.GetLength(0)!=Resolution||waterDistance.GetLength(1)!=Resolution)throw new System.ArgumentException("Saved map dimensions differ.");
            Heights=heights;WaterDistance=waterDistance;
        }

        public PrototypeRegionData(int seed, bool includeMacroRelief = true, bool useFoothills = true)
        {
            Seed = seed;Heights=new float[Resolution,Resolution];WaterDistance=new float[Resolution,Resolution];
            var offset = new System.Random(seed).Next(100, 10000);
            for (var z = 0; z < Resolution; z++)
            for (var x = 0; x < Resolution; x++)
            {
                var u = x / (float)(Resolution - 1);
                var v = z / (float)(Resolution - 1);
                var coast = Coast(u);
                var coastDistance = (v - coast) * Side;
                var riverDistance = RiverDistance(u, v) * Side;
                WaterDistance[z, x] = Mathf.Min(coastDistance, riverDistance);
                // Frozen original channel/coast bank elevations for footprint compatibility.
                // This legacy surface is replaced throughout the inland region below.
                var broad = Mathf.PerlinNoise(offset + u * 4f, offset + v * 4f);
                var elevation = 35f + 22f * broad + v * 15f;
                elevation += 240f * OriginalBankProfile(u, v, .15f, .86f, .095f, .11f);
                elevation += 140f * OriginalBankProfile(u, v, .85f, .91f, .09f, .07f);
                var originalInlandElevation = elevation;
                var shoreBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-900f, 1000f, coastDistance));
                elevation = Mathf.Lerp(10f, elevation, shoreBlend);
                if (coastDistance < 0f) elevation = Mathf.Max(0f, 10f + coastDistance * .012f);
                // Actual river beds, connected to the sea at a single level. No decorative painted water.
                if (v >= coast - .015f)
                {
                    var valley = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(180f, 550f, riverDistance));
                    elevation = Mathf.Lerp(5f, elevation, valley);
                }
                if (includeMacroRelief)
                {
                    // Rivers define the broad valley floor before surrounding land rises.
                    var distance = Fields.river[z, x];
                    var north = Smooth(.30f, 1f, v);
                    var gradient = 65f * north * (1f - .30f * u);
                    var valleyRise = 180f * Smooth(600f, 9000f, distance)
                        * (.35f + .65f * Smooth(.30f, .95f, v));
                    // Connected spline uplands have varying widths, uneven crests,
                    // a broad saddle and two tapering shoulders. Branches form a
                    // shared envelope rather than stacking into extra peaks.
                    var uplands = Fields.uplands[z, x];
                    var drainage = Smooth(550f, 4500f, distance);
                    var coastPlain = Smooth(1000f, 5000f, coastDistance);
                    var irregularity = 3f * (Mathf.PerlinNoise(offset + u * 1.5f, offset + v * 1.5f) - .5f);
                    var geographicElevation = 45f + (gradient * Smooth(550f, 6000f, distance) + valleyRise + uplands * drainage
                        + irregularity) * coastPlain;
                    if (useFoothills)
                    {
                        // Deliberate asymmetry: preserve every eastern plain sample.
                        var westWeight = 1f - Smooth(.20f, .58f, u);
                        var foothillValley = (180f + 40f * westWeight) * Smooth(600f, 9000f, distance)
                            * (.35f + .65f * Smooth(.30f, .95f, v));
                        var foothillElevation = 45f + (gradient * Smooth(550f, 6000f, distance)
                            + foothillValley + Fields.foothills[z,x] * Smooth(550f, 7000f, distance)
                            + irregularity) * coastPlain;
                        geographicElevation = Mathf.Lerp(geographicElevation, foothillElevation, westWeight);
                    }
                    // Only retain the original height in the immediate shore/channel
                    // bands, to preserve their exact wet footprint. Inland is replaced.
                    elevation += (geographicElevation - originalInlandElevation)
                        * Smooth(550f, 850f, riverDistance) * Smooth(1000f, 1500f, coastDistance);

                }
                Heights[z, x] = elevation / Height;
            }
        }

        public static float Coast(float u) => .235f + .026f * Mathf.Sin(u * 11f)
            + .017f * Mathf.Sin(u * 23f + 1f) - .055f * Mathf.Exp(-Mathf.Pow((u - .53f) / .13f, 2f));

        public float MainRiverCenter(float v) => .50f + .035f * Mathf.Sin(v * 10f) + .018f * Mathf.Sin(v * 21f);
        public float TributaryRiverCenter(float v)
        {
            var t=Mathf.InverseLerp(.52f,1f,v);
            var confluence=MainRiverCenter(.52f);
            return Mathf.Lerp(confluence,.80f,t)+.017f*Mathf.Sin(t*13f);
        }
        private static float RiverDistance(float u, float v)
        {
            var trunk = .50f + .035f * Mathf.Sin(v * 10f) + .018f * Mathf.Sin(v * 21f);
            var distance = Mathf.Abs(u - trunk);
            if (v >= .52f)
            {
                var t = Mathf.InverseLerp(.52f, 1f, v);
                var confluence = .50f + .035f * Mathf.Sin(.52f * 10f) + .018f * Mathf.Sin(.52f * 21f);
                var tributary = Mathf.Lerp(confluence, .80f, t) + .017f * Mathf.Sin(t * 13f);
                distance = Mathf.Min(distance, Mathf.Abs(u - tributary) * .85f + .002f);
            }
            return distance;
        }

        private static float OriginalBankProfile(float u, float v, float x, float z, float sx, float sz) =>
            Mathf.Exp(-Mathf.Pow((u - x) / sx, 2f) - Mathf.Pow((v - z) / sz, 2f));

        private static float Smooth(float a, float b, float value) =>
            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, value));

        private sealed class GeographicFields
        {
            public readonly float[,] river = new float[Resolution, Resolution];
            public readonly float[,] uplands = new float[Resolution, Resolution];
            public readonly float[,] foothills = new float[Resolution, Resolution];
        }

        // These axes belong to the fixed test geography, like its existing waterways.
        // Cache distances once; repeated seeds only vary the subtle residual noise.
        // Increment when fixed water/ridge controls or their field equations change;
        // rebuild with CityForge / Bake Geographic Fields before shipping.
        private const int FieldRevision = 1;
        private static GeographicFields cachedFields;
        private static GeographicFields Fields => cachedFields ??= LoadGeographicFields();

        private static GeographicFields LoadGeographicFields()
        {
            var asset = Resources.Load<TextAsset>("Region/GeographicFields");
            if (asset == null) return BuildGeographicFields();
            using var reader = new System.IO.BinaryReader(new System.IO.MemoryStream(asset.bytes));
            if (reader.ReadInt32() != FieldRevision || reader.ReadInt32() != Resolution)
                return BuildGeographicFields();
            var fields = new GeographicFields();
            foreach (var field in new[] { fields.river, fields.uplands, fields.foothills })
                for (var z=0;z<Resolution;z++) for(var x=0;x<Resolution;x++) field[z,x]=reader.ReadSingle();
            return fields;
        }

#if UNITY_EDITOR
        public static void BakeGeographicFields(string path)
        {
            var fields = BuildGeographicFields();
            using (var writer = new System.IO.BinaryWriter(System.IO.File.Create(path)))
            {
                writer.Write(FieldRevision); writer.Write(Resolution);
                foreach(var field in new[] {fields.river,fields.uplands,fields.foothills})
                    for(var z=0;z<Resolution;z++) for(var x=0;x<Resolution;x++) writer.Write(field[z,x]);
            }
            // Float serialization must preserve the generated fields exactly.
            using var reader = new System.IO.BinaryReader(System.IO.File.OpenRead(path));
            reader.ReadInt32(); reader.ReadInt32();
            foreach(var field in new[] {fields.river,fields.uplands,fields.foothills})
                for(var z=0;z<Resolution;z++) for(var x=0;x<Resolution;x++)
                    if(reader.ReadSingle()!=field[z,x]) throw new System.InvalidOperationException("Geographic cache mismatch.");
        }
#endif

        private static GeographicFields BuildGeographicFields()
        {
            var trunk = new Vector2[81];
            for (var i = 0; i < trunk.Length; i++)
            {
                var v = Mathf.Lerp(.15f, 1.2f, i / 80f);
                trunk[i] = new Vector2(.50f + .035f * Mathf.Sin(v * 10f) + .018f * Mathf.Sin(v * 21f), v);
            }
            var tributary = new Vector2[41];
            var confluence = .50f + .035f * Mathf.Sin(.52f * 10f) + .018f * Mathf.Sin(.52f * 21f);
            for (var i = 0; i < tributary.Length; i++)
            {
                var t = i / 40f;
                tributary[i] = new Vector2(Mathf.Lerp(confluence, .80f, t) + .017f * Mathf.Sin(t * 13f), .52f + .48f * t);
            }
            // Vector4 holds horizontal position, crest metres, shoulder width metres.
            var west = SampleRidge(new[] {
                new Vector4(-.60f,.36f,450f,5000f), new Vector4(-.30f,.54f,480f,5000f),
                new Vector4(-.02f,.63f,680f,4800f), new Vector4(.17f,.80f,570f,5500f),
                new Vector4(.32f,.94f,760f,4500f), new Vector4(.58f,1.12f,850f,5200f),
                new Vector4(.84f,1.26f,850f,5200f) });
            var east = SampleRidge(new[] {
                new Vector4(.80f,1.60f,90f,3000f), new Vector4(.83f,1.25f,81f,3500f),
                new Vector4(.90f,1.02f,99f,3600f), new Vector4(1f,.81f,63f,3200f),
                new Vector4(1.08f,.55f,36f,2800f), new Vector4(1.18f,.28f,0f,2200f),
                new Vector4(1.30f,.05f,0f,2200f) });
            var westernShoulder = SampleRidge(new[] {
                new Vector4(.07f,.91f,550f,3200f), new Vector4(.17f,.80f,550f,3200f),
                new Vector4(.25f,.69f,320f,3000f), new Vector4(.31f,.59f,130f,2600f),
                new Vector4(.32f,.48f,0f,1800f), new Vector4(.34f,.36f,0f,1800f) });
            var northernShoulder = SampleRidge(new[] {
                new Vector4(.21f,1.04f,650f,2500f), new Vector4(.32f,.94f,600f,2500f),
                new Vector4(.38f,.86f,320f,2400f), new Vector4(.39f,.77f,100f,2000f),
                new Vector4(.40f,.68f,0f,1400f), new Vector4(.42f,.59f,0f,1400f) });
            var fields = new GeographicFields();
            for (var z = 0; z < Resolution; z++)
            for (var x = 0; x < Resolution; x++)
            {
                var point = new Vector2(x / 512f, z / 512f);
                fields.river[z,x] = Mathf.Min(DistanceToPath(point, trunk), DistanceToPath(point, tributary)) * Side;
                var w = RidgeElevation(point, west, .60f, out var broadWest);
                var e = RidgeElevation(point, east, .60f, out _);
                var a = RidgeElevation(point, westernShoulder, 0f, out var broadShoulder);
                var b = RidgeElevation(point, northernShoulder, 0f, out var broadNorth);
                fields.uplands[z,x] = Mathf.Max(Mathf.Max(w, e), Mathf.Max(a, b));
                fields.foothills[z,x] = SoftUnion(SoftUnion(broadWest, broadShoulder), broadNorth);

            }
            return fields;
        }

        private static Vector4[] SampleRidge(Vector4[] controls)
        {
            const int steps = 16;
            var points = new Vector4[(controls.Length - 3) * steps + 1];
            var index = 0;
            for (var i = 1; i < controls.Length - 2; i++)
            for (var j = 0; j < steps; j++)
            {
                var t = j / (float)steps;
                var a = controls[i-1]; var b = controls[i]; var c = controls[i+1]; var d = controls[i+2];
                points[index++] = .5f * (2f * b + (-a + c) * t
                    + (2f*a - 5f*b + 4f*c - d) * t*t + (-a + 3f*b - 3f*c + d) * t*t*t);
            }
            points[index] = controls[controls.Length - 2];
            return points;
        }

        private static float RidgeElevation(Vector2 point, Vector4[] path, float crestFraction, out float foothill)
        {
            var nearest = float.PositiveInfinity;
            for (var i = 1; i < path.Length; i++)
            {
                Project(i, out _, out var squared);
                nearest = Mathf.Min(nearest, squared);
            }
            // Average neighbouring axis profiles over 1.8 km. A hard nearest-segment
            // choice can jump between differing profiles around a bend and form ledges.
            // Distance still defines the elongated shoulder; averaging defines its crest.
            var total = 0f; var height = 0f; var width = 0f;
            var blendRadiusSquared = Mathf.Pow(1800f / Side, 2f);
            for (var i = 1; i < path.Length; i++)
            {
                Project(i, out var t, out var squared);
                var weight = Mathf.Exp(-(squared - nearest) / blendRadiusSquared);
                total += weight;
                height += Mathf.Lerp(path[i-1].z, path[i].z, t) * weight;
                width += Mathf.Lerp(path[i-1].w, path[i].w, t) * weight;
            }
            height = Mathf.Max(0f, height / total);
            width = Mathf.Max(100f, width / total);
            var distance = Mathf.Sqrt(nearest) * Side;
            // Broad, rounded cross-sections replace the former flat crest/short shoulder.
            foothill = height * .25f * (1f - Smooth(0f, width * 2.4f, distance));
            return height * (1f - Smooth(width * crestFraction, width, distance));

            void Project(int i, out float t, out float squared)
            {
                var a = new Vector2(path[i-1].x, path[i-1].y);
                var segment = new Vector2(path[i].x, path[i].y) - a;
                t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / segment.sqrMagnitude);
                squared = (point - a - segment * t).sqrMagnitude;
            }
        }

        private static float SoftUnion(float a, float b)
        {
            const float transition = 80f;
            var overlap = Mathf.Clamp01(1f - Mathf.Abs(a - b) / transition);
            return Mathf.Max(a,b) + transition * .25f * overlap * overlap
                * Mathf.Clamp01(Mathf.Min(a,b) / transition);
        }

        private static float DistanceToPath(Vector2 point, Vector2[] path)
        {
            var squared = float.PositiveInfinity;
            for (var i = 1; i < path.Length; i++)
            {
                var segment = path[i] - path[i-1];
                var t = Mathf.Clamp01(Vector2.Dot(point - path[i-1], segment) / segment.sqrMagnitude);
                squared = Mathf.Min(squared, (point - path[i-1] - segment * t).sqrMagnitude);
            }
            return Mathf.Sqrt(squared);
        }

        public float Elevation(float u, float v)
        {
            if(SmoothRiverHeights)return SmoothHeight(u,v)*Height;
            return Heights[Mathf.Clamp(Mathf.RoundToInt(v * (Resolution - 1)), 0, Resolution - 1),
                Mathf.Clamp(Mathf.RoundToInt(u * (Resolution - 1)), 0, Resolution - 1)] * Height;
        }
    }
}
