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
        private readonly Button[] townCategories = new Button[6];
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

            townToolbar = BottomPanel(hud, "Town construction toolbar", 63, 886, 82);
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
            TownConstructionVisible = town;
            if (!town) SetRoadPaths(false);
            GetComponent<NatureToolbar>()?.Dismiss();
            RefreshTownConstruction();
        }

        private void CloseTownSection()
        {
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
                _ => 560
            };
            townPanel = FounderRect(townToolbar, "Town tool panel", new Vector2(0, 92), new Vector2(width, 276));
            townPanel.anchorMin = townPanel.anchorMax = townPanel.pivot = new Vector2(.5f, 0);
            StyleTownPanel(townPanel, TownPanelColor);
            if (section != TownSection.Transport)
                FounderText(townPanel, category.name, new Vector2(0, 113), new Vector2(440, 34), 25).color = TownGold;
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
            if (!transportModal) return;
            transportModal.gameObject.SetActive(false);
            Destroy(transportModal.gameObject);
            transportModal = null;
        }

        private void ShowTownLots(bool parks)
        {
            LotDefinition[] lots;
            try
            {
                lots = LotLibrary.Local.List().Select(entry => entry.Lot).Where(lot =>
                    LotClassification.FindCategory(lot.category)?.id == "civics" &&
                    !IsFoundersLot(lot) && parks == (lot.subcategory == "parks" || lot.subcategory == "Parks")).ToArray();
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            {
                FounderText(townPanel, "Could not read the saved lot library.", Vector2.zero, new Vector2(480, 90), 17);
                return;
            }
            if (lots.Length == 0)
            {
                FounderText(townPanel, parks ? "Save a Civics → Parks lot in the Lot Editor." :
                    "Save a playable civic lot in the Lot Editor.", Vector2.zero, new Vector2(480, 90), 17);
                return;
            }

            var window = FounderRect(townPanel, "Saved town lots", new Vector2(0, -24), new Vector2(524, 205));
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

            var catalog = LotAssetCatalog.Load();
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
