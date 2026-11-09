using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.CameraSystem;
using CityForge.World;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CityForge.UI
{
    public sealed partial class RegionTownTools : MonoBehaviour
    {
        static readonly Color Gold = new Color(.98f, .87f, .60f);
        static readonly Color Panel = new Color(.10f, .12f, .09f, .98f);

        readonly List<(RectTransform label, Vector3 point)> markers = new();
        PrototypeRegionView view;
        RectTransform hud, markerRoot, dialog, cursor;
        Camera mapCamera;
        CityForgeCameraController cameraControls;
        DistrictCombiner combiner;
        Font font;
        Button addTown, manageTowns, deleteTown;
        RectTransform manager;
        Dropdown townChoices;
        Text manageStatus;
        RegionTown movingTown;
        bool confirmDelete;
        bool labelsVisible = true;
        int displayedDistrict = int.MinValue;
        Text instruction, error, capitalLabel;
        InputField townName, year;
        GameObject autogenerateHint;
        PlaceNameGenerator townNames;
        RegionTown[] displayedTowns;
        RegionReliefMap displayedRelief;
        Vector2 pendingPosition;
        bool cursorWasVisible, cameraWasEnabled;

        public bool Placing { get; private set; }

        public void Initialize(PrototypeRegionView source, Canvas canvas, Font uiFont)
        {
            view = source;
            hud = (RectTransform)canvas.transform;
            font = uiFont;
            mapCamera = view.GetComponentInChildren<Camera>();
            cameraControls = mapCamera.GetComponent<CityForgeCameraController>();
            combiner = view.GetComponent<DistrictCombiner>();

            markerRoot = Rect(hud, "Town markers", Vector2.zero, Vector2.zero);
            var topLeft = new Vector2(0, 1);
            addTown = MakeButton(hud, "Add Town", new Vector2(25, -155), new Vector2(168, 40), BeginPlacement);
            var buttonRect = (RectTransform)addTown.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = topLeft;

            manageTowns = MakeButton(hud, "Manage Towns", new Vector2(201, -155), new Vector2(168, 40), OpenManager);
            var manageRect = (RectTransform)manageTowns.transform;
            manageRect.anchorMin = manageRect.anchorMax = manageRect.pivot = topLeft;

            instruction = Label(hud, "Town placement hint", "", new Vector2(25, -202), new Vector2(370, 35));
            instruction.rectTransform.anchorMin = instruction.rectTransform.anchorMax = instruction.rectTransform.pivot = topLeft;
            instruction.alignment = TextAnchor.UpperLeft;
            cursor = Label(hud, "Town placement cursor", "●", Vector2.zero, new Vector2(30, 30), 24).rectTransform;
            cursor.gameObject.SetActive(false);
            instruction.gameObject.SetActive(false);
            BuildDialog();
            BuildManager();
            BuildDistrictContext();
        }

        void BeginPlacement()
        {
            if (Cancel() || view.SelectedDistrict >= 0 || view.DistrictLoading || view.IsSaving) return;
            if (combiner.Active) combiner.SetActive(false);
            cursorWasVisible = Cursor.visible;
            Placing = true;
            instruction.text = "Click a spot on land · Esc to cancel";
        }

        public bool HandleMapClick(Ray ray)
        {
            if (!Placing) return false;
            if (view.IsSaving || view.DistrictLoading) return true;
            if (!view.Relief.TryPick(ray, out int district, out var hit))
            {
                instruction.text = "Click a spot on land · Esc to cancel";
                return true;
            }
            var position = new Vector2(hit.x, hit.z) / view.MapSide + Vector2.one * .5f;
            if (!view.IsTownLand(position))
            {
                instruction.text = "Choose a spot on land · Esc to cancel";
                return true;
            }

            if (movingTown != null)
            {
                var problem = view.MoveTown(movingTown, position);
                if (problem == null) Cancel();
                else instruction.text = problem + " · Esc to cancel";
                return true;
            }

            Cancel();
            pendingPosition = position;
            townName.text = "";
            year.text = view.TownYear(district).ToString();
            UpdateCapitalLabel();
            error.text = "";
            cameraWasEnabled = cameraControls.enabled;
            cameraControls.enabled = false;
            dialog.SetAsLastSibling();
            dialog.gameObject.SetActive(true);
            townName.Select();
            townName.ActivateInputField();
            return true;
        }

        public bool Cancel()
        {
            if (DistrictContextOpen)
            {
                CloseDistrictContext();
                return true;
            }
            if (Placing)
            {
                Placing = false;
                movingTown = null;
                Cursor.visible = cursorWasVisible;
                cursor.gameObject.SetActive(false);
                instruction.gameObject.SetActive(false);
                return true;
            }
            if (manager && manager.gameObject.activeSelf)
            {
                manager.gameObject.SetActive(false);
                cameraControls.enabled = cameraWasEnabled;
                return true;
            }
            if (dialog && dialog.gameObject.activeSelf)
            {
                autogenerateHint.SetActive(false);
                dialog.gameObject.SetActive(false);
                cameraControls.enabled = cameraWasEnabled;
                return true;
            }
            return false;
        }

        internal void ToggleLabels()
        {
            if (Placing || dialog.gameObject.activeSelf || manager.gameObject.activeSelf) return;
            labelsVisible = !labelsVisible;
        }

        void LateUpdate()
        {
            bool overview = view.SelectedDistrict < 0 && !view.DistrictLoading;
            markerRoot.gameObject.SetActive(labelsVisible && !view.DistrictLoading);
            addTown.gameObject.SetActive(overview);
            addTown.interactable = !view.IsSaving;
            manageTowns.gameObject.SetActive(overview && view.Towns.Length > 0);
            manageTowns.interactable = !view.IsSaving;
            if (!overview || view.IsSaving) Cancel();
            if (!labelsVisible || view.DistrictLoading || !view.Relief || !view.Relief.SurfaceReady) return;
            if (Placing && combiner.Active) Cancel();
            instruction.gameObject.SetActive(Placing);
            bool showCursor = Placing && Application.isFocused &&
                RectTransformUtility.RectangleContainsScreenPoint(hud, Input.mousePosition);
            cursor.gameObject.SetActive(showCursor);
            if (Placing) Cursor.visible = !showCursor && cursorWasVisible;
            if (showCursor && RectTransformUtility.ScreenPointToLocalPointInRectangle(hud, Input.mousePosition, null, out var local))
                cursor.anchoredPosition = local;

            if (displayedTowns != view.Towns || displayedRelief != view.Relief || displayedDistrict != view.SelectedDistrict)
                RebuildMarkers();
            foreach (var marker in markers)
            {
                var screen = mapCamera.WorldToScreenPoint(marker.point);
                marker.label.gameObject.SetActive(screen.z > 0);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(hud, screen, null, out var point))
                    marker.label.anchoredPosition = point;
            }
        }

        void RebuildMarkers()
        {
            foreach (var marker in markers)
            {
                marker.label.gameObject.SetActive(false);
                Destroy(marker.label.gameObject);
            }
            markers.Clear();
            displayedTowns = view.Towns;
            displayedRelief = view.Relief;
            displayedDistrict = view.SelectedDistrict;
            if (!displayedRelief) return;
            var visibleTowns = displayedDistrict < 0 ? displayedTowns : view.TownsInDistrict(displayedDistrict);
            foreach (var town in visibleTowns)
            {
                var point = view.TownMarkerPoint(town);
                var root = Rect(markerRoot, town.name, Vector2.zero, Vector2.zero);
                Label(root, "Name", town.name, new Vector2(0, 24), new Vector2(400, 24), 17);
                Label(root, "Symbol", town.capital ? "★" : "●", Vector2.zero, new Vector2(28, 28), town.capital ? 23 : 16);
                markers.Add((root, point));
            }
        }

        void BuildManager()
        {
            manager = Rect(hud, "Manage Towns Dialog", Vector2.zero, Vector2.zero);
            manager.anchorMin = Vector2.zero;
            manager.anchorMax = Vector2.one;
            manager.offsetMin = manager.offsetMax = Vector2.zero;
            manager.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .55f);
            var panel = Rect(manager, "Manage Towns", Vector2.zero, new Vector2(560, 244));
            panel.gameObject.AddComponent<Image>().color = Panel;
            Label(panel, "Heading", "Manage Towns", new Vector2(0, 88), new Vector2(520, 40), 25);

            var choices = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            choices.transform.SetParent(panel, false);
            var rect = (RectTransform)choices.transform;
            rect.anchoredPosition = new Vector2(0, 36);
            rect.sizeDelta = new Vector2(510, 36);
            townChoices = choices.GetComponent<Dropdown>();
            townChoices.captionText.font = townChoices.itemText.font = font;
            townChoices.onValueChanged.AddListener(_ => ResetDelete());

            manageStatus = Label(panel, "Town edit status", "", new Vector2(0, -20), new Vector2(514, 62), 16);
            MakeButton(panel, "Move Town", new Vector2(-170, -82), new Vector2(158, 38), BeginMove);
            deleteTown = MakeButton(panel, "Delete Town", new Vector2(0, -82), new Vector2(158, 38), DeleteSelectedTown);
            MakeButton(panel, "Close", new Vector2(170, -82), new Vector2(158, 38), () => Cancel());
            manager.gameObject.SetActive(false);
        }

        void OpenManager()
        {
            if (view.SelectedDistrict >= 0 || view.IsSaving || view.DistrictLoading || view.Towns.Length == 0) return;
            Cancel();
            if (combiner.Active) combiner.SetActive(false);
            townChoices.ClearOptions();
            townChoices.AddOptions(view.Towns.Select(town =>
            {
                int district = town.DistrictIndex(view.GridSide);
                return $"{town.name}{(town.capital ? " (Capital)" : "")}  ·  {town.establishedYear}  ·  District {district % view.GridSide + 1}–{district / view.GridSide + 1}";
            }).ToList());
            townChoices.SetValueWithoutNotify(0);
            ResetDelete();
            cameraWasEnabled = cameraControls.enabled;
            cameraControls.enabled = false;
            manager.SetAsLastSibling();
            manager.gameObject.SetActive(true);
        }

        void ResetDelete()
        {
            confirmDelete = false;
            deleteTown.GetComponentInChildren<Text>().text = "Delete Town";
            manageStatus.text = "Town edits update the map marker. Placed lots stay where they are.";
        }

        void BeginMove()
        {
            var town = view.Towns[townChoices.value];
            Cancel();
            BeginPlacement();
            if (!Placing) return;
            movingTown = town;
            instruction.text = $"Choose a new spot for {town.name} · Esc to cancel";
        }

        void DeleteSelectedTown()
        {
            var town = view.Towns[townChoices.value];
            if (!confirmDelete)
            {
                confirmDelete = true;
                deleteTown.GetComponentInChildren<Text>().text = "Confirm Delete";
                manageStatus.text = $"Delete {town.name}{(town.capital ? " (Capital)" : "")}?" +
                    (town.capital && view.Towns.Length > 1 ? " The oldest remaining town becomes the capital." : "");
                return;
            }
            string problem = view.DeleteTown(town);
            if (problem != null) manageStatus.text = problem;
            else
            {
                Cancel();
                OpenManager();
            }
        }

        void BuildDialog()
        {
            dialog = Rect(hud, "Establish Town Dialog", Vector2.zero, Vector2.zero);
            dialog.anchorMin = Vector2.zero;
            dialog.anchorMax = Vector2.one;
            dialog.offsetMin = dialog.offsetMax = Vector2.zero;
            dialog.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .55f);

            var panel = Rect(dialog, "Establish Town", Vector2.zero, new Vector2(460, 310));
            panel.gameObject.AddComponent<Image>().color = Panel;
            Label(panel, "Heading", "Establish Town", new Vector2(0, 125), new Vector2(420, 40), 25);
            Label(panel, "Name label", "Town name", new Vector2(-130, 81), new Vector2(120, 28));
            townName = MakeInput(panel, "Town name", new Vector2(-23, 48), new Vector2(334, 37));
            townName.characterLimit = RegionTown.NameLimit;
            townName.textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            BuildAutogenerateButton(panel);

            Label(panel, "Year label", "Year established", new Vector2(-121, 5), new Vector2(144, 28));
            year = MakeInput(panel, "Year established", new Vector2(-134, -28), new Vector2(112, 37));
            year.contentType = InputField.ContentType.IntegerNumber;
            year.characterLimit = 4;
            capitalLabel = Label(panel, "Capital status", "", new Vector2(74, -28), new Vector2(228, 37), 16);
            error = Label(panel, "Validation", "", new Vector2(0, -74), new Vector2(420, 38), 15);
            MakeButton(panel, "Cancel", new Vector2(-103, -120), new Vector2(176, 38), () => Cancel());
            MakeButton(panel, "Establish Town", new Vector2(103, -120), new Vector2(176, 38), Submit);
            dialog.gameObject.SetActive(false);
        }

        void BuildAutogenerateButton(RectTransform panel)
        {
            var refresh = MakeButton(panel, "↻", new Vector2(171.5f, 48), new Vector2(37, 37), AutogenerateName);
            refresh.name = "Autogenerate";
            refresh.GetComponentInChildren<Text>().fontSize = 26;

            var hint = Rect(panel, "Autogenerate hint", new Vector2(129, 83), new Vector2(122, 27));
            var background = hint.gameObject.AddComponent<Image>();
            background.color = new Color(.08f, .10f, .07f);
            background.raycastTarget = false;
            Label(hint, "Text", "Autogenerate", Vector2.zero, hint.sizeDelta, 15);
            autogenerateHint = hint.gameObject;
            autogenerateHint.SetActive(false);

            var hover = refresh.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => autogenerateHint.SetActive(true));
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => autogenerateHint.SetActive(false));
            hover.triggers.Add(enter);
            hover.triggers.Add(exit);
        }

        void AutogenerateName()
        {
            townNames ??= new PlaceNameGenerator("TownNames", RegionTown.NameLimit);
            var name = townNames.Next(townName.text);
            if (name == null)
            {
                error.text = "No town names are available.";
                return;
            }
            townName.text = name;
            error.text = "";
        }

        void UpdateCapitalLabel()
        {
            capitalLabel.text = view.Towns.Length == 0 ? "Regional capital" : "";
        }

        void Submit()
        {
            int.TryParse(year.text, out int establishedYear);
            string problem = view.AddTown(pendingPosition, townName.text.Trim(), establishedYear);
            if (problem == null)
            {
                Cancel();
                var capital = view.Capital;
                if (capital != null && capital.awaitingFoundersBuilding && view.Towns.Length == 1)
                    view.RequestDistrict(capital.DistrictIndex(view.GridSide));
            }
            else error.text = problem;
        }

        static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        Text Label(Transform parent, string name, string caption, Vector2 position, Vector2 size, int fontSize = 17)
        {
            var text = Rect(parent, name, position, size).gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.text = caption;
            text.color = Gold;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.supportRichText = false;
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, .9f);
            return text;
        }

        Button MakeButton(Transform parent, string caption, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction click)
        {
            var rect = Rect(parent, caption, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(.21f, .25f, .17f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(click);
            Label(rect, "Caption", caption, Vector2.zero, size);
            return button;
        }

        InputField MakeInput(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = Rect(parent, name, position, size);
            var background = rect.gameObject.AddComponent<Image>();
            background.color = new Color(.18f, .21f, .15f);
            var input = rect.gameObject.AddComponent<InputField>();
            input.targetGraphic = background;
            input.textComponent = Label(rect, "Value", "", Vector2.zero, size - new Vector2(16, 4));
            input.textComponent.alignment = TextAnchor.MiddleLeft;
            input.textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
            return input;
        }

        void OnDisable() => Cancel();
    }
}
