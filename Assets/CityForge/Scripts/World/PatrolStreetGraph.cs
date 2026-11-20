using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    // A reusable walking network. Geometry certificates live until a navigation edit,
    // not until the next frame or the next crime.
    internal sealed class PatrolStreetGraph
    {
        private sealed class Segment
        {
            internal Vector2 A, B;
            internal readonly List<float> Cuts = new() { 0, 1 };
        }
        private sealed class Node
        {
            internal Vector2 Point;
            internal readonly List<int> Edges = new();
        }
        private sealed class Edge
        {
            internal int A, B;
            internal float Length;
            internal byte Clear;
        }
        private readonly List<Node> nodes = new();
        private readonly List<Edge> edges = new();
        private readonly Func<Vector2, Vector2, bool> available;
        internal int Count => nodes.Count;
        internal Vector2 Point(int index) => nodes[index].Point;
        internal int GeometryChecks { get; private set; }

        internal PatrolStreetGraph(IEnumerable<(Vector2[] Points, bool Closed)> paths, Func<Vector2, Vector2, bool> available)
        {
            this.available = available;
            var segments = new List<Segment>();
            foreach (var (points, closed) in paths)
                for (int i = 0; i < points.Length - (closed ? 0 : 1); i++)
                    if ((points[(i + 1) % points.Length] - points[i]).sqrMagnitude > .0001f)
                    {
                        var segment = new Segment { A = points[i], B = points[(i + 1) % points.Length] };
                        int sections = Mathf.CeilToInt(Vector2.Distance(segment.A, segment.B) / 20);
                        for (int part = 1; part < sections; part++) segment.Cuts.Add((float)part / sections);
                        segments.Add(segment);
                    }
            var cells = new Dictionary<Vector2Int, List<int>>();
            var pairs = new HashSet<(int, int)>();
            for (int i = 0; i < segments.Count; i++)
            {
                var s = segments[i]; var lo = Vector2.Min(s.A, s.B); var hi = Vector2.Max(s.A, s.B);
                for (int x = Mathf.FloorToInt(lo.x / 16); x <= Mathf.FloorToInt(hi.x / 16); x++)
                for (int y = Mathf.FloorToInt(lo.y / 16); y <= Mathf.FloorToInt(hi.y / 16); y++)
                {
                    var key = new Vector2Int(x, y);
                    if (!cells.TryGetValue(key, out var bucket)) cells[key] = bucket = new List<int>();
                    foreach (int j in bucket)
                    {
                        if (!pairs.Add((j, i))) continue;
                        var t = segments[j];
                        if (StreetJunctions.Intersection(s.A, s.B, t.A, t.B, out var p)) { Cut(s, p); Cut(t, p); }
                        else { Cut(s, t.A); Cut(s, t.B); Cut(t, s.A); Cut(t, s.B); }
                    }
                    bucket.Add(i);
                }
            }
            var lookup = new Dictionary<Vector2, int>();
            var unique = new HashSet<(int, int)>();
            foreach (var s in segments)
            {
                s.Cuts.Sort();
                for (int i = 1; i < s.Cuts.Count; i++)
                {
                    int a = Vertex(Vector2.Lerp(s.A, s.B, s.Cuts[i - 1])), b = Vertex(Vector2.Lerp(s.A, s.B, s.Cuts[i]));
                    if (a == b || !unique.Add((Mathf.Min(a, b), Mathf.Max(a, b)))) continue;
                    int e = edges.Count; edges.Add(new Edge { A = a, B = b, Length = Vector2.Distance(nodes[a].Point, nodes[b].Point) });
                    nodes[a].Edges.Add(e); nodes[b].Edges.Add(e);
                }
            }
            int Vertex(Vector2 p)
            {
                p = new Vector2(Mathf.Round(p.x * 100) * .01f, Mathf.Round(p.y * 100) * .01f);
                if (lookup.TryGetValue(p, out int at)) return at;
                lookup[p] = at = nodes.Count; nodes.Add(new Node { Point = p }); return at;
            }
        }
        private static void Cut(Segment s, Vector2 p)
        {
            var delta = s.B - s.A; float t = Mathf.Clamp01(Vector2.Dot(p - s.A, delta) / delta.sqrMagnitude);
            if ((p - Vector2.Lerp(s.A, s.B, t)).sqrMagnitude < .0004f) s.Cuts.Add(t);
        }
        private bool Clear(int index)
        {
            var e = edges[index];
            if (e.Clear == 0) { GeometryChecks++; e.Clear = available(nodes[e.A].Point, nodes[e.B].Point) ? (byte)1 : (byte)2; }
            return e.Clear == 1;
        }
        private bool Anchor(Vector2 point, out int edge, out Vector2 projected)
        {
            edge = -1; projected = point; float best = 12 * 12;
            for (int i = 0; i < edges.Count; i++)
            {
                var e = edges[i]; var a = nodes[e.A].Point; var b = nodes[e.B].Point; var d = b - a;
                var p = a + d * Mathf.Clamp01(Vector2.Dot(point - a, d) / d.sqrMagnitude);
                float distance = (p - point).sqrMagnitude;
                if (distance >= best || !Clear(i) || !available(point, p)) continue;
                edge = i; projected = p; best = distance;
            }
            return edge >= 0;
        }
        internal bool TryRoute(Vector2 from, Vector2 to, out Vector2[] route)
        {
            route = Array.Empty<Vector2>();
            if (!Anchor(from, out int start, out var s) || !Anchor(to, out int finish, out var t)) return false;
            if (start == finish) { route = Compact(new List<Vector2> { from, s, t, to }); return true; }
            var distance = new float[nodes.Count]; var previous = new int[nodes.Count]; var visited = new bool[nodes.Count];
            for (int i = 0; i < distance.Length; i++) { distance[i] = float.PositiveInfinity; previous[i] = -1; }
            var first = edges[start]; var last = edges[finish];
            distance[first.A] = Vector2.Distance(s, nodes[first.A].Point);
            distance[first.B] = Vector2.Distance(s, nodes[first.B].Point);
            int end = -1; float best = float.PositiveInfinity;
            for (int pass = 0; pass < nodes.Count; pass++)
            {
                int at = -1; float nearest = float.PositiveInfinity;
                for (int i = 0; i < nodes.Count; i++) if (!visited[i] && distance[i] < nearest) { nearest = distance[i]; at = i; }
                if (at < 0 || nearest >= best) break;
                visited[at] = true;
                if (at == last.A || at == last.B)
                {
                    float cost = nearest + Vector2.Distance(nodes[at].Point, t);
                    if (cost < best) { best = cost; end = at; }
                }
                foreach (int index in nodes[at].Edges)
                {
                    var e = edges[index]; int next = at == e.A ? e.B : e.A;
                    if (visited[next] || !Clear(index)) continue;
                    float cost = nearest + e.Length;
                    if (cost >= distance[next]) continue;
                    distance[next] = cost; previous[next] = at;
                }
            }
            if (end < 0) return false;
            var chain = new List<Vector2>();
            for (int at = end; at >= 0; at = previous[at]) chain.Add(nodes[at].Point);
            chain.Reverse(); chain.Insert(0, s); chain.Insert(0, from); chain.Add(t); chain.Add(to);
            route = Compact(chain); return true;
        }
        private static Vector2[] Compact(List<Vector2> points)
        {
            for (int i = points.Count - 1; i > 0; i--) if ((points[i] - points[i - 1]).sqrMagnitude < .0001f) points.RemoveAt(i);
            return points.ToArray();
        }
    }

    internal sealed class PatrolJourney
    {
        internal readonly Vector2[] Points;
        internal readonly NavigationState State;
        private readonly float[] lengths;
        internal float Distance;
        internal float Length => lengths[^1];
        internal bool Finished => Distance >= Length - .001f;
        internal PatrolJourney(Vector2[] points, NavigationState state)
        {
            Points = points; State = state; lengths = new float[points.Length];
            for (int i = 1; i < points.Length; i++) lengths[i] = lengths[i - 1] + Vector2.Distance(points[i - 1], points[i]);
        }
        internal Vector2 At(float distance)
        {
            for (int i = 1; i < Points.Length; i++)
                if (distance <= lengths[i]) return Vector2.Lerp(Points[i - 1], Points[i], Mathf.InverseLerp(lengths[i - 1], lengths[i], distance));
            return Points[^1];
        }
    }
}
