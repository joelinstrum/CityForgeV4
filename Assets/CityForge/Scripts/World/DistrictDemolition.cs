using System.Collections.Generic;
using System.Linq;
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
        private DistrictLotRecord detonateHoverLot;
        private DistrictRoadRecord detonateHoverRoad;
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
            else if (!detonateLastPoint.HasValue)
            {
                var (lot, road) = DetonateTarget(Input.mousePosition);
                if (lot != detonateHoverLot || road != detonateHoverRoad)
                {
                    detonateHoverLot = lot;
                    detonateHoverRoad = road;
                    RefreshDetonatePreview();
                }
            }
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
                    RefreshDetonatePreview();
                    break;
                case EventType.MouseDrag:
                case EventType.MouseUp:
                    if (!detonateLastPoint.HasValue) return;
                    var previous = detonateLastPoint.Value;
                    int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(previous, screen) / 6f));
                    int previousCount = detonateLots.Count + detonateRoads.Count;
                    for (int i = 1; i <= steps; i++)
                        CollectDetonateTarget(Vector2.Lerp(previous, screen, (float)i / steps));
                    detonateLastPoint = screen;
                    if (pointer.type == EventType.MouseUp) CommitDetonateStroke();
                    else if (previousCount != detonateLots.Count + detonateRoads.Count) RefreshDetonatePreview();
                    break;
                default:
                    return;
            }
            RefreshDetonateHint();
            pointer.Use();
        }

        private void CollectDetonateTarget(Vector2 screen)
        {
            var (lot, road) = DetonateTarget(screen);
            if (lot != null) detonateLots.Add(lot);
            if (road != null) detonateRoads.Add(road);
        }

        private (DistrictLotRecord Lot, DistrictRoadRecord Road) DetonateTarget(Vector2 screen)
        {
            if (RoadContextOverUi(screen)) return default;
            var camera = GetComponentInChildren<Camera>();
            if (!camera) return default;
            var lot = PickDistrictLot(camera.ScreenPointToRay(screen));
            if (lot != null) return (lot, null);
            return (null, RoadTerrain && TryRoadGround(screen, out var point) ? PickDistrictRoad(point) : null);
        }

        private void RefreshDetonatePreview()
        {
            if (detonateLots.Count + detonateRoads.Count == 0 && detonateHoverLot == null && detonateHoverRoad == null)
            {
                if (zonePreviewRenderer) zonePreviewRenderer.enabled = false;
                return;
            }

            var tiles = new HashSet<Vector2Int>();
            var origin = ZoneCorner(Vector2Int.zero);
            float tile = DistrictGridCellMeters;
            foreach (var lot in detonateLots.Append(detonateHoverLot))
            {
                if (lot == null) continue;
                var footprint = new DistrictLotFootprint(lot);
                var bounds = footprint.Bounds;
                foreach (var cell in Cells(bounds.min, bounds.max).allPositionsWithin)
                    if (footprint.Overlaps(new DistrictLotFootprint(ZoneCorner(cell) + Vector2.one * (tile * .5f),
                        Vector2.one * (tile * .5f))))
                        tiles.Add(cell);
            }

            foreach (var road in detonateRoads.Append(detonateHoverRoad))
            {
                if (road == null) continue;
                var line = RoadCenterline(road);
                int segments = road.closed ? line.Count : line.Count - 1;
                float radius = road.width * .5f;
                for (int i = 0; i < segments; i++)
                {
                    var start = line[i];
                    var end = line[(i + 1) % line.Count];
                    int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(start, end) / (radius * .5f)));
                    for (int step = 0; step <= steps; step++)
                    {
                        var point = Vector2.Lerp(start, end, (float)step / steps);
                        foreach (var cell in Cells(point - Vector2.one * radius, point + Vector2.one * radius).allPositionsWithin)
                        {
                            var corner = ZoneCorner(cell);
                            var nearest = Vector2.Max(corner, Vector2.Min(corner + Vector2.one * tile, point));
                            if ((nearest - point).sqrMagnitude < radius * radius) tiles.Add(cell);
                        }
                    }
                }
            }

            EnsureZoneSurfaces();
            FillZoneMesh(zonePreviewMesh, tiles.Where(cell => InActiveDistrict(ZoneCorner(cell) + Vector2.one * (tile * .5f)))
                .Select(cell => new KeyValuePair<Vector2Int, TownZone>(cell, TownZone.None)), .32f);
            zonePreviewRenderer.enabled = tiles.Count > 0;

            RectInt Cells(Vector2 min, Vector2 max)
            {
                var first = Vector2Int.FloorToInt((min - origin) / tile);
                var last = Vector2Int.CeilToInt((max - origin) / tile);
                return new RectInt(first, last - first);
            }
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
            detonateHoverLot = null;
            detonateHoverRoad = null;
            if (zonePreviewRenderer && !zoneDragStart.HasValue) zonePreviewRenderer.enabled = false;
        }
    }
}
