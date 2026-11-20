using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.CameraSystem;
using CityForge.Lots;
using CityForge.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private LotAssetThumbnails civicThumbnails;
        private Font civicFont;
        private CityForgeCameraController civicCamera;
        private bool civicCameraWasEnabled;
        private static readonly Rect[] CivicIconCrops =
        {
            new(90, 85, 390, 350), new(540, 100, 355, 310), new(950, 85, 345, 350), new(1340, 75, 330, 365),
            new(65, 470, 400, 350), new(510, 465, 380, 355), new(950, 455, 340, 370), new(1340, 470, 370, 350)
        };

        private void BuildCivicSelectionModal()
        {
            if (!civicFont)
                civicFont = Own(Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman" }, 24));
            civicCamera = GetComponentInChildren<CityForgeCameraController>();
            if (civicCamera) { civicCameraWasEnabled = civicCamera.enabled; civicCamera.enabled = false; }
            var serif = civicFont ? civicFont : lotFont;
            var cream = new Color(1, .91f, .73f);
            var muted = new Color(.81f, .81f, .72f);
            var catalog = LotAssetCatalog.Load();
            var icons = Resources.Load<Texture2D>("UI/TownConstruction/Civics/Icons");
            var banner = Resources.Load<Texture2D>("UI/TownConstruction/Civics/Sunset");
            civicThumbnails = new LotAssetThumbnails(256, transparent: true, frontView: true);
            var saved = Array.Empty<LotDefinition>();
            string libraryProblem = null;
            try { saved = LotLibrary.Local.List().Select(entry => entry.Lot).ToArray(); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            { libraryProblem = "Saved lots could not be read. Close this popup and try again."; }
            var choices = CivicBuildingChoices.Collect(saved);
            if (catalog && catalog.Find("timber-council-hall") is { prefab: not null })
                choices.Insert(0, DistrictTownCenter());
            var eras = CityForgeTimeline.Eras;
            int era = Array.FindIndex(eras, e => TownYear(SelectedDistrict) >= e.FirstYear && TownYear(SelectedDistrict) <= e.LastYear);
            if (era < 0) era = 0;
            int category = 0, rotation = ((lotRotation % 8) + 8) % 8;
            LotDefinition selected = null;
            var categoryButtons = new Button[CivicBuildingChoices.Categories.Length];
            var cards = new List<(LotDefinition Lot, Button Button)>();
            Text resultsTitle = null, empty = null, detailTitle = null, detailCategory = null,
                detailDescription = null, metrics = null, facing = null, status = null;
            Button eraButton = null, rotateLeft = null, rotateRight = null, place = null;
            ScrollRect scroll = null;
            RectTransform content = null;
            RawImage detailPreview = null;

            transportModal = FounderRect(townToolbar.parent, "Civic buildings modal", Vector2.zero, Vector2.zero);
            transportModal.anchorMin = Vector2.zero; transportModal.anchorMax = Vector2.one;
            transportModal.offsetMin = transportModal.offsetMax = Vector2.zero;
            transportModal.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .64f);
            var panel = FounderRect(transportModal, "Civic buildings panel", Vector2.zero, new Vector2(1480, 790));
            var hud = (RectTransform)townToolbar.parent;
            float scale = Mathf.Min(1, Mathf.Max(.1f, (hud.rect.width - 48) / 1480), Mathf.Max(.1f, (hud.rect.height - 48) / 790));
            panel.localScale = Vector3.one * scale;
            Skin(panel, ZoneModalArtwork.Panel);
            var shadow = panel.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .5f); shadow.effectDistance = new Vector2(0, -7);
            var header = FounderRect(panel, "Civic sunset banner", new Vector2(0, 316), new Vector2(1440, 134));
            header.gameObject.AddComponent<RectMask2D>();
            if (banner)
                TownArtwork(header, "Sunset town artwork", banner, new Rect(0, 160f / 724 * banner.height, banner.width, 210f / 724 * banner.height), Vector2.zero, header.sizeDelta);
            var veil = FounderRect(header, "Header shade", Vector2.zero, header.sizeDelta).gameObject.AddComponent<Image>();
            veil.color = new Color(.025f, .035f, .04f, .53f); veil.raycastTarget = false;
            Label(panel, "Civic Buildings", new Vector2(0, 342), new Vector2(1050, 58), 44, true);
            Label(panel, "Select a civic category, choose a building, and place it in your city.", new Vector2(0, 299), new Vector2(1160, 34), 23);
            ActionButton(panel, "Close civic buildings", new Vector2(685, 342), new Vector2(44, 44), "×", 26, CloseTownSection);

            Section(panel, "Civic categories", new Vector2(-556, -23), new Vector2(320, 540));
            Label(panel, "Civic Categories", new Vector2(-556, 218), new Vector2(280, 40), 26, true).alignment = TextAnchor.MiddleLeft;
            for (int i = 0; i < categoryButtons.Length; i++)
            {
                int index = i;
                var item = CivicBuildingChoices.Categories[i];
                var button = categoryButtons[i] = ActionButton(panel, "Civic category " + item.Id,
                    new Vector2(-556, 166 - i * 59), new Vector2(292, 52), item.Name, 23,
                    () => { category = index; Rebuild(); });
                var caption = button.GetComponentInChildren<Text>();
                caption.rectTransform.offsetMin = new Vector2(64, 0); caption.rectTransform.offsetMax = new Vector2(-8, 0);
                caption.alignment = TextAnchor.MiddleLeft;
                Icon(button.transform, i, new Vector2(-111, 0), 42);
            }
            Section(panel, "Civic building results", new Vector2(170, -23), new Vector2(1090, 540));
            resultsTitle = Label(panel, "", new Vector2(-44, 218), new Vector2(630, 40), 27, true);
            resultsTitle.alignment = TextAnchor.MiddleLeft;
            eraButton = ActionButton(panel, "Civic era selector", new Vector2(514, 218), new Vector2(367, 42), "", 21, null);
            var eraMenu = FounderRect(panel, "Civic era choices", new Vector2(514, 190), new Vector2(367, eras.Length * 43 + 12));
            eraMenu.pivot = new Vector2(.5f, 1); Skin(eraMenu, ZoneModalArtwork.Panel);
            for (int i = 0; i < eras.Length; i++)
            {
                int index = i;
                var option = ActionButton(eraMenu, "Civic era " + eras[i].id, new Vector2(0, eraMenu.sizeDelta.y * .5f - 28 - i * 43),
                    new Vector2(350, 40), eras[i].Name + $" ({eras[i].FirstYear}–{eras[i].LastYear})", 20,
                    () => { era = index; eraMenu.gameObject.SetActive(false); Rebuild(); });
            }
            eraMenu.gameObject.SetActive(false);
            eraButton.onClick.AddListener(() => { eraMenu.gameObject.SetActive(!eraMenu.gameObject.activeSelf); eraMenu.SetAsLastSibling(); });
            var viewport = FounderRect(panel, "Civic building scroll", new Vector2(170, 42), new Vector2(1070, 300));
            viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .04f);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 35;
            scroll.viewport = viewport;
            content = FounderRect(viewport, "Civic building cards", Vector2.zero, new Vector2(1070, 300));
            content.anchorMin = content.anchorMax = content.pivot = new Vector2(.5f, 1); scroll.content = content;
            var barRect = FounderRect(viewport, "Civic scroll indicator", new Vector2(529, 0), new Vector2(6, 280));
            var bar = barRect.gameObject.AddComponent<Scrollbar>(); bar.direction = Scrollbar.Direction.BottomToTop;
            var handleRect = FounderRect(barRect, "Civic scroll handle", Vector2.zero, Vector2.zero);
            var handle = handleRect.gameObject.AddComponent<Image>(); handle.color = new Color(.72f, .62f, .40f, .8f);
            bar.handleRect = handleRect; bar.targetGraphic = handle;
            scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            empty = Label(viewport, "", Vector2.zero, new Vector2(940, 230), 24);

            Section(panel, "Selected civic building", new Vector2(170, -204), new Vector2(1070, 170));
            detailPreview = FounderRect(panel, "Selected civic preview", new Vector2(-217, -201), new Vector2(208, 154)).gameObject.AddComponent<RawImage>();
            detailPreview.raycastTarget = false;
            detailTitle = Label(panel, "", new Vector2(135, -151), new Vector2(440, 38), 29, true);
            detailTitle.alignment = TextAnchor.MiddleLeft;
            detailCategory = Label(panel, "", new Vector2(135, -187), new Vector2(440, 30), 20);
            detailCategory.alignment = TextAnchor.MiddleLeft;
            detailDescription = Label(panel, "", new Vector2(135, -237), new Vector2(440, 68), 20);
            detailDescription.alignment = TextAnchor.UpperLeft;
            metrics = Label(panel, "", new Vector2(537, -200), new Vector2(294, 144), 20);
            metrics.alignment = TextAnchor.MiddleLeft;
            Rule(panel, new Vector2(-101, -204), new Vector2(1, 144));
            Rule(panel, new Vector2(373, -204), new Vector2(1, 144));
            Section(panel, "Civic placement actions", new Vector2(0, -343), new Vector2(1432, 78));
            Label(panel, "Rotate", new Vector2(-644, -343), new Vector2(100, 40), 23);
            rotateLeft = ActionButton(panel, "Rotate civic left", new Vector2(-552, -343), new Vector2(58, 50), "↶", 30, () => Rotate(-1));
            rotateRight = ActionButton(panel, "Rotate civic right", new Vector2(-482, -343), new Vector2(58, 50), "↷", 30, () => Rotate(1));
            facing = Label(panel, "", new Vector2(-393, -343), new Vector2(100, 40), 21);
            status = Label(panel, "", new Vector2(-34, -343), new Vector2(580, 54), 18);
            status.color = muted;
            ActionButton(panel, "Cancel civic buildings", new Vector2(365, -343), new Vector2(192, 52), "Cancel", 25, CloseTownSection);
            place = ActionButton(panel, "Place civic building", new Vector2(590, -343), new Vector2(228, 52), "Place Building", 25, () =>
            {
                if (selected == null || !CanOpenCityMenu || !TownConstructionVisible || IsSaving || Problem(selected) != null) return;
                var lot = selected; int facingSteps = rotation;
                CloseTownSection(); lotRotation = facingSteps; BeginLotPlacement(lot);
            });
            place.image.sprite = ZoneModalArtwork.Gold;
            Rebuild();

            void Rebuild()
            {
                foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
                cards.Clear(); selected = null;
                var visible = choices.Where(lot => CivicBuildingChoices.Category(lot) == CivicBuildingChoices.Categories[category].Id && CivicBuildingChoices.Era(lot) == eras[era].id).ToArray();
                for (int i = 0; i < categoryButtons.Length; i++)
                    categoryButtons[i].image.sprite = i == category ? ZoneModalArtwork.Gold : ZoneModalArtwork.Dark;
                resultsTitle.text = CivicBuildingChoices.Categories[category].Name + $" Buildings ({visible.Length})";
                eraButton.GetComponentInChildren<Text>().text = $"Era: {eras[era].Name} ({eras[era].FirstYear}–{eras[era].LastYear})  ▾";
                content.sizeDelta = new Vector2(1070, Mathf.Max(300, Mathf.CeilToInt(visible.Length / 4f) * 292));
                content.anchoredPosition = Vector2.zero; scroll.verticalNormalizedPosition = 1;
                empty.gameObject.SetActive(visible.Length == 0);
                empty.text = $"No {CivicBuildingChoices.Categories[category].Name.ToLowerInvariant()} buildings available in this era.\n\nSave a Civics → {LotClassification.SubcategoryName("civics", CivicBuildingChoices.Categories[category].Id)} lot in the Lot Editor to add one.";
                for (int i = 0; i < visible.Length; i++)
                {
                    var lot = visible[i];
                    var button = ActionButton(content, "Civic building " + lot.id, new Vector2(-402 + i % 4 * 268, -142 - i / 4 * 292),
                        new Vector2(254, 280), "", 20, () => { selected = lot; ShowSelection(); });
                    var cardRect = (RectTransform)button.transform;
                    cardRect.anchorMin = cardRect.anchorMax = new Vector2(.5f, 1);
                    button.GetComponentInChildren<Text>().gameObject.SetActive(false);
                    var image = FounderRect(button.transform, "Building preview", new Vector2(0, 48), new Vector2(222, 172)).gameObject.AddComponent<RawImage>();
                    image.texture = Preview(lot); image.enabled = image.texture; image.raycastTarget = false;
                    Label(button.transform, lot.name, new Vector2(0, -58), new Vector2(232, 58), 23, true);
                    var description = Label(button.transform, CivicBuildingChoices.CardDescription(lot), new Vector2(0, -113), new Vector2(228, 48), 17);
                    description.color = muted;
                    cards.Add((lot, button));
                }
                Canvas.ForceUpdateCanvases();
                scroll.Rebuild(CanvasUpdate.PostLayout); scroll.verticalNormalizedPosition = 1;
                if (visible.Length > 0) selected = visible[0];
                ShowSelection();
            }
            void Rotate(int step) { rotation = (rotation + step + 8) % 8; ShowSelection(); }
            void ShowSelection()
            {
                foreach (var card in cards) card.Button.image.sprite = card.Lot == selected ? ZoneModalArtwork.Gold : ZoneModalArtwork.Dark;
                facing.text = $"{rotation * 45}°";
                rotateLeft.interactable = rotateRight.interactable = selected != null;
                var problem = selected == null ? "Choose another category or era." : Problem(selected);
                place.interactable = selected != null && problem == null;
                detailPreview.texture = selected == null ? null : Preview(selected, rotation);
                detailPreview.enabled = detailPreview.texture;
                detailTitle.text = selected?.name ?? "No building selected";
                detailCategory.text = selected == null ? "" : CivicBuildingChoices.Categories[category].Name;
                detailDescription.text = selected == null ? "Choose an available civic building above." : CivicBuildingChoices.Description(selected);
                metrics.text = selected == null ? "" : $"Lot Size: {selected.width} × {selected.length} tiles\nFootprint: {selected.Size.x:0.#} × {selected.Size.y:0.#} m\nFacing: {rotation * 45}°";
                if (selected != null && selected.cost > 0) metrics.text += $"\nLot Cost: {selected.cost:N0}";
                status.text = libraryProblem ?? problem ?? "Click Place Building, then choose a site in your district.";
                status.color = problem != null && selected != null ? new Color(1, .70f, .48f) : muted;
            }
            string Problem(LotDefinition lot)
            {
                if (!CanBuildTown && lot.id != DistrictTownCenterId) return "Place a Town Center first.";
                if (lot.objects == null || lot.objects.Count == 0) return "This saved lot has no placed assets.";
                string assetProblem = LotAssetProblem(lot, catalog);
                if (assetProblem != null) return assetProblem;
                int firstYear = LotClassification.Era(lot)?.FirstYear ?? lot.firstEraYear;
                return firstYear > TownYear(SelectedDistrict) ? $"Available from {firstYear}." : null;
            }
            Texture Preview(LotDefinition lot, int steps = 0) => LotAssetProblem(lot, catalog) == null ? civicThumbnails.GetLot(lot, catalog, steps) : null;
            Text Label(Transform parent, string caption, Vector2 position, Vector2 size, int fontSize, bool bold = false)
            {
                var label = FounderText(parent, caption, position, size, fontSize); label.font = serif; label.color = cream;
                label.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal; label.raycastTarget = false; return label;
            }
            Button ActionButton(Transform parent, string name, Vector2 position, Vector2 size, string caption, int fontSize, UnityEngine.Events.UnityAction action)
            {
                var rect = FounderRect(parent, name, position, size); var image = Skin(rect, ZoneModalArtwork.Dark);
                var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
                var colors = button.colors; colors.normalColor = colors.selectedColor = Color.white;
                colors.highlightedColor = new Color(1.35f, 1.27f, 1.12f); colors.pressedColor = new Color(.77f, .72f, .62f); colors.disabledColor = new Color(.5f, .5f, .5f, .7f); colors.fadeDuration = .10f;
                button.colors = colors; if (action != null) button.onClick.AddListener(action);
                var text = Label(rect, caption, Vector2.zero, size - new Vector2(16, 4), fontSize);
                text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.offsetMin = new Vector2(8, 2); text.rectTransform.offsetMax = new Vector2(-8, -2);
                return button;
            }
            void Section(Transform parent, string name, Vector2 position, Vector2 size) => Skin(FounderRect(parent, name, position, size), ZoneModalArtwork.Section);
            Image Skin(RectTransform rect, Sprite sprite) { var image = rect.gameObject.AddComponent<Image>(); image.sprite = sprite; image.type = Image.Type.Sliced; return image; }
            void Rule(Transform parent, Vector2 position, Vector2 size) { var image = FounderRect(parent, "Civic divider", position, size).gameObject.AddComponent<Image>(); image.color = new Color(.6f, .55f, .41f, .65f); image.raycastTarget = false; }
            void Icon(Transform parent, int index, Vector2 position, float height)
            {
                if (!icons) return; var crop = CivicIconCrops[index];
                var importedCrop = new Rect(crop.x / 1774 * icons.width, crop.y / 887 * icons.height,
                    crop.width / 1774 * icons.width, crop.height / 887 * icons.height);
                TownArtwork(parent, "Civic service icon", icons, importedCrop, position, new Vector2(height * crop.width / crop.height, height));
            }
        }
    }
}
