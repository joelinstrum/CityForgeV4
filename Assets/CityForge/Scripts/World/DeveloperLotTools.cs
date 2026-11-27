using System;
using System.IO;
using System.Linq;
using CityForge.Lots;
using CityForge.UI;
using UnityEngine;
using UnityEngine.UI;
namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private RectTransform developerToolsHud;
        private Button developerModeButton, developerLotsButton;
        private bool developerLotPicker, developerLotPlacement;
        private RegionTown LotPlacementFounder => DeveloperMode && (developerLotPlacement || developerLotPicker) ? null : PendingFounder;

        private void BuildDeveloperTools(Transform hud, Font font)
        {
            developerToolsHud = FounderRect(hud, "Game mode and developer lots", Vector2.zero, new Vector2(205, 94));
            developerToolsHud.anchorMin = developerToolsHud.anchorMax = developerToolsHud.pivot = Vector2.one;
            developerToolsHud.anchoredPosition = new Vector2(-25, -84);
            developerModeButton = TownButton(developerToolsHud, font, "Mode: Player", 0, 0, 205, () => { if (!DistrictLoading && !IsSaving) SetDeveloperMode(!DeveloperMode); });
            developerModeButton.gameObject.name = "Switch player developer mode";
            developerLotsButton = TownButton(developerToolsHud, font, "Lots", 0, -46, 205, OpenDeveloperLotPicker);
            developerLotsButton.gameObject.name = "Developer Lots";
            var label = developerLotsButton.GetComponentInChildren<Text>();
            label.rectTransform.offsetMin = new Vector2(42, 0); label.rectTransform.offsetMax = new Vector2(-8, 0);
            var iconRect = FounderRect(developerLotsButton.transform, "Lots folder icon", new Vector2(-79, 0), new Vector2(28, 28));
            var icon = iconRect.gameObject.AddComponent<LotEditorIcon>(); icon.Glyph = LotGlyph.Open; icon.color = TownGold; icon.raycastTarget = false;
            RefreshDeveloperTools();
        }

        private void RefreshDeveloperTools()
        {
            if (!developerToolsHud)
            {
                // Also attach when an already-open region picks up this HUD after a script reload.
                if (regionHudVisibility && lotFont) BuildDeveloperTools(regionHudVisibility.transform, lotFont);
                return;
            }
            bool available = !DistrictLoading && !IsSaving;
            developerModeButton.interactable = available;
            developerModeButton.GetComponentInChildren<Text>().text = DeveloperMode ? "Mode: Developer" : "Mode: Player";
            MarkTownButton(developerModeButton, DeveloperMode);
            developerLotsButton.gameObject.SetActive(DeveloperMode);
            developerLotsButton.interactable = available && SelectedDistrict >= 0 && !playerLotWorkspace;
            if (developerLotPicker && savedLotPicker && Input.GetKeyDown(KeyCode.Escape)) CloseSavedLotPicker();
        }

        private void CloseDeveloperLotTools()
        {
            if (developerLotPlacement) CancelLotPlacement();
            else if (developerLotPicker) CloseSavedLotPicker();
        }

        internal static LotDefinition[] DeveloperLots(LotLibrary library) =>
            library.List(LotType.Playable).Concat(library.List(LotType.Zoning)).Concat(library.List(LotType.Template))
                .Select(entry => entry.Lot).GroupBy(lot => lot.id).Select(group => group.First())
                .OrderBy(lot => lot.name, StringComparer.OrdinalIgnoreCase).ToArray();

        internal static LotDefinition DeveloperLotDraft(LotDefinition source, LotAssetCatalog catalog)
        {
            var draft = LotDefinition.Parse(JsonUtility.ToJson(source));
            if (draft.type == LotType.Zoning && draft.zoningLayout.Configured &&
                !draft.objects.Any(item => catalog.Find(item.assetId)?.category == "Buildings"))
            {
                var building = LotGrowableBuildings.Candidates(draft, catalog).FirstOrDefault();
                if (building != null) draft = LotGrowableBuildings.Materialize(draft, building, "developer-preview:" + draft.id);
            }
            // A placement is an instance, not an edit to a saved growable blueprint/template.
            draft.type = LotType.Playable;
            return draft;
        }

        private void OpenDeveloperLotPicker() => BuildDeveloperLotPicker(LotLibrary.Local);
        private void BuildDeveloperLotPicker(LotLibrary library)
        {
            if (!DeveloperMode || SelectedDistrict < 0 || DistrictLoading || IsSaving || playerLotWorkspace) return;
            LotDefinition[] lots;
            try { lots = DeveloperLots(library); }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            { SetLotFeedback("Could not read the saved lot library."); return; }
            CancelLotPlacement(); CancelRoadPlacement(); CloseFounderPicker(); CloseTownSection(); CloseGameplayTools();
            GetComponent<NatureToolbar>()?.Dismiss(); SetRoadPaths(false); CancelDistrictActor();
            lastRepaveSurface = null;
            developerLotPicker = true;
            savedLotPicker = FounderRect(developerToolsHud.parent, "Developer lot library", Vector2.zero, Vector2.zero);
            savedLotPicker.anchorMin = Vector2.zero; savedLotPicker.anchorMax = Vector2.one;
            savedLotPicker.offsetMin = savedLotPicker.offsetMax = Vector2.zero;
            savedLotPicker.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .65f);
            savedLotPicker.SetAsLastSibling();
            var panel = FounderRect(savedLotPicker, "Lots", Vector2.zero, new Vector2(760, 660));
            StyleTownPanel(panel, TownPanelColor);
            panel.gameObject.AddComponent<GrowableModalFit>().Initialize(new Vector2(760, 660));
            FounderText(panel, "Lots · Developer Mode", new Vector2(0, 286), new Vector2(650, 40), 27).color = TownGold;
            TownButton(panel, lotFont, "×", 704, -12, 36, CloseSavedLotPicker);
            var searchObject = DefaultControls.CreateInputField(new DefaultControls.Resources()); searchObject.name = "Search developer lots";
            searchObject.transform.SetParent(panel, false); var searchRect = (RectTransform)searchObject.transform;
            searchRect.anchorMin = searchRect.anchorMax = searchRect.pivot = new Vector2(.5f, .5f);
            searchRect.anchoredPosition = new Vector2(-123, 225); searchRect.sizeDelta = new Vector2(452, 38);
            var search = searchObject.GetComponent<InputField>();
            foreach (var text in searchObject.GetComponentsInChildren<Text>()) { text.font = lotFont; text.fontSize = 18; text.color = Color.black; }
            ((Text)search.placeholder).text = "Search lots…";
            var typeObject = DefaultControls.CreateDropdown(new DefaultControls.Resources()); typeObject.name = "Developer lot type";
            typeObject.transform.SetParent(panel, false); var typeRect = (RectTransform)typeObject.transform;
            typeRect.anchorMin = typeRect.anchorMax = typeRect.pivot = new Vector2(.5f, .5f);
            typeRect.anchoredPosition = new Vector2(230, 225); typeRect.sizeDelta = new Vector2(240, 38);
            var arrow = typeObject.transform.Find("Arrow");
            if (arrow)
            {
                var originalArrow = arrow.GetComponent<Graphic>(); if (originalArrow) originalArrow.enabled = false;
                var chevronRect = new GameObject("Filter chevron", typeof(RectTransform)).GetComponent<RectTransform>(); chevronRect.SetParent(arrow, false);
                chevronRect.anchorMin = Vector2.zero; chevronRect.anchorMax = Vector2.one; chevronRect.offsetMin = chevronRect.offsetMax = Vector2.zero;
                var chevron = chevronRect.gameObject.AddComponent<LotEditorIcon>(); chevron.Glyph = LotGlyph.ChevronDown; chevron.color = Color.black; chevron.raycastTarget = false;
            }
            var filter = typeObject.GetComponent<Dropdown>(); filter.ClearOptions();
            filter.AddOptions(new System.Collections.Generic.List<string> { "All lots", "Lots", "Growable Lots", "Templates" });
            foreach (var text in typeObject.GetComponentsInChildren<Text>(true)) { text.font = lotFont; text.fontSize = 18; }
            var status = FounderText(panel, "", new Vector2(0, 182), new Vector2(700, 30), 17);
            var viewport = FounderRect(panel, "Lot list", new Vector2(0, -28), new Vector2(700, 380));
            viewport.gameObject.AddComponent<Image>().color = new Color(.05f, .07f, .09f); viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.viewport = viewport;
            var content = FounderRect(viewport, "Developer lot rows", Vector2.zero, Vector2.zero);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1); scroll.content = content;
            FounderText(panel, "Click a lot, then click the map to place it. Tab rotates · Esc cancels.\nSave the region to keep placed lots.", new Vector2(0, -278), new Vector2(700, 62), 17);
            var catalog = LotAssetCatalog.Load();
            void Refresh()
            {
                foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
                var visible = lots.Where(lot => (filter.value == 0 || lot.type == (filter.value == 1 ? LotType.Playable : filter.value == 2 ? LotType.Zoning : LotType.Template)) &&
                    (string.IsNullOrWhiteSpace(search.text) || (lot.name ?? "").IndexOf(search.text.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
                status.text = visible.Length + " lots";
                int top = 6;
                foreach (var lot in visible)
                {
                    string kind = lot.type == LotType.Zoning ? "Growable" : lot.type == LotType.Template ? "Template" : "Lot";
                    string problem = LotAssetProblem(lot, catalog);
                    var button = TownButton(content, lotFont, $"{lot.name} · {lot.width}×{lot.length} · {kind}" + (problem == null ? "" : " · Unavailable"), 8, -top, 680,
                        () => BeginDeveloperLotPlacement(lot, catalog));
                    button.gameObject.name = "Developer lot · " + lot.id; button.interactable = problem == null;
                    button.GetComponentInChildren<Text>().fontSize = 18; top += 48;
                }
                if (visible.Length == 0) { var text = FounderText(content, lots.Length == 0 ? "Save a lot in the Lot Editor to see it here." : "No matching lots.", Vector2.zero, new Vector2(670, 80), 19); text.rectTransform.anchoredPosition = new Vector2(0, -50); }
                content.sizeDelta = new Vector2(0, Mathf.Max(380, top)); content.anchoredPosition = Vector2.zero;
            }
            search.onValueChanged.AddListener(_ => Refresh()); filter.onValueChanged.AddListener(_ => Refresh()); Refresh(); search.ActivateInputField();
        }

        private void BeginDeveloperLotPlacement(LotDefinition lot, LotAssetCatalog catalog)
        {
            if (!DeveloperMode || SelectedDistrict < 0 || DistrictLoading || IsSaving) return;
            var draft = DeveloperLotDraft(lot, catalog);
            if (LotAssetProblem(draft, catalog) is { } problem) { SetLotFeedback(problem); return; }
            BeginLotPlacement(draft); developerLotPlacement = true;
            SetLotFeedback("Click to place " + draft.name + " · Tab to rotate · Esc to cancel");
        }
    }
}
