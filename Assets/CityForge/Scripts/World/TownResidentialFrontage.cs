using System.Collections.Generic;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        // Road edits replace their mesh, so cached arrays follow the mesh lifetime.
        private readonly System.Runtime.CompilerServices.ConditionalWeakTable<Mesh, RoadGeometry> frontageGeometry = new();

        private sealed class RoadGeometry
        {
            public readonly Vector3[] Vertices;
            public readonly int[] Triangles;

            public RoadGeometry(Mesh mesh)
            {
                Vertices = mesh.vertices;
                Triangles = mesh.triangles;
            }
        }

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
            bool? frontageAlongX = null, bool allowEnclosedBlock = false)
        {
            frontage = TownZoneFacing.None;
            const float edgeTolerance = .15f;
            // Rounded grid-aligned corners can clip a small part of the outside cells.
            const float cornerInset = .85f;
            string depthProblem = $"This residential density allows up to {maxDepth} cells deep from the road.";
            bool enclosedBlock = allowEnclosedBlock && cells.width > 6 && cells.height > 6;
            if (!enclosedBlock && Mathf.Min(cells.width, cells.height) > maxDepth) return depthProblem;
            var coverage = enclosedBlock ? new List<(float start, float end)>[4] : null;
            if (coverage != null)
                for (int side = 0; side < coverage.Length; side++) coverage[side] = new();

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
                var geometry = frontageGeometry.GetValue(mesh, source => new RoadGeometry(source));
                var vertices = geometry.Vertices;
                var triangles = geometry.Triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var a = vertices[triangles[i]];
                    var b = vertices[triangles[i + 1]];
                    var c = vertices[triangles[i + 2]];
                    if (RoadTriangleSpan(a, b, c, min.y + cornerInset, max.y - cornerInset, false,
                            out float innerWest, out float innerEast) &&
                        innerWest < max.x - cornerInset && innerEast > min.x + cornerInset)
                        return "Keep residential zoning clear of the road surface.";
                    if (coverage != null)
                    {
                        Cover(0, true, max.x - edgeTolerance, max.x + reach);
                        Cover(1, false, max.y - edgeTolerance, max.y + reach);
                        Cover(2, true, min.x - reach, min.x + edgeTolerance);
                        Cover(3, false, min.y - reach, min.y + edgeTolerance);
                        continue;
                    }

                    void Cover(int side, bool alongZ, float low, float high)
                    {
                        if (RoadTriangleSpan(a, b, c, low, high, alongZ, out float start, out float end))
                            coverage[side].Add((start, end));
                    }
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

            if (coverage != null)
            {
                for (int side = 0; side < coverage.Length; side++)
                {
                    bool eastOrWest = side % 2 == 0;
                    float coveredTo = (eastOrWest ? min.y : min.x) + cornerInset;
                    float target = (eastOrWest ? max.y : max.x) - cornerInset;
                    var spans = coverage[side];
                    spans.Sort((a, b) => a.start.CompareTo(b.start));
                    foreach (var span in spans)
                    {
                        if (span.start > coveredTo + .01f) break;
                        coveredTo = Mathf.Max(coveredTo, span.end);
                        if (coveredTo >= target) break;
                    }
                    if (coveredTo < target)
                        return "Surround a large residential block with roads on all four sides.";
                }
                return null;
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

        private static TownZoneFacing EnclosedBlockFacing(RectInt bounds, Vector2Int cell)
        {
            // Keep each street's frontage in a rectangle so lots can share the same facing.
            bool westHalf = cell.x < bounds.xMin + bounds.width / 2;
            bool southHalf = cell.y < bounds.yMin + bounds.height / 2;
            if (southHalf) return westHalf ? TownZoneFacing.West : TownZoneFacing.South;
            return westHalf ? TownZoneFacing.North : TownZoneFacing.East;
        }

        private void AddResidentialEntryPath(DistrictLotRecord record, LotObjectDefinition building)
        {
            var lot = record.definition;
            var layout = lot.zoningLayout;
            if (!layout.autoPaths || layout.faceAwayFromRoad || lot.paths.Count > 0) return;
            var info = LotBuildingInfo.LoadEntry(building.assetId);
            var asset = LotAssetCatalog.Load()?.Find(building.assetId);
            if (asset == null || (info == null && !building.assetId.StartsWith("v3-building-low-wealth-",
                    System.StringComparison.Ordinal))) return;

            int frontage = (int)layout.frontage;
            var forward = LotGroundOverlay.QuarterTurn(Vector2.down, frontage);
            var entry = info != null
                ? building.position + LotGroundOverlay.QuarterTurn(info.entrance * building.scale,
                    Mathf.RoundToInt(building.yaw / 90f))
                : building.position + forward * (asset.footprint.y * building.scale * .5f);
            var front = LotGroundOverlay.QuarterTurn(lot.LandBounds, -frontage);
            var aligned = LotGroundOverlay.QuarterTurn(entry, -frontage);
            const float halfPath = .5f;
            if (aligned.x < front.xMin + halfPath || aligned.x > front.xMax - halfPath ||
                aligned.y < front.yMin || aligned.y > front.yMax) return;

            var footprint = new DistrictLotFootprint(record);
            float setback = aligned.y - front.yMin;
            var edge = footprint.WorldPoint(entry + forward * setback);
            var outward = footprint.WorldPoint(entry + forward) - footprint.WorldPoint(entry);
            bool alongZ = Mathf.Abs(outward.y) > .5f;
            float sign = alongZ ? Mathf.Sign(outward.y) : Mathf.Sign(outward.x);
            float across = alongZ ? edge.x : edge.y;
            float edgeAlong = (alongZ ? edge.y : edge.x) * sign;
            float gap = DistrictGridCellMeters;
            foreach (var filter in districtRoadMeshes.Values)
            {
                var mesh = filter.sharedMesh;
                var bounds = mesh.bounds;
                var near = new Vector3(edge.x, bounds.center.y, edge.y);
                if (bounds.SqrDistance(near) > DistrictGridCellMeters * DistrictGridCellMeters) continue;
                var geometry = frontageGeometry.GetValue(mesh, source => new RoadGeometry(source));
                var vertices = geometry.Vertices;
                var triangles = geometry.Triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    if (!RoadTriangleSpan(vertices[triangles[i]], vertices[triangles[i + 1]],
                        vertices[triangles[i + 2]], across - .02f, across + .02f, alongZ,
                        out float low, out float high)) continue;
                    float first = sign > 0f ? low : -high;
                    float last = sign > 0f ? high : -low;
                    if (last < edgeAlong - .15f || first - edgeAlong >= gap) continue;
                    gap = Mathf.Max(-.15f, first - edgeAlong);
                }
            }

            float length = setback + gap;
            if (gap >= DistrictGridCellMeters || length < .10f) return;
            lot.paths.Add(new LotPathDefinition
            {
                points = new[] { entry, entry + forward * length },
                width = 1, materialId = LotPathMaterial.ResidentialFootpath, edgeBlend = 75
            });
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
