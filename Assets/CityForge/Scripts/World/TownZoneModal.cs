using CityForge.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private Font zoneModalFont;
        private void BuildZoneSelectionModal()
        {
            if (!zoneModalFont)
            {
                zoneModalFont = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman" }, 24);
                if (zoneModalFont) Own(zoneModalFont);
            }
            var serif = zoneModalFont ? zoneModalFont : lotFont;
            var cream = new Color(1, .91f, .72f);
            var face = townPanel.gameObject.AddComponent<Image>(); face.sprite = ZoneModalArtwork.Panel; face.type = Image.Type.Sliced;
            var shadow = townPanel.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .45f); shadow.effectDistance = new Vector2(0, -5);
            var layout = FounderRect(townPanel, "Zone modal layout", Vector2.zero, new Vector2(1320, 600));
            layout.localScale = Vector3.one * 1.1f;
            Label(layout, "Zones", new Vector2(0, 258), new Vector2(900, 58), 45, true);
            Label(layout, "Choose a zone type, size, and density, then drag onto the map.", new Vector2(0, 212), new Vector2(1120, 34), 23);
            Rule(new Vector2(0, 180), new Vector2(1310, 1));
            Button(layout, "Close zones", new Vector2(604, 258), new Vector2(46, 46), "×", 25, CloseTownSection);
            var types = Section("Zone Type", new Vector2(-433, -18), new Vector2(370, 354));
            var sizes = Section("Zone Size  (Lot Footprint)", new Vector2(200, 60), new Vector2(850, 198));
            var densities = Section("Density", new Vector2(200, -123), new Vector2(850, 144));
            Rule(new Vector2(-236, -18), new Vector2(1, 354));
            var footer = FounderRect(layout, "Zone selection footer", new Vector2(0, -245), new Vector2(1272, 76));
            var footerImage = footer.gameObject.AddComponent<Image>(); footerImage.sprite = ZoneModalArtwork.Section; footerImage.type = Image.Type.Sliced;
            Rule(new Vector2(-344, -245), new Vector2(1, 44)); Rule(new Vector2(344, -245), new Vector2(1, 44));
            var erase = Button(layout, "Erase Zoning", new Vector2(-491, -245), new Vector2(258, 56), "Erase Zoning", 24, () => { });
            erase.image.color = new Color(1, .88f, .86f);
            ShiftCaption(erase, 22); Icon(erase.transform, ZoneModalGlyph.Trash, new Vector2(-98, 0), new Vector2(30, 34), new Color(.88f, .28f, .22f));
            var place = Button(layout, "Select & Place", new Vector2(492, -245), new Vector2(270, 56), "Select & Place", 24, () => { });
            place.image.sprite = ZoneModalArtwork.Gold; ShiftCaption(place, 17);
            Icon(place.transform, ZoneModalGlyph.Place, new Vector2(-105, 0), Vector2.one * 28, cream);
            var summary = Label(layout, "", new Vector2(0, -245), new Vector2(665, 50), 21);
            summary.color = new Color(1, .78f, .36f);
            int selectedType = 0, selectedSize = 3, selectedDensity = 2;
            string[] typeNames = { "Residential", "Commercial", "Industrial" };
            string[] densityNames = { "Low", "Medium", "High" };
            var typeButtons = new Button[3]; var sizeButtons = new Button[4]; var densityButtons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var button = typeButtons[i] = Button(types, typeNames[i], new Vector2(0, 88 - i * 82), new Vector2(338, 70), typeNames[i], 28,
                    () => { selectedType = index; Refresh(); });
                ShiftCaption(button, 28);
                Icon(button.transform, (ZoneModalGlyph)i, new Vector2(-116, 0), new Vector2(54, 54), cream);
            }
            for (int i = 0; i < 4; i++)
            {
                int size = i == 0 ? 0 : i + 1; int index = i;
                var button = sizeButtons[i] = Button(sizes, size == 0 ? "Auto footprint" : size + " by " + size + " footprint",
                    new Vector2(-312 + i * 206, -18), new Vector2(190, 134), "", 26,
                    () => { selectedSize = size; Refresh(); });
                Icon(button.transform, size == 0 ? ZoneModalGlyph.Auto : ZoneModalGlyph.Footprint, new Vector2(0, 25), Vector2.one * 55,
                    size == 0 ? new Color(.81f, .89f, .12f) : cream, size);
                Label(button.transform, size == 0 ? "Auto" : $"{size} × {size}", new Vector2(0, -22), new Vector2(170, 31), 27);
                Label(button.transform, size == 0 ? "(Drag)" : $"({size * size} tiles)", new Vector2(0, -49), new Vector2(170, 27), 19);
            }
            for (int i = 0; i < 3; i++)
            {
                int density = i + 1;
                var button = densityButtons[i] = Button(densities, densityNames[i] + " density", new Vector2(-285 + i * 285, -16), new Vector2(260, 78), "", 24,
                    () => { selectedDensity = density; Refresh(); });
                Icon(button.transform, ZoneModalGlyph.Density, new Vector2(0, 16), new Vector2(88, 36), cream, density);
                Label(button.transform, densityNames[i], new Vector2(0, -20), new Vector2(230, 30), 25);
            }
            Refresh();

            void Refresh()
            {
                for (int i = 0; i < 3; i++) typeButtons[i].image.sprite = i == selectedType ? ZoneModalArtwork.Gold : ZoneModalArtwork.Dark;
                for (int i = 0; i < 4; i++) sizeButtons[i].image.sprite = (i == 0 ? 0 : i + 1) == selectedSize ? ZoneModalArtwork.Gold : ZoneModalArtwork.Dark;
                for (int i = 0; i < 3; i++) densityButtons[i].image.sprite = i + 1 == selectedDensity ? ZoneModalArtwork.Gold : ZoneModalArtwork.Dark;
                summary.text = $"{typeNames[selectedType]} · {densityNames[selectedDensity - 1]} Density · " +
                    (selectedSize == 0 ? "Auto (Drag)" : $"{selectedSize} × {selectedSize} ({selectedSize * selectedSize} tiles)");
            }
            RectTransform Section(string title, Vector2 position, Vector2 size)
            {
                var section = FounderRect(layout, title, position, size);
                var image = section.gameObject.AddComponent<Image>(); image.sprite = ZoneModalArtwork.Section; image.type = Image.Type.Sliced;
                var caption = Label(section, title, new Vector2(26, size.y * .5f - 27), new Vector2(size.x - 52, 34), 27, true);
                caption.rectTransform.anchoredPosition = new Vector2(0, size.y * .5f - 27); caption.alignment = TextAnchor.MiddleLeft;
                return section;
            }
            Text Label(Transform parent, string text, Vector2 position, Vector2 size, int fontSize, bool bold = false)
            {
                var label = FounderText(parent, text, position, size, fontSize); label.font = serif; label.color = cream;
                label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; return label;
            }
            Button Button(Transform parent, string name, Vector2 position, Vector2 size, string caption, int fontSize, UnityEngine.Events.UnityAction action)
            {
                var rect = FounderRect(parent, name, position, size);
                var image = rect.gameObject.AddComponent<Image>(); image.sprite = ZoneModalArtwork.Dark; image.type = Image.Type.Sliced;
                var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
                var colors = button.colors; colors.highlightedColor = new Color(1, .97f, .88f); colors.pressedColor = new Color(.80f, .75f, .65f); button.colors = colors;
                button.onClick.AddListener(action);
                if (caption.Length > 0) Label(rect, caption, Vector2.zero, size - new Vector2(16, 6), fontSize);
                return button;
            }
            void ShiftCaption(Button button, float x)
            {
                var caption = button.GetComponentInChildren<Text>(); caption.rectTransform.anchoredPosition = new Vector2(x, 0);
                caption.rectTransform.sizeDelta = new Vector2(button.image.rectTransform.sizeDelta.x - x * 2 - 10, button.image.rectTransform.sizeDelta.y - 6);
            }
            void Icon(Transform parent, ZoneModalGlyph glyph, Vector2 position, Vector2 size, Color tint, int count = 1)
            {
                var rect = FounderRect(parent, glyph + " icon", position, size); var icon = rect.gameObject.AddComponent<ZoneModalIcon>();
                icon.glyph = glyph; icon.count = count; icon.color = tint; icon.raycastTarget = false;
            }
            void Rule(Vector2 position, Vector2 size)
            {
                var rect = FounderRect(layout, "Panel rule", position, size); rect.gameObject.AddComponent<Image>().color = new Color(.44f, .48f, .44f, .55f);
            }
        }
    }
}
