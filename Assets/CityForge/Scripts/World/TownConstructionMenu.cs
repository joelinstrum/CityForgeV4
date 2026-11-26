using System;
using System.IO;
using System.Linq;
using CityForge.Lots;
using CityForge.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private enum TownSection { None, Transport, Zones, CivicLots, Utilities, Parks, TownInfo }

        private RectTransform townModeSwitch, townToolbar, townPanel;
        private Button natureModeButton, townModeButton, addTownCenterButton;
        private RectTransform transportModal;
        private readonly Button[] townCategories = new Button[8];
        private TownSection townSection;
        private Texture2D darkTownButtons, goldTownButtons, transportButtonArtwork;
        private Texture2D darkIndustryButtons, goldIndustryButtons;
        private Material townButtonArtwork;
        private static readonly Color TownPanelColor = new(.09f, .10f, .10f, .98f);
        private static readonly Color TownGold = new(.98f, .86f, .63f);

        internal bool TownConstructionVisible { get; private set; }
        private bool CanOpenCityMenu => SelectedDistrict >= 0 && !DistrictLoading &&
            Capital is { awaitingFoundersBuilding: false } && PlacedLots.Exists(lot => IsFoundersLot(lot.definition));
        private bool CanBuildTown => SelectedDistrict >= 0 && !DistrictLoading &&
            Capital is { awaitingFoundersBuilding: false } && PlacedLots.Exists(lot => IsFoundersLot(lot.definition)) &&
            Towns.Any(town => ActiveDistrictMembers.Contains(town.DistrictIndex(GridSide)));

        private void BuildTownConstructionMenu(Transform hud, Font font)
        {
            darkTownButtons = Resources.Load<Texture2D>("UI/TownConstruction/Dark");
            goldTownButtons = Resources.Load<Texture2D>("UI/TownConstruction/Gold");
            transportButtonArtwork = Resources.Load<Texture2D>("UI/TownConstruction/Transport");
            darkIndustryButtons = Resources.Load<Texture2D>("UI/TownConstruction/IndustryStreetscapeDark");
            goldIndustryButtons = Resources.Load<Texture2D>("UI/TownConstruction/IndustryStreetscapeGold");
            var artworkShader = Resources.Load<Shader>("UI/TownButtonArtwork");
            if (artworkShader && ((darkTownButtons && goldTownButtons) || transportButtonArtwork || darkIndustryButtons))
                townButtonArtwork = Own(new Material(artworkShader));
            townModeSwitch = BottomPanel(hud, "District tool modes", 64, 310, 44);
            natureModeButton = TownButton(townModeSwitch, font, "Nature", 0, 0, 154, () => SetTownConstruction(false));
            townModeButton = TownButton(townModeSwitch, font, "Town", 156, 0, 154, () => SetTownConstruction(true));
            townModeButton.GetComponentInChildren<Text>(includeInactive: true).text = "City";
            SkinTownButton(natureModeButton, new Rect(740, 462, 347, 112), 44);
            SkinTownButton(townModeButton, new Rect(1087, 462, 349, 112), 44);

            var recovery = BottomPanel(hud, "Missing Town Center", 14, 250, 40);
            addTownCenterButton = TownButton(recovery, font, "Add Town Center", 0, 0, 250, OpenMissingTownCenter);
            addTownCenterButton.image.sprite = ZoneModalArtwork.Dark;
            addTownCenterButton.image.type = Image.Type.Sliced;
            addTownCenterButton.image.color = Color.white;
            addTownCenterButton.GetComponentInChildren<Text>().color = TownGold;

            townToolbar = BottomPanel(hud, "Town construction toolbar", 117, 1336, 82);
            string[] labels = { "Transport", "Zones", "Civic Lots", "Utilities", "Parks", "Town Info" };
            int[] edges = { 409, 752, 1090, 1435, 1778, 2121 };
            for (int i = 0; i < labels.Length; i++)
            {
                var section = (TownSection)(i + 1);
                townCategories[i] = TownButton(townToolbar, font, labels[i], i * 148, 0, 146,
                    () => ShowTownSection(section));
                if (section == TownSection.Transport)
                    SkinTownButton(townCategories[i], TransportCrop(false), 82, transportButtonArtwork);
                else
                    SkinTownButton(townCategories[i], new Rect(edges[i - 1], 236, edges[i] - edges[i - 1], 206), 82);
            }
            var streetscape = TownButton(townToolbar, font, "Streetscape", 888, 0, 146, OpenStreetscape);
            townCategories[7] = streetscape;
            SkinTownButton(streetscape, new Rect(1101, 100, 1005, 512), 82, darkIndustryButtons, preserveAspect: true);
            var industry = TownButton(townToolbar, font, "Industry", 1036, 0, 146,
                OpenIndustry);
            townCategories[6] = industry;
            SkinTownButton(industry, new Rect(72, 100, 1007, 512), 82, darkIndustryButtons, preserveAspect: true);
            BuildDetonateButton(font);
            BuildLevelerButton(font);
            RefreshTownConstruction();

            RectTransform BottomPanel(Transform parent, string title, float bottom, float width, float height)
            {
                var rect = FounderRect(parent, title, Vector2.zero, new Vector2(width, height));
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 0);
                rect.anchoredPosition = new Vector2(0, bottom);
                return rect;
            }
        }

        private static Button TownButton(Transform parent, Font font, string title, int x, int y, int width,
            UnityEngine.Events.UnityAction action)
        {
            var label = AddLotButton(parent, font, title, x, width, action);
            var button = label.GetComponentInParent<Button>(includeInactive: true);
            ((RectTransform)button.transform).anchoredPosition = new Vector2(x, y);
            return button;
        }

        private static Rect TransportCrop(bool active) => new Rect(active ? 1101 : 263, 146, 806, 446);

        private void SkinTownButton(Button button, Rect crop, float height) =>
            SkinTownButton(button, crop, height, darkTownButtons && goldTownButtons ? darkTownButtons : null);

        private void SkinTownButton(Button button, Rect crop, float height, Texture2D artwork, bool preserveAspect = false)
        {
            var rect = (RectTransform)button.transform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            if (!artwork) return;

            button.image.color = Color.clear;
            button.GetComponentInChildren<Text>(includeInactive: true).enabled = false;
            var artworkSize = rect.sizeDelta;
            if (preserveAspect)
                artworkSize = crop.size * Mathf.Min(artworkSize.x / crop.width, artworkSize.y / crop.height);
            var face = TownArtwork(rect, "Button artwork", artwork, crop, Vector2.zero, artworkSize);
            face.material = townButtonArtwork;
            button.targetGraphic = face;
            var colors = button.colors;
            colors.highlightedColor = new Color(.95f, .95f, .95f);
            colors.pressedColor = new Color(.80f, .80f, .80f);
            button.colors = colors;
        }

        private static RawImage TownArtwork(Transform parent, string name, Texture2D atlas, Rect crop,
            Vector2 position, Vector2 size)
        {
            var rect = FounderRect(parent, name, position, size);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.texture = atlas;
            image.uvRect = TownArtworkUV(atlas, crop);
            image.raycastTarget = false;
            return image;
        }

        private static Rect TownArtworkUV(Texture2D atlas, Rect crop) =>
            new Rect(crop.x / atlas.width, 1f - crop.yMax / atlas.height,
                crop.width / atlas.width, crop.height / atlas.height);

        private static void StyleTownPanel(RectTransform rect, Color color)
        {
            rect.gameObject.AddComponent<Image>().color = color;
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.48f, .43f, .34f, .9f);
            outline.effectDistance = new Vector2(1, -1);
        }

        private bool MissingTownCenter => SelectedDistrict >= 0 && !DistrictLoading &&
            (PendingFounder is { } capital && InCapitalDistrict(capital) ||
             CanOpenCityMenu && Towns.Any(town => ActiveDistrictMembers.Contains(town.DistrictIndex(GridSide))) &&
             !PlacedLots.Exists(lot => ActiveDistrictMembers.Contains(lot.district) &&
                 (IsFoundersLot(lot.definition) || IsTownCenterLot(lot.definition))));

        private void OpenMissingTownCenter()
        {
            if (!MissingTownCenter || IsSaving) return;
            CancelRoadPlacement();
            CloseTownSection();
            if (PendingFounder != null) OpenFounderPicker();
            else
            {
                SetTownConstruction(true);
                ShowTownSection(TownSection.CivicLots);
            }
        }

        private void RefreshTownConstruction()
        {
            if (!townModeSwitch) return;
            addTownCenterButton.transform.parent.gameObject.SetActive(MissingTownCenter &&
                !founderPicker && !transportModal && lotPlacementDraft == null && !playerLotWorkspace);
            addTownCenterButton.interactable = !IsSaving;
            bool available = CanOpenCityMenu;
            if (!available && TownConstructionVisible)
            {
                TownConstructionVisible = false;
                CloseTownSection();
            }
            townModeSwitch.anchoredPosition = new Vector2(0, 64);
            townModeSwitch.gameObject.SetActive(available);
            townToolbar.gameObject.SetActive(available && TownConstructionVisible);
            MarkTownButton(natureModeButton, !TownConstructionVisible);
            MarkTownButton(townModeButton, TownConstructionVisible);
        }

        private void SetTownConstruction(bool town)
        {
            if (districtTexturePaint) return;
            if (TownConstructionVisible == town || (town && !CanOpenCityMenu) || IsSaving) return;
            CancelRoadPlacement();
            CancelLotPlacement();
            SelectDistrictLot(null);
            CloseTownSection();
            lastRepaveSurface = null;
            TownConstructionVisible = town;
            if (!town) SetRoadPaths(false);
            GetComponent<NatureToolbar>()?.Dismiss();
            RefreshTownConstruction();
        }

        private void CloseTownSection()
        {
            CancelTexturePaint();
            CancelParkPath();
            CancelParkTreeBrush();
            CancelStreetscape();
            SetDetonate(false);
            SetLeveler(false);
            CloseRoadContext();
            CloseTransportModal();
            CloseZoning();
            if (townPanel)
            {
                townPanel.gameObject.SetActive(false);
                Destroy(townPanel.gameObject);
                townPanel = null;
            }
            townSection = TownSection.None;
            SetZoningGameplayPause(false);
            foreach (var button in townCategories)
                if (button) MarkTownButton(button, false);
        }

        private bool DismissTownConstruction()
        {
            if (!townPanel) return false;
            CloseTownSection();
            return true;
        }

        private void MarkTownButton(Button button, bool active)
        {
            if (!button) return;
            if (button.targetGraphic is RawImage face)
            {
                if (face.texture == transportButtonArtwork)
                {
                    face.uvRect = TownArtworkUV(transportButtonArtwork, TransportCrop(active));
                    return;
                }
                bool industrySheet = face.texture == darkIndustryButtons || face.texture == goldIndustryButtons;
                var texture = industrySheet
                    ? (active ? goldIndustryButtons : darkIndustryButtons)
                    : (active ? goldTownButtons : darkTownButtons);
                if (face.texture != texture) face.texture = texture;
                return;
            }
            button.image.color = active ? new Color(.48f, .33f, .15f) : new Color(.16f, .17f, .17f);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const float ZoneSelectionHeight = 904f;
#else
        private const float ZoneSelectionHeight = 772f;
#endif

        private void ShowTownSection(TownSection section)
        {
            if (districtTexturePaint) return;
            if (!TownConstructionVisible || !CanOpenCityMenu || IsSaving) return;
            if (!CanBuildTown && section != TownSection.CivicLots)
            { ShowDistrictMessage("Place a Town Center from Civics → Government first."); return; }
            CancelRoadPlacement();
            if (section != TownSection.Transport) lastRepaveSurface = null;
            if (section == TownSection.Zones && townSection == section && townPanel && !townPanel.gameObject.activeSelf)
            {
                CancelZonedBlock(false); zoneToolArmed = false;
                CancelZoneDrag();
                townPanel.gameObject.SetActive(true);
                SetZoningGameplayPause(true);
                return;
            }
            bool toggleOff = townSection == section;
            CloseTownSection();
            if (toggleOff) return;
            CancelLotPlacement();
            townSection = section;
            SetZoningGameplayPause(section==TownSection.Zones);
            var category = townCategories[(int)section - 1];
            MarkTownButton(category, true);

            if (section == TownSection.CivicLots)
            {
                BuildCivicSelectionModal();
                return;
            }

            if(section==TownSection.Parks){BuildParksOverview();return;}

            float width = section switch
            {
                TownSection.Transport => 1120,
                TownSection.Zones => 1460,
                TownSection.Parks => 820,
                _ => 560
            };
            townPanel = FounderRect(townToolbar, "Town tool panel", new Vector2(0, section == TownSection.Zones ? 120 : 92), new Vector2(width, section == TownSection.Zones ? ZoneSelectionHeight : section == TownSection.Parks ? 376 : 276));
            townPanel.anchorMin = townPanel.anchorMax = townPanel.pivot = new Vector2(.5f, 0);
            if(section==TownSection.Zones){townPanel.SetParent(townToolbar.parent,false);townPanel.anchorMin=townPanel.anchorMax=townPanel.pivot=Vector2.one*.5f;townPanel.anchoredPosition=Vector2.zero;}
            if (section != TownSection.Zones) StyleTownPanel(townPanel, TownPanelColor);
            if (section != TownSection.Transport && section != TownSection.Zones)
                FounderText(townPanel, category.name, new Vector2(0, section == TownSection.Parks ? 163 : 113), new Vector2(440, 34), 25).color = TownGold;
            if (section != TownSection.Zones) TownButton(townPanel, lotFont, "×", (int)width - 47, -9, 36, CloseTownSection);

            switch (section)
            {
                case TownSection.Transport:
                    ShowTransport();
                    break;
                case TownSection.Zones:
                    ShowZoning();
                    break;
                case TownSection.Parks:
                    ShowTownLots(true);
                    break;
                case TownSection.TownInfo:
                    var towns = TownsInDistrict(SelectedDistrict);
                    int lotCount = PlacedLots.Count(lot => ActiveDistrictMembers.Contains(lot.district));
                    var names = string.Join("\n", towns.Select(town => town.name + (town.capital ? " · Capital" : "")));
                    FounderText(townPanel, names + $"\n\n{lotCount} placed {(lotCount == 1 ? "lot" : "lots")}",
                        Vector2.up * 5f, new Vector2(490, 160), 18);
                    break;
                default:
                    FounderText(townPanel, "Water, sanitation and power\n\nConstruction tools are not available yet.",
                        Vector2.up * 10f, new Vector2(490, 160), 18);
                    break;
            }
        }

        private void ShowTransport()
        {
            const string artPath = "UI/TownConstruction/Transport";
            var concept = Resources.Load<Texture2D>(artPath + "Concept");
            if (concept)
            {
                TownArtwork(townPanel, "Transport heading", concept, new Rect(674, 470, 331, 42),
                    new Vector2(0, 113), new Vector2(331, 42));
            }
            else
                FounderText(townPanel, "Transport", new Vector2(0, 113), new Vector2(440, 34), 25).color = TownGold;

            var cards = new[]
            {
                (Label: "Roads", Art: "", ConceptCrop: new Rect(292, 533, 160, 112)),
                (Label: "Bridges", Art: "Bridge", ConceptCrop: new Rect(484, 533, 157, 112)),
                (Label: "Rail", Art: "Rail", ConceptCrop: new Rect(669, 529, 160, 116)),
                (Label: "Water", Art: "Water", ConceptCrop: new Rect(855, 529, 160, 116)),
                (Label: "Transit", Art: "Transit", ConceptCrop: new Rect(1041, 533, 160, 112)),
                (Label: "Air", Art: "", ConceptCrop: new Rect(1228, 533, 156, 112))
            };
            for (int i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                var button = TownButton(townPanel, lotFont, card.Label, 17 + i * 183, -54, 171,
                    () => OpenTransportModal(card.Label));
                button.image.color = Color.white;
                var colors = button.colors;
                colors.normalColor = colors.selectedColor = new Color(.16f, .17f, .17f);
                colors.highlightedColor = new Color(.40f, .28f, .14f);
                colors.pressedColor = new Color(.48f, .33f, .15f);
                button.colors = colors;
                var rect = (RectTransform)button.transform;
                rect.sizeDelta = new Vector2(171, 174);
                var outline = button.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(.57f, .49f, .37f);
                outline.effectDistance = new Vector2(1, -1);
                var label = button.GetComponentInChildren<Text>();
                label.rectTransform.offsetMax = new Vector2(0, -135);
                label.color = TownGold;
                label.fontSize = 22;

                var art = string.IsNullOrEmpty(card.Art) ? null : Resources.Load<Texture2D>(artPath + card.Art);
                if (art)
                    TownArtwork(rect, card.Label + " illustration", art, new Rect(0, 0, art.width, art.height),
                        new Vector2(0, 13), new Vector2(163, 109)).material = townButtonArtwork;
                else if (concept)
                    TownArtwork(rect, card.Label + " illustration", concept, card.ConceptCrop,
                        new Vector2(0, 13), new Vector2(160, 112));
            }
        }

        private LotAssetThumbnails industryThumbnails;

        private void OpenIndustry()
        {
            if (!TownConstructionVisible || !CanBuildTown || IsSaving) return;
            CancelRoadPlacement();
            CancelLotPlacement();
            CloseTownSection();
            lastRepaveSurface = null;
            MarkTownButton(townCategories[6], true);

            transportModal = FounderRect(townToolbar.parent, "Industry modal", Vector2.zero, Vector2.zero);
            transportModal.anchorMin = Vector2.zero;
            transportModal.anchorMax = Vector2.one;
            transportModal.offsetMin = transportModal.offsetMax = Vector2.zero;
            transportModal.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .66f);
            var panel = FounderRect(transportModal, "Industry", Vector2.zero, new Vector2(1740, 900));
            StyleTownPanel(panel, new Color(.045f, .085f, .10f));
            var brass = new Color(.63f, .45f, .25f);
            var border = panel.GetComponent<Outline>();
            border.effectColor = brass;
            border.effectDistance = Vector2.one * 2f;
            Label(panel, "Industry", new Vector2(-560, 396), new Vector2(510, 72), 50);
            TownButton(panel, lotFont, "×", 1667, -29, 45, CloseTransportModal);
            Rule(new Vector2(0, 345), new Vector2(1736, 2));
            Rule(new Vector2(-511, -30), new Vector2(2, 726));
            Rule(new Vector2(425, -30), new Vector2(2, 726));
            Rule(new Vector2(0, -403), new Vector2(1736, 2));
            Label(panel, "Hover for details  •  Click a lot to place it", new Vector2(-43, -426),
                new Vector2(870, 34), 19).alignment = TextAnchor.MiddleCenter;
            Label(panel, "Tile grid snapping", new Vector2(-689, -426), new Vector2(296, 34), 18);

            LotDefinition[] lots;
            try
            {
                lots = LotLibrary.Local.List().Select(entry => entry.Lot)
                    .Where(lot => LotClassification.FindCategory(lot.category)?.id == "industrial" && !IsFoundersLot(lot))
                    .OrderBy(lot => lot.name).ToArray();
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                Label(panel, "Could not read the saved lot library.", new Vector2(-43, 0), new Vector2(820, 70), 21);
                return;
            }

            var catalog = LotAssetCatalog.Load();
            var tabs = Resources.Load<Texture2D>("UI/TownConstruction/IndustryTabs");
            var buildings = Resources.Load<Texture2D>("UI/TownConstruction/IndustryBuildings");
            industryThumbnails = new LotAssetThumbnails();
            var subcategories = LotClassification.ForCategory("industrial");
            var industryTabs = subcategories.Where(item => item.industryTabOrder > 0)
                .OrderBy(item => item.industryTabOrder).ToArray();
            var featured = subcategories.Where(item => !string.IsNullOrEmpty(item.industryCardTitle))
                .OrderBy(item => item.industryArtwork).ToArray();
            var labels = new[] { "All Industries" }.Concat(industryTabs.Select(item => item.name)).ToArray();
            int[] edges = { 18, 372, 728, 1086, 1444, 1802, 2157 };
            var buttons = new Button[labels.Length];
            var eras = CityForgeTimeline.Eras;
            int era = Mathf.Max(0, Array.FindLastIndex(eras, item => item.FirstYear <= TownYear(SelectedDistrict)));
            int period = Mathf.Max(0, Array.FindLastIndex(eras[era].Periods,
                item => item.FirstYear <= TownYear(SelectedDistrict)));
            int selectedTab = 0;
            RectTransform results = null;
            RectTransform details = null;
            var eraLabel = Label(panel, "", new Vector2(546, 397), new Vector2(295, 45), 26);
            eraLabel.alignment = TextAnchor.MiddleCenter;
            var previous = TownButton(panel, lotFont, "‹", 1220, -33, 46, null);
            var next = TownButton(panel, lotFont, "›", 1566, -33, 46, null);

            var periodLabel = Label(panel, "", new Vector2(57, 397), new Vector2(320, 56), 21);
            periodLabel.alignment = TextAnchor.MiddleCenter;
            var previousPeriod = TownButton(panel, lotFont, "‹", 734, -33, 42, null);
            var nextPeriod = TownButton(panel, lotFont, "›", 1082, -33, 42, null);
            bool showTimeline = false;
            var timeline = TownButton(panel, lotFont, "Timeline overview", 488, -33, 215, null);
            previousPeriod.onClick.AddListener(() => ChangePeriod(-1));
            nextPeriod.onClick.AddListener(() => ChangePeriod(1));

            previous.onClick.AddListener(() => ChangeEra(-1));
            next.onClick.AddListener(() => ChangeEra(1));

            timeline.onClick.AddListener(() =>
            {
                showTimeline = !showTimeline;
                RefreshIndustry();
            });

            for (int i = 0; i < labels.Length; i++)
            {
                int index = i;
                buttons[i] = TownButton(panel, lotFont, labels[i], 48, -133 - i * 94, 260, () =>
                {
                    selectedTab = index;
                    showTimeline = false;
                    RefreshIndustry();
                });
                SkinTownButton(buttons[i], HasTabArtwork(i) ? TabCrop(i - 1, false) : default, 90, HasTabArtwork(i) ? tabs : null, preserveAspect: true);
            }
            RefreshIndustry();

            void ChangeEra(int direction)
            {
                era = Mathf.Clamp(era + direction, 0, eras.Length - 1);
                period = 0;
                RefreshIndustry();
            }

            void ChangePeriod(int direction)
            {
                period = Mathf.Clamp(period + direction, 0, eras[era].Periods.Length - 1);
                RefreshIndustry();
            }

            bool HasTabArtwork(int index) => index > 0 && index < edges.Length;

            Rect TabCrop(int index, bool active) =>
                new Rect(edges[index], active ? 359 : 231, edges[index + 1] - edges[index], 122);

            void RefreshIndustry()
            {
                var selectedPeriod = eras[era].Periods[period];
                eraLabel.text = eras[era].Name;
                periodLabel.text = $"{selectedPeriod.Name}\n{selectedPeriod.FirstYear}–{selectedPeriod.LastYear}";
                previousPeriod.interactable = period > 0;
                nextPeriod.interactable = period < eras[era].Periods.Length - 1;
                previous.interactable = era > 0;
                next.interactable = era < eras.Length - 1;
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (HasTabArtwork(i) && tabs && buttons[i].targetGraphic is RawImage face)
                        face.uvRect = TownArtworkUV(tabs, TabCrop(i - 1, i == selectedTab));
                    else MarkTownButton(buttons[i], i == selectedTab);
                }

                MarkTownButton(timeline, showTimeline);
                if (showTimeline)
                {
                    Replace(ref results, "Timeline overview", new Vector2(178, -29), new Vector2(1332, 722));
                    results.gameObject.AddComponent<Image>().color = new Color(.045f, .085f, .10f);
                    if (details) details.gameObject.SetActive(false);
                    ShowTimelineColumn(eras[era].Name, eras[era].summary, eras[era].suggestions, -333);
                    ShowTimelineColumn(selectedPeriod.Name, selectedPeriod.summary, selectedPeriod.suggestions, 333);
                    return;
                }

                Replace(ref results, "Industry catalog", new Vector2(-43, -29), new Vector2(908, 722));
                Label(results, labels[selectedTab], new Vector2(0, 328), new Vector2(880, 50), 31);
                var availableLots = lots.Where(lot => (selectedTab == 0 || IndustryTab(lot) == industryTabs[selectedTab - 1].id) &&
                    (LotClassification.Era(lot)?.FirstYear ?? lot.firstEraYear) <= selectedPeriod.LastYear).ToArray();
                var choices = new System.Collections.Generic.List<(string Name, LotDefinition Lot, int Artwork)>();
                var featuredLots = new System.Collections.Generic.HashSet<LotDefinition>();
                foreach (var facility in featured)
                {
                    if (selectedTab != 0 && facility.industryTab != industryTabs[selectedTab - 1].id) continue;
                    var lot = availableLots.FirstOrDefault(candidate =>
                        string.Equals(candidate.subcategory, facility.id, StringComparison.OrdinalIgnoreCase))
                        ?? availableLots.FirstOrDefault(candidate => IndustryAssignment(candidate) == facility);
                    choices.Add((facility.industryCardTitle, lot, facility.industryArtwork - 1));
                    if (lot != null) featuredLots.Add(lot);
                }
                choices.AddRange(availableLots.Where(lot => !featuredLots.Contains(lot))
                    .Select(lot => (lot.name, lot, (IndustryAssignment(lot)?.industryArtwork ?? 0) - 1)));
                if (choices.Count > 0)
                {
                    var first = choices[0];
                    ShowDetails(first.Name, first.Lot, first.Artwork);
                }
                else if (details)
                {
                    details.gameObject.SetActive(false);
                }
                if (choices.Count == 0)
                {
                    Label(results, "No saved lots in this category for the selected period.", new Vector2(0, 175),
                        new Vector2(880, 50), 23);
                    return;
                }

                var viewport = FounderRect(results, "Industry cards", new Vector2(0, -27), new Vector2(888, 646));
                viewport.gameObject.AddComponent<Image>().color = Color.clear;
                viewport.gameObject.AddComponent<RectMask2D>();
                var scroll = viewport.gameObject.AddComponent<ScrollRect>();
                scroll.horizontal = false;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.viewport = viewport;
                var grid = FounderRect(viewport, "Lot choices", Vector2.zero, new Vector2(0, ((choices.Count + 2) / 3) * 272));
                grid.anchorMin = new Vector2(0, 1);
                grid.anchorMax = Vector2.one;
                grid.pivot = new Vector2(.5f, 1);
                scroll.content = grid;

                for (int i = 0; i < choices.Count; i++)
                {
                    var (title, lot, artwork) = choices[i];
                    var card = FounderRect(grid, title, new Vector2(-296 + i % 3 * 296, -130 - i / 3 * 272),
                        new Vector2(282, 256));
                    card.anchorMin = card.anchorMax = new Vector2(.5f, 1);
                    StyleTownPanel(card, new Color(.06f, .11f, .13f));
                    var outline = card.GetComponent<Outline>();
                    outline.effectColor = brass;
                    var background = card.GetComponent<Image>();
                    var button = card.gameObject.AddComponent<IndustryLotButton>();
                    button.targetGraphic = background;
                    button.onClick.AddListener(() =>
                    {
                        if (lot == null || !CanBuildTown || !TownConstructionVisible || IsSaving || PlacementBlock(lot) != null) return;
                        CloseTransportModal();
                        BeginLotPlacement(lot);
                    });
                    DrawLot(card, lot, artwork, Vector2.up * 20f, new Vector2(258, 178));
                    var name = Label(card, title, new Vector2(0, -97), new Vector2(254, 49), 23);
                    name.alignment = TextAnchor.MiddleCenter;
                    button.ShowDetails = () => ShowDetails(title, lot, artwork);
                }
            }

            void ShowTimelineColumn(string title, string summary, string[] suggestions, float x)
            {
                Label(results, title, new Vector2(x, 275), new Vector2(595, 55), 32);
                Label(results, summary ?? "", new Vector2(x, 160), new Vector2(595, 155), 25)
                    .alignment = TextAnchor.UpperLeft;
                if (suggestions == null || suggestions.Length == 0) return;
                Label(results, "Suggested priorities", new Vector2(x, 35), new Vector2(595, 40), 24);
                Label(results, string.Join("\n\n", suggestions.Select(item => "•  " + item)),
                    new Vector2(x, -122), new Vector2(595, 250), 22).alignment = TextAnchor.UpperLeft;
            }

            string PlacementBlock(LotDefinition lot)
            {
                if (lot.objects.Count == 0) return "This lot has no placed assets.";
                var firstYear = LotClassification.Era(lot)?.FirstYear ?? lot.firstEraYear;
                return firstYear > TownYear(SelectedDistrict) ? $"Available from {firstYear}." : LotAssetProblem(lot, catalog);
            }

            void ShowDetails(string title, LotDefinition lot, int artwork)
            {
                Replace(ref details, "Industry details", new Vector2(648, -28), new Vector2(406, 722));
                Label(details, title, new Vector2(0, 326), new Vector2(378, 54), 29);
                DrawLot(details, lot, artwork, new Vector2(0, 215), new Vector2(372, 174));
                var facility = artwork >= 0
                    ? Array.Find(featured, item => item.industryArtwork == artwork + 1)
                    : lot == null ? null : IndustryAssignment(lot);
                var messages = facility?.industryMessages;
                int firstYear = eras[era].Periods[period].FirstYear;
                var message = messages == null ? null : Array.Find(messages, entry => entry != null && entry.firstYear == firstYear);
                if (!string.IsNullOrEmpty(message?.text))
                    Label(details, message.text, new Vector2(0, 84), new Vector2(378, 76), 21);
                if (lot == null)
                {
                    Label(details, "Save an Industrial lot for this facility in the Lot Editor to place it.",
                        new Vector2(0, -5), new Vector2(378, 90), 21);
                    return;
                }
                Label(details, $"{lot.width} × {lot.length} tiles  •  Cost {lot.cost:N0}",
                    new Vector2(0, 37), new Vector2(378, 36), 21);
                Label(details, "Requirements", new Vector2(0, -19), new Vector2(378, 36), 24);
                var requirements = lot.requiredResources.Select(FounderResourceName).ToList();
                if (lot.requiresKiln) requirements.Add("Kiln access");
                if (lot.IsWaterfront) requirements.Add("Water access");
                Label(details, requirements.Count == 0 ? "None" : string.Join(", ", requirements),
                    new Vector2(0, -83), new Vector2(378, 80), 19);
                Label(details, "Lot effects", new Vector2(0, -147), new Vector2(378, 36), 24);
                Label(details, FounderBonus(lot.capitalBonus), new Vector2(0, -197), new Vector2(378, 65), 19);
                Label(details, PlacementBlock(lot) ?? "Click the lot to place it.", new Vector2(0, -305),
                    new Vector2(378, 68), 19);
            }

            void DrawLot(Transform parent, LotDefinition lot, int illustration, Vector2 position, Vector2 size)
            {
                if (buildings && illustration >= 0 && illustration < 6)
                {
                    var crop = new Rect(illustration % 3 * (buildings.width / 3f),
                        illustration / 3 * (buildings.height / 2f), buildings.width / 3f, buildings.height / 2f);
                    TownArtwork(parent, "Industry illustration", buildings, crop, position,
                        new Vector2(size.y, size.y));
                    return;
                }
                if (lot == null) return;
                var preview = FounderRect(parent, "Lot preview", position, new Vector2(size.y, size.y))
                    .gameObject.AddComponent<RawImage>();
                preview.texture = industryThumbnails.GetLot(lot, catalog);
                preview.enabled = preview.texture;
                preview.raycastTarget = false;
            }

            void Replace(ref RectTransform pane, string title, Vector2 position, Vector2 size)
            {
                if (pane)
                {
                    pane.gameObject.SetActive(false);
                    Destroy(pane.gameObject);
                }
                pane = FounderRect(panel, title, position, size);
            }

            Text Label(Transform parent, string caption, Vector2 position, Vector2 size, int fontSize)
            {
                var label = FounderText(parent, caption, position, size, fontSize);
                label.color = TownGold;
                label.alignment = TextAnchor.MiddleLeft;
                label.raycastTarget = false;
                return label;
            }

            void Rule(Vector2 position, Vector2 size)
            {
                var line = FounderRect(panel, "Divider", position, size).gameObject.AddComponent<Image>();
                line.color = brass;
                line.raycastTarget = false;
            }
        }

        private sealed class IndustryLotButton : Button
        {
            public Action ShowDetails;

            public override void OnPointerEnter(PointerEventData eventData)
            {
                base.OnPointerEnter(eventData);
                ShowDetails?.Invoke();
            }

            public override void OnSelect(BaseEventData eventData)
            {
                base.OnSelect(eventData);
                ShowDetails?.Invoke();
            }
        }

        private static LotContentCatalog.Subcategory IndustryAssignment(LotDefinition lot)
        {
            var choices = LotClassification.ForCategory("industrial");
            var assigned = Array.Find(choices, item => string.Equals(lot.subcategory, item.id, StringComparison.OrdinalIgnoreCase));
            if (assigned != null && !string.IsNullOrEmpty(assigned.industryCardTitle)) return assigned;

            var byAsset = Array.Find(choices, item => item.industryAssetIds != null &&
                lot.objects.Any(obj => item.industryAssetIds.Contains(obj.assetId)));
            if (byAsset != null) return byAsset;

            var words = System.Text.RegularExpressions.Regex.Replace(lot.name.ToLowerInvariant(), @"[^a-z0-9]+", " ");
            words = " " + words.Trim() + " ";
            var byName = Array.Find(choices, item => item.industryLotNames != null &&
                item.industryLotNames.Any(name => words.Contains(" " + name.ToLowerInvariant() + " ", StringComparison.Ordinal)));
            return byName ?? assigned;
        }

        private static string IndustryTab(LotDefinition lot)
        {
            var assignment = IndustryAssignment(lot);
            return assignment?.industryTab ?? (assignment?.industryTabOrder > 0 ? assignment.id : "specialized");
        }

        private void OpenStreetscape()
        {
            if (!TownConstructionVisible || !CanBuildTown || IsSaving) return;
            CancelRoadPlacement();
            CancelLotPlacement();
            CloseTownSection();
            lastRepaveSurface = null;
            MarkTownButton(townCategories[7], true);

            transportModal = FounderRect(townToolbar.parent, "Streetscape modal", Vector2.zero, Vector2.zero);
            transportModal.anchorMin = Vector2.zero;
            transportModal.anchorMax = Vector2.one;
            transportModal.offsetMin = transportModal.offsetMax = Vector2.zero;
            transportModal.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .58f);
            var panel = FounderRect(transportModal, "Streetscape", Vector2.zero, new Vector2(780, 450));
            StyleTownPanel(panel, TownPanelColor);
            FounderText(panel, "Streetscape", new Vector2(0, 190), new Vector2(520, 34), 27).color = TownGold;
            TownButton(panel, lotFont, "×", 730, -12, 36, CloseTransportModal).Select();

            var catalog = LotAssetCatalog.Load();
            var assets = catalog ? catalog.entries.Where(asset => asset != null && asset.prefab).ToArray()
                : Array.Empty<LotAssetCatalogEntry>();
            var viewport = FounderRect(panel, "Streetscape assets", new Vector2(92, -40), new Vector2(550, 310));
            viewport.gameObject.AddComponent<Image>().color = new Color(.05f, .07f, .09f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = viewport;
            var list = FounderRect(viewport, "Assets", Vector2.zero, Vector2.zero);
            list.anchorMin = new Vector2(0, 1);
            list.anchorMax = Vector2.one;
            list.pivot = new Vector2(.5f, 1);
            scroll.content = list;

            var lamps = assets.Where(asset => asset.category == LotAssetCatalog.StreetFurniture && asset.group == "Lighting");
            var fences = assets.Where(asset => asset.category == "Fences");
            bool IsWall(LotAssetCatalogEntry asset) => asset.category == "Walls" ||
                asset.label?.IndexOf("wall", StringComparison.OrdinalIgnoreCase) >= 0;
            var walls = assets.Where(asset => IsWall(asset) &&
                (asset.category == "Walls" || asset.category == "Fences" || asset.category == LotAssetCatalog.StreetFurniture));
            var outdoor = assets.Where(asset => asset.category == LotAssetCatalog.StreetFurniture &&
                asset.group != "Lighting" && !IsWall(asset) &&
                EraAppearance.Allows(new LotDefinition(), asset, DistrictAppearanceEra(SelectedDistrict)));
            int districtYear = TownYear(SelectedDistrict);
            string districtEra = DistrictAppearanceEra(SelectedDistrict);
            var categories = new[] { ("Street Lamps", lamps), ("Fences", fences.Where(asset => !IsWall(asset))),
                ("Walls", walls), ("Outdoor Details", outdoor), ("Vehicles",
                    assets.Where(asset => asset.category == LotAssetCatalog.Transport &&
                        VehicleProfile.Find(asset.id) is { } profile &&
                        profile.AvailableIn(districtEra))) };
            var buttons = new Button[categories.Length];
            int currentCategory = 0;
            InputField search = null;
            for (int index = 0; index < categories.Length; index++)
            {
                int category = index;
                buttons[index] = TownButton(panel, lotFont, categories[index].Item1, 18, -70 - index * 48, 174,
                    () => ShowCategory(category));
            }
            search = DefaultControls.CreateInputField(new DefaultControls.Resources()).GetComponent<InputField>();
            search.transform.SetParent(panel, false);
            Position((RectTransform)search.transform, new Vector2(207, -55), new Vector2(550, 34));
            search.textComponent.font = lotFont;
            if (search.placeholder is Text placeholder) { placeholder.font = lotFont; placeholder.text = "Search Streetscape"; }
            search.onValueChanged.AddListener(_ => ShowCategory(currentCategory));
            ShowCategory(0);

            void ShowCategory(int category)
            {
                currentCategory = category;
                for (int i = 0; i < buttons.Length; i++) MarkTownButton(buttons[i], i == category);
                foreach (Transform child in list)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
                string query = search.text.Trim();
                var entries = (query.Length == 0 ? categories[category].Item2 : categories.SelectMany(group => group.Item2).Distinct())
                    .Where(asset => asset.label.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(asset => asset.label).ToArray();
                var recent = StreetscapeHistory().Select(id => entries.FirstOrDefault(asset => asset.id == id))
                    .Where(asset => asset != null).Take(4).ToArray();
                int row = 0;
                if (recent.Length > 0)
                {
                    Heading("Recently used");
                    foreach (var asset in recent) AddAsset(asset);
                    if (entries.Length > recent.Length) Heading("More assets");
                }
                foreach (var asset in entries.Except(recent)) AddAsset(asset);
                if (entries.Length == 0) Heading("No matching assets.");
                list.sizeDelta = new Vector2(0, Mathf.Max(310, row * 42 + 12));
                list.anchoredPosition = Vector2.zero;

                void AddAsset(LotAssetCatalogEntry asset) =>
                    TownButton(list, lotFont, asset.label, 14, -8 - row++ * 42, 522, () => ArmStreetscape(asset));

                void Heading(string title)
                {
                    var label = FounderText(list, title, Vector2.zero, new Vector2(522, 34), 17);
                    Position(label.rectTransform, new Vector2(14, -8 - row++ * 42), new Vector2(522, 34));
                    label.alignment = TextAnchor.MiddleLeft;
                }
            }
        }

        private void OpenTransportModal(string category)
        {
            if (transportModal) return;
            transportModal = FounderRect(townToolbar.parent, "Transport modal", Vector2.zero, Vector2.zero);
            transportModal.anchorMin = Vector2.zero;
            transportModal.anchorMax = Vector2.one;
            transportModal.offsetMin = transportModal.offsetMax = Vector2.zero;
            transportModal.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .58f);

            if (category == "Bridges")
            {
                BuildBridgesPopup();
                return;
            }
            if (category == "Roads")
            {
                BuildRoadsPopup();
                return;
            }

            var panel = FounderRect(transportModal, category + " construction", Vector2.zero, new Vector2(440, 170));
            StyleTownPanel(panel, TownPanelColor);
            FounderText(panel, category, new Vector2(0, 49), new Vector2(340, 36), 25).color = TownGold;
            FounderText(panel, "Construction tools are not available yet.", Vector2.zero, new Vector2(400, 38), 17);
            var close = TownButton(panel, lotFont, "Close", 168, -119, 104, CloseTransportModal);
            close.navigation = new Navigation { mode = Navigation.Mode.None };
            close.Select();
        }

        private void CloseTransportModal()
        {
            if (civicThumbnails != null)
            {
                civicThumbnails.Dispose();
                civicThumbnails = null;
                if (civicCamera) civicCamera.enabled = civicCameraWasEnabled;
                civicCamera = null;
                MarkTownButton(townCategories[2], false);
                if (townSection == TownSection.CivicLots) townSection = TownSection.None;
            }
            industryThumbnails?.Dispose();
            industryThumbnails = null;
            repaveTarget = null;
            MarkTownButton(townCategories[6], false);
            MarkTownButton(townCategories[7], false);
            if (!transportModal) return;
            transportModal.gameObject.SetActive(false);
            Destroy(transportModal.gameObject);
            transportModal = null;
        }

        private void ShowTownLots(bool parks, bool agriculture = false)
        {
            if (parks && !agriculture)
            {
                ParkTreeCard("Add Trees", "Scatter random trees", -266, () => ArmParkTreeBrush(false));
                ParkTreeCard("Place a Tree", "Choose a tree to plant", 0, OpenParkTreePicker, single: true);
                ParkTreeCard("Remove Trees", "Brush away individual trees", 266, () => ArmParkTreeBrush(true), remove: true);
            }
            if (parks && !agriculture)
            {
                TownButton(townPanel, lotFont, "Paths", 440, -9, 145, OpenParkPaths);
                var paint = TownButton(townPanel, lotFont, "▧ Texturize", 610, -9, 145, OpenTexturize);
            }
            if (parks)
                TownButton(townPanel, lotFont, agriculture ? "‹ Parks" : "Agriculture", 18, -9, 146, () =>
                {
                    var current = townPanel;
                    foreach (Transform child in current)
                    {
                        if (child.name == "Saved town lots" || child.GetSiblingIndex() >= 2)
                        {
                            child.gameObject.SetActive(false);
                            Destroy(child.gameObject);
                        }
                    }
                    ShowTownLots(true, !agriculture);
                });
            var catalog = LotAssetCatalog.Load();
            LotDefinition[] lots;
            try
            {
                lots = LotLibrary.Local.List().Select(entry => entry.Lot).Where(lot =>
                    LotClassification.FindCategory(lot.category)?.id == "civics" &&
                    !IsFoundersLot(lot) && (agriculture
                        ? lot.subcategory == "agriculture"
                        : lot.subcategory != "agriculture" && parks == (lot.subcategory == "parks" || lot.subcategory == "Parks"))).ToArray();
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                FounderText(townPanel, "Could not read the saved lot library.", Vector2.zero, new Vector2(480, 90), 17);
                return;
            }
            if (agriculture && catalog)
                lots = new[] { "corn-garden", "wheat-garden" }.Select(catalog.Find)
                    .Where(garden => garden != null && garden.prefab)
                    .Select(BuildGardenLot).Concat(lots).ToArray();

            static LotDefinition BuildGardenLot(LotAssetCatalogEntry garden)
            {
                var builtIn = new LotDefinition
                {
                    name = garden.label, category = "civics", subcategory = "agriculture",
                    width = 1, length = 1
                };
                builtIn.objects.Add(new LotObjectDefinition { assetId = garden.id });
                return builtIn;
            }
            if (lots.Length == 0)
            {
                FounderText(townPanel, agriculture ? "Save a Civics → Agriculture lot in the Lot Editor." :
                    parks ? "Save a Civics → Parks lot in the Lot Editor." :
                    "Save a playable civic lot in the Lot Editor.", Vector2.zero, new Vector2(480, 90), 17);
                return;
            }

            var window = FounderRect(townPanel, "Saved town lots", new Vector2(0, parks && !agriculture ? -74 : -24), new Vector2(524, 205));
            window.gameObject.AddComponent<Image>().color = new Color(.05f, .07f, .09f);
            window.gameObject.AddComponent<RectMask2D>();
            var scroll = window.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = window;
            var content = FounderRect(window, "Lot choices", Vector2.zero, new Vector2(0, lots.Length * 48));
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            scroll.content = content;

            for (int i = 0; i < lots.Length; i++)
            {
                var lot = lots[i];
                var button = TownButton(content, lotFont, $"{lot.name}  ·  {lot.width} × {lot.length}", 7, -6 - i * 48, 510, () =>
                {
                    if (!CanBuildTown || !TownConstructionVisible || IsSaving) return;
                    CloseTownSection();
                    BeginLotPlacement(lot);
                });
                button.interactable = lot.objects.Count > 0 && LotAssetProblem(lot, catalog) == null;
            }
        }
    }
}
