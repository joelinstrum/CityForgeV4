using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    internal static class NeighborhoodFootpaths
    {
        internal readonly struct Frontage
        {
            internal readonly Vector2 A, B, Outward;
            internal Frontage(Vector2 a, Vector2 b, Vector2 outward) { A = a; B = b; Outward = outward; }
        }

        internal static bool Eligible(LotDefinition lot) => !lot.IsWaterfront &&
            lot.zoningLayout.wealth == LotWealth.Low &&
            lot.zoningLayout.zone is TownZone.Residential2 or TownZone.Residential3;

        internal static List<Frontage> Collect(IEnumerable<DistrictLotRecord> lots)
        {
            var result = new List<Frontage>();
            foreach (var record in lots)
            {
                var lot = record.definition;
                if (!Eligible(lot)) continue;
                int facing = (int)lot.zoningLayout.frontage;
                var aligned = LotGroundOverlay.QuarterTurn(lot.LandBounds, -facing);
                var rotation = Quaternion.Euler(0, record.FacingDegrees, 0);
                Vector2 WorldPoint(Vector2 local)
                {
                    var point = rotation * new Vector3(local.x, 0, local.y);
                    return record.center + new Vector2(point.x, point.z);
                }
                Add(new Vector2(aligned.xMin, aligned.yMin), new Vector2(aligned.xMax, aligned.yMin), Vector2.down);
                if (lot.SideStreetTurn != 0)
                {
                    float x = lot.SideStreetTurn > 0 ? aligned.xMin : aligned.xMax;
                    Add(new Vector2(x, aligned.yMin), new Vector2(x, aligned.yMax),
                        lot.SideStreetTurn > 0 ? Vector2.left : Vector2.right);
                }
                void Add(Vector2 a, Vector2 b, Vector2 normal)
                {
                    a = LotGroundOverlay.QuarterTurn(a, facing); b = LotGroundOverlay.QuarterTurn(b, facing);
                    var n = LotGroundOverlay.QuarterTurn(normal, facing);
                    var worldA = WorldPoint(a);
                    result.Add(new Frontage(worldA, WorldPoint(b), (WorldPoint(a + n) - worldA).normalized));
                }
            }
            return result;
        }

        internal static float Coverage(Vector2 point, Vector2 roadOutward, List<Frontage> frontages)
        {
            float coverage = 0;
            foreach (var edge in frontages)
            {
                if (Vector2.Dot(edge.Outward, roadOutward) > -.35f) continue;
                var span = edge.B - edge.A; float length = span.magnitude;
                if (length < .01f) continue;
                var relative = point - edge.A;
                float along = Vector2.Dot(relative, span / length);
                float across = Vector2.Dot(relative, edge.Outward);
                if (across < -1.5f || across > 4.25f) continue;
                // Fade over the frontage ends, taking the union where neighboring lots meet.
                float fade = Mathf.SmoothStep(0, 1, Mathf.Min(along + .65f, length - along + .65f) / 1.3f);
                fade *= Mathf.SmoothStep(0, 1, (across + 1.5f) / .7f) * Mathf.SmoothStep(0, 1, (4.25f - across) / .7f);
                coverage = Mathf.Min(1, coverage + fade);
            }
            return coverage;
        }

        internal static void ApplyCoverage(Mesh mesh, IReadOnlyList<Vector2> line, List<Frontage> frontages, bool inverse)
        {
            var vertices = mesh.vertices;
            var colors = new Color[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                var point = new Vector2(vertices[i].x, vertices[i].z);
                float best = float.PositiveInfinity; Vector2 nearest = point;
                for (int j = 0; j < line.Count - 1; j++)
                {
                    var edge = line[j + 1] - line[j];
                    var p = line[j] + edge * Mathf.Clamp01(Vector2.Dot(point - line[j], edge) / Mathf.Max(.0001f, edge.sqrMagnitude));
                    float distance = (p - point).sqrMagnitude;
                    if (distance < best) { best = distance; nearest = p; }
                }
                float value = Coverage(point, (point - nearest).normalized, frontages);
                colors[i] = new Color(1, 1, 1, inverse ? 1 - value : value);
            }
            mesh.colors = colors;
            var source = mesh.triangles;
            var kept = new List<int>();
            for (int i = 0; i < source.Length; i += 3)
                if (colors[source[i]].a > .001f || colors[source[i + 1]].a > .001f || colors[source[i + 2]].a > .001f)
                { kept.Add(source[i]); kept.Add(source[i + 1]); kept.Add(source[i + 2]); }
            mesh.SetTriangles(kept, 0);
        }
    }
}
