using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    /// <summary>Small, on-demand four-neighbor walking search; no terrain-wide nav mesh or corner cutting.</summary>
    internal static class BoundedGroundRoute
    {
        internal static bool TryFind(Vector2 from, Vector2 to, Func<Vector2, Vector2, bool> clear, out Vector2[] route)
        {
            route = Array.Empty<Vector2>();
            const float step = 2, padding = 24;
            var delta = to - from;
            var lower = new Vector2(Mathf.Floor((Mathf.Min(0, delta.x) - padding) / step) * step,
                Mathf.Floor((Mathf.Min(0, delta.y) - padding) / step) * step);
            var origin = from + lower;
            int width = Mathf.CeilToInt((Mathf.Max(0, delta.x) + padding - lower.x) / step) + 1;
            int height = Mathf.CeilToInt((Mathf.Max(0, delta.y) + padding - lower.y) / step) + 1;
            if (width > 129 || height > 129) return false;
            int start = Mathf.RoundToInt(-lower.x / step) + Mathf.RoundToInt(-lower.y / step) * width;
            var previous = new int[width * height];
            var distance = new float[previous.Length];
            Array.Fill(previous, -1); Array.Fill(distance, float.PositiveInfinity);
            var visited = new bool[previous.Length];
            var open = new SortedSet<(float Score, int Id)>();
            distance[start] = 0; previous[start] = start; open.Add((Heuristic(from), start));
            int expanded = 0;
            while (open.Count > 0 && expanded++ < 2048)
            {
                var first = open.Min; open.Remove(first);
                int at = first.Id;
                if (visited[at]) continue;
                visited[at] = true;
                var point = Point(at);
                if ((point - to).sqrMagnitude <= step * step * 2.1f && clear(point, to))
                {
                    var reverse = new List<Vector2> { to };
                    for (int node = at; node != start; node = previous[node]) reverse.Add(Point(node));
                    reverse.Add(from); reverse.Reverse(); route = reverse.ToArray();
                    return true;
                }
                int x = at % width, y = at / width;
                if (x > 0) Consider(at - 1);
                if (x < width - 1) Consider(at + 1);
                if (y > 0) Consider(at - width);
                if (y < height - 1) Consider(at + width);

                void Consider(int next)
                {
                    if (visited[next] || distance[at] + step >= distance[next]) return;
                    var target = Point(next);
                    if (!clear(point, target)) return;
                    previous[next] = at; distance[next] = distance[at] + step;
                    open.Add((distance[next] + Heuristic(target), next));
                }
            }
            return false;
            Vector2 Point(int i) => origin + new Vector2(i % width, i / width) * step;
            float Heuristic(Vector2 p) => Vector2.Distance(p, to);
        }
    }
}
