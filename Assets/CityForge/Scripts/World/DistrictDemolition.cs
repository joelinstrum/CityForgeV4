using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private Button detonateButton;
        private Text detonateHint;
        private bool detonateArmed;
        private bool detonateHovered;
        private Vector2? detonateLastPoint;
        private readonly HashSet<DistrictLotRecord> detonateLots = new();
        private readonly HashSet<DistrictRoadRecord> detonateRoads = new();

        private bool CanDetonate => TownConstructionVisible && CanBuildTown && !IsSaving &&
            !RemakeBlocksInput && !playerLotWorkspace && !founderPicker && !transportModal;

        private void BuildDetonateButton(Font font)
        {
            detonateButton = TownButton(townToolbar, font, "Detonate", 900, -14, 64,
                () => SetDetonate(!detonateArmed));
            detonateButton.GetComponentInChildren<Text>(true).gameObject.SetActive(false);
            var icon = FounderRect(detonateButton.transform, "Dynamite icon", new Vector2(0, -1), new Vector2(36, 24));
            icon.localRotation = Quaternion.Euler(0, 0, 19);
            for (int y = -6; y <= 6; y += 6)
                Shape(icon, "Dynamite stick", new Vector2(-2, y), new Vector2(27, 6), new Color(.76f, .22f, .15f));
            Shape(icon, "Binding", new Vector2(-4, 0), new Vector2(8, 18), new Color(.25f, .15f, .13f));
            Shape(icon, "Fuse", new Vector2(15, 5), new Vector2(8, 2), TownGold);
            Shape(icon, "Spark", new Vector2(19, 8), new Vector2(4, 4), TownGold);

            detonateHint = FounderText(townToolbar, "", new Vector2(290, 110), new Vector2(465, 30), 17);
            detonateHint.raycastTarget = false;
            detonateHint.color = TownGold;
            var events = detonateButton.gameObject.AddComponent<EventTrigger>();
            Bind(EventTriggerType.PointerEnter, true);
            Bind(EventTriggerType.PointerExit, false);

            void Bind(EventTriggerType type, bool hovered)
            {
                var entry = new EventTrigger.Entry { eventID = type };
                entry.callback.AddListener(_ => { detonateHovered = hovered; RefreshDetonateHint(); });
                events.triggers.Add(entry);
            }

            void Shape(RectTransform parent, string name, Vector2 center, Vector2 size, Color color)
            {
                var image = FounderRect(parent, name, center, size).gameObject.AddComponent<Image>();
                image.color = color;
                image.raycastTarget = false;
            }
        }

        private void SetDetonate(bool armed)
        {
            if (armed)
            {
                if (!CanDetonate) return;
                CloseTownSection();
                CancelRoadPlacement();
                CancelLotPlacement();
                SetRoadPaths(false);
                SelectDistrictLot(null);
            }
            detonateArmed = armed;
            ClearDetonateStroke();
            if (!detonateButton) return;
            MarkTownButton(detonateButton, armed);
            RefreshDetonateHint();
        }

        private void RefreshDetonateHint()
        {
            if (!detonateHint) return;
            int count = detonateLots.Count + detonateRoads.Count;
            detonateHint.text = !detonateArmed ? (detonateHovered ? "Detonate" : "")
                : count > 0 ? $"Release to remove {count} · Esc to cancel"
                : "Detonate · Click or drag over lots and road runs · Esc to exit";
        }

        private bool UpdateDetonate()
        {
            if (!detonateArmed) return false;
            if (!CanDetonate || !Application.isFocused || Input.GetKeyDown(KeyCode.Escape))
                SetDetonate(false);
            return true;
        }

        private void HandleDetonatePointer(Event pointer)
        {
            if (pointer.button != 0 || !CanDetonate || !Application.isFocused) return;
            var screen = new Vector2(pointer.mousePosition.x, Screen.height - pointer.mousePosition.y);
            switch (pointer.type)
            {
                case EventType.MouseDown:
                    if (RoadContextOverUi(screen)) return;
                    ClearDetonateStroke();
                    detonateLastPoint = screen;
                    CollectDetonateTarget(screen);
                    break;
                case EventType.MouseDrag:
                case EventType.MouseUp:
                    if (!detonateLastPoint.HasValue) return;
                    var previous = detonateLastPoint.Value;
                    int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(previous, screen) / 6f));
                    for (int i = 1; i <= steps; i++)
                        CollectDetonateTarget(Vector2.Lerp(previous, screen, (float)i / steps));
                    detonateLastPoint = screen;
                    if (pointer.type == EventType.MouseUp) CommitDetonateStroke();
                    break;
                default:
                    return;
            }
            RefreshDetonateHint();
            pointer.Use();
        }

        private void CollectDetonateTarget(Vector2 screen)
        {
            if (RoadContextOverUi(screen)) return;
            var camera = GetComponentInChildren<Camera>();
            if (!camera) return;
            var lot = PickDistrictLot(camera.ScreenPointToRay(screen));
            if (lot != null) detonateLots.Add(lot);
            else if (RoadTerrain && TryRoadGround(screen, out var point) && PickDistrictRoad(point) is { } road)
                detonateRoads.Add(road);
        }

        private void CommitDetonateStroke()
        {
            int lots = PlacedLots.RemoveAll(detonateLots.Contains);
            int roads = districtRoads.RemoveAll(detonateRoads.Contains);
            if (roads > 0)
            {
                undoRoadEdits.RemoveAll(edit => detonateRoads.Contains(edit.Road));
                redoRoadEdits.RemoveAll(edit => detonateRoads.Contains(edit.Road));
                RebuildDistrictRoads();
            }
            if (lots > 0)
            {
                QueueDemolitionGrowth(detonateLots);
                RebuildDistrictLots();
            }
            ClearDetonateStroke();
            if (lots + roads > 0) ShowDistrictMessage($"Removed {lots} lots and {roads} road runs.");
        }

        private void ClearDetonateStroke()
        {
            detonateLastPoint = null;
            detonateLots.Clear();
            detonateRoads.Clear();
        }
    }
}
