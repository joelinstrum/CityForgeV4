using UnityEngine;
using UnityEngine.UI;

namespace CityForge.UI
{
    internal static class ZoneModalArtwork
    {
        private static Sprite dark, gold, panel, section;
        internal static Sprite Dark => dark ? dark : dark = Skin(false, false, false);
        internal static Sprite Gold => gold ? gold : gold = Skin(true, false, false);
        internal static Sprite Panel => panel ? panel : panel = Skin(false, true, false);
        internal static Sprite Section => section ? section : section = Skin(false, false, true);
        private static Sprite Skin(bool selected, bool outer, bool subtle)
        {
            const int width = 256, height = 128;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "Zone modal skin", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float qx = Mathf.Abs(x + .5f - width * .5f) - (width * .5f - 18);
                float qy = Mathf.Abs(y + .5f - height * .5f) - (height * .5f - 18);
                float d = Mathf.Min(Mathf.Max(qx, qy), 0) + new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude - 15;
                float t = (float)y / height;
                var color = selected ? Color.Lerp(new Color(.23f, .12f, .025f), new Color(.57f, .34f, .045f), t)
                    : Color.Lerp(new Color(.035f, .052f, .060f), new Color(.105f, .14f, .16f), t);
                float grain = (((x * 17 + y * 43) % 23) / 23f - .5f) * .003f;
                color += new Color(grain, grain, grain, 0);
                var border = selected ? new Color(1, .76f, .29f) : outer ? new Color(.72f, .63f, .40f) : new Color(.48f, .54f, .55f);
                if (subtle) border *= .68f;
                float stroke = 1 - Mathf.SmoothStep(0, 1, Mathf.Abs(d + 1.7f) / (selected ? 1.8f : 1.05f));
                color = Color.Lerp(color, border, stroke);
                if (selected) color += new Color(.16f, .09f, .01f, 0) * Mathf.Exp(-Mathf.Abs(d + 4) * .4f);
                color.a = 1 - Mathf.SmoothStep(0, 1, (d + .6f) / 1.2f);
                pixels[y * width + x] = color;
            }
            texture.SetPixels(pixels); texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), Vector2.one * .5f, 100, 0, SpriteMeshType.FullRect, new Vector4(21, 21, 21, 21));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }

    internal enum ZoneModalGlyph { House, Shop, Factory, Footprint, Auto, Density, Trash, Place }
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class ZoneModalIcon : MaskableGraphic
    {
        internal ZoneModalGlyph glyph;
        internal int count = 1;
        private VertexHelper mesh;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); mesh = vh;
            switch (glyph)
            {
                case ZoneModalGlyph.House: House(10, 10, 80); break;
                case ZoneModalGlyph.Shop:
                    Box(20, 17, 60, 47); Box(14, 15, 72, 5);
                    Polygon(new Vector2(12, 58), new Vector2(24, 83), new Vector2(76, 83), new Vector2(88, 58));
                    for (int i = 0; i < 5; i++) Box(18 + i * 13, 55, 10, 17, i % 2 == 0 ? Light : Shade);
                    Box(42, 20, 17, 29, Ink); Box(25, 35, 10, 12, Ink); Box(65, 35, 10, 12, Ink); break;
                case ZoneModalGlyph.Factory:
                    Polygon(new Vector2(10, 18), new Vector2(90, 18), new Vector2(90, 57), new Vector2(67, 45), new Vector2(67, 58), new Vector2(43, 45), new Vector2(43, 58), new Vector2(10, 42));
                    Box(73, 51, 11, 33); Box(70, 81, 17, 6); Box(8, 12, 84, 6);
                    for (int i = 0; i < 4; i++) Box(19 + i * 17, 24, 9, 14, Ink);
                    Circle(79, 94, 5); Circle(88, 95, 4); break;
                case ZoneModalGlyph.Footprint:
                    int n = Mathf.Clamp(count, 2, 4); float cell = 78f / n;
                    for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
                    { Box(11 + x * cell, 11 + y * cell, cell - 2, cell - 2, new Color(.58f, .70f, .055f)); Box(11 + x * cell, 11 + y * cell + cell - 5, cell - 2, 2, new Color(.80f, .86f, .20f)); }
                    break;
                case ZoneModalGlyph.Auto:
                    for (int i = 0; i < 4; i++) { Box(12 + i * 20, 12, 14, 3); Box(12 + i * 20, 85, 14, 3); Box(12, 12 + i * 20, 3, 14); Box(85, 12 + i * 20, 3, 14); } break;
                case ZoneModalGlyph.Density:
                    int houses = Mathf.Clamp(count, 1, 3); float size = houses == 3 ? 30 : 34;
                    for (int i = 0; i < houses; i++) House((100 - houses * size) * .5f + i * size, (100 - size) * .5f, size);
                    break;
                case ZoneModalGlyph.Trash:
                    Box(25, 20, 50, 57); Box(19, 80, 62, 7); Box(37, 88, 26, 7);
                    for (int i = 0; i < 3; i++) Box(34 + i * 13, 28, 5, 41, Ink); break;
                case ZoneModalGlyph.Place:
                    Box(46, 15, 8, 70); Box(15, 46, 70, 8);
                    Polygon(new Vector2(50, 100), new Vector2(34, 79), new Vector2(66, 79));
                    Polygon(new Vector2(50, 0), new Vector2(34, 21), new Vector2(66, 21));
                    Polygon(new Vector2(0, 50), new Vector2(21, 34), new Vector2(21, 66));
                    Polygon(new Vector2(100, 50), new Vector2(79, 34), new Vector2(79, 66)); break;
            }
        }
        private Color Light => color * new Color(1, .98f, .91f);
        private Color Shade => color * new Color(.74f, .62f, .39f);
        private Color Ink => new Color(.10f, .12f, .12f, color.a);
        private void House(float x, float y, float size)
        {
            Box(x + size * .18f, y + size * .12f, size * .64f, size * .52f);
            Polygon(new Vector2(x, y + size * .57f), new Vector2(x + size * .5f, y + size * .98f), new Vector2(x + size, y + size * .57f));
            Box(x + size * .69f, y + size * .68f, size * .10f, size * .20f);
            Box(x + size * .43f, y + size * .13f, size * .17f, size * .33f, Ink);
            Box(x + size * .27f, y + size * .39f, size * .10f, size * .13f, Ink);
            Box(x + size * .65f, y + size * .39f, size * .10f, size * .13f, Ink);
            Box(x + size * .15f, y + size * .09f, size * .70f, size * .035f, Shade);
        }
        private void Circle(float x, float y, float r)
        {
            var points = new Vector2[16]; for (int i = 0; i < 16; i++) { float a = i * Mathf.PI / 8; points[i] = new Vector2(x + Mathf.Cos(a) * r, y + Mathf.Sin(a) * r); } Polygon(points);
        }
        private void Box(float x, float y, float w, float h, Color? tint = null) => Polygon(tint ?? color, new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h));
        private void Polygon(params Vector2[] points) => Polygon(color, points);
        private void Polygon(Color tint, params Vector2[] points)
        {
            int start = mesh.currentVertCount; var rect = rectTransform.rect;
            foreach (var p in points) mesh.AddVert(new Vector3(rect.x + p.x / 100 * rect.width, rect.y + (glyph == ZoneModalGlyph.Density ? 50 + (p.y - 50) * 2.4f : p.y) / 100 * rect.height), tint, Vector2.zero);
            for (int i = 1; i < points.Length - 1; i++) mesh.AddTriangle(start, start + i, start + i + 1);
        }
    }
}
