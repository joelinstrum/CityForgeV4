using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.Rendering;

namespace CityForge.World
{
    // Presentation-only union of convex footprints, indexed in world XZ.
    // Mesh clipping removes overlap rather than relying on transparent draw sorting.
    internal sealed class StreetSurfaceMask
    {
        private const float CellSize = 16f;
        private sealed class Piece
        {
            internal Vector2[] Points;
            internal Rect Bounds;
            internal DistrictRoadRecord Owner;
        }
        private readonly List<Piece> pieces = new();
        private readonly Dictionary<Vector2Int, List<int>> cells = new();
        internal void AddRibbon(DistrictRoadRecord road, IReadOnlyList<Vector2> line, float width)
        {
            for (int i = 0; i < line.Count - (road.closed ? 0 : 1); i++)
            {
                int j = (i + 1) % line.Count;
                var a = LotPathSurface.RibbonSide(line, null, road.closed, i, width);
                var b = LotPathSurface.RibbonSide(line, null, road.closed, j, width);
                AddTriangle(line[i] - a, line[i] + a, line[j] + b, road);
                AddTriangle(line[i] - a, line[j] + b, line[j] - b, road);
            }
        }
        internal void AddTriangle(Vector2 a, Vector2 b, Vector2 c, DistrictRoadRecord owner = null)
        {
            float cross = StreetJunctions.Cross(b - a, c - a);
            if (Mathf.Abs(cross) < .000001f) return;
            var p = cross > 0 ? new[] { a, b, c } : new[] { a, c, b };
            var min = Vector2.Min(a, Vector2.Min(b, c));
            var max = Vector2.Max(a, Vector2.Max(b, c));
            var piece = new Piece { Points = p, Bounds = Rect.MinMaxRect(min.x, min.y, max.x, max.y), Owner = owner };
            int id = pieces.Count; pieces.Add(piece);
            var start = Cell(min); var end = Cell(max);
            for (int y = start.y; y <= end.y; y++)
            for (int x = start.x; x <= end.x; x++)
            {
                var key = new Vector2Int(x, y);
                if (!cells.TryGetValue(key, out var list)) cells.Add(key, list = new List<int>());
                list.Add(id);
            }
        }
        private static Vector2Int Cell(Vector2 p) => Vector2Int.FloorToInt(p / CellSize);
        private void Query(Rect bounds, HashSet<int> result, DistrictRoadRecord exclude)
        {
            result.Clear(); var min = Cell(bounds.min); var max = Cell(bounds.max);
            for (int y = min.y; y <= max.y; y++)
            for (int x = min.x; x <= max.x; x++)
                if (cells.TryGetValue(new Vector2Int(x, y), out var list))
                    foreach (int id in list)
                        if ((exclude == null || pieces[id].Owner != exclude) && pieces[id].Bounds.Overlaps(bounds, true)) result.Add(id);
        }
        internal bool Contains(Vector2 point)
        {
            if (!cells.TryGetValue(Cell(point), out var list)) return false;
            foreach (int id in list)
            {
                bool inside = true;
                var p = pieces[id].Points;
                for (int i = 0; i < p.Length; i++)
                    inside &= StreetJunctions.Cross(p[(i + 1) % p.Length] - p[i], point - p[i]) >= -.000001f;
                if (inside) return true;
            }
            return false;
        }
        internal List<(Vector2 A, Vector2 B)> BoundaryEdges()
        {
            var edges = new List<(Vector2, Vector2)>(); var ids = new HashSet<int>();
            foreach (var piece in pieces)
            for (int edge = 0; edge < piece.Points.Length; edge++)
            {
                var a = piece.Points[edge]; var b = piece.Points[(edge + 1) % piece.Points.Length];
                var delta = b - a; if (delta.sqrMagnitude < .000001f) continue;
                var outward = -Vector2.Perpendicular(delta.normalized) * .003f;
                var min = Vector2.Min(a, b) - Vector2.one * .01f;
                var max = Vector2.Max(a, b) + Vector2.one * .01f;
                Query(Rect.MinMaxRect(min.x, min.y, max.x, max.y), ids, null);
                var covered = new List<(float A, float B)>();
                foreach (int id in ids)
                {
                    var other = pieces[id]; if (other == piece) continue;
                    float from = 0, to = 1; bool valid = true;
                    for (int i = 0; i < other.Points.Length; i++)
                    {
                        var p = other.Points[i]; var d = other.Points[(i + 1) % other.Points.Length] - p;
                        float start = StreetJunctions.Cross(d, a - p), rate = StreetJunctions.Cross(d, delta);
                        if (Mathf.Abs(rate) < .000001f) { if (start < -.000001f) valid = false; continue; }
                        float t = -start / rate;
                        if (rate > 0) from = Mathf.Max(from, t); else to = Mathf.Min(to, t);
                    }
                    if (!valid || to - from < .00001f) continue;
                    var sample = a + delta * ((from + to) * .5f) + outward;
                    bool inside = true;
                    for (int i = 0; i < other.Points.Length; i++)
                        inside &= StreetJunctions.Cross(other.Points[(i + 1) % other.Points.Length] - other.Points[i], sample - other.Points[i]) >= -.000001f;
                    if (inside) covered.Add((from, to));
                }
                covered.Sort((x, y) => x.A.CompareTo(y.A)); float cursor = 0;
                foreach (var interval in covered)
                {
                    if (interval.A > cursor + .00001f) edges.Add((a + delta * cursor, a + delta * interval.A));
                    cursor = Mathf.Max(cursor, interval.B);
                }
                if (cursor < 1 - .00001f) edges.Add((a + delta * cursor, b));
            }
            return edges;
        }
        private struct Vertex
        {
            internal Vector3 Position, Normal;
            internal Vector4 Tangent, UV0, UV1;
            internal Color Color;
            internal Vector2 World;
            internal static Vertex Lerp(Vertex a, Vertex b, float t) => new()
            {
                Position = Vector3.LerpUnclamped(a.Position, b.Position, t),
                Normal = Vector3.LerpUnclamped(a.Normal, b.Normal, t),
                Tangent = Vector4.LerpUnclamped(a.Tangent, b.Tangent, t),
                UV0 = Vector4.LerpUnclamped(a.UV0, b.UV0, t), UV1 = Vector4.LerpUnclamped(a.UV1, b.UV1, t),
                Color = Color.LerpUnclamped(a.Color, b.Color, t), World = Vector2.LerpUnclamped(a.World, b.World, t)
            };
        }
        // Returns the original mesh when unaffected; callers own any returned replacement.
        internal Mesh Clip(Mesh source, Matrix4x4 world, float fade = 0f, DistrictRoadRecord exclude = null)
        {
            if (pieces.Count == 0 || source.vertexCount == 0) return source;
            var positions = source.vertices; var normals = source.normals; var tangents = source.tangents; var colors = source.colors;
            var uv0 = new List<Vector4>(); var uv1 = new List<Vector4>(); source.GetUVs(0, uv0); source.GetUVs(1, uv1);
            var input = new Vertex[positions.Length];
            for (int i = 0; i < input.Length; i++)
            {
                var p = world.MultiplyPoint3x4(positions[i]);
                input[i] = new Vertex { Position = positions[i], World = new Vector2(p.x, p.z),
                    Normal = normals.Length > i ? normals[i] : Vector3.up,
                    Tangent = tangents.Length > i ? tangents[i] : new Vector4(1, 0, 0, 1),
                    Color = colors.Length > i ? colors[i] : Color.white,
                    UV0 = uv0.Count > i ? uv0[i] : Vector4.zero, UV1 = uv1.Count > i ? uv1[i] : Vector4.zero };
            }
            bool changed = false;
            var output = new List<Vertex>(); var ids = new HashSet<int>();
            var triangles = source.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var a = input[triangles[i]]; var b = input[triangles[i + 1]]; var c = input[triangles[i + 2]];
                var min = Vector2.Min(a.World, Vector2.Min(b.World, c.World)) - Vector2.one * (fade + .001f);
                var max = Vector2.Max(a.World, Vector2.Max(b.World, c.World)) + Vector2.one * (fade + .001f);
                Query(Rect.MinMaxRect(min.x, min.y, max.x, max.y), ids, exclude);
                var remaining = new List<List<Vertex>> { new() { a, b, c } };
                foreach (int id in ids)
                {
                    var next = new List<List<Vertex>>();
                    foreach (var polygon in remaining) Subtract(polygon, pieces[id], next, ref changed);
                    remaining = next;
                    if (remaining.Count == 0) break;
                }
                foreach (var polygon in remaining)
                    for (int j = 1; j + 1 < polygon.Count; j++)
                    {
                        if (Mathf.Abs(StreetJunctions.Cross(polygon[j].World - polygon[0].World,
                            polygon[j + 1].World - polygon[0].World)) < .000001f) continue;
                        Emit(polygon[0]); Emit(polygon[j]); Emit(polygon[j + 1]);
                    }
                void Emit(Vertex vertex)
                {
                    if (fade > 0f)
                    {
                        float distance = fade;
                        foreach (int id in ids) distance = Mathf.Min(distance, Distance(vertex.World, pieces[id]));
                        float alpha = Mathf.SmoothStep(0, 1, distance / fade);
                        changed |= alpha < .9999f;
                        vertex.Color.a *= alpha;
                    }
                    output.Add(vertex);
                }
            }
            if (!changed) return source;
            var mesh = new Mesh { name = source.name + " · street mask", indexFormat = output.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            var verts = new List<Vector3>(); var norms = new List<Vector3>(); var tans = new List<Vector4>();
            var col = new List<Color>(); uv0.Clear(); uv1.Clear(); var indices = new List<int>();
            foreach (var vertex in output)
            {
                indices.Add(verts.Count); verts.Add(vertex.Position); norms.Add(vertex.Normal); tans.Add(vertex.Tangent);
                col.Add(vertex.Color); uv0.Add(vertex.UV0); uv1.Add(vertex.UV1);
            }
            mesh.SetVertices(verts); mesh.SetNormals(norms); mesh.SetTangents(tans); mesh.SetColors(col);
            mesh.SetUVs(0, uv0); mesh.SetUVs(1, uv1); mesh.SetTriangles(indices, 0); mesh.RecalculateBounds();
            return mesh;
        }
        private static float Distance(Vector2 point, Piece piece)
        {
            bool inside = true; float nearest = float.PositiveInfinity;
            for (int i = 0; i < piece.Points.Length; i++)
            {
                var a = piece.Points[i]; var edge = piece.Points[(i + 1) % piece.Points.Length] - a;
                inside &= StreetJunctions.Cross(edge, point - a) >= -.00001f;
                float t = Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude);
                nearest = Mathf.Min(nearest, Vector2.Distance(point, a + edge * t));
            }
            return inside ? 0 : nearest;
        }
        private static void Subtract(List<Vertex> polygon, Piece piece, List<List<Vertex>> result, ref bool changed)
        {
            // First reject triangles wholly outside one of the convex footprint's planes.
            for (int edge = 0; edge < piece.Points.Length; edge++)
            {
                var a = piece.Points[edge]; var d = piece.Points[(edge + 1) % piece.Points.Length] - a;
                bool outside = true;
                foreach (var vertex in polygon) outside &= StreetJunctions.Cross(d, vertex.World - a) <= .000001f;
                if (outside) { result.Add(polygon); return; }
            }
            var inside = polygon;
            for (int edge = 0; edge < piece.Points.Length && inside.Count > 0; edge++)
            {
                var a = piece.Points[edge]; var d = piece.Points[(edge + 1) % piece.Points.Length] - a;
                var kept = new List<Vertex>(); var outside = new List<Vertex>();
                var previous = inside[^1]; float before = StreetJunctions.Cross(d, previous.World - a);
                foreach (var current in inside)
                {
                    float after = StreetJunctions.Cross(d, current.World - a);
                    if ((before >= 0) != (after >= 0))
                    {
                        var crossing = Vertex.Lerp(previous, current, before / (before - after));
                        kept.Add(crossing); outside.Add(crossing);
                    }
                    if (after >= 0) kept.Add(current); else outside.Add(current);
                    previous = current; before = after;
                }
                if (outside.Count >= 3) result.Add(outside);
                inside = kept;
            }
            if (inside.Count >= 3) changed = true;
        }
    }
}
