using UnityEngine;
using CityForge.Lots;
using System;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CityForge.UI
{
    internal static class LotEditorWorkspace
    {
        private static readonly Color Background = new Color32(18, 26, 34, 255);
        private static readonly Color Header = new Color32(12, 19, 27, 255);
        private static readonly Color Gold = new Color32(198, 154, 80, 255);
        private static readonly Color Ivory = new Color32(247, 234, 205, 255);

        public static Button Create(Font titleFont, UnityAction goBack,
            LotDefinition playerLot = null, LotCustomization customization = null, Action<LotCustomization> savePlayer = null,
            string placedLotId = null)
        {
            var root = new GameObject("Lot Editor Workspace", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvas.sortingOrder = 100;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1672f, 941f);
            scaler.matchWidthOrHeight = 0.5f;
            root.GetComponent<Image>().color = Background;

            var header = AddImage("Header", root.transform, Header);
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = Vector2.one;
            header.rectTransform.offsetMin = new Vector2(0f, -62f);
            header.rectTransform.offsetMax = Vector2.zero;

            var rule = AddImage("Header rule", header.transform, Gold);
            rule.rectTransform.anchorMin = Vector2.zero;
            rule.rectTransform.anchorMax = Vector2.right;
            rule.rectTransform.offsetMin = Vector2.zero;
            rule.rectTransform.offsetMax = new Vector2(0f, 1f);

            var backImage = AddImage("Back to Main Menu", header.transform, new Color32(42, 52, 61, 255));
            backImage.raycastTarget = true;
            SetHeaderPosition(backImage.rectTransform, 16f, 104f);
            var back = backImage.gameObject.AddComponent<Button>();
            back.targetGraphic = backImage;
            back.navigation = new Navigation { mode = Navigation.Mode.None };


            var uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var label = AddText("Back label", back.transform, "Back", uiFont, 21, TextAnchor.MiddleCenter);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(22f, 0f);
            label.rectTransform.offsetMax = Vector2.zero;

            var title = AddText("Title", header.transform, "CityForge — Lot Editor", titleFont ? titleFont : uiFont,
                30, TextAnchor.MiddleLeft);
            SetHeaderPosition(title.rectTransform, 137f, 420f);
            var panel = root.AddComponent<LotEditorPanel>();
            panel.Initialize(uiFont, titleFont, playerLot, customization, savePlayer, goBack, placedLotId);
            LotEditorPanel.AddGlyph(back.transform, LotGlyph.Back, 10, 11, 22);
            LotEditorPanel.AddOutline(backImage, new Color32(65, 81, 93, 255));
            panel.Describe(back, playerLot == null ? "Return to the main menu" : "Return to the district");
            back.onClick.AddListener(panel.RequestBack);
            return back;
        }

        internal static Image AddImage(string name, Transform parent, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        internal static Text AddText(string name, Transform parent, string value, Font font, int size,
            TextAnchor alignment)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(parent, false);
            label.text = value;
            label.font = font;
            label.fontSize = size;
            label.color = Ivory;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        private static void SetHeaderPosition(RectTransform rect, float left, float width)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(left, 0f);
            rect.sizeDelta = new Vector2(width, 44f);
        }
    }
}
