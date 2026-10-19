using UnityEngine;

namespace CityForge.World
{
    /// <summary>Shared deterministic vegetation coverage for ground shading and close grass.</summary>
    public static class MeadowCoverFields
    {
        static float Ribbon(Vector2 p, Vector2 a, Vector2 b, float width)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            var d = Vector2.Distance(p, a + ab * t);
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(width * .35f, width, d));
        }

        public static float WoodlandDensity(Vector2 world, float side)
        {
            var p = world / side;
            var q = p + new Vector2(Mathf.PerlinNoise(p.x*31+9,p.y*31+17)-.5f,
                Mathf.PerlinNoise(p.x*31+33,p.y*31+5)-.5f)*.022f;
            // A compact woodland edge beside the Northeast test clearing, plus two distant stands.
            return Mathf.Max(Ribbon(q,new Vector2(.19f,.29f),new Vector2(.32f,.33f),.043f),
                Mathf.Max(Ribbon(q,new Vector2(-.32f,.24f),new Vector2(-.20f,.34f),.065f),
                    Ribbon(q,new Vector2(-.27f,-.31f),new Vector2(-.13f,-.25f),.055f)));
        }

        public static Color Sample(Vector2 world, float side)
        {
            var p = world / side;
            // Irregular edges, subordinate to a few long vegetation bands and open clearings.
            var edge = new Vector2(Mathf.PerlinNoise(p.x * 11f + 17, p.y * 11f + 29),
                Mathf.PerlinNoise(p.x * 11f + 43, p.y * 11f + 7)) - Vector2.one * .5f;
            var q = p + edge * .065f;
            var rough = Mathf.Max(
                Ribbon(q, new Vector2(-.42f, .35f), new Vector2(.36f, .20f), .105f),
                Ribbon(q, new Vector2(-.29f, -.35f), new Vector2(.05f, .15f), .085f));
            rough = Mathf.Max(rough, Ribbon(q, new Vector2(.18f, -.36f), new Vector2(.38f, -.08f), .07f)) * .45f;
            var dry = Ribbon(q, new Vector2(-.36f, .08f), new Vector2(-.08f, .36f), .09f) * .28f;
            dry = Mathf.Max(dry, Ribbon(q, new Vector2(.29f, .34f), new Vector2(.42f, .12f), .065f) * .22f);
            var sparse = Ribbon(q, new Vector2(.06f, .33f), new Vector2(.31f, .27f), .039f) * .12f;
            sparse = Mathf.Max(sparse, Ribbon(q, new Vector2(-.30f, -.24f), new Vector2(-.17f, -.10f), .028f) * .10f);
            var shortTurf = Mathf.Max(.08f, 1f - rough - dry - sparse);
            var total = shortTurf + rough + dry + sparse;
            return new Color(shortTurf / total, rough / total, dry / total, sparse / total);
        }

        public static Texture2D Build(float side,RegionBiomeMap biomes=null)
        {
            const int n = 257;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false, true)
                { name = "Turf / rough meadow / dry grass / sparse soil", wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear };
            var colors = new Color[n * n];
            for (var z = 0; z < n; z++) for (var x = 0; x < n; x++)
                {
                var uv=new Vector2(x/(float)(n-1),z/(float)(n-1));
                if(biomes==null)colors[z*n+x]=Sample((uv-Vector2.one*.5f)*side,side);
                else {var sample=biomes.At(uv);float rough=sample.woodland*.65f,dry=sample.dryness*.27f,sparse=sample.dryness*.03f;colors[z*n+x]=new Color(1-rough-dry-sparse,rough,dry,sparse);}
                }
            texture.SetPixels(colors); texture.Apply(false, true);
            return texture;
        }
    }
}
