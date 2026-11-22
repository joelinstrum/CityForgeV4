using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CityForge.Lots;
using CityForge.World;
using UnityEngine;
using UnityEngine.UI;
using static CityForge.UI.LotEditorWorkspace;

namespace CityForge.UI
{
    internal sealed partial class LotEditorPanel
    {
        private Button buildingPropertiesButton;
        private LotAssetThumbnails propertyThumbnails;
        private Font propertyFont;

        private void StyleBuildingDetailModal(RectTransform frame)
        {
            if (!propertyFont) propertyFont = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman" }, 24);
            var cream = new Color(1, .91f, .73f);
            frame.GetComponent<Image>().sprite = ZoneModalArtwork.Panel;
            frame.GetComponent<Image>().type = Image.Type.Sliced; frame.GetComponent<Image>().color = Color.white;
            if (frame.TryGetComponent<Outline>(out var border)) border.enabled = false;
            frame.gameObject.AddComponent<GrowableModalFit>().Initialize(frame.rect.size);
            foreach (var label in frame.GetComponentsInChildren<Text>()) { label.color = cream; if (propertyFont) label.font = propertyFont; }
            foreach (var button in frame.GetComponentsInChildren<Button>()) { button.image.sprite = ZoneModalArtwork.Dark; button.image.type = Image.Type.Sliced; button.image.color = Color.white; }
            foreach (var field in frame.GetComponentsInChildren<InputField>()) { field.image.sprite = ZoneModalArtwork.Dark; field.image.type = Image.Type.Sliced; field.image.color = Color.white; field.textComponent.font = font; }
            var banner = new GameObject("Building detail sunset", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            banner.transform.SetParent(frame, false); At(banner.rectTransform, 15, 6, frame.rect.width - 30, 47);
            banner.texture = Resources.Load<Texture2D>("UI/TownConstruction/Civics/Sunset"); banner.uvRect = new Rect(0, 160f / 724, 1, 210f / 724); banner.raycastTarget = false; banner.transform.SetAsFirstSibling();
            var veil = AddImage("Building detail shade", frame, new Color(.025f, .035f, .04f, .64f)); At(veil.rectTransform, 15, 6, frame.rect.width - 30, 47); veil.raycastTarget = false; veil.transform.SetSiblingIndex(1);
        }

        private void OpenBuildingProperties(LotAssetCatalogEntry asset, Action back, LotBuildingProperties draft = null)
        {
            if (PlayerMode || asset == null || asset.category != "Buildings") return;
            CloseAssetPopup();
            draft ??= LotBuildingProperties.From(asset);
            bool restoring = false, updatingFields = false;
            var frame = CreateModal("Building Properties", 1440, 840);
            foreach (Transform child in frame) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            if (frame.TryGetComponent<Outline>(out var outline)) outline.enabled = false;
            var image = frame.GetComponent<Image>(); image.sprite = ZoneModalArtwork.Panel; image.type = Image.Type.Sliced; image.color = Color.white;
            frame.gameObject.AddComponent<GrowableModalFit>().Initialize(new Vector2(1440, 840));
            if (!propertyFont) propertyFont = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman" }, 24);
            var serif = propertyFont ? propertyFont : headingFont;
            var cream = new Color(1, .91f, .73f); var muted = new Color(.81f, .81f, .72f);
            var banner = new GameObject("Building properties sunset", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            banner.transform.SetParent(frame, false); At(banner.rectTransform, 20, 18, 1400, 118);
            banner.texture = Resources.Load<Texture2D>("UI/TownConstruction/Civics/Sunset"); banner.uvRect = new Rect(0, 160f / 724, 1, 210f / 724); banner.raycastTarget = false;
            var veil = AddImage("Sunset shade", frame, new Color(.025f, .035f, .04f, .58f)); At(veil.rectTransform, 20, 18, 1400, 118); veil.raycastTarget = false;
            Text Caption(string text, float x, float y, float width, int size = 21, bool title = false, float height = 36)
            {
                var label = Label(frame, text, x, y, width, size); label.font = serif; label.color = cream;
                label.fontStyle = title ? FontStyle.Bold : FontStyle.Normal; label.raycastTarget = false;
                label.supportRichText = false; At(label.rectTransform, x, y, width, height); return label;
            }
            Button Action(string text, float x, float y, float width, System.Action action, bool primary = false, float height = 46)
            {
                var button = Control(frame, text, x, y, width, action ?? (() => { })); At((RectTransform)button.transform, x, y, width, height);
                button.image.sprite = primary ? ZoneModalArtwork.Gold : ZoneModalArtwork.Dark; button.image.type = Image.Type.Sliced; button.image.color = Color.white;
                var colors = button.colors; colors.normalColor = colors.selectedColor = Color.white; colors.highlightedColor = new Color(1.25f, 1.2f, 1.1f); colors.pressedColor = new Color(.77f, .72f, .62f); button.colors = colors;
                var textLabel = button.GetComponentInChildren<Text>(); textLabel.font = serif; textLabel.fontSize = 21; textLabel.color = cream;
                return button;
            }
            void Box(string name, float x, float y, float w, float h)
            {
                var box = AddImage(name, frame, Color.white); box.sprite = ZoneModalArtwork.Section; box.type = Image.Type.Sliced; At(box.rectTransform, x, y, w, h);
            }
            InputField Edit(string name, float x, float y, float width)
            {
                var field = Field(frame, name, x, y, width); field.gameObject.name = name + " input";
                field.image.sprite = ZoneModalArtwork.Dark; field.image.type = Image.Type.Sliced; field.image.color = Color.white;
                field.textComponent.color = cream; field.textComponent.supportRichText = false; field.textComponent.fontSize = 21; return field;
            }
            Caption("Building Properties", 50, 29, 1120, 42, true, 58);
            Caption("Edit the building’s identity, classification, and default placement size.", 52, 88, 1240, 22);
            Action("×", 1350, 42, 44, () => CloseAssetPopup());
            Box("Building preview section", 20, 154, 390, 550);
            Box("Building identity and defaults", 430, 154, 990, 550);
            Caption("Building Preview", 42, 170, 340, 26, true);
            propertyThumbnails = new LotAssetThumbnails(512, transparent: true, frontView: true);
            var preview = new GameObject("Building properties preview", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            preview.transform.SetParent(frame, false); At(preview.rectTransform, 40, 215, 350, 310); preview.raycastTarget = false;
            var previewLot = new LotDefinition { id = "building-properties:" + asset.id, name = asset.label };
            previewLot.objects.Add(new LotObjectDefinition { assetId = asset.id });
            int rotation = 0;
            void Rotate(int amount) { rotation = (rotation + amount + 8) % 8; preview.texture = propertyThumbnails.GetLot(previewLot, catalog, rotation); }
            Action("↶", 68, 538, 70, () => Rotate(-1)); Action("↷", 272, 538, 70, () => Rotate(1));
            Caption("Rotate preview", 145, 543, 122, 17);
            var measurements = Caption("", 43, 601, 335, 20, false, 80); measurements.color = muted;
            Caption("Identity", 455, 169, 890, 26, true);
            Caption("Building name", 455, 212, 600, 18); var name = Edit("Building name", 455, 246, 940); name.characterLimit = 100;
            Caption("Description", 455, 291, 600, 18); var description = Edit("Building description", 455, 324, 940); description.characterLimit = 600;
            At(description.GetComponent<RectTransform>(), 455, 324, 940, 60); description.lineType = InputField.LineType.MultiLineNewline; description.textComponent.fontSize = 18;
            var refreshers = new List<Action>(); GameObject menu = null;
            var feedback = Caption("", 455, 653, 940, 17, false, 44);
            var save = Action("Save Properties", 1187, 759, 211, null, true, 54);
            var scale = Edit("Default scale percent", 1110, 605, 180); scale.characterLimit = 8; scale.contentType = InputField.ContentType.DecimalNumber;
            bool ReadInputs()
            {
                draft.name = name.text.Trim(); draft.description = description.text.Trim();
                if (!float.TryParse(scale.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float percent) || !float.IsFinite(percent)) return false;
                draft.defaultScale = percent / 100; return true;
            }
            void RefreshValues()
            {
                foreach (var update in refreshers) update();
                bool numeric = ReadInputs(); var problem = numeric ? draft.Problem() : "Enter a default scale between 10% and 500%.";
                feedback.text = problem ?? ""; feedback.color = new Color(1, .74f, .49f); save.interactable = problem == null;
                var size = ApproximateBuildingDimensions(asset) * (numeric ? draft.defaultScale : asset.DefaultPlacementScale);
                measurements.text = $"At default scale\n{size.x:0.##} m wide × {size.z:0.##} m deep\n{size.y:0.##} m tall";
            }
            void Choice(string label, float x, float y, float w, Func<(string Id, string Name)[]> options, Func<string> selected, Action<string> changed)
            {
                Caption(label, x, y, w, 18); Button button = null;
                button = Action(label, x, y + 34, w, () =>
                {
                    if (menu) Destroy(menu);
                    var overlay = AddImage("Property choices overlay", frame, Color.clear); Place(overlay.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); menu = overlay.gameObject;
                    var dismiss = overlay.gameObject.AddComponent<Button>(); dismiss.onClick.AddListener(() => { Destroy(menu); menu = null; });
                    var values = options(); float h = Mathf.Min(276, values.Length * 43 + 10); float top = Mathf.Min(y + 82, 718 - h);
                    var list = AddImage(label + " choices", overlay.transform, Color.white); list.sprite = ZoneModalArtwork.Panel; list.type = Image.Type.Sliced; At(list.rectTransform, x, top, w, h); list.gameObject.AddComponent<RectMask2D>();
                    var scroll = list.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.viewport = list.rectTransform; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 36;
                    var content = AddImage("Choice rows", list.transform, Color.clear).rectTransform; content.anchorMin = content.anchorMax = content.pivot = Vector2.up; content.sizeDelta = new Vector2(w, values.Length * 43 + 10); scroll.content = content;
                    var barRect = AddImage("Choice scroll track", list.transform, Color.clear).rectTransform; At(barRect, w - 10, 6, 4, h - 12);
                    var bar = barRect.gameObject.AddComponent<Scrollbar>(); bar.direction = Scrollbar.Direction.BottomToTop;
                    var handle = AddImage("Choice scroll handle", barRect, new Color(.72f, .62f, .40f, .8f)); bar.handleRect = handle.rectTransform; bar.targetGraphic = handle;
                    scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
                    for (int i = 0; i < values.Length; i++)
                    {
                        var option = values[i]; var row = Action(option.Name, 0, 0, w - 24, () => { changed(option.Id); restoring = false; Destroy(menu); menu = null; RefreshValues(); }, option.Id == selected(), 40);
                        row.transform.SetParent(content, false); At((RectTransform)row.transform, 6, 5 + i * 43, w - 24, 40);
                    }
                });
                refreshers.Add(() => button.GetComponentInChildren<Text>().text = (options().FirstOrDefault(o => o.Id == selected()).Name ?? "Unspecified") + "  ▾");
            }
            Choice("First era", 455, 403, 300, () => CityForgeTimeline.Eras.Select(e => (e.id, e.Name)).ToArray(), () => draft.firstEra, v => draft.firstEra = v);
            Choice("Category", 775, 403, 300, () => LotClassification.Categories.Select(c => (c.id, c.name)).ToArray(), () => draft.category, v => { draft.category = v; draft.subcategory = ""; if (!LotBuildingProperties.Zones(v).Contains(draft.zone)) draft.zone = TownZone.None; });
            Choice("Wealth", 1095, 403, 300, () => Enum.GetValues(typeof(LotWealth)).Cast<LotWealth>().Select(w => (w.ToString(), w == LotWealth.None ? "Unspecified" : w.ToString())).ToArray(), () => draft.wealth.ToString(), v => draft.wealth = Enum.Parse<LotWealth>(v));
            Choice("Use / subcategory", 455, 500, 460, () => new[] { ("", "Unspecified") }.Concat(LotClassification.ForCategory(draft.category).Select(s => (s.id, s.name))).ToArray(), () => draft.subcategory, v => draft.subcategory = v);
            Choice("Growable type", 935, 500, 460, () => LotBuildingProperties.Zones(draft.category).Select(z => (z.ToString(), z == TownZone.None ? "Ploppable / not growable" : z.ToString().Replace("Residential", "Residential ").Replace("Commercial", "Commercial ").Replace("Industrial", "Industrial "))).ToArray(), () => draft.zone.ToString(), v => draft.zone = Enum.Parse<TownZone>(v));
            Caption("Default scale", 935, 611, 112, 18);
            Action("−", 1060, 605, 44, () => StepScale(-5)); Action("+", 1298, 605, 44, () => StepScale(5)); Caption("%", 1350, 610, 40, 23);
            void StepScale(float delta)
            {
                float.TryParse(scale.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value);
                scale.text = Mathf.Clamp(value + delta, 10, 500).ToString("0.##", CultureInfo.InvariantCulture);
            }
            Action("Entrances & dimensions…", 455, 605, 270, () => Advanced(false));
            Action("Row alignment…", 735, 605, 180, () => Advanced(true));
            void Advanced(bool row)
            {
                if (!ReadInputs()) { feedback.text = "Enter a valid default scale before opening placement details."; return; }
                var pending = draft.Copy(); Action reopen = () => OpenBuildingProperties(asset, back, pending);
                if (row) OpenRowBuildingInfo(asset, reopen); else OpenBuildingInfo(asset, reopen);
            }
            Box("Building properties actions", 20, 725, 1400, 99);
            Action("Restore Defaults", 40, 759, 218, () =>
            {
                var defaults = LotBuildingPropertiesStore.Original(asset);
                string defaultPercent = (defaults.defaultScale * 100).ToString("0.##", CultureInfo.InvariantCulture);
                updatingFields = true;
                name.SetTextWithoutNotify(defaults.name); description.SetTextWithoutNotify(defaults.description); scale.SetTextWithoutNotify(defaultPercent);
                draft = defaults; updatingFields = false; restoring = true; RefreshValues();
            });
            Caption("Defaults apply to new placements and growth.\nSaved lot scales stay as authored.", 280, 748, 680, 18, false, 65).color = muted;
            Action("Cancel", 1007, 759, 160, () => { CloseAssetPopup(); back?.Invoke(); }, false, 54);
            save.transform.SetAsLastSibling();
            save.onClick.AddListener(() =>
            {
                if (!ReadInputs()) { RefreshValues(); return; }
                try
                {
                    if (restoring) LotBuildingPropertiesStore.Reset(catalog, asset.id); else LotBuildingPropertiesStore.Save(catalog, draft);
                    CloseAssetPopup(); Refresh("Saved building properties for " + asset.label); back?.Invoke();
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
                { feedback.text = "Could not save: " + e.Message; }
            });
            name.SetTextWithoutNotify(draft.name); description.SetTextWithoutNotify(draft.description); scale.SetTextWithoutNotify((draft.defaultScale * 100).ToString("0.##", CultureInfo.InvariantCulture));
            name.onValueChanged.AddListener(_ => { if (updatingFields) return; restoring = false; RefreshValues(); }); description.onValueChanged.AddListener(_ => { if (updatingFields) return; restoring = false; RefreshValues(); }); scale.onValueChanged.AddListener(_ => { if (updatingFields) return; restoring = false; RefreshValues(); });
            Rotate(0); RefreshValues();
        }
    }
}
