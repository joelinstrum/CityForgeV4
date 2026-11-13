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
        private enum TownSection { None, Transport, Zones, CivicLots, Utilities, Parks, TownInfo }

        private RectTransform townModeSwitch, townToolbar, townPanel;
        private Button natureModeButton, townModeButton;
        private RectTransform transportModal;
        private readonly Button[] townCategories = new Button[7];
        private TownSection townSection;
        private Texture2D darkTownButtons, goldTownButtons, transportButtonArtwork;
        private Material townButtonArtwork;
        private static readonly Color TownPanelColor = new(.09f, .10f, .10f, .98f);
        private static readonly Color TownGold = new(.98f, .86f, .63f);

        internal bool TownConstructionVisible { get; private set; }
        private bool CanBuildTown => SelectedDistrict >= 0 && !DistrictLoading &&
            Capital is { awaitingFoundersBuilding: false } && PlacedLots.Exists(lot => IsFoundersLot(lot.definition)) &&
            Towns.Any(town => ActiveDistrictMembers.Contains(town.DistrictIndex(GridSide)));

        private void BuildTownConstructionMenu(Transform hud, Font font)
        {
            darkTownButtons = Resources.Load<Texture2D>("UI/TownConstruction/Dark");
            goldTownButtons = Resources.Load<Texture2D>("UI/TownConstruction/Gold");
            transportButtonArtwork = Resources.Load<Texture2D>("UI/TownConstruction/Transport");
            var artworkShader = Resources.Load<Shader>("UI/TownButtonArtwork");
            if (artworkShader && ((darkTownButtons && goldTownButtons) || transportButtonArtwork))
                townButtonArtwork = Own(new Material(artworkShader));
            townModeSwitch = BottomPanel(hud, "District tool modes", 10, 310, 44);
            natureModeButton = TownButton(townModeSwitch, font, "Nature", 0, 0, 154, () => SetTownConstruction(false));
            townModeButton = TownButton(townModeSwitch, font, "Town", 156, 0, 154, () => SetTownConstruction(true));
            townModeButton.GetComponentInChildren<Text>(includeInactive: true).text = "City";
            SkinTownButton(natureModeButton, new Rect(740, 462, 347, 112), 44);
            SkinTownButton(townModeButton, new Rect(1087, 462, 349, 112), 44);

            townToolbar = BottomPanel(hud, "Town construction toolbar", 63, 1034, 82);
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
            townCategories[6] = streetscape;
            ((RectTransform)streetscape.transform).sizeDelta = new Vector2(146, 82);
            var caption = streetscape.GetComponentInChildren<Text>();
            caption.rectTransform.offsetMax = new Vector2(0, -49);
            caption.color = TownGold;
            LotEditorPanel.AddGlyph(streetscape.transform, LotGlyph.StreetFurniture, 43, 7, 43);
            LotEditorPanel.AddGlyph(streetscape.transform, LotGlyph.Fence, 76, 20, 30);
            BuildDetonateButton(font);
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

        private void SkinTownButton(Button button, Rect crop, float height, Texture2D artwork)
        {
            var rect = (RectTransform)button.transform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            if (!artwork) return;

            button.image.color = Color.clear;
            button.GetComponentInChildren<Text>(includeInactive: true).enabled = false;
            var face = TownArtwork(rect, "Button artwork", artwork, crop, Vector2.zero, rect.sizeDelta);
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

        private void RefreshTownConstruction()
        {
            if (!townModeSwitch) return;
            bool available = CanBuildTown;
            if (!available && TownConstructionVisible)
            {
                TownConstructionVisible = false;
                CloseTownSection();
            }
            townModeSwitch.gameObject.SetActive(available);
            townToolbar.gameObject.SetActive(available && TownConstructionVisible);
            MarkTownButton(natureModeButton, !TownConstructionVisible);
            MarkTownButton(townModeButton, TownConstructionVisible);
        }

        private void SetTownConstruction(bool town)
        {
            if (TownConstructionVisible == town || (town && !CanBuildTown) || IsSaving) return;
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
            CancelParkTreeBrush();
            CancelStreetscape();
            SetDetonate(false);
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
                var texture = active ? goldTownButtons : darkTownButtons;
                if (face.texture != texture) face.texture = texture;
                return;
            }
            button.image.color = active ? new Color(.48f, .33f, .15f) : new Color(.16f, .17f, .17f);
        }

        private void ShowTownSection(TownSection section)
        {
            if (!TownConstructionVisible || !CanBuildTown || IsSaving) return;
            CancelRoadPlacement();
            if (section != TownSection.Transport) lastRepaveSurface = null;
            if (section == TownSection.Zones && townSection == section && townPanel && !townPanel.gameObject.activeSelf)
            {
                CancelZoneDrag();
                townPanel.gameObject.SetActive(true);
                return;
            }
            bool toggleOff = townSection == section;
            CloseTownSection();
            if (toggleOff) return;
            CancelLotPlacement();
            townSection = section;
            var category = townCategories[(int)section - 1];
            MarkTownButton(category, true);

            float width = section switch
            {
                TownSection.Transport => 1120,
                TownSection.Zones => 850,
                TownSection.Parks => 820,
                _ => 560
            };
            townPanel = FounderRect(townToolbar, "Town tool panel", new Vector2(0, 92), new Vector2(width, section == TownSection.Parks ? 376 : 276));
            townPanel.anchorMin = townPanel.anchorMax = townPanel.pivot = new Vector2(.5f, 0);
            StyleTownPanel(townPanel, TownPanelColor);
            if (section != TownSection.Transport)
                FounderText(townPanel, category.name, new Vector2(0, section == TownSection.Parks ? 163 : 113), new Vector2(440, 34), 25).color = TownGold;
            TownButton(townPanel, lotFont, "×", (int)width - 47, -9, 36, CloseTownSection);

            switch (section)
            {
                case TownSection.Transport:
                    ShowTransport();
                    break;
                case TownSection.Zones:
                    ShowZoning();
                    break;
                case TownSection.CivicLots:
                case TownSection.Parks:
                    ShowTownLots(section == TownSection.Parks);
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

        private void OpenStreetscape()
        {
            if (!TownConstructionVisible || !CanBuildTown || IsSaving) return;
            CancelRoadPlacement();
            CancelLotPlacement();
            CloseTownSection();
            lastRepaveSurface = null;
            MarkTownButton(townCategories[6], true);

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
                asset.group != "Lighting" && !IsWall(asset));
            var categories = new[] { ("Street Lamps", lamps), ("Fences", fences.Where(asset => !IsWall(asset))),
                ("Walls", walls), ("Outdoor Details", outdoor) };
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
            repaveTarget = null;
            MarkTownButton(townCategories[6], false);
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
