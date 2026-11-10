using System.Collections.Generic;
using CityForge.Lots;
using CityForge.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private const float RoadEndpointSnapDistance = 4f;
        private RectTransform roadContext;
        private Vector2? roadContextPress;

        private readonly List<RaycastResult> roadContextUiHits = new();

        private bool UpdateRoadContext()
        {
            if (!roadContext) return false;
            if (!CanEditRoad || Input.GetKeyDown(KeyCode.Escape)) CloseRoadContext();
            return true;
        }

        private void HandleRoadContextPointer(Event mouseEvent)
        {
            if (!CanEditRoad) return;
            if (roadContext)
            {
                if (mouseEvent.type == EventType.MouseDown)
                {
                    CloseRoadContext();
                    mouseEvent.Use();
                }
                return;
            }

            var nature = GetComponent<NatureToolbar>();
            if (nature && nature.Selected != NatureTool.None) return;
            var pointer = new Vector2(mouseEvent.mousePosition.x, Screen.height - mouseEvent.mousePosition.y);
            if (mouseEvent.type == EventType.MouseDown)
            {
                roadContextPress = RoadContextOverUi(pointer) ? null : pointer;
                return;
            }
            if (roadContextPress.HasValue && (pointer - roadContextPress.Value).sqrMagnitude > 64f)
                roadContextPress = null;
            if (mouseEvent.type != EventType.MouseUp || !roadContextPress.HasValue) return;

            roadContextPress = null;
            if (RoadContextOverUi(pointer) || !TryOpenRoadContext(pointer)) return;
            mouseEvent.Use();
        }

        private bool RoadContextOverUi(Vector2 pointer)
        {
            var events = EventSystem.current;
            if (!events) return false;
            roadContextUiHits.Clear();
            events.RaycastAll(new PointerEventData(events) { position = pointer }, roadContextUiHits);
            foreach (var hit in roadContextUiHits)
                if (hit.gameObject.transform.IsChildOf(townToolbar.parent)) return true;
            return false;
        }

        private bool TryOpenRoadContext(Vector2 pointer)
        {
            if (!TryRoadGround(pointer, out var point)) return false;
            var road = PickDistrictRoad(point);
            if (road == null) return false;
            OpenRoadContext(road, pointer);
            return true;
        }

        private DistrictRoadRecord PickDistrictRoad(Vector2 point)
        {
            DistrictRoadRecord picked = null;
            float closest = float.PositiveInfinity;
            foreach (var road in districtRoads)
            {
                if (!districtRoadMeshes.ContainsKey(road)) continue;
                var distance = (ClosestRoadPoint(road, point) - point).sqrMagnitude;
                float pickRadius = road.width * .5f + .5f;
                if (distance > pickRadius * pickRadius || distance > closest) continue;
                closest = distance;
                picked = road;
            }
            return picked;
        }

        private void OpenRoadContext(DistrictRoadRecord road, Vector2 screen)
        {
            CloseTownSection();
            SelectDistrictLot(null);
            var hud = (RectTransform)townToolbar.parent;
            roadContext = FounderRect(hud, "Road context menu", Vector2.zero, Vector2.zero);
            roadContext.anchorMin = Vector2.zero;
            roadContext.anchorMax = Vector2.one;
            roadContext.offsetMin = roadContext.offsetMax = Vector2.zero;
            var backdrop = roadContext.gameObject.AddComponent<Image>();
            backdrop.color = Color.clear;
            var dismiss = roadContext.gameObject.AddComponent<Button>();
            dismiss.targetGraphic = backdrop;
            dismiss.transition = Selectable.Transition.None;
            dismiss.navigation = new Navigation { mode = Navigation.Mode.None };
            dismiss.onClick.AddListener(CloseRoadContext);

            var panel = FounderRect(roadContext, "Road actions", Vector2.zero, new Vector2(284, 304));
            StyleTownPanel(panel, TownPanelColor);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(roadContext, screen, null, out var point))
            {
                var half = panel.sizeDelta * .5f;
                var area = roadContext.rect;
                panel.anchoredPosition = new Vector2(
                    Mathf.Clamp(point.x + half.x, area.xMin + half.x, area.xMax - half.x),
                    Mathf.Clamp(point.y - half.y, area.yMin + half.y, area.yMax - half.y));
            }

            FounderText(panel, RoadSurfaces[FindRoadSurface(road.surfaceId)].Label,
                new Vector2(0, 129), new Vector2(258, 30), 21).color = TownGold;
            var feedback = FounderText(panel, "", new Vector2(0, -126), new Vector2(258, 46), 15);

            TownButton(panel, lotFont, road.gridAligned ? "Align to Grid: On" : "Align to Grid: Off",
                12, -43, 260, () => Apply(road.points, road.curved, "Road alignment updated.", !road.gridAligned));
            TownButton(panel, lotFont, road.curved ? "Straighten angles" : "Curve angles", 12, -82, 260,
                () => Apply(road.points, !road.curved, road.curved ? "Road angles straightened." : "Road angles curved."))
                .interactable = road.points.Length > 2;
            TownButton(panel, lotFont, "Clean up endpoints", 12, -121, 260, () =>
            {
                var points = SnapRoadEndpoints(road);
                if (points == null)
                {
                    feedback.text = "No loose endpoints within 4 m of another road.";
                    return;
                }
                Apply(points, road.curved, "Road endpoints connected.");
            }).interactable = !road.closed;
            TownButton(panel, lotFont, showRoadPaths ? "Hide Road Paths" : "Show Road Paths", 12, -160, 260, () =>
            {
                CloseRoadContext();
                SetRoadPaths(!showRoadPaths);
                if (showRoadPaths) SelectRoadPoint(road, -1);
            });
            TownButton(panel, lotFont, "Repave", 12, -199, 260, null).interactable = false;

            void Apply(Vector2[] points, bool curved, string success, bool? gridAligned = null)
            {
                var problem = CommitRoadEdit(road, points, curved, gridAligned ?? road.gridAligned);
                if (problem != null)
                {
                    feedback.text = problem;
                    return;
                }
                CloseRoadContext();
                ShowDistrictMessage(success);
            }
        }

        private void CloseRoadContext()
        {
            roadContextPress = null;
            if (!roadContext) return;
            roadContext.gameObject.SetActive(false);
            Destroy(roadContext.gameObject);
            roadContext = null;
        }

        private Vector2[] SnapRoadEndpoints(DistrictRoadRecord road)
        {
            var points = (Vector2[])road.points.Clone();
            bool changed = Snap(0, 1);
            changed |= Snap(points.Length - 1, points.Length - 2);
            return changed ? points : null;

            bool Snap(int endpoint, int neighbor)
            {
                var original = points[endpoint];
                var closest = original;
                float limit = RoadEndpointSnapDistance * RoadEndpointSnapDistance;
                foreach (var other in districtRoadMeshes.Keys)
                {
                    if (other == road) continue;
                    var candidate = ClosestRoadPoint(other, original);
                    float distance = (candidate - original).sqrMagnitude;
                    if (distance > limit || (candidate - points[neighbor]).sqrMagnitude < 1f) continue;
                    limit = distance;
                    closest = candidate;
                }
                points[endpoint] = closest;
                return (closest - original).sqrMagnitude > .0001f;
            }
        }

        private static IReadOnlyList<Vector2> RoadCenterline(DistrictRoadRecord road)
        {
            if (!road.curved) return road.points;
            var rounded = new List<Vector2>();
            LotPathSurface.RoundCorners(road.points, null, road.closed, road.width, rounded);
            return rounded;
        }

        private static Vector2 ClosestRoadPoint(DistrictRoadRecord road, Vector2 point)
        {
            var centerline = RoadCenterline(road);
            var nearest = centerline[0];
            float best = float.PositiveInfinity;
            int segments = road.closed ? centerline.Count : centerline.Count - 1;
            for (int i = 0; i < segments; i++)
            {
                var start = centerline[i];
                var edge = centerline[(i + 1) % centerline.Count] - start;
                float t = Mathf.Clamp01(Vector2.Dot(point - start, edge) / edge.sqrMagnitude);
                var candidate = start + edge * t;
                float distance = (point - candidate).sqrMagnitude;
                if (distance >= best) continue;
                nearest = candidate;
                best = distance;
            }
            return nearest;
        }

        private string EditDistrictRoad(DistrictRoadRecord road, Vector2[] points, bool curved, bool gridAligned)
        {
            if (!CanEditRoad || !districtRoadMeshes.TryGetValue(road, out var filter))
                return "This road is unavailable.";
            var problem = RoadGuideProblem(points, road.closed, MapSide * .5f);
            if (problem != null) return problem;
            var candidate = new DistrictRoadRecord
            {
                district = road.district,
                surfaceId = road.surfaceId,
                width = road.width,
                closed = road.closed,
                curved = curved,
                points = points
            };
            var mesh = BuildDistrictRoadMesh(candidate);
            problem = RoadSiteProblem(mesh);
            if (problem != null)
            {
                Destroy(mesh);
                return problem;
            }

            var oldMesh = filter.sharedMesh;
            filter.sharedMesh = mesh;
            roadPresentationAssets.Remove(oldMesh);
            roadPresentationAssets.Add(mesh);
            Destroy(oldMesh);
            road.points = points;
            road.curved = curved;
            road.gridAligned = gridAligned;
            if (showRoadPaths) RefreshRoadPathGuide(road);
            RefreshRoadShoulderDetails();
            RefreshDirtRoadEndFades();
            RefreshDirtJunctionWear();
            return null;
        }
    }
}
