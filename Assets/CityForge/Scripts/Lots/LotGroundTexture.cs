using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForge.Lots
{
    [Serializable]
    public sealed class LotGroundOverlay
    {
        internal const int MaxCount = 1024;
        public string texture;
        public int span;
        public int rotation;
        public Vector2 min, max;
        public bool anchored;
        public Vector2 anchor;
        internal Vector2 TextureCenter => anchored ? anchor : Bounds.center;
        internal Rect Bounds => Rect.MinMaxRect(min.x, min.y, max.x, max.y);

        internal bool IsValid()
        {
            float limit = LotDefinition.MaxTiles * LotDefinition.TileMeters / 2f;
            return !string.IsNullOrEmpty(texture) && LotGroundTexture.Find(true, texture) != null &&
                (span == 1 || span == 2) && rotation >= 0 && rotation < 4 && min.x >= -limit && min.y >= -limit &&
                max.x <= limit && max.y <= limit && min.x < max.x && min.y < max.y &&
                (!anchored || float.IsFinite(anchor.x) && float.IsFinite(anchor.y));
        }

        internal static List<LotGroundOverlay> Cut(LotDefinition lot, IReadOnlyList<Rect> tiles, int rotate = 0)
        {
            var remaining = new List<LotGroundOverlay>(lot.overlays.Count + 1);
            if (!string.IsNullOrEmpty(lot.overlayTexture))
            {
                var min = -lot.Size * .5f;
                remaining.Add(new LotGroundOverlay
                {
                    texture = lot.overlayTexture, span = lot.overlayTextureSpan, min = min, max = -min,
                    rotation = lot.overlayTextureRotation, anchored = true,
                    anchor = FullTextureAnchor(lot.Size, lot.overlayTextureSpan, lot.overlayTextureRotation)
                });
            }
            remaining.AddRange(lot.overlays);
            foreach (var tile in tiles)
            {
                for (int i = remaining.Count - 1; i >= 0; i--)
                {
                    var overlay = remaining[i];
                    var bounds = overlay.Bounds;
                    if (!bounds.Overlaps(tile)) continue;
                    float left = Mathf.Max(bounds.xMin, tile.xMin);
                    float right = Mathf.Min(bounds.xMax, tile.xMax);
                    var pieces = new List<LotGroundOverlay>(5);
                    Add(bounds.xMin, bounds.yMin, left, bounds.yMax);
                    Add(right, bounds.yMin, bounds.xMax, bounds.yMax);
                    Add(left, bounds.yMin, right, tile.yMin);
                    Add(left, tile.yMax, right, bounds.yMax);
                    if (rotate != 0)
                    {
                        var a = tile.center + QuarterTurn(new Vector2(left, Mathf.Max(bounds.yMin, tile.yMin)) - tile.center, rotate);
                        var b = tile.center + QuarterTurn(new Vector2(right, Mathf.Min(bounds.yMax, tile.yMax)) - tile.center, rotate);
                        pieces.Add(new LotGroundOverlay
                        {
                            texture = overlay.texture, span = overlay.span, rotation = (overlay.rotation + rotate + 4) % 4,
                            min = Vector2.Min(a, b), max = Vector2.Max(a, b), anchored = true,
                            anchor = tile.center + QuarterTurn(overlay.TextureCenter - tile.center, rotate)
                        });
                    }
                    remaining.RemoveAt(i);
                    remaining.InsertRange(i, pieces);

                    void Add(float x0, float y0, float x1, float y1)
                    {
                        if (x0 >= x1 || y0 >= y1) return;
                        pieces.Add(new LotGroundOverlay
                        {
                            texture = overlay.texture, span = overlay.span, rotation = overlay.rotation,
                            min = new Vector2(x0, y0), max = new Vector2(x1, y1),
                            anchored = true, anchor = overlay.TextureCenter
                        });
                    }
                }
            }
            return remaining;
        }

        internal static Vector2 FullTextureAnchor(Vector2 size, int span, int rotation)
        {
            var localSize = rotation % 2 == 0 ? size : new Vector2(size.y, size.x);
            return QuarterTurn(-localSize * .5f + Vector2.one * (LotGroundTexture.RepeatMeters * span * .5f), rotation);
        }

        internal static Rect QuarterTurn(Rect area, int turns)
        {
            var a = QuarterTurn(area.min, turns);
            var b = QuarterTurn(area.max, turns);
            var min = Vector2.Min(a, b);
            var max = Vector2.Max(a, b);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        internal static Vector2 QuarterTurn(Vector2 point, int turns)
        {
            switch ((turns + 4) % 4)
            {
                case 1: return new Vector2(point.y, -point.x);
                case 2: return -point;
                case 3: return new Vector2(-point.y, point.x);
                default: return point;
            }
        }
    }

    internal sealed class LotGroundTexture
    {
        public static readonly LotGroundTexture[] Base =
        {
            // An empty base leaves the district terrain visible. The color is for the editor preview.
            new LotGroundTexture("", "None", "", new Color32(112, 125, 64, 255)),
            new LotGroundTexture("lush-grass", "Lush Grass", "Lots/GroundTextures/lush-grass", new Color32(93, 132, 55, 255)),
            new LotGroundTexture("patchy-grass", "Patchy Grass", "Lots/GroundTextures/patchy-grass", new Color32(135, 128, 79, 255)),
            new LotGroundTexture("low-wealth-4x4", "Low Wealth Grass · 4 × 4", "Lots/GroundTextures/low-wealth-4x4-tile",
                new Color32(112, 125, 64, 255), 2),
            new LotGroundTexture("residential-worn-grass", "Low Wealth · Worn Grass",
                "Lots/GroundTextures/Residential/worn-grass-base", new Color32(112, 125, 64, 255), 2),
            new LotGroundTexture("residential-mixed-earth", "Low Wealth · Mixed Earth",
                "Lots/GroundTextures/Residential/mixed-earth-base", new Color32(125, 109, 69, 255), 2),
            new LotGroundTexture("residential-maintained-grass", "Medium Wealth · Maintained Grass",
                "Lots/GroundTextures/Residential/maintained-grass-base", new Color32(93, 132, 55, 255), 2),
            new LotGroundTexture("packed-earth", "Packed Earth", "Lots/PathMaterials/packed-earth", new Color32(130, 97, 67, 255))
        };

        public static readonly LotGroundTexture[] Overlay =
        {
            new LotGroundTexture("", "Clear All Overlays", "", Color.clear),
            new LotGroundTexture("tileable-grass", "Tileable Grass", "Lots/GroundTextures/tileable-grass", Color.clear),
            new LotGroundTexture("worn-sidewalk", "Worn Sidewalk", "Lots/GroundTextures/worn-sidewalk", Color.clear),
            new LotGroundTexture("brick-walkway", "Brick Walkway", "Lots/GroundTextures/brick-walkway", Color.clear)
        };

        public readonly string Id, Label;
        public readonly Color Fallback;
        public readonly int DefaultSpan;
        private readonly string resource;

        private LotGroundTexture(string id, string label, string resource, Color fallback, int defaultSpan = 1)
        {
            Id = id;
            Label = label;
            this.resource = resource;
            Fallback = fallback;
            DefaultSpan = defaultSpan;
        }

        public Texture2D Texture => resource.Length == 0 ? null : Resources.Load<Texture2D>(resource);
        public static LotGroundTexture Find(bool overlay, string id) =>
            Array.Find(overlay ? Overlay : Base, entry => entry.Id == (id ?? ""));
        internal const float RepeatMeters = 8f;
        public static Vector2 Repeat(Vector2 size, int span) => size / (RepeatMeters * span);

        internal static void ConfigureTransparentLayer(Material material, int order)
        {
            material.SetFloat("_Surface", 1);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + order;
        }
    }
}
