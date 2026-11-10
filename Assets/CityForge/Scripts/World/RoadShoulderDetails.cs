using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    /// <summary>Deterministic shoulder placements indexed by the meadow streamer's tile grid.</summary>
    internal sealed class RoadShoulderDetails
    {
        internal const int TileSide = 32;
        private const float StationSpacing = 1.8f;
        private readonly Dictionary<Vector2Int, List<Segment>> tiles = new();
        private readonly int seed;

        private readonly struct Segment
        {
            public readonly int Road;
            public readonly Vector2 A, B;
            public readonly float Start, Length, RoadLength, HalfWidth;
            public readonly bool Dirt, Closed;

            public Segment(int road, DistrictRoadRecord record, Vector2 a, Vector2 b, float start, float roadLength)
            {
                Road = road;
                A = a;
                B = b;
                Start = start;
                Length = Vector2.Distance(a, b);
                RoadLength = roadLength;
                HalfWidth = record.width * .5f;
                Dirt = record.surfaceId == "dirt";
                Closed = record.closed;
            }

            public float DistanceSquared(Vector2 point)
            {
                var edge = B - A;
                var nearest = A + edge * Mathf.Clamp01(Vector2.Dot(point - A, edge) / edge.sqrMagnitude);
                return (point - nearest).sqrMagnitude;
            }
        }

        internal RoadShoulderDetails(IReadOnlyList<DistrictRoadRecord> roads, int[] districts, int seed)
        {
            this.seed = seed;
            var active = new HashSet<int>(districts);
            for (int i = 0; i < roads.Count; i++)
            {
                var road = roads[i];
                if (!active.Contains(road.district)) continue;
                HasDirt |= road.surfaceId == "dirt";
                IReadOnlyList<Vector2> points = road.points;
                if (road.curved)
                {
                    var rounded = new List<Vector2>();
                    LotPathSurface.RoundCorners(points, null, road.closed, road.width, rounded);
                    points = rounded;
                }

                int segmentCount = road.closed ? points.Count : points.Count - 1;
                float length = 0;
                for (int j = 0; j < segmentCount; j++)
                    length += Vector2.Distance(points[j], points[(j + 1) % points.Count]);

                float station = 0;
                for (int j = 0; j < segmentCount; j++)
                {
                    var a = points[j];
                    var b = points[(j + 1) % points.Count];
                    float span = Vector2.Distance(a, b);
                    if (span < .001f) continue;
                    int pieces = Mathf.CeilToInt(span / 16f);
                    for (int part = 0; part < pieces; part++)
                    {
                        var start = Vector2.Lerp(a, b, (float)part / pieces);
                        var end = Vector2.Lerp(a, b, (float)(part + 1) / pieces);
                        AddSegment(new Segment(i, road, start, end, station + span * part / pieces, length));
                    }
                    station += span;
                }
            }
        }

        internal bool HasDirt { get; }
        internal bool HasTile(Vector2Int cell) => tiles.ContainsKey(cell);
        internal static Vector2Int CellAt(Vector2 point) => Vector2Int.FloorToInt(point / TileSide);

        private void AddSegment(Segment segment)
        {
            var padding = Vector2.one * (segment.HalfWidth + .85f);
            var min = CellAt(Vector2.Min(segment.A, segment.B) - padding);
            var max = CellAt(Vector2.Max(segment.A, segment.B) + padding);
            for (int z = min.y; z <= max.y; z++)
            for (int x = min.x; x <= max.x; x++)
            {
                var cell = new Vector2Int(x, z);
                if (!tiles.TryGetValue(cell, out var list)) tiles.Add(cell, list = new List<Segment>());
                list.Add(segment);
            }
        }

        internal bool CoversTraveledArea(Vector2Int cell, Vector2 point)
        {
            if (!tiles.TryGetValue(cell, out var segments)) return false;
            foreach (var segment in segments)
            {
                float clearance = Mathf.Max(0, segment.HalfWidth - .28f);
                if (segment.Dirt && segment.DistanceSquared(point) < clearance * clearance) return true;
            }
            return false;
        }

        internal IEnumerable<(Vector2 Point, int Type, float Scale, float Yaw, bool OnRoad)> Samples(Vector2Int cell)
        {
            if (!tiles.TryGetValue(cell, out var segments)) yield break;
            int count = 0;
            var occupied = new HashSet<Vector2Int>();
            foreach (var segment in segments)
            {
                if (!segment.Dirt) continue;
                var direction = (segment.B - segment.A) / segment.Length;
                var normal = Vector2.Perpendicular(direction);
                int first = Mathf.FloorToInt(segment.Start / StationSpacing);
                int last = Mathf.FloorToInt((segment.Start + segment.Length) / StationSpacing);
                for (int station = first; station <= last; station++)
                for (int side = -1; side <= 1; side += 2)
                {
                    var random = new System.Random(unchecked(seed ^ (segment.Road * 73856093) ^
                        (station * 19349663) ^ (side * 83492791)));
                    float R() => (float)random.NextDouble();
                    float along = (station + .14f + R() * .72f) * StationSpacing;
                    if (along < segment.Start || along >= segment.Start + segment.Length || R() < .26f) continue;
                    if (!segment.Closed && (along < .75f || along > segment.RoadLength - .75f)) continue;
                    var center = segment.A + direction * (along - segment.Start);
                    bool cluster = R() < .34f;
                    int members = cluster ? 3 : 1;
                    for (int member = 0; member < members; member++)
                    {
                        float offset = member == 0 ? Mathf.Lerp(-.13f, .32f, R()) : Mathf.Lerp(-.25f, .35f, R());
                        var point = center + direction * (member * .25f) + normal * (side * (segment.HalfWidth + offset));
                        float kind = R(), scale = R(), yaw = R() * 360;
                        if (CellAt(point) != cell || !ClearOfRoads(point, segment.Road, segments) ||
                            !occupied.Add(Vector2Int.FloorToInt(point * 4f))) continue;
                        int type = kind < .09f ? 3 : kind < .24f ? 2 : kind < .62f ? 0 : 1;
                        yield return (point, type, scale, yaw, offset < 0);
                        if (++count == 96) yield break;
                    }
                }
            }
        }

        private static bool ClearOfRoads(Vector2 point, int road, List<Segment> segments)
        {
            foreach (var other in segments)
            {
                float clearance = other.HalfWidth + (other.Road == road ? -.25f : .55f);
                if (other.DistanceSquared(point) < clearance * clearance) return false;
            }
            return true;
        }
    }
}
