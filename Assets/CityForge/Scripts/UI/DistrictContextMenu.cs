using System;
using System.Linq;
using CityForge.World;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityForge.UI
{
    public sealed partial class RegionTownTools
    {
        private Button renameAction, capitalAction, coastlineAction, beachChoice, harborChoice;
        private RectTransform districtContext, districtPanel;
        private Text contextTitle, contextStatus;
        private Dropdown districtTownChoice;
        private InputField districtRename;
        private Button applyRename;
        private RegionTown[] contextTowns = Array.Empty<RegionTown>();
        private int contextDistrict = -1;
        private Vector2? contextPress;
        private bool coastlineChoices, confirmingDistrictReset;
        private Button resetDistrictAction;

        private bool DistrictContextOpen => districtContext && districtContext.gameObject.activeSelf;
        private RegionTown ContextTown => contextTowns.Length == 0 ? null : contextTowns[districtTownChoice.value];

        private void BuildDistrictContext()
        {
            districtContext = Rect(hud, "District context menu", Vector2.zero, Vector2.zero);
            districtContext.anchorMin = Vector2.zero;
            districtContext.anchorMax = Vector2.one;
            districtContext.offsetMin = districtContext.offsetMax = Vector2.zero;
            var backdrop = Rect(districtContext, "Dismiss district context", Vector2.zero, Vector2.zero);
            backdrop.anchorMin = Vector2.zero;
            backdrop.anchorMax = Vector2.one;
            backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            var shade = backdrop.gameObject.AddComponent<Image>();
            shade.color = new Color(0, 0, 0, .3f);
            var dismiss = backdrop.gameObject.AddComponent<Button>();
            dismiss.targetGraphic = shade;
            dismiss.transition = Selectable.Transition.None;
            dismiss.onClick.AddListener(CloseDistrictContext);
            districtPanel = Rect(districtContext, "District actions", Vector2.zero, new Vector2(510, 366));
            var background = districtPanel.gameObject.AddComponent<Image>();
            background.color = Panel;
            RegionBrassArtwork.SetImage(background, "panel", sliced: true);
            contextTitle = Label(districtPanel, "Heading", "", new Vector2(-40, 156), new Vector2(386, 34), 26);
            var close = MakeButton(districtPanel, "Close", new Vector2(218, 147), new Vector2(35, 35), CloseDistrictContext);
            RegionBrassArtwork.SetButton(close, "close");

            districtTownChoice = DefaultControls.CreateDropdown(new DefaultControls.Resources()).GetComponent<Dropdown>();
            districtTownChoice.name = "District town";
            var choiceRect = (RectTransform)districtTownChoice.transform;
            choiceRect.SetParent(districtPanel, false);
            choiceRect.anchoredPosition = new Vector2(0, 81);
            choiceRect.sizeDelta = new Vector2(460, 34);
            districtTownChoice.captionText.font = districtTownChoice.itemText.font = font;
            districtTownChoice.captionText.supportRichText = districtTownChoice.itemText.supportRichText = false;
            if (RegionBrassArtwork.SetImage(districtTownChoice.GetComponent<Image>(), "button"))
            {
                districtTownChoice.captionText.color = Gold;
                districtTownChoice.captionText.rectTransform.offsetMin = new Vector2(16f, 2f);
                var arrow = choiceRect.Find("Arrow");
                if (arrow && arrow.TryGetComponent<Image>(out var arrowImage)) arrowImage.color = Gold;
            }
            districtTownChoice.onValueChanged.AddListener(_ => RefreshDistrictContext());

            renameAction = MakeButton(districtPanel, "Change name of town", new Vector2(-116, 38), new Vector2(224, 36), () =>
            {
                districtRename.SetTextWithoutNotify(ContextTown.name);
                districtRename.gameObject.SetActive(true);
                applyRename.gameObject.SetActive(true);
                districtRename.Select();
                districtRename.ActivateInputField();
            });
            capitalAction = MakeButton(districtPanel, "Move capital to this town", new Vector2(116, 38), new Vector2(224, 36), () =>
            {
                var town = ContextTown;
                var problem = view.MoveCapitalToDistrictTown(contextDistrict, town);
                if (problem != null) { contextStatus.text = problem; return; }
                CloseDistrictContext();
            });

            coastlineAction = MakeButton(districtPanel, "Design Coastline", new Vector2(0, -9), new Vector2(460, 36), () =>
            {
                coastlineChoices = !coastlineChoices;
                RefreshDistrictContext();
            });
            beachChoice = MakeButton(districtPanel, "Beach", new Vector2(-116, -52), new Vector2(224, 36), () => ChooseCoastline(false));
            harborChoice = MakeButton(districtPanel, "Deep Harbor", new Vector2(116, -52), new Vector2(224, 36), () => ChooseCoastline(true));

            districtRename = MakeInput(districtPanel, "New town name", new Vector2(-74, -96), new Vector2(312, 34));
            districtRename.characterLimit = RegionTown.NameLimit;
            applyRename = MakeButton(districtPanel, "Save name", new Vector2(160, -96), new Vector2(140, 34), SubmitDistrictRename);
            resetDistrictAction = MakeButton(districtPanel, "Delete District", new Vector2(0, -137),
                new Vector2(460, 35), ConfirmDistrictReset);
            contextStatus = Label(districtPanel, "Status", "", new Vector2(0, -167), new Vector2(465, 29), 15);
            foreach (var button in new[] { renameAction, capitalAction, coastlineAction, beachChoice, harborChoice, applyRename, resetDistrictAction })
                RegionBrassArtwork.SetButton(button, "button", bakedCaption: false);
            districtContext.gameObject.SetActive(false);
        }

        private void ConfirmDistrictReset()
        {
            if (!confirmingDistrictReset)
            {
                confirmingDistrictReset = true;
                resetDistrictAction.GetComponentInChildren<Text>().text = "Confirm reset";
                bool combined = view.DistrictMembers(contextDistrict).Length > 1;
                contextStatus.text = combined ? "Reset all joined districts and dissolve the group?"
                    : "Clear player changes and restore original nature?";
                return;
            }

            var problem = view.ResetDistrict(contextDistrict);
            contextTowns = view.Towns.Where(town => town.DistrictIndex(view.GridSide) == contextDistrict).ToArray();
            RefreshDistrictContext();
            contextStatus.text = problem ?? "District reset. Save the region to keep it.";
        }

        private void SubmitDistrictRename()
        {
            var town = ContextTown;
            var problem = view.RenameDistrictTown(contextDistrict, town, districtRename.text);
            if (problem != null) { contextStatus.text = problem; return; }
            RefreshDistrictContext();
            contextStatus.text = "Town renamed. Save the region to keep it.";
        }

        private void ChooseCoastline(bool deep)
        {
            var problem = view.SetCoastlineDesign(contextDistrict, deep);
            if (problem != null) { contextStatus.text = problem; return; }
            RefreshDistrictContext();
            contextStatus.text = deep ? "Deep Harbor will appear when you open this district." :
                "Beach will appear when you open this district.";
        }

        private void RefreshDistrictContext()
        {
            confirmingDistrictReset = false;
            resetDistrictAction.GetComponentInChildren<Text>().text = "Delete District";
            int chosen = districtTownChoice.value;
            districtTownChoice.ClearOptions();
            districtTownChoice.AddOptions(contextTowns.Select(town => town.name + (town.capital ? " (Capital)" : "")).ToList());
            districtTownChoice.SetValueWithoutNotify(chosen);
            districtTownChoice.RefreshShownValue();
            bool hasTown = ContextTown != null;
            districtTownChoice.gameObject.SetActive(hasTown);
            renameAction.gameObject.SetActive(hasTown);
            capitalAction.gameObject.SetActive(hasTown);
            capitalAction.interactable = hasTown && !ContextTown.capital;
            districtRename.gameObject.SetActive(false);
            applyRename.gameObject.SetActive(false);

            bool coastal = view.CanDesignCoastline(contextDistrict);
            coastlineAction.gameObject.SetActive(coastal);
            beachChoice.gameObject.SetActive(coastal && coastlineChoices);
            harborChoice.gameObject.SetActive(coastal && coastlineChoices);
            bool deep = view.HasDeepHarbor(contextDistrict);
            beachChoice.interactable = deep;
            harborChoice.interactable = !deep;
            contextStatus.text = coastal ? (deep ? "Current coastline: Deep Harbor" : "Current coastline: Beach") :
                !hasTown ? "No town or editable ocean coastline in this district." : "";
        }

        private void OpenDistrictContext(int district, Vector2? screen)
        {
            if (!view.CanOpenDistrictContext || district < 0 || district >= view.DistrictCount || DistrictContextOpen ||
                Placing || dialog.gameObject.activeSelf || manager.gameObject.activeSelf || (combiner && combiner.Active)) return;
            contextDistrict = district;
            contextTowns = view.Towns.Where(town => town.DistrictIndex(view.GridSide) == district).ToArray();
            districtTownChoice.SetValueWithoutNotify(0);
            coastlineChoices = false;
            RefreshDistrictContext();
            if (screen.HasValue && contextTowns.Length > 1)
            {
                int nearest = 0;
                float best = float.PositiveInfinity;
                for (int i = 0; i < contextTowns.Length; i++)
                {
                    var projected = mapCamera.WorldToScreenPoint(view.TownMarkerPoint(contextTowns[i]));
                    if (projected.z <= 0) continue;
                    float distance = ((Vector2)projected - screen.Value).sqrMagnitude;
                    if (distance >= best) continue;
                    best = distance;
                    nearest = i;
                }
                districtTownChoice.SetValueWithoutNotify(nearest);
                RefreshDistrictContext();
            }
            contextTitle.text = $"District {district % view.GridSide + 1}–{district / view.GridSide + 1}";
            districtPanel.anchoredPosition = Vector2.zero;
            if (screen.HasValue && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    districtContext, screen.Value, null, out var point))
            {
                var half = districtPanel.sizeDelta * .5f;
                var area = districtContext.rect;
                districtPanel.anchoredPosition = new Vector2(
                    Mathf.Clamp(point.x, area.xMin + half.x, area.xMax - half.x),
                    Mathf.Clamp(point.y, area.yMin + half.y, area.yMax - half.y));
            }
            GetComponent<NatureToolbar>()?.Dismiss();
            cameraWasEnabled = cameraControls.enabled;
            cameraControls.enabled = false;
            districtContext.SetAsLastSibling();
            districtContext.gameObject.SetActive(true);
        }

        private void CloseDistrictContext()
        {
            if (!DistrictContextOpen) return;
            districtContext.gameObject.SetActive(false);
            contextDistrict = -1;
            contextPress = null;
            cameraControls.enabled = cameraWasEnabled;
        }

        internal bool HandleDistrictContext()
        {
            if (DistrictContextOpen)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) CloseDistrictContext();
                return true;
            }
            if (!view.CanOpenDistrictContext || Placing || dialog.gameObject.activeSelf ||
                manager.gameObject.activeSelf || (combiner && combiner.Active))
            {
                contextPress = null;
                return false;
            }
            var position = (Vector2)Input.mousePosition;
            bool overUi = EventSystem.current && EventSystem.current.IsPointerOverGameObject();
            if (Input.GetMouseButtonDown(1)) contextPress = overUi ? null : position;
            if (contextPress.HasValue && (position - contextPress.Value).sqrMagnitude > 64f) contextPress = null;
            if (Input.GetMouseButtonUp(1))
            {
                bool open = contextPress.HasValue && !overUi;
                contextPress = null;
                if (open)
                {
                    OpenDistrictContext(DistrictAtScreen(position), position);
                    return DistrictContextOpen;
                }
            }
            return false;
        }

        private int DistrictAtScreen(Vector2 point) =>
            view.Relief && view.Relief.TryPick(mapCamera.ScreenPointToRay(point), out int district) ? district : -1;
    }
}
