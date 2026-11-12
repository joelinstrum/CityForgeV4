using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private static int ResidentialDepth(TownZone zone) => zone switch
        {
            TownZone.Residential1 => 6,
            TownZone.Residential2 => 4,
            TownZone.Residential3 => 5,
            _ => 0
        };

        private static string ResidentialDragHint(TownZone zone) =>
            $"Drag beside a road, up to {ResidentialDepth(zone)} cells deep · Tab to rotate frontage";

        private TownZoneFacing ResidentialFacing(TownZoneFacing frontage) =>
            frontage != TownZoneFacing.None
                ? (TownZoneFacing)(1 + ((int)frontage - 1 + 4 - residentialFacingTurns) % 4)
                : frontage;

        private string ResidentialFrontageProblem(RectInt cells, int maxDepth, out TownZoneFacing frontage,
            bool? frontageAlongX = null)
        {
            frontage = TownZoneFacing.None;
            const float edgeTolerance = .15f;
            // Rounded grid-aligned corners can clip a small part of the outside cells.
            const float cornerInset = .85f;
            string depthProblem = $"This residential density allows up to {maxDepth} cells deep from the road.";
            if (Mathf.Min(cells.width, cells.height) > maxDepth) return depthProblem;

            var min = ZoneCorner(cells.min);
            var max = ZoneCorner(cells.max);
            float reach = DistrictGridCellMeters - edgeTolerance;
            var search = new Rect(min - Vector2.one * reach, max - min + Vector2.one * (2 * reach));
            float bestGap = reach;
            int bestDepth = int.MaxValue;
            var bestFacing = TownZoneFacing.None;

            foreach (var filter in districtRoadMeshes.Values)
            {
                var mesh = filter.sharedMesh;
                var bounds = mesh.bounds;
                if (!search.Overlaps(Rect.MinMaxRect(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z))) continue;
                var vertices = mesh.vertices;
                var triangles = mesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var a = vertices[triangles[i]];
                    var b = vertices[triangles[i + 1]];
                    var c = vertices[triangles[i + 2]];
                    if (RoadTriangleSpan(a, b, c, min.y + cornerInset, max.y - cornerInset, false,
                            out float innerWest, out float innerEast) &&
                        innerWest < max.x - cornerInset && innerEast > min.x + cornerInset)
                        return "Keep residential zoning clear of the road surface.";
                    if (RoadTriangleSpan(a, b, c, min.y + edgeTolerance, max.y - edgeTolerance, false,
                            out float west, out float east))
                    {
                        Consider(west - max.x, cells.width, TownZoneFacing.East);
                        Consider(min.x - east, cells.width, TownZoneFacing.West);
                    }
                    if (RoadTriangleSpan(a, b, c, min.x + edgeTolerance, max.x - edgeTolerance, true,
                            out float south, out float north))
                    {
                        Consider(south - max.y, cells.height, TownZoneFacing.North);
                        Consider(min.y - north, cells.height, TownZoneFacing.South);
                    }
                }
            }

            frontage = bestFacing;
            if (frontage == TownZoneFacing.None) return "Drag residential zoning beside a road.";
            return bestDepth > maxDepth ? depthProblem : null;

            void Consider(float gap, int depth, TownZoneFacing facing)
            {
                bool alongX = facing is TownZoneFacing.East or TownZoneFacing.West;
                if (frontageAlongX.HasValue && alongX != frontageAlongX.Value) return;
                if (gap < -cornerInset || gap >= reach) return;
                gap = Mathf.Max(0f, gap);
                const float tie = .01f;
                if (gap > bestGap + tie || (Mathf.Abs(gap - bestGap) <= tie &&
                    (depth > bestDepth || (depth == bestDepth && facing >= bestFacing)))) return;
                bestGap = gap;
                bestDepth = depth;
                bestFacing = facing;
            }
        }

        // Project the part of the pavement triangle within the frontage's transverse interval.
        private static bool RoadTriangleSpan(Vector3 a, Vector3 b, Vector3 c, float min, float max, bool alongZ,
            out float low, out float high)
        {
            float first = float.PositiveInfinity, last = float.NegativeInfinity;
            ClipEdge(a, b);
            ClipEdge(b, c);
            ClipEdge(c, a);
            low = first;
            high = last;
            return low <= high;

            void ClipEdge(Vector3 from, Vector3 to)
            {
                float start = alongZ ? from.x : from.z;
                float end = alongZ ? to.x : to.z;
                if (Mathf.Max(start, end) <= min || Mathf.Min(start, end) >= max) return;
                float startAlong = alongZ ? from.z : from.x;
                float endAlong = alongZ ? to.z : to.x;
                if (Mathf.Abs(end - start) > .0001f)
                {
                    Include(Mathf.Lerp(startAlong, endAlong, (min - start) / (end - start)));
                    Include(Mathf.Lerp(startAlong, endAlong, (max - start) / (end - start)));
                }
                else
                {
                    Include(startAlong);
                    Include(endAlong);
                }
            }

            void Include(float value)
            {
                first = Mathf.Min(first, value);
                last = Mathf.Max(last, value);
            }
        }

        private Material CreateArrowMaterial()
        {
            var material = Own(new Material(Shader.Find("Universal Render Pipeline/Unlit")));
            material.SetColor("_BaseColor", new Color(.10f, .20f, .09f, 1f));
            CityForge.Lots.LotGroundTexture.ConfigureTransparentLayer(material, 2);
            return material;
        }

        private void AddResidentialArrow(List<Vector3> vertices, List<int> triangles, Vector2 center,
            TownZoneFacing facing, float lift)
        {
            var forward = facing switch
            {
                TownZoneFacing.East => Vector2.right,
                TownZoneFacing.North => Vector2.up,
                TownZoneFacing.West => Vector2.left,
                _ => Vector2.down
            };
            var right = Vector2.Perpendicular(forward);
            int first = vertices.Count;
            Add(-.20f, -1.00f);
            Add(-.20f, .20f);
            Add(.20f, .20f);
            Add(.20f, -1.00f);
            Add(-.70f, .15f);
            Add(0f, 1.15f);
            Add(.70f, .15f);
            triangles.Add(first);
            triangles.Add(first + 2);
            triangles.Add(first + 1);
            triangles.Add(first);
            triangles.Add(first + 3);
            triangles.Add(first + 2);
            triangles.Add(first + 4);
            triangles.Add(first + 6);
            triangles.Add(first + 5);

            void Add(float across, float along)
            {
                var point = center + right * across + forward * along;
                vertices.Add(new Vector3(point.x, LotHeight(point) + lift, point.y));
            }
        }
    }
}
