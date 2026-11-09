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
        private enum TownSection { None, Roads, Zones, CivicLots, Utilities, Parks, TownInfo }

        private RectTransform townModeSwitch, townToolbar, townPanel;
        private Button natureModeButton, townModeButton;
        private readonly Button[] townCategories = new Button[6];
        private TownSection townSection;

        internal bool TownConstructionVisible { get; private set; }
        private bool CanBuildTown => SelectedDistrict >= 0 && !DistrictLoading &&
            Capital is { awaitingFoundersBuilding: false } && PlacedLots.Exists(lot => IsFoundersLot(lot.definition)) &&
            Towns.Any(town => ActiveDistrictMembers.Contains(town.DistrictIndex(GridSide)));

        private void BuildTownConstructionMenu(Transform hud, Font font)
        {
            townModeSwitch = BottomPanel(hud, "District tool modes", 6, 222, 38);
            natureModeButton = TownButton(townModeSwitch, font, "Nature", 3, 0, 104, () => SetTownConstruction(false));
            townModeButton = TownButton(townModeSwitch, font, "Town", 115, 0, 104, () => SetTownConstruction(true));

            townToolbar = BottomPanel(hud, "Town construction toolbar", 61, 712, 46);
            string[] labels = { "Roads & Transit", "Zones", "Civic Lots", "Utilities", "Parks", "Town Info" };
            for (int i = 0; i < labels.Length; i++)
            {
                var section = (TownSection)(i + 1);
                townCategories[i] = TownButton(townToolbar, font, labels[i], 5 + i * 118, -5, 112,
                    () => ShowTownSection(section));
            }
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
            var button = label.GetComponentInParent<Button>();
            ((RectTransform)button.transform).anchoredPosition = new Vector2(x, y);
            return button;
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
            CancelLotPlacement();
            SelectDistrictLot(null);
            CloseTownSection();
            TownConstructionVisible = town;
            GetComponent<NatureToolbar>()?.Dismiss();
            RefreshTownConstruction();
        }

        private void CloseTownSection()
        {
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

        private static void MarkTownButton(Button button, bool active)
        {
            if (button) button.image.color = active ? new Color(.39f, .29f, .15f, .98f) : new Color(.12f, .17f, .21f, .97f);
        }

        private void ShowTownSection(TownSection section)
        {
            if (!TownConstructionVisible || !CanBuildTown || IsSaving) return;
            bool toggleOff = townSection == section;
            CloseTownSection();
            if (toggleOff) return;
            CancelLotPlacement();
            townSection = section;
            var category = townCategories[(int)section - 1];
            MarkTownButton(category, true);

            townPanel = FounderRect(townToolbar, "Town tool panel", new Vector2(0, 52), new Vector2(560, 280));
            townPanel.anchorMin = townPanel.anchorMax = townPanel.pivot = new Vector2(.5f, 0);
            townPanel.gameObject.AddComponent<Image>().color = new Color(.07f, .10f, .14f, .98f);
            FounderText(townPanel, category.name, new Vector2(-32, 110), new Vector2(440, 34), 23);
            TownButton(townPanel, lotFont, "Close", 478, -12, 68, CloseTownSection);

            switch (section)
            {
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
                    string tools = section switch
                    {
                        TownSection.Roads => "Streets, roads, rail and transit",
                        _ => "Water, sanitation and power"
                    };
                    FounderText(townPanel, tools + "\n\nConstruction tools are not available yet.",
                        Vector2.up * 10f, new Vector2(490, 160), 18);
                    break;
            }
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
