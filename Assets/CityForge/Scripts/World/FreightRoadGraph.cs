using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace CityForge.World
{
    /// <summary>Road-only shortest paths with projected access and split crossing/T junctions.</summary>
    internal static class FreightRoadGraph
    {
        private sealed class Segment
        {
            internal Vector2 a, b;
            internal readonly List<float> cuts = new() { 0, 1 };
        }
        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        private static Vector2 Key(Vector2 p) => new(Mathf.Round(p.x * 100) * .01f, Mathf.Round(p.y * 100) * .01f);
        private static float Projection(Segment s, Vector2 p) => Mathf.Clamp01(Vector2.Dot(p - s.a, s.b - s.a) / (s.b - s.a).sqrMagnitude);

        internal static bool TryRoute(IEnumerable<(Vector2[] points, bool closed)> roads, Vector2 from, Vector2 to,
            float fromAccess, float toAccess, out Vector2[] route, Func<bool> cancelled=null)
        {
            var search=new Search(roads,from,to,fromAccess,toAccess);
            while(!search.Complete)
            {
                if(cancelled!=null && cancelled()){route=Array.Empty<Vector2>();return false;}
                search.Step();
            }
            route=search.Route;return route.Length>1;
        }

        internal sealed class Search
        {
            internal Vector2[] Route { get; private set; }=Array.Empty<Vector2>();
            internal bool Complete { get; private set; }
            private readonly IEnumerator steps;
            internal Search(IEnumerable<(Vector2[] points,bool closed)> roads,Vector2 from,Vector2 to,float fromAccess,float toAccess)
            {steps=RouteSteps(roads,from,to,fromAccess,toAccess,r=>Route=r);}
            internal void Step(){if(!Complete)Complete=!steps.MoveNext();}
        }

        private static IEnumerator RouteSteps(IEnumerable<(Vector2[] points,bool closed)> roads,Vector2 from,Vector2 to,
            float fromAccess,float toAccess,Action<Vector2[]> finished)
        {
            var segments = new List<Segment>();
            foreach (var (points, closed) in roads)
                for (int i = 0; i < points.Length - (closed ? 0 : 1); i++)
                    if ((points[(i + 1) % points.Length] - points[i]).sqrMagnitude > .001f)
                        segments.Add(new Segment { a = points[i], b = points[(i + 1) % points.Length] });
            if (!Anchor(from, fromAccess, out var start) || !Anchor(to, toAccess, out var end)) yield break;
            for (int i = 0; i < segments.Count; i++)
            for (int j = i + 1; j < segments.Count; j++)
            {
                if ((j & 31)==0) yield return null;
                var s = segments[i]; var t = segments[j];
                var ab = s.b - s.a; var cd = t.b - t.a;
                float cross = Cross(ab, cd);
                if (Mathf.Abs(cross) > .00001f)
                {
                    float x = Cross(t.a - s.a, cd) / cross;
                    float y = Cross(t.a - s.a, ab) / cross;
                    if (x >= 0 && x <= 1 && y >= 0 && y <= 1) { s.cuts.Add(x); t.cuts.Add(y); }
                }
                else
                {
                    AddEndpoint(s, t.a); AddEndpoint(s, t.b);
                    AddEndpoint(t, s.a); AddEndpoint(t, s.b);
                }
            }
            var graph = new Dictionary<Vector2, Dictionary<Vector2, float>>();
            foreach (var s in segments)
            {
                yield return null;
                s.cuts.Sort();
                for (int i = 1; i < s.cuts.Count; i++)
                {
                    var a = Key(Vector2.Lerp(s.a, s.b, s.cuts[i - 1]));
                    var b = Key(Vector2.Lerp(s.a, s.b, s.cuts[i]));
                    if (a == b) continue;
                    if (!graph.ContainsKey(a)) graph[a] = new();
                    if (!graph.ContainsKey(b)) graph[b] = new();
                    graph[a][b] = graph[b][a] = Vector2.Distance(a, b);
                }
            }
            if (!graph.ContainsKey(start) || !graph.ContainsKey(end)) yield break;
            var distance = new Dictionary<Vector2, float> { [start] = 0 };
            var previous = new Dictionary<Vector2, Vector2>();
            var queue = new List<Vector2> { start };
            var visited = new HashSet<Vector2>();
            while (queue.Count > 0)
            {
                yield return null;
                int best = 0;
                for (int i = 1; i < queue.Count; i++) if (distance[queue[i]] < distance[queue[best]]) best = i;
                var node = queue[best]; queue.RemoveAt(best);
                if (!visited.Add(node)) continue;
                if (node == end) break;
                foreach (var edge in graph[node])
                {
                    float cost = distance[node] + edge.Value;
                    if (distance.TryGetValue(edge.Key, out float old) && old <= cost) continue;
                    distance[edge.Key] = cost; previous[edge.Key] = node; queue.Add(edge.Key);
                }
            }
            if (!visited.Contains(end)) yield break;
            var path = new List<Vector2> { end };
            while (path[^1] != start) path.Add(previous[path[^1]]);
            path.Reverse();
            if ((from - path[0]).sqrMagnitude > .001f) path.Insert(0, from);
            finished(path.ToArray());
            yield break;

            bool Anchor(Vector2 point, float limit, out Vector2 anchor)
            {
                Segment best = null; float at = 0, nearest = limit * limit;
                foreach (var segment in segments)
                {
                    float t = Projection(segment, point);
                    float d = (point - Vector2.Lerp(segment.a, segment.b, t)).sqrMagnitude;
                    if (d > nearest) continue;
                    nearest = d; best = segment; at = t;
                }
                anchor = default;
                if (best == null) return false;
                best.cuts.Add(at); anchor = Key(Vector2.Lerp(best.a, best.b, at)); return true;
            }
        }
        private static void AddEndpoint(Segment s, Vector2 p)
        {
            float t = Projection(s, p);
            if ((p - Vector2.Lerp(s.a, s.b, t)).sqrMagnitude < .0001f) s.cuts.Add(t);
        }
    }
}
