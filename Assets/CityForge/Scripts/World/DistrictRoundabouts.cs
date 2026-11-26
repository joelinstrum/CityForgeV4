using System.Collections.Generic;
using CityForge.UI;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        internal enum RoadStampShape { None, Roundabout, D, T, X, S, Intersection }
        private RoadStampShape roadStampShape;
        private int roadStampQuarterTurns;
        private bool stampWasLarge;
        private static bool LargeRoundaboutHeld => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        internal static float RoundaboutRadius(int width, bool large) =>
            Mathf.Ceil(Mathf.Max(12f, width * 1.5f) / DistrictGridCellMeters) * DistrictGridCellMeters * (large ? 2 : 1);

        private static Vector2 RoadStampCenter(Vector2 point, float mapSide)
        {
            float origin = -mapSide * .5f;
            float Center(float value) => origin + (Mathf.Floor((value - origin) / DistrictGridCellMeters) + .5f) * DistrictGridCellMeters;
            return new Vector2(Center(point.x), Center(point.y));
        }

        internal static Vector2[] RoundaboutGuide(Vector2 point, float mapSide, int width, bool large) =>
            (Vector2[])RoadShapeGuides(RoadStampShape.Roundabout, point, mapSide, width, large, 0)[0].Points;

        internal static PointWireStampPath[] RoadShapeGuides(RoadStampShape shape, Vector2 point,
            float mapSide, int width, bool large, int quarterTurns)
        {
            var center = RoadStampCenter(point, mapSide);
            float radius = RoundaboutRadius(width, large);
            var paths = new List<PointWireStampPath>();
            switch (shape)
            {
                case RoadStampShape.Roundabout:
                    var circle = new Vector2[48];
                    for (int i = 0; i < circle.Length; i++)
                    {
                        float angle = i * Mathf.PI * 2 / circle.Length;
                        circle[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    }
                    Add(circle, true); break;
                case RoadStampShape.D:
                    // Flat west side, a broad half ellipse to the east. Closed without a duplicate seam.
                    var d = new Vector2[33];
                    for (int i = 0; i < d.Length; i++)
                    {
                        float angle = -Mathf.PI * .5f + i * Mathf.PI / (d.Length - 1);
                        d[i] = new Vector2(-radius + 2 * radius * Mathf.Cos(angle), radius * Mathf.Sin(angle));
                    }
                    Add(d, true); break;
                case RoadStampShape.T:
                    Add(new[] { new Vector2(-radius, 0), Vector2.zero, new Vector2(radius, 0) });
                    Add(new[] { new Vector2(0, -radius), Vector2.zero }); break;
                case RoadStampShape.X:
                    Add(new[] { new Vector2(-radius, -radius), Vector2.zero, new Vector2(radius, radius) });
                    Add(new[] { new Vector2(-radius, radius), Vector2.zero, new Vector2(radius, -radius) }); break;
                case RoadStampShape.S:
                    // Gentle offset connector: horizontal end tangents, one broad inflection, no hairpins.
                    var s = new Vector2[33];
                    for (int i = 0; i < s.Length; i++)
                    {
                        float u = -1 + 2f * i / (s.Length - 1);
                        s[i] = new Vector2(2 * radius * u, radius * (1.5f * u - .5f * u * u * u));
                    }
                    Add(s); break;
                case RoadStampShape.Intersection:
                    Add(new[] { new Vector2(-radius, 0), Vector2.zero, new Vector2(radius, 0) });
                    Add(new[] { new Vector2(0, -radius), Vector2.zero, new Vector2(0, radius) }); break;
            }
            return paths.ToArray();

            void Add(Vector2[] points, bool closed = false)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    var p = points[i];
                    int turns = ((quarterTurns % 4) + 4) % 4;
                    points[i] = center + (turns switch
                    {
                        1 => new Vector2(p.y, -p.x), 2 => -p,
                        3 => new Vector2(-p.y, p.x), _ => p
                    });
                }
                paths.Add(new PointWireStampPath(points, closed));
            }
        }

        private string RoadStampHint()
        {
            string name = roadStampShape switch
            {
                RoadStampShape.Roundabout => "roundabout", RoadStampShape.D => "D loop",
                RoadStampShape.T => "T junction", RoadStampShape.X => "diagonal intersection",
                RoadStampShape.S => "gentle S-curve", _ => "intersection"
            };
            return roadWire.Label + $" · {(LargeRoundaboutHeld ? "Large" : "Small")} {name}\n" +
                "Click to place · Shift: larger · Tab: rotate · r/d/t/x/s/i: shape · Esc: normal road";
        }

        private void SetRoundaboutStamp(bool enabled) => SetRoadStamp(enabled ? RoadStampShape.Roundabout : RoadStampShape.None);

        private void SetRoadStamp(RoadStampShape shape)
        {
            roadStampShape = shape;
            preferredRoadExtension = null; roadGestureStart = null; streetSnapTarget = null;
            roadWire.StampGuide = null; roadWire.StampCompletionRequested = null;
            roadWire.StampPaths = shape != RoadStampShape.None ? point => RoadShapeGuides(roadStampShape, point, MapSide,
                DefaultRoadWidth(RoadSurfaces[roadSurfaceIndex].Id), LargeRoundaboutHeld, roadStampQuarterTurns) : null;
            roadWire.StampPathsCompletionRequested = shape != RoadStampShape.None ? PlaceRoadStamp : null;
            roadWire.ConnectionPreview = shape == RoadStampShape.None && roadWire.PlaceOnRelease ? PreviewStreetConnection : null;
            roadWire.Arm();
            RefreshRoadPlacementHint(force: true);
            if (shape != RoadStampShape.None) RefreshRoadStampHint();
        }

        private void RefreshRoadStampHint()
        {
            stampWasLarge = LargeRoundaboutHeld;
            roadWire.DrawingHint = RoadStampHint(); roadWire.SetMessage(roadWire.DrawingHint);
        }

        private void UpdateRoadStampShortcut()
        {
            // Modified S remains the game's save shortcut; Shift is reserved for stamp size.
            if (!Application.isFocused || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand) ||
                Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) return;
            var events = UnityEngine.EventSystems.EventSystem.current;
            if (events && events.currentSelectedGameObject &&
                events.currentSelectedGameObject.TryGetComponent<UnityEngine.UI.InputField>(out var input) && input.isFocused) return;
            RoadStampShape selected = Input.GetKeyDown(KeyCode.R) ? RoadStampShape.Roundabout :
                Input.GetKeyDown(KeyCode.D) ? RoadStampShape.D : Input.GetKeyDown(KeyCode.T) ? RoadStampShape.T :
                Input.GetKeyDown(KeyCode.X) ? RoadStampShape.X : Input.GetKeyDown(KeyCode.S) ? RoadStampShape.S :
                Input.GetKeyDown(KeyCode.I) ? RoadStampShape.Intersection : RoadStampShape.None;
            if (selected != RoadStampShape.None) SetRoadStamp(selected == roadStampShape ? RoadStampShape.None : selected);
            if (roadStampShape != RoadStampShape.None && Input.GetKeyDown(KeyCode.Tab))
            { roadStampQuarterTurns = (roadStampQuarterTurns + 1) % 4; RefreshRoadStampHint(); }
        }

        private string PlaceRoadStamp(IReadOnlyList<PointWireStampPath> paths)
        {
            if (!CanPlaceRoad) return "Road placement is unavailable.";
            var records = new List<DistrictRoadRecord>();
            string surface = RoadSurfaces[roadSurfaceIndex].Id;
            foreach (var path in paths)
            {
                string problem = RoadGuideProblem(path.Points, path.Closed, MapSide * .5f);
                if (problem != null) return problem;
                var guide = new Vector2[path.Points.Count];
                for (int i = 0; i < guide.Length; i++) guide[i] = path.Points[i];
                records.Add(new DistrictRoadRecord
                {
                    district = SelectedDistrict, surfaceId = surface,
                    streetTreeStyle = HasSidewalks(surface) ? newStreetTreeStyle : null,
                    points = guide, closed = path.Closed,
                    curved = roadStampShape == RoadStampShape.Roundabout || roadStampShape == RoadStampShape.D,
                    gridAligned = false, width = DefaultRoadWidth(surface)
                });
            }
            // Validate every ribbon before any mutation; multi-arm junctions commit as one stamp.
            return CommitNewRoadPieces(records);
        }
    }
}
