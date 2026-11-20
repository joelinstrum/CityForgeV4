using UnityEngine;
using UnityEngine.UI;

namespace CityForge.UI
{
    // Reuse the City's actual rounded frame without its baked category icon/caption.
    // The empty side column supplies the center, so no new raster artwork is needed.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DistrictCategoryButtonArtwork : MaskableGraphic
    {
        private Texture2D dark, gold;
        private bool selected;
        private static readonly Rect Frame = new(409, 236, 343, 206);
        public override Texture mainTexture
        {
            get { var sheet = selected ? gold : dark; return sheet ? sheet : Texture2D.whiteTexture; }
        }

        internal void Configure(Texture2D normal, Texture2D active, Material artworkMaterial)
        {
            dark = normal; gold = active; material = artworkMaterial;
            SetAllDirty();
        }
        internal void SetSelected(bool active)
        {
            if (selected == active) return;
            selected = active; SetMaterialDirty(); SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var bounds = GetPixelAdjustedRect();
            var texture = selected ? gold : dark;
            if (!texture)
            {
                Quad(bounds.xMin, bounds.xMax, 0, 1, 0, 1,
                    color * (selected ? new Color(.48f, .33f, .15f) : new Color(.12f, .15f, .18f)));
                return;
            }
            float edge = Mathf.Min(bounds.width * .5f, bounds.width * 60 / Frame.width);
            float top = 1 - Frame.yMin / texture.height, bottom = 1 - Frame.yMax / texture.height;
            Quad(bounds.xMin, bounds.xMin + edge, Frame.xMin / texture.width, (Frame.xMin + 60) / texture.width, bottom, top, color);
            Quad(bounds.xMin + edge, bounds.xMax - edge, (Frame.xMin + 65) / texture.width, (Frame.xMin + 66) / texture.width, bottom, top, color);
            Quad(bounds.xMax - edge, bounds.xMax, (Frame.xMax - 60) / texture.width, Frame.xMax / texture.width, bottom, top, color);

            void Quad(float left, float right, float u0, float u1, float v0, float v1, Color tint)
            {
                int first = mesh.currentVertCount;
                mesh.AddVert(new Vector3(left, bounds.yMin), tint, new Vector2(u0, v0));
                mesh.AddVert(new Vector3(left, bounds.yMax), tint, new Vector2(u0, v1));
                mesh.AddVert(new Vector3(right, bounds.yMax), tint, new Vector2(u1, v1));
                mesh.AddVert(new Vector3(right, bounds.yMin), tint, new Vector2(u1, v0));
                mesh.AddTriangle(first, first + 1, first + 2); mesh.AddTriangle(first, first + 2, first + 3);
            }
        }
    }
}
