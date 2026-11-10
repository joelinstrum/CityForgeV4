using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private bool alignNewRoads;

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
            var horizontal = new bool[segments];
            var fixedCoordinate = new float[segments];
            var directions = originalGuide ?? points;
            for (int i = 0; i < segments; i++)
            {
                var delta = directions[(i + 1) % count] - directions[i];
                horizontal[i] = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
            }

            // Start at a turn in a loop so a straight run crossing the seam is assigned once.
            int first = 0;
            if (closed)
                for (int i = 0; i < segments; i++)
                    if (horizontal[i] != horizontal[(i + segments - 1) % segments]) { first = i; break; }

            for (int step = 0; step < segments; step++)
            {
                int i = (first + step) % segments;
                int previous = (i + segments - 1) % segments;
                if (step > 0 && horizontal[i] == horizontal[previous])
                    fixedCoordinate[i] = fixedCoordinate[previous];
                else
                    fixedCoordinate[i] = SnapRoadEdge(horizontal[i] ? points[i].y : points[i].x, width);
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
                result[i] = points[i];
                if (closed || i > 0) Constrain(i, (i + segments - 1) % segments);
                if (closed || i < segments) Constrain(i, i);
            }
            return result;

            void MoveRun(int segment)
            {
                bool alongX = horizontal[segment];
                float coordinate = SnapRoadEdge(alongX ? points[movedPoint].y : points[movedPoint].x, width);
                fixedCoordinate[segment] = coordinate;
                for (int direction = -1; direction <= 1; direction += 2)
                    for (int distance = 1; distance < segments; distance++)
                    {
                        int next = segment + direction * distance;
                        if (!closed && (next < 0 || next >= segments)) break;
                        next = (next + segments) % segments;
                        if (horizontal[next] != alongX) break;
                        fixedCoordinate[next] = coordinate;
                    }
            }

            void Constrain(int point, int segment)
            {
                if (horizontal[segment]) result[point].y = fixedCoordinate[segment];
                else result[point].x = fixedCoordinate[segment];
            }
        }

        private float SnapRoadEdge(float clicked, float width)
        {
            float half = width * .5f;
            float lower = NearestRoadGridLine(clicked - half) + half;
            float upper = NearestRoadGridLine(clicked + half) - half;
            return Mathf.Abs(clicked - lower) <= Mathf.Abs(clicked - upper) ? lower : upper;
        }

        private float NearestRoadGridLine(float coordinate)
        {
            float origin = -MapSide * .5f;
            return origin + Mathf.Round((coordinate - origin) / DistrictGridCellMeters) * DistrictGridCellMeters;
        }
    }
}
