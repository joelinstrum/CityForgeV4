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
        private Vector2? detonateAreaStart;
        private Rect? detonateArea;
        private LineRenderer detonateAreaOutline;
        private DistrictLotRecord detonateHoverLot;
        private DistrictRoadRecord detonateHoverRoad;
        private readonly HashSet<DistrictLotRecord> detonateLots = new();
        private readonly HashSet<DistrictRoadRecord> detonateRoads = new();
        private readonly Dictionary<DistrictRoadRecord, List<DistrictRoadRecord>> roadRuns = new();

        private bool CanDetonate => TownConstructionVisible && CanBuildTown && !IsSaving &&
            !RemakeBlocksInput && !playerLotWorkspace && !founderPicker && !transportModal;

        private void BuildDetonateButton(Font font)
        {
            detonateButton = TownButton(townToolbar, font, "Detonate", 1200, -14, 64,
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
                lastRepaveSurface = null;
                CloseTownSection();
                CancelRoadPlacement();
                CancelLotPlacement();
                SetRoadPaths(false);
                SelectDistrictLot(null);
            }
            detonateArmed = armed;
            roadRuns.Clear();
            if (armed) BuildDetonateRoadRuns();
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
                : detonateArea.HasValue ? "Release to clear the outlined area · Esc to cancel"
                : count > 0 ? $"Release to remove {count} · Esc to cancel"
                : "Detonate · Click an item or drag a rectangle to clear everything · Esc to exit";
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
                    if (RoadContextOverUi(screen) || !TryRoadGround(screen, out var start)) return;
                    ClearDetonateStroke();
                    detonateLastPoint = screen; detonateAreaStart = start;
                    CollectDetonateTarget(screen);
                    RefreshDetonatePreview();
                    break;
                case EventType.MouseDrag:
                case EventType.MouseUp:
                    if (!detonateLastPoint.HasValue) return;
                    if (detonateAreaStart.HasValue && (detonateArea.HasValue || Vector2.Distance(detonateLastPoint.Value, screen) > 6f) &&
                        !RoadContextOverUi(screen) && TryRoadGround(screen, out var end))
                    {
                        var a = detonateAreaStart.Value;
                        detonateArea = Rect.MinMaxRect(Mathf.Min(a.x, end.x), Mathf.Min(a.y, end.y), Mathf.Max(a.x, end.x), Mathf.Max(a.y, end.y));
                        RefreshDetonateAreaOutline();
                        if (zonePreviewRenderer) zonePreviewRenderer.enabled = false;
                    }
                    if (pointer.type == EventType.MouseUp)
                    {
                        if (RoadContextOverUi(screen)) ClearDetonateStroke();
                        else if (detonateArea is Rect area)
                        { if (area.width > .1f && area.height > .1f) CommitDetonateArea(area); else ClearDetonateStroke(); }
                        else CommitDetonateStroke();
                    }
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
            if (road != null && detonateRoads.Count == 0) detonateRoads.Add(road);
        }

        private (DistrictLotRecord Lot, DistrictRoadRecord Road) DetonateTarget(Vector2 screen)
        {
            if (RoadContextOverUi(screen)) return default;
            var camera = GetComponentInChildren<Camera>();
            if (!camera) return default;
            var lot = PickDistrictLot(camera.ScreenPointToRay(screen));
            if (lot != null) return (lot, null);
            if (!RoadTerrain || !TryRoadGround(screen, out var point)) return default;
            var road = PickDistrictRoad(point);
            if (road == null || !roadRuns.TryGetValue(road, out var runs)) return default;
            return (null, runs.OrderBy(run => (ClosestRoadPoint(run, point) - point).sqrMagnitude).FirstOrDefault());
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

        private void BuildDetonateRoadRuns()
        {
            roadRuns.Clear();
            var roads = districtRoadMeshes.Keys.ToDictionary(road => road, RoadCenterline);
            foreach (var road in roads.Keys)
            {
                var runs = new List<DistrictRoadRecord>();
                int count = road.points.Length;
                int segments = road.closed ? count : count - 1;
                var current = new List<Vector2> { road.points[0] };
                bool firstIsTurn = false;
                for (int i = 0; i < segments; i++)
                {
                    Vector2 start = road.points[i], end = road.points[(i + 1) % count];
                    var cuts = new SortedSet<float>();
                    foreach (var (other, line) in roads)
                    {
                        if (other == road) continue;
                        int length = other.closed ? line.Count : line.Count - 1;
                        for (int j = 0; j < length; j++)
                        {
                            Vector2 delta = end - start, across = line[(j + 1) % line.Count] - line[j];
                            Vector2 offset = line[j] - start;
                            float cross = Cross(delta, across);
                            if (Mathf.Abs(cross) < .0001f) continue;
                            float t = Cross(offset, across) / cross;
                            float u = Cross(offset, delta) / cross;
                            if (u >= -.001f && u <= 1.001f && t >= -.001f && t <= 1.001f)
                                cuts.Add(Mathf.Clamp01(t));
                        }
                    }

                    foreach (float t in cuts)
                    {
                        var cut = Vector2.Lerp(start, end, t);
                        if ((cut - current[^1]).sqrMagnitude >= 1f && (cut - end).sqrMagnitude >= 1f)
                        {
                            current.Add(cut);
                            FinishRun();
                        }
                    }

                    current.Add(end);
                    var next = road.points[(i + 2) % count];
                    bool turn = Vector2.Dot((end - start).normalized, (next - end).normalized) < .995f;
                    if (i == segments - 1 && road.closed) firstIsTurn = turn || cuts.Contains(1f);
                    if (turn || cuts.Contains(1f) || i == segments - 1) FinishRun();
                }
                if (road.closed && !firstIsTurn && runs.Count > 1)
                {
                    var first = runs[0];
                    first.points = runs[^1].points.Concat(first.points.Skip(1)).ToArray();
                    runs.RemoveAt(runs.Count - 1);
                }
                roadRuns.Add(road, runs);

                void FinishRun()
                {
                    if (current.Count < 2) return;
                    runs.Add(new DistrictRoadRecord
                    {
                        district = road.district, surfaceId = road.surfaceId, width = road.width,
                        gridAligned = road.gridAligned, points = current.ToArray()
                    });
                    current = new List<Vector2> { current[^1] };
                }
            }

            static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        }

        private int RemoveDetonatedRoadRun()
        {
            foreach (var (road, runs) in roadRuns)
            {
                int selected = runs.FindIndex(detonateRoads.Contains);
                if (selected < 0) continue;
                var remaining = road.closed
                    ? new[] { Join(runs.Skip(selected + 1).Concat(runs.Take(selected))) }
                    : new[] { Join(runs.Take(selected)), Join(runs.Skip(selected + 1)) };
                var replacements = remaining.Where(piece => piece != null).ToArray();
                if (districtRoads.Count - 1 + replacements.Length > MaxDistrictRoads ||
                    replacements.Any(piece => RoadGuideProblem(piece.points, false, MapSide * .5f) != null))
                {
                    ShowDistrictMessage("This road section cannot be separated from the remaining road.");
                    return 0;
                }
                int index = districtRoads.IndexOf(road);
                districtRoads.RemoveAt(index);
                districtRoads.InsertRange(index, replacements);
                undoRoadEdits.RemoveAll(edit => edit.Road == road);
                redoRoadEdits.RemoveAll(edit => edit.Road == road);
                return 1;

                DistrictRoadRecord Join(IEnumerable<DistrictRoadRecord> pieces)
                {
                    var points = new List<Vector2>();
                    foreach (var piece in pieces)
                        points.AddRange(points.Count == 0 ? piece.points : piece.points.Skip(1));
                    return points.Count < 2 ? null : new DistrictRoadRecord
                    {
                        district = road.district, surfaceId = road.surfaceId, width = road.width,
                        gridAligned = road.gridAligned, curved = road.curved, points = points.ToArray()
                    };
                }
            }
            return 0;
        }

        private void DetonateSelectedLot()
        {
            var lot = selectedDistrictLot;
            if (lot == null || SelectedDistrict < 0 || DistrictLoading || IsSaving || RemakeBlocksInput ||
                playerLotWorkspace || movingDistrictLot != null || lotPlacementDraft != null || !PlacedLots.Contains(lot)) return;
            // Reuse the existing demolition path, with a fresh target set containing
            // only this lot. Roads and neighboring lots are not part of this action.
            SetDetonate(false);
            SelectDistrictLot(null);
            detonateLots.Add(lot);
            CommitDetonateStroke();
        }

        private void CommitDetonateStroke()
        {
            int lots = PlacedLots.RemoveAll(detonateLots.Contains);
            int roads = RemoveDetonatedRoadRun();
            if (roads > 0)
            {
                RebuildDistrictRoads();
                BuildDetonateRoadRuns();
                QueueLotGrowth();
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
            detonateLastPoint = null; detonateAreaStart = null; detonateArea = null;
            if (detonateAreaOutline) detonateAreaOutline.enabled = false;
            detonateLots.Clear();
            detonateRoads.Clear();
            detonateHoverLot = null;
            detonateHoverRoad = null;
            if (zonePreviewRenderer && !zoneDragStart.HasValue) zonePreviewRenderer.enabled = false;
        }
    }
}
