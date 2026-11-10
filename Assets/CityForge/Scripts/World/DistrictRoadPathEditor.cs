using System.Collections.Generic;
using System.Linq;
using CityForge.CameraSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private bool showRoadPaths;
        private Camera roadPathCamera;
        private DistrictRoadRecord selectedRoadPath;
        private int selectedRoadPoint = -1;
        private Vector2[] roadPointPreview;
        private bool draggingRoadPoint;
        private RectTransform roadPathControls;
        private Button undoRoadButton, redoRoadButton, deleteRoadPointButton;

        private readonly List<RoadEdit> undoRoadEdits = new();
        private readonly List<RoadEdit> redoRoadEdits = new();
        private const int RoadHistoryLimit = 64;

        private readonly struct RoadEditState
        {
            public readonly Vector2[] Points;
            public readonly bool Curved, Aligned;

            public RoadEditState(Vector2[] points, bool curved, bool aligned)
            {
                Points = points;
                Curved = curved;
                Aligned = aligned;
            }
        }

        private readonly struct RoadEdit
        {
            public readonly DistrictRoadRecord Road;
            public readonly RoadEditState Before, After;

            public RoadEdit(DistrictRoadRecord road, RoadEditState before, RoadEditState after)
            {
                Road = road;
                Before = before;
                After = after;
            }
        }

        private string CommitRoadEdit(DistrictRoadRecord road, Vector2[] points, bool curved, bool aligned, int movedPoint = -1)
        {
            if (aligned) points = AlignRoadGuide(points, road.closed, road.width, movedPoint, movedPoint >= 0 ? road.points : null);
            if (curved == road.curved && aligned == road.gridAligned && points.SequenceEqual(road.points)) return null;
            var before = new RoadEditState(road.points, road.curved, road.gridAligned);
            var problem = EditDistrictRoad(road, points, curved, aligned);
            if (problem != null) return problem;
            undoRoadEdits.Add(new RoadEdit(road, before, new RoadEditState(points, curved, aligned)));
            if (undoRoadEdits.Count > RoadHistoryLimit) undoRoadEdits.RemoveAt(0);
            redoRoadEdits.Clear();
            return null;
        }

        private bool RoadHistoryAvailable(List<RoadEdit> edits) =>
            edits.Count > 0 && districtRoadMeshes.ContainsKey(edits[^1].Road);

        private void ReplayRoadEdit(bool redo)
        {
            var source = redo ? redoRoadEdits : undoRoadEdits;
            if (!CanEditRoad || !RoadHistoryAvailable(source)) return;
            var edit = source[^1];
            var state = redo ? edit.After : edit.Before;
            var problem = EditDistrictRoad(edit.Road, state.Points, state.Curved, state.Aligned);
            if (problem != null) { ShowDistrictMessage(problem); return; }
            source.RemoveAt(source.Count - 1);
            (redo ? undoRoadEdits : redoRoadEdits).Add(edit);
            SelectRoadPoint(edit.Road, -1);
        }

        private void SetRoadPaths(bool visible)
        {
            if (showRoadPaths == visible) return;
            showRoadPaths = visible;
            SelectRoadPoint(null, -1);
            foreach (var pair in districtRoadMeshes)
                pair.Value.GetComponent<MeshRenderer>().enabled = !visible;
            if (!visible)
            {
                if (roadPathControls) roadPathControls.gameObject.SetActive(false);
                ClearRoadPathGuides();
                return;
            }

            CancelRoadPlacement();
            CancelLotPlacement();
            CloseTownSection();
            SelectDistrictLot(null);
            roadPathCamera = GetComponentInChildren<CityForgeCameraController>().GetComponent<Camera>();
            if (!roadPathControls)
            {
                roadPathControls = FounderRect(townToolbar.parent, "Road path toolbar", new Vector2(0, 164), new Vector2(660, 72));
                roadPathControls.anchorMin = roadPathControls.anchorMax = roadPathControls.pivot = new Vector2(.5f, 0);
                var help = FounderText(roadPathControls, "Click an endpoint to continue · Shift-click to select · Drag to move · Double-click to insert",
                    new Vector2(0, 22), new Vector2(660, 30), 17);
                help.raycastTarget = false;
                TownButton(roadPathControls, lotFont, "Hide Road Paths", 70, -31, 144, () => SetRoadPaths(false));
                undoRoadButton = TownButton(roadPathControls, lotFont, "Undo", 220, -31, 86, () => ReplayRoadEdit(false));
                redoRoadButton = TownButton(roadPathControls, lotFont, "Redo", 312, -31, 86, () => ReplayRoadEdit(true));
                deleteRoadPointButton = TownButton(roadPathControls, lotFont, "Delete Point", 404, -31, 130, DeleteRoadPoint);
            }
            roadPathControls.gameObject.SetActive(true);
            foreach (var road in districtRoadMeshes.Keys) RefreshRoadPathGuide(road);
        }

        private bool UpdateRoadPaths()
        {
            if (!showRoadPaths) return false;
            if (SelectedDistrict < 0 || !TownConstructionVisible)
            {
                SetRoadPaths(false);
                return false;
            }
            undoRoadButton.interactable = RoadHistoryAvailable(undoRoadEdits);
            redoRoadButton.interactable = RoadHistoryAvailable(redoRoadEdits);
            deleteRoadPointButton.interactable = selectedRoadPath != null && selectedRoadPoint >= 0 &&
                selectedRoadPath.points.Length > (selectedRoadPath.closed ? 3 : 2);
            if (!CanEditRoad || !Application.isFocused) { draggingRoadPoint = false; roadPointPreview = null; return true; }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (draggingRoadPoint) SelectRoadPoint(selectedRoadPath, selectedRoadPoint);
                else SetRoadPaths(false);
                return true;
            }
            var selected = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
            if (selected && selected.TryGetComponent<InputField>(out var field) && field.isFocused) return true;
            bool modifier = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
            if (modifier && Input.GetKeyDown(KeyCode.Z))
                ReplayRoadEdit(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            else if (modifier && Input.GetKeyDown(KeyCode.Y)) ReplayRoadEdit(true);
            else if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)) DeleteRoadPoint();
            return true;
        }

        private void SelectRoadPoint(DistrictRoadRecord road, int index)
        {
            selectedRoadPath = road;
            selectedRoadPoint = index;
            draggingRoadPoint = false;
            roadPointPreview = null;
        }

        private bool HandleRoadPathPointer(Event pointer)
        {
            if (!showRoadPaths || !CanEditRoad || roadContext) return false;
            var screen = new Vector2(pointer.mousePosition.x, Screen.height - pointer.mousePosition.y);
            if (draggingRoadPoint && pointer.type == EventType.MouseDrag)
            {
                if (TryRoadGround(screen, out var point))
                {
                    var road = selectedRoadPath;
                    var points = (Vector2[])road.points.Clone();
                    points[selectedRoadPoint] = point;
                    roadPointPreview = road.gridAligned ? AlignRoadGuide(points, road.closed, road.width, selectedRoadPoint, road.points) : points;
                }
                pointer.Use();
            }
            else if (draggingRoadPoint && pointer.type == EventType.MouseUp)
            {
                if (roadPointPreview != null && !RoadContextOverUi(screen) && TryRoadGround(screen, out var point))
                {
                    var points = (Vector2[])selectedRoadPath.points.Clone();
                    points[selectedRoadPoint] = point;
                    ApplyPathEdit(selectedRoadPath, points, selectedRoadPoint, moved: true);
                }
                else if (roadPointPreview == null && !pointer.shift && !RoadContextOverUi(screen) &&
                    !selectedRoadPath.closed &&
                    (selectedRoadPoint == 0 || selectedRoadPoint == selectedRoadPath.points.Length - 1))
                {
                    var road = selectedRoadPath;
                    var endpoint = road.points[selectedRoadPoint];
                    StartRoadPlacement(FindRoadSurface(road.surfaceId));
                    if (RoadPlacementArmed) roadWire.PlaceMarker(endpoint);
                }
                draggingRoadPoint = false;
                roadPointPreview = null;
                pointer.Use();
            }
            else if (pointer.type == EventType.MouseDown && !RoadContextOverUi(screen))
            {
                float closest = 14f * 14f;
                DistrictRoadRecord road = null;
                int index = -1;
                foreach (var candidate in districtRoadMeshes.Keys)
                    for (int i = 0; i < candidate.points.Length; i++)
                    {
                        if (!ProjectRoadPoint(candidate.points[i], out var position)) continue;
                        float distance = (position - pointer.mousePosition).sqrMagnitude;
                        if (distance >= closest) continue;
                        closest = distance;
                        road = candidate;
                        index = i;
                    }
                if (road != null)
                {
                    SelectRoadPoint(road, index);
                    draggingRoadPoint = true;
                }
                else if (TryRoadGround(screen, out var point))
                {
                    road = PickDistrictRoad(point);
                    SelectRoadPoint(road, -1);
                    if (road != null && pointer.clickCount == 2) InsertRoadPoint(road, point);
                }
                pointer.Use();
            }
            return true;
        }

        private bool TryRoadGround(Vector2 screen, out Vector2 point)
        {
            var camera = roadPathCamera ? roadPathCamera : GetComponentInChildren<CityForgeCameraController>().GetComponent<Camera>();
            var collider = RoadTerrain.GetComponent<TerrainCollider>();
            if (!collider || !collider.enabled) collider = terrain.GetComponent<TerrainCollider>();
            point = default;
            if (!collider || !collider.Raycast(camera.ScreenPointToRay(screen), out var hit, 100000)) return false;
            point = new Vector2(hit.point.x, hit.point.z);
            return true;
        }

        private void InsertRoadPoint(DistrictRoadRecord road, Vector2 point)
        {
            int index = 0;
            float best = float.PositiveInfinity;
            Vector2 inserted = point;
            int segments = road.closed ? road.points.Length : road.points.Length - 1;
            for (int i = 0; i < segments; i++)
            {
                var start = road.points[i];
                var delta = road.points[(i + 1) % road.points.Length] - start;
                var projected = start + delta * Mathf.Clamp01(Vector2.Dot(point - start, delta) / delta.sqrMagnitude);
                float distance = (point - projected).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                index = i + 1;
                inserted = projected;
            }
            var points = new List<Vector2>(road.points);
            points.Insert(index, road.gridAligned ? inserted : ClosestRoadPoint(road, point));
            ApplyPathEdit(road, points.ToArray(), index);
        }

        private void DeleteRoadPoint()
        {
            if (!CanEditRoad || selectedRoadPath == null || selectedRoadPoint < 0 ||
                selectedRoadPath.points.Length <= (selectedRoadPath.closed ? 3 : 2)) return;
            var points = new List<Vector2>(selectedRoadPath.points);
            points.RemoveAt(selectedRoadPoint);
            ApplyPathEdit(selectedRoadPath, points.ToArray(), -1);
        }

        private void ApplyPathEdit(DistrictRoadRecord road, Vector2[] points, int index, bool moved = false)
        {
            var problem = CommitRoadEdit(road, points, road.curved, road.gridAligned, moved ? index : -1);
            if (problem != null) { ShowDistrictMessage(problem); return; }
            SelectRoadPoint(road, index);
        }
    }
}
