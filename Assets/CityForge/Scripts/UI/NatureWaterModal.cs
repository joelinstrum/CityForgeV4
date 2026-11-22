using System;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.UI
{
    public sealed partial class NatureToolPopup
    {
        private RectTransform waterModal;
        private Font waterFont;
        public CityForge.World.StreamWidthVariation StreamWidthChoice {get; private set;} = CityForge.World.StreamWidthVariation.Gentle;
        public bool Visible => gameObject.activeSelf || waterModal && waterModal.gameObject.activeSelf || terrainModal && terrainModal.gameObject.activeSelf;
        internal bool WaterModalOpen => waterModal;
        private void OpenWaterModal()
        {
            Option = 0; WaterTypesOpen = true; WaterBody = -1;
            OptionChanged?.Invoke(0);
            var sheet = Resources.Load<Texture2D>("UI/WaterFeatures/Icons");
            var concept = Resources.Load<Texture2D>("UI/WaterFeatures/Concept");
            if (!waterFont) waterFont = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman" }, 24);
            waterModal = Center(GetComponentInParent<Canvas>().transform, "Water features modal", Vector2.zero, Vector2.zero);
            waterModal.anchorMin = Vector2.zero; waterModal.anchorMax = Vector2.one;
            waterModal.offsetMin = waterModal.offsetMax = Vector2.zero;
            waterModal.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .62f);
            var panel = Center(waterModal, "Water Features", Vector2.zero, new Vector2(1500, 790));
            var face = panel.gameObject.AddComponent<Image>(); face.sprite = ZoneModalArtwork.Panel; face.type = Image.Type.Sliced;
            if (concept)
            {
                var banner = Picture(panel, "Water mill panorama", concept, new Rect(1030f / 1675, 1 - 195f / 939, 530f / 1675, 170f / 939), new Vector2(400, 306), new Vector2(620, 150));
                banner.color = new Color(1, 1, 1, .65f);
                Picture(panel, "Water and lilies", concept, new Rect(180f / 1675, 1 - 824f / 939, 1320f / 1675, 88f / 939), new Vector2(0, -359), new Vector2(1420, 75));
            }
            TextLabel(panel, "Water Features", new Vector2(-400, 325), new Vector2(620, 64), 44, true);
            TextLabel(panel, "Add and shape water features in your district.", new Vector2(-311, 273), new Vector2(800, 36), 23);
            var closeButton = Action(panel, "Close water features", "×", new Vector2(705, 346), new Vector2(45, 45), () => close());
            Rule(panel, new Vector2(0, 231), new Vector2(1430, 1));
            Rule(panel, new Vector2(-420, -12), new Vector2(1, 450));
            var feedback = TextLabel(panel, "Choose a feature, then select Draw Feature.", new Vector2(140, -265), new Vector2(1080, 46), 20);
            var add = Action(panel, "Add Feature", "≋  Add Feature", new Vector2(-585, 157), new Vector2(280, 86), () => feedback.text = "Choose a feature, then select Draw Feature.");
            add.image.sprite = ZoneModalArtwork.Gold;
            var edit = Action(panel, "Edit Water", "Edit Water", new Vector2(-585, 52), new Vector2(280, 86), null);
            edit.interactable = false;
            TextLabel(panel, "Shape editing coming soon", new Vector2(-585, -3), new Vector2(280, 28), 16);
            Action(panel, "Remove Water", "≋  Remove Water", new Vector2(-585, -96), new Vector2(280, 86), () =>
            { CloseWaterModal(); WaterRemovalRequested?.Invoke(); });
            TextLabel(panel, "Select a Water Feature", new Vector2(145, 192), new Vector2(1060, 44), 30, true);
            string[] names = { "Stream", "Canal", "Pond", "Lake", "Marsh" };
            var frames = new RawImage[5];
            var buttons = new Button[5];
            int selectedFeature = 0;
            TextLabel(panel, "Drawing", new Vector2(145, -101), new Vector2(1060, 40), 28, true);
            var drawingHint = TextLabel(panel, "", new Vector2(145, -149), new Vector2(1060, 60), 20);
            RectTransform widthRow = null;
            Button[] widthButtons = null;
            var draw = Action(panel, "Draw Feature", "Draw Feature", new Vector2(581, -311), new Vector2(270, 52), () =>
            {
                int kind = selectedFeature == 0 ? 1 : selectedFeature == 1 ? 3 : selectedFeature == 4 ? 4 : 0;
                WaterBody = kind; CloseWaterModal(); WaterBodySelected?.Invoke(true, kind);
            });
            draw.image.sprite = ZoneModalArtwork.Gold;
            for (int i = 0; i < names.Length; i++)
            {
                int index = i; var center = new Vector2(-290 + i * 218, 49);
                var frame = frames[i] = Picture(panel, names[i] + " frame", sheet, SheetRect(1045, 392, 292, 260), center, new Vector2(212, 220));
                frame.raycastTarget = true;
                var button = buttons[i] = frame.gameObject.AddComponent<Button>(); button.targetGraphic = frame;
                var colors = button.colors; colors.highlightedColor = new Color(1.15f, 1.10f, .94f); colors.pressedColor = new Color(.8f, .7f, .5f); button.colors = colors;
                Picture(frame.transform, names[i] + " illustration", sheet, SheetRect(new[] { 120, 490, 860, 1230, 1598 }[i], 43, 350, 316), new Vector2(0, 23), new Vector2(184, 150));
                TextLabel(frame.transform, names[i], new Vector2(0, -75), new Vector2(200, 38), 25, true).alignment = TextAnchor.MiddleCenter;
                button.onClick.AddListener(() => { selectedFeature = index; Refresh(); });
            }
            widthRow = Center(panel, "Stream width variation", new Vector2(145, -220), new Vector2(1060, 64));
            TextLabel(widthRow, "Vary width", new Vector2(-420, 0), new Vector2(200, 42), 23, true);
            string[] widthNames = { "Uniform", "Gentle", "Natural" };
            widthButtons = new Button[3];
            for (int i=0;i<3;i++)
            {
                int mode=i;
                widthButtons[i]=Action(widthRow,"Width "+widthNames[i],widthNames[i],new Vector2(-170+i*245,0),new Vector2(225,42),()=>{StreamWidthChoice=(CityForge.World.StreamWidthVariation)mode;Refresh();});
            }
            Refresh();
            void Refresh()
            {
                for (int i = 0; i < frames.Length; i++)
                    frames[i].uvRect = SheetRect(i == selectedFeature ? 711 : 1045, 392, i == selectedFeature ? 300 : 292, 260);
                widthRow.gameObject.SetActive(selectedFeature==0);
                for(int i=0;i<3;i++)widthButtons[i].image.sprite=i==(int)StreamWidthChoice?ZoneModalArtwork.Gold:ZoneModalArtwork.Dark;
                draw.interactable = true;
                feedback.text = names[selectedFeature] + " selected · Draw Feature to begin";
                drawingHint.text = selectedFeature is 0 or 1
                    ? (selectedFeature==1?"Trace straight canal reaches with masonry banks.":"Stream width: 1–2 tiles (4–8 m). Trace downstream along the terrain.")+"\nPress Enter to create · Backspace to undo · Esc to cancel"
                    : "Click markers to outline your " + names[selectedFeature].ToLowerInvariant() + ".\nClick the first marker to close and create · Backspace to undo · Esc to cancel";
            }
        }
        private void CloseWaterModal()
        {
            if (!waterModal) return;
            waterModal.gameObject.SetActive(false); Destroy(waterModal.gameObject); waterModal = null;
        }
        private void OnDestroy() { CloseWaterModal();CloseTerrainModal(); if (waterFont) Destroy(waterFont); }
        private static Rect SheetRect(float x, float y, float w, float h) => new(x / 2048, 1 - (y + h) / 683, w / 2048, h / 683);
        private static RectTransform Center(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f; rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static RawImage Picture(Transform parent, string name, Texture texture, Rect crop, Vector2 position, Vector2 size)
        {
            var image = Center(parent, name, position, size).gameObject.AddComponent<RawImage>(); image.texture = texture; image.uvRect = crop; image.raycastTarget = false; return image;
        }
        private Text TextLabel(Transform parent, string value, Vector2 position, Vector2 size, int fontSize, bool bold = false)
        {
            var label = Center(parent, value, position, size).gameObject.AddComponent<Text>(); label.font = waterFont ? waterFont : font;
            label.text = value; label.fontSize = fontSize; label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            label.color = new Color(1, .90f, .70f); label.alignment = TextAnchor.MiddleLeft; label.raycastTarget = false; return label;
        }
        private Button Action(Transform parent, string name, string caption, Vector2 position, Vector2 size, Action action)
        {
            var host = Center(parent, name, position, size); var image = host.gameObject.AddComponent<Image>(); image.sprite = ZoneModalArtwork.Dark; image.type = Image.Type.Sliced;
            var button = host.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var label = TextLabel(host, caption, Vector2.zero, size - new Vector2(20, 6), 23); label.alignment = TextAnchor.MiddleCenter;
            var colors = button.colors; colors.highlightedColor = new Color(1.12f, 1.08f, .94f); colors.pressedColor = new Color(.75f, .68f, .52f); button.colors = colors;
            if (action != null) button.onClick.AddListener(() => action()); return button;
        }
        private static void Rule(Transform parent, Vector2 position, Vector2 size)
        { var line = Center(parent, "Gold rule", position, size).gameObject.AddComponent<Image>(); line.color = new Color(.65f, .51f, .28f, .7f); line.raycastTarget = false; }
    }
}
