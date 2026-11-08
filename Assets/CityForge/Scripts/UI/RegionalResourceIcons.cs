using UnityEngine;

namespace CityForge.UI
{
    internal static class RegionalResourceIcons
    {
        // Coordinates are on a 24-unit drawing grid, with a separate path per stroke.
        static readonly int[][][] Paths =
        {
            new[] { new[] { 12,4, 6,11, 9,11, 5,16, 19,16, 15,11, 18,11, 12,4 }, new[] { 12,16, 12,20 } },
            new[] { new[] { 4,17, 6,12, 10,11, 12,16, 11,19, 5,19, 4,17 }, new[] { 10,11, 12,6, 17,6, 20,11, 18,15, 13,15 }, new[] { 14,9, 16,8, 18,11 } },
            new[] { new[] { 6,19, 15,8 }, new[] { 6,9, 10,5, 15,5, 19,8, 20,13 }, new[] { 10,5, 13,7, 17,10, 20,13 } },
            new[] { new[] { 5,10, 10,5, 17,6, 20,12, 16,19, 8,19, 4,15, 5,10 }, new[] { 5,10, 11,11, 17,6 }, new[] { 11,11, 13,16, 16,19 }, new[] { 4,15, 13,16, 20,12 } },
            new[] { new[] { 4,8, 9,11, 13,7, 17,8, 21,12, 17,16, 13,17, 9,13, 4,16, 4,8 }, new[] { 17,11, 17,12 } },
            new[] { new[] { 7,20, 15,7, 16,4 }, new[] { 9,17, 6,14, 7,11, 11,15 }, new[] { 11,14, 15,15, 18,12, 13,12 }, new[] { 12,12, 9,9, 10,6, 14,9 }, new[] { 14,9, 17,9, 19,6, 15,7 } },
            new[] { new[] { 5,19, 19,19 }, new[] { 9,19, 7,13, 4,10 }, new[] { 12,19, 12,11, 10,5 }, new[] { 14,19, 16,12, 20,8 }, new[] { 12,14, 16,7 } },
            new[] { new[] { 12,4, 9,9, 6,13, 6,16, 9,19, 15,19, 18,16, 18,13, 15,9, 12,4 }, new[] { 9,14, 9,16, 11,17 } },
            new[] { new[] { 12,4, 10,6, 12,8, 14,6, 12,4 }, new[] { 12,8, 12,20 }, new[] { 8,10, 16,10 }, new[] { 4,13, 5,17, 8,19, 12,20, 16,19, 19,17, 20,13 }, new[] { 4,13, 7,14 }, new[] { 20,13, 17,14 } },
            new[] { new[] { 4,18, 10,7, 14,14, 17,10, 21,18, 4,18 }, new[] { 8,11, 10,13, 12,11 }, new[] { 16,5, 18,5 } }
        };

        public static Texture2D Draw(int type)
        {
            const int size = 72;
            var pixels = new Color32[size * size];
            var ink = type == 3 ? new Color(.68f, .68f, .63f) : new Color(.93f, .84f, .63f);
            var backing = new Color(.10f, .14f, .12f, .92f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var point = new Vector2((x + .5f) / 3, 24 - (y + .5f) / 3);
                float rim = Vector2.Distance(point, new Vector2(12, 12));
                if (rim >= 11.6f) continue;
                float distance = float.PositiveInfinity;
                foreach (var path in Paths[type])
                for (int i = 2; i < path.Length; i += 2)
                {
                    var start = new Vector2(path[i - 2], path[i - 1]);
                    var delta = new Vector2(path[i], path[i + 1]) - start;
                    float t = Mathf.Clamp01(Vector2.Dot(point - start, delta) / delta.sqrMagnitude);
                    distance = Mathf.Min(distance, (point - start - delta * t).sqrMagnitude);
                }
                var color = Color.Lerp(backing, ink, 1 - Mathf.InverseLerp(.52f, .92f, Mathf.Sqrt(distance)));
                color.a *= 1 - Mathf.InverseLerp(11.1f, 11.6f, rim);
                pixels[y * size + x] = color;
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                { name = "Resource map engraving", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
