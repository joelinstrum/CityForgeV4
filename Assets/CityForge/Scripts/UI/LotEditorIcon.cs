using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityForge.UI
{
    internal enum LotGlyph
    {
        None, House, Tree, Fence, Select, Move, Rotate, Shrink, Grow, Copy, Trash,
        Grid, Snap, View, Undo, Redo, Save, Check, Catalog, Back, Edit, Path, Lighting, Textures, Character, Landscaping, StreetFurniture, RotateLot
    }

    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class LotEditorIcon : MaskableGraphic
    {
        public LotGlyph Glyph { get; set; }
        private VertexHelper mesh;
        public override Texture mainTexture => Texture2D.whiteTexture;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            mesh = vh;
            switch (Glyph)
            {
                case LotGlyph.House:
                    Line(2, 11, 12, 21, 22, 11); Line(5, 12, 5, 3, 19, 3, 19, 12);
                    Line(10, 3, 10, 9, 14, 9, 14, 3); break;
                case LotGlyph.Tree:
                    Line(12, 3, 12, 9); Line(8, 3, 16, 3);
                    Line(6, 9, 3, 11, 3, 15, 7, 17, 8, 20, 14, 21, 17, 18, 21, 16, 21, 12, 18, 9, 6, 9); break;
                case LotGlyph.Landscaping:
                    Line(3, 8, 5, 3, 19, 3, 21, 8, 3, 8);
                    Line(12, 8, 12, 19); Line(12, 12, 8, 16, 5, 16);
                    Line(12, 14, 16, 18, 19, 18); Arc(12, 20, 2, 0, 360); break;
                case LotGlyph.StreetFurniture:
                    Line(7, 3, 17, 3); Line(12, 3, 12, 12);
                    Line(9, 12, 7, 18, 12, 22, 17, 18, 15, 12, 9, 12);
                    Line(7, 18, 17, 18); break;
                case LotGlyph.Fence:
                    for (int x = 4; x <= 20; x += 8) Line(x, 4, x, 19);
                    Line(3, 9, 21, 9); Line(3, 15, 21, 15); break;
                case LotGlyph.Character:
                    Arc(12, 17, 4, 0, 360);
                    Line(3, 3, 3, 7, 7, 11, 17, 11, 21, 7, 21, 3, 3, 3); break;
                case LotGlyph.Textures:
                    Line(2, 15, 12, 21, 22, 15, 12, 9, 2, 15);
                    Line(2, 10, 12, 4, 22, 10);
                    Line(7, 18, 17, 12); Line(7, 12, 17, 18); break;
                case LotGlyph.Lighting:
                    Arc(12, 12, 5, 0, 360);
                    Line(12, 1, 12, 4); Line(12, 20, 12, 23);
                    Line(1, 12, 4, 12); Line(20, 12, 23, 12);
                    Line(4, 4, 6, 6); Line(18, 18, 20, 20);
                    Line(4, 20, 6, 18); Line(18, 6, 20, 4); break;
                case LotGlyph.Path:
                    Line(3, 3, 10, 11, 9, 15, 15, 21); Line(9, 3, 16, 11, 15, 15, 21, 21); break;
                case LotGlyph.Select:
                    Triangle(5, 21, 8, 4, 12, 10); Triangle(5, 21, 12, 10, 20, 10);
                    Line(11, 11, 16, 4); break;
                case LotGlyph.Move:
                    Line(3, 12, 21, 12); Line(12, 3, 12, 21);
                    Line(6, 9, 3, 12, 6, 15); Line(18, 9, 21, 12, 18, 15);
                    Line(9, 6, 12, 3, 15, 6); Line(9, 18, 12, 21, 15, 18); break;
                case LotGlyph.Rotate:
                    Arc(12, 12, 8, 68, 364); Line(10, 15, 16, 19, 16, 23); break;
                case LotGlyph.RotateLot:
                    Arc(12, 12, 10, 18, -304, 6); Line(13, 21, 17.6f, 17, 12, 15); break;
                case LotGlyph.Shrink:
                case LotGlyph.Grow:
                    Box(4, 4, 20, 20); Line(8, 12, 16, 12);
                    if (Glyph == LotGlyph.Grow) Line(12, 8, 12, 16);
                    break;
                case LotGlyph.Copy:
                    Line(15, 20, 4, 20, 4, 9); Box(8, 4, 20, 16); break;
                case LotGlyph.Trash:
                    Line(3, 18, 21, 18); Line(9, 21, 15, 21);
                    Line(6, 18, 7, 3, 17, 3, 18, 18); Line(10, 14, 10, 7); Line(14, 14, 14, 7); break;
                case LotGlyph.Grid:
                    Box(4, 4, 20, 20); Line(4, 12, 20, 12); Line(12, 4, 12, 20); break;
                case LotGlyph.Snap:
                    Arc(12, 11, 7, 135, 410); Line(4, 16, 8, 13); Line(15, 17, 18, 21); break;
                case LotGlyph.View:
                    Line(3, 16, 12, 21, 21, 16, 21, 8, 12, 3, 3, 8, 3, 16, 12, 11, 21, 16);
                    Line(12, 11, 12, 3); break;
                case LotGlyph.Undo:
                    Line(4, 15, 14, 15, 19, 13, 20, 9, 18, 6, 14, 5); Line(9, 20, 4, 15, 9, 10); break;
                case LotGlyph.Redo:
                    Line(20, 15, 10, 15, 5, 13, 4, 9, 6, 6, 10, 5); Line(15, 20, 20, 15, 15, 10); break;
                case LotGlyph.Save:
                    Line(4, 4, 4, 20, 17, 20, 20, 17, 20, 4, 4, 4);
                    Box(8, 4, 16, 10); Line(8, 20, 8, 14, 15, 14, 15, 20); break;
                case LotGlyph.Check:
                    Arc(12, 12, 9, 0, 360); Line(7, 12, 11, 8, 17, 16); break;
                case LotGlyph.Catalog:
                    Box(3, 7, 21, 19); Line(3, 13, 21, 13); Line(9, 7, 9, 19); Line(15, 7, 15, 19); break;
                case LotGlyph.Back:
                    Line(5, 12, 20, 12); Line(11, 18, 5, 12, 11, 6); break;
                case LotGlyph.Edit:
                    Line(4, 4, 6, 10, 17, 21, 21, 17, 10, 6, 4, 4); break;
            }
            mesh = null;
        }

        private void Box(float x, float y, float right, float top) => Line(x, y, right, y, right, top, x, top, x, y);

        private void Arc(float x, float y, float radius, int from, int to, float? verticalRadius = null)
        {
            var previous = Point(from);
            for (int i = 1; i <= 20; i++)
            {
                var next = Point(Mathf.Lerp(from, to, i / 20f));
                Segment(previous, next);
                previous = next;
            }
            Vector2 Point(float degrees)
            {
                float angle = degrees * Mathf.Deg2Rad;
                return new Vector2(x + radius * Mathf.Cos(angle), y + (verticalRadius ?? radius) * Mathf.Sin(angle));
            }
        }

        private void Line(params float[] points)
        {
            for (int i = 2; i < points.Length; i += 2)
                Segment(new Vector2(points[i - 2], points[i - 1]), new Vector2(points[i], points[i + 1]));
        }

        private void Segment(Vector2 a, Vector2 b)
        {
            var normal = Vector2.Perpendicular((b - a).normalized) * .9f;
            AddTriangle(a - normal, b - normal, b + normal);
            AddTriangle(a - normal, b + normal, a + normal);
        }

        private void Triangle(float ax, float ay, float bx, float by, float cx, float cy) =>
            AddTriangle(new Vector2(ax, ay), new Vector2(bx, by), new Vector2(cx, cy));

        private void AddTriangle(Vector2 a, Vector2 b, Vector2 c)
        {
            var rect = rectTransform.rect;
            int first = mesh.currentVertCount;
            Add(a); Add(b); Add(c);
            var cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            if (cross > 0) mesh.AddTriangle(first, first + 2, first + 1);
            else mesh.AddTriangle(first, first + 1, first + 2);
            void Add(Vector2 point) => mesh.AddVert(rect.min + Vector2.Scale(point / 24f, rect.size), color, Vector2.zero);
        }
    }

    internal sealed class LotToolDescription : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler
    {
        public string Description { get; private set; }
        private LotEditorPanel panel;
        private bool hovered, focused;

        public void Configure(LotEditorPanel owner, string description)
        {
            panel = owner;
            Description = description;
        }

        public void OnPointerEnter(PointerEventData data) { hovered = true; UpdateLabel(); }
        public void OnPointerExit(PointerEventData data) { hovered = false; UpdateLabel(); }
        public void OnSelect(BaseEventData data) { focused = true; UpdateLabel(); }
        public void OnDeselect(BaseEventData data) { focused = false; UpdateLabel(); }
        private void OnDisable() { hovered = focused = false; UpdateLabel(); }
        private void UpdateLabel()
        {
            if (panel) panel.ShowDescription(this, hovered || focused);
        }
    }
}
