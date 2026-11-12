using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.UI
{
    internal static class RegionBrassArtwork
    {
        private static readonly Dictionary<string, Sprite> Sprites = new();

        private static Sprite Load(string name, bool sliced)
        {
            if (Sprites.TryGetValue(name, out var sprite) && sprite) return sprite;
            var texture = Resources.Load<Texture2D>("UI/RegionBrass/" + name);
            if (!texture) return null;
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * .5f,
                100f, 0, SpriteMeshType.FullRect, sliced ? new Vector4(38, 38, 38, 38) : Vector4.zero);
            Sprites[name] = sprite;
            return sprite;
        }

        internal static bool SetImage(Image image, string name, bool sliced = false)
        {
            var sprite = Load(name, sliced);
            if (!sprite) return false;
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = Color.white;
            return true;
        }

        internal static bool SetButton(Button button, string name, bool bakedCaption = true)
        {
            if (!SetImage(button.image, name)) return false;
            var caption = button.GetComponentInChildren<Text>();
            if (bakedCaption && caption) caption.canvasRenderer.SetAlpha(0f);
            var highlighted = Load(name + "-hover", false);
            var active = Load(name + "-active", false);
            if (highlighted)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState
                {
                    highlightedSprite = highlighted, pressedSprite = active, selectedSprite = active
                };
            }
            return true;
        }

        internal static bool SetSelected(Button button, string name, bool selected)
        {
            if (!SetImage(button.image, selected ? name + "-active" : name)) return false;
            if (selected) button.image.overrideSprite = button.image.sprite;
            else button.image.overrideSprite = null;
            return true;
        }
    }
}
