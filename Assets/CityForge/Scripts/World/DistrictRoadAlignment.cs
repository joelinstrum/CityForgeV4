using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private Vector2[] AlignRoadGuide(IReadOnlyList<Vector2> points, bool closed, int width, int movedPoint = -1, IReadOnlyList<Vector2> originalGuide = null)
        {
            int count = points.Count;
            var result = new Vector2[count];
            if (count < 2)
            {
                if (count == 1) result[0] = points[0];
                return result;
            }

            int segments = closed ? count : count - 1;
            var normals = new Vector2[segments];
            var fixedCoordinate = new float[segments];
            var directions = originalGuide ?? points;
            for (int i = 0; i < segments; i++)
            {
                var delta = directions[(i + 1) % count] - directions[i];
                normals[i] = RoadSnapNormal(delta);
            }

            // Start at a turn in a loop so a straight run crossing the seam is assigned once.
            int first = 0;
            if (closed)
                for (int i = 0; i < segments; i++)
                    if (normals[i] != normals[(i + segments - 1) % segments]) { first = i; break; }

            for (int step = 0; step < segments; step++)
            {
                int i = (first + step) % segments;
                int previous = (i + segments - 1) % segments;
                if (step > 0 && normals[i] == normals[previous])
                    fixedCoordinate[i] = fixedCoordinate[previous];
                else
                    fixedCoordinate[i] = SnapRoadEdge(Vector2.Dot(points[i], normals[i]), width, normals[i]);
            }

            // A dragged corner can reposition both incident runs. Their other
            // endpoints follow the same edge line, so the join stays connected.
            if (movedPoint >= 0)
            {
                if (closed || movedPoint > 0) MoveRun((movedPoint + segments - 1) % segments);
                if (closed || movedPoint < segments) MoveRun(movedPoint);
            }

            for (int i = 0; i < count; i++)
            {
                bool incoming = closed || i > 0, outgoing = closed || i < segments;
                int before = (i + segments - 1) % segments;
                int after = i % segments;
                var n = normals[incoming ? before : after];
                float c = fixedCoordinate[incoming ? before : after];
                result[i] = points[i] + n * (c - Vector2.Dot(points[i], n));
                if (incoming && outgoing && normals[before] != normals[after])
                {
                    var m = normals[after];
                    float d = fixedCoordinate[after];
                    float determinant = n.x * m.y - n.y * m.x;
                    result[i] = new Vector2((c * m.y - n.y * d) / determinant,
                        (n.x * d - c * m.x) / determinant);
                }
            }
            return result;

            void MoveRun(int segment)
            {
                var normal = normals[segment];
                float coordinate = SnapRoadEdge(Vector2.Dot(points[movedPoint], normal), width, normal);
                fixedCoordinate[segment] = coordinate;
                for (int direction = -1; direction <= 1; direction += 2)
                    for (int distance = 1; distance < segments; distance++)
                    {
                        int next = segment + direction * distance;
                        if (!closed && (next < 0 || next >= segments)) break;
                        next = (next + segments) % segments;
                        if (normals[next] != normal) break;
                        fixedCoordinate[next] = coordinate;
                    }
            }

        }

        // Four undirected orientations give eight mouse directions, at 45-degree intervals.
        private static Vector2 RoadSnapNormal(Vector2 delta)
        {
            int direction = ((Mathf.RoundToInt(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg / 45f) % 4) + 4) % 4;
            const float diagonal = .7071067811865475f;
            return direction switch
            {
                0 => Vector2.up,
                1 => new Vector2(-diagonal, diagonal),
                2 => Vector2.right,
                _ => new Vector2(diagonal, diagonal)
            };
        }

        private Vector2 SnapRoadGridPoint(Vector2 point)
        {
            float origin = -MapSide * .5f;
            if (roadWire.Points.Count == 0)
                return new Vector2(TileCenter(point.x), TileCenter(point.y));
            var anchor = roadWire.Points[^1];
            var normal = RoadSnapNormal(point - anchor);
            var along = new Vector2(normal.y, -normal.x);
            // Use the same four-metre pitch in the rotated frame. Direction comes
            // from the current mouse displacement, never accumulated snapped input.
            float distance = Mathf.Round(Vector2.Dot(point - anchor, along) / DistrictGridCellMeters) * DistrictGridCellMeters;
            return anchor + along * distance;

            float TileCenter(float coordinate) => origin +
                (Mathf.Floor((coordinate - origin) / DistrictGridCellMeters) + .5f) * DistrictGridCellMeters;
        }

        private float SnapRoadEdge(float clicked, float width, Vector2 normal)
        {
            float half = width * .5f;
            float lower = NearestRoadGridLine(clicked - half, normal) + half;
            float upper = NearestRoadGridLine(clicked + half, normal) - half;
            return Mathf.Abs(clicked - lower) <= Mathf.Abs(clicked - upper) ? lower : upper;
        }

        private float NearestRoadGridLine(float coordinate) => NearestRoadGridLine(coordinate, Vector2.up);

        private float NearestRoadGridLine(float coordinate, Vector2 normal)
        {
            float origin = Vector2.Dot(Vector2.one * (-MapSide * .5f), normal);
            return origin + Mathf.Round((coordinate - origin) / DistrictGridCellMeters) * DistrictGridCellMeters;
        }
    }
}
