using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityForge.World
{
    // Derived from saved centerlines, never from renderer overlap or enumeration order.
    internal static class StreetJunctions
    {
        internal static bool IsStreet(string id) => id == "paved-street" || PrototypeRegionView.IsFoundersStreet(id);
        internal sealed class Arm
        {
            internal DistrictRoadRecord Road;
            internal Vector2 Direction;
            internal float Length;
        }
        internal sealed class Node
        {
            internal Vector2 Point;
            internal readonly List<Arm> Arms = new();
        }
        internal static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        internal static bool Intersection(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 point)
        {
            point = default;
            var ab = b - a; var cd = d - c;
            float cross = Cross(ab, cd);
            if (ab.sqrMagnitude < .000001f || cd.sqrMagnitude < .000001f ||
                Mathf.Abs(cross) < .001f * ab.magnitude * cd.magnitude) return false;
            float t = Cross(c - a, cd) / cross, u = Cross(c - a, ab) / cross;
            if (t < -.00001f || t > 1.00001f || u < -.00001f || u > 1.00001f) return false;
            point = a + ab * Mathf.Clamp01(t);
            return true;
        }
        internal static List<Node> Build(IEnumerable<DistrictRoadRecord> records,
            Func<DistrictRoadRecord, IReadOnlyList<Vector2>> centerline)
        {
            var roads = records.ToArray();
            var lines = roads.Select(centerline).ToArray();
            var nodes = new List<Node>();
            for (int r = 0; r < roads.Length; r++)
            for (int s = r; s < roads.Length; s++)
            {
            for (int i = 0; i < lines[r].Count - (roads[r].closed ? 0 : 1); i++)
            for (int j = 0; j < lines[s].Count - (roads[s].closed ? 0 : 1); j++)
            {
                var a = lines[r][i]; var b = lines[r][(i + 1) % lines[r].Count];
                var c = lines[s][j]; var d = lines[s][(j + 1) % lines[s].Count];
                if (r == s && (j <= i || roads[r].curved && (j == i+1 || roads[r].closed && i==0 && j==lines[r].Count-1))) continue;
                bool intersects = Intersection(a, b, c, d, out var p);
                if (!intersects)
                {
                    if(r==s)continue;
                    if(Vector2.Distance(a,c)<.02f || Vector2.Distance(a,d)<.02f)p=a;
                    else if(Vector2.Distance(b,c)<.02f || Vector2.Distance(b,d)<.02f)p=b;
                    else continue;
                }
                var node = nodes.Find(n => (n.Point - p).sqrMagnitude < .0004f);
                if (node == null) { node = new Node { Point = p }; nodes.Add(node); }
                Add(node, roads[r], a); Add(node, roads[r], b);
                Add(node, roads[s], c); Add(node, roads[s], d);
            }
            }
            // Rounded corners can move the rendered centerline away from an authored
            // connection at that corner. Preserve that deliberate guide connection when
            // both ribbons still cover it; do not invent links between nearby roads.
            for (int r = 0; r < roads.Length; r++)
            for (int s = r + 1; s < roads.Length; s++)
            {
                if (!roads[r].curved && !roads[s].curved) continue;
                var first = roads[r]; var second = roads[s];
                for (int i = 0; i < first.points.Length - (first.closed ? 0 : 1); i++)
                for (int j = 0; j < second.points.Length - (second.closed ? 0 : 1); j++)
                {
                    var a = first.points[i]; var b = first.points[(i + 1) % first.points.Length];
                    var c = second.points[j]; var d = second.points[(j + 1) % second.points.Length];
                    if (!Intersection(a, b, c, d, out var p) ||
                        !OnRibbon(p, lines[r], first) || !OnRibbon(p, lines[s], second)) continue;
                    var node = nodes.Find(n => (n.Point - p).sqrMagnitude < .0004f);
                    // A sampled intersection nearby already expresses this same connection.
                    if (node == null && nodes.Any(n => (n.Point - p).sqrMagnitude < 1f &&
                        n.Arms.Any(arm => arm.Road == first) && n.Arms.Any(arm => arm.Road == second))) continue;
                    if (node == null) { node = new Node { Point = p }; nodes.Add(node); }
                    Add(node, first, a); Add(node, first, b); Add(node, second, c); Add(node, second, d);
                }
            }
            // Include every incident guide leg at an established node. At a rounded
            // receiving corner the intersecting horizontal leg alone does not describe
            // the long vertical continuation hidden by an overlapping endpoint stub.
            foreach (var node in nodes)
            {
                var incident = node.Arms.Select(arm => arm.Road).Distinct().ToArray();
                foreach (var road in incident)
                for (int i = 0; i < road.points.Length - (road.closed ? 0 : 1); i++)
                {
                    var a = road.points[i]; var b = road.points[(i + 1) % road.points.Length];
                    var delta = b - a;
                    float t = Vector2.Dot(node.Point - a, delta) / Mathf.Max(.000001f, delta.sqrMagnitude);
                    if (t < -.00001f || t > 1.00001f || (node.Point - Vector2.Lerp(a, b, t)).sqrMagnitude > .0004f) continue;
                    Add(node, road, a); Add(node, road, b);
                }
            }
            foreach (var node in nodes)
            foreach (var arm in node.Arms)
            foreach (var other in nodes)
            {
                var delta=other.Point-node.Point;
                if(other!=node && Vector2.Dot(delta,arm.Direction)>.02f && Mathf.Abs(Cross(delta,arm.Direction))<.02f)
                    arm.Length=Mathf.Min(arm.Length,delta.magnitude*.5f);
            }
            foreach (var node in nodes)
                node.Arms.Sort((a, b) => Mathf.Atan2(a.Direction.y, a.Direction.x).CompareTo(Mathf.Atan2(b.Direction.y, b.Direction.x)));
            return nodes.OrderBy(n => n.Point.x).ThenBy(n => n.Point.y).ToList();

            static void Add(Node node, DistrictRoadRecord road, Vector2 end)
            {
                var delta = end - node.Point;
                if (delta.magnitude < .025f) return;
                var direction = delta.normalized;
                var arm = node.Arms.Find(a => Vector2.Dot(a.Direction, direction) > .999f);
                if (arm == null) node.Arms.Add(new Arm { Road = road, Direction = direction, Length = delta.magnitude });
                else
                {
                    // Overlapping runs form a union. A short end stub must not erase the
                    // longer receiving arm, otherwise one side of a T loses its fillet.
                    arm.Length = Mathf.Max(arm.Length, delta.magnitude);
                    if (road.width > arm.Road.width || road.width == arm.Road.width &&
                        string.CompareOrdinal(road.surfaceId, arm.Road.surfaceId) < 0) arm.Road = road;
                }
            }
        }
        private static bool OnRibbon(Vector2 point, IReadOnlyList<Vector2> line, DistrictRoadRecord road)
        {
            for (int i = 0; i < line.Count - (road.closed ? 0 : 1); i++)
            {
                var a = line[i]; var delta = line[(i + 1) % line.Count] - a;
                var nearest = a + delta * Mathf.Clamp01(Vector2.Dot(point - a, delta) / Mathf.Max(.000001f, delta.sqrMagnitude));
                if ((point - nearest).sqrMagnitude <= road.width * road.width * .25f) return true;
            }
            return false;
        }
        internal static float Reach(Node node, Arm arm)
        {
            float reach=arm.Road.width*.5f+5f;
            foreach(var other in node.Arms)
            {
                float cross=Cross(arm.Direction,other.Direction);
                if(Mathf.Abs(cross)<.15f)continue;
                var side=Vector2.Perpendicular(arm.Direction)*(arm.Road.width*.5f)*Mathf.Sign(cross);
                var otherSide=-Vector2.Perpendicular(other.Direction)*(other.Road.width*.5f)*Mathf.Sign(cross);
                float along=Cross(otherSide-side,other.Direction)/cross;
                reach=Mathf.Max(reach,along+1.1f);
            }
            return Mathf.Min(arm.Length,reach);
        }
        // CCW boundary with quadratic fillets at the inner corners; straight arms remain unchanged.
        internal static List<Vector2> Outline(Node node)
        {
            var result = new List<Vector2>();
            for (int i = 0; i < node.Arms.Count; i++)
            {
                var a = node.Arms[i]; var b = node.Arms[(i + 1) % node.Arms.Count];
                float h = a.Road.width * .5f, k = b.Road.width * .5f;
                var left = Vector2.Perpendicular(a.Direction) * h;
                var right = -Vector2.Perpendicular(b.Direction) * k;
                float reach = Reach(node,a);
                result.Add(node.Point + a.Direction * reach - left);
                result.Add(node.Point + a.Direction * reach + left);
                float cross = Cross(a.Direction, b.Direction);
                if (Mathf.Abs(cross) <= .15f) continue;
                float alongA = Cross(right - left, b.Direction) / cross;
                float alongB = Cross(right - left, a.Direction) / cross;
                if(cross<0){result.Add(node.Point+left+a.Direction*alongA);continue;}
                float radius = Mathf.Min(1.1f, Mathf.Min(a.Length - alongA, b.Length - alongB) * .35f);
                if (alongA < 0 || alongB < 0 || alongA > reach || alongB > Reach(node,b) || radius <= .05f) continue;
                radius = Mathf.Min(radius, Mathf.Min(reach - alongA, Reach(node,b) - alongB));
                var corner = node.Point + left + a.Direction * alongA;
                var from = corner + a.Direction * radius;
                var to = corner + b.Direction * radius;
                for (int step = 0; step <= 8; step++)
                {
                    float t = step / 8f;
                    result.Add((1 - t) * (1 - t) * from + 2 * (1 - t) * t * corner + t * t * to);
                }
            }
            for(int i=result.Count-1;i>=0;i--)
                if((result[i]-result[(i+1)%result.Count]).sqrMagnitude<.000001f)result.RemoveAt(i);
            return result;
        }
    }
}
