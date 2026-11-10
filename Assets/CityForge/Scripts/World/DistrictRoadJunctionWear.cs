using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private const int MaxDirtJunctions = 32;
        private static readonly int DirtJunctionCount = Shader.PropertyToID("_DirtJunctionCount");
        private static readonly int DirtJunctions = Shader.PropertyToID("_DirtJunctions");

        private static readonly int DirtJunctionAxes = Shader.PropertyToID("_DirtJunctionAxes");

        private void RefreshDirtJunctionWear()
        {
            var visible = new List<(DistrictRoadRecord Road, IReadOnlyList<Vector2> Line, MeshRenderer Renderer)>();
            foreach (var pair in districtRoadMeshes)
                visible.Add((pair.Key, RoadCenterline(pair.Key), pair.Value.GetComponent<MeshRenderer>()));

            var upload = new Vector4[MaxDirtJunctions];
            var axes = new Vector4[MaxDirtJunctions];
            foreach (var road in visible)
            {
                if (road.Road.surfaceId != "dirt") continue;
                int count = 0;
                var bounds = road.Renderer.bounds;
                bounds.Expand(1f);
                foreach (var other in visible)
                {
                    if (road.Road == other.Road || count == upload.Length) continue;
                    var otherBounds = other.Renderer.bounds;
                    if (bounds.min.x > otherBounds.max.x || bounds.max.x < otherBounds.min.x ||
                        bounds.min.z > otherBounds.max.z || bounds.max.z < otherBounds.min.z) continue;
                    FindDirtJunctions(road.Road, road.Line, other.Road, other.Line, upload, axes, ref count);
                }

                dirtEndProperties ??= new MaterialPropertyBlock();
                road.Renderer.GetPropertyBlock(dirtEndProperties);
                dirtEndProperties.SetFloat(DirtJunctionCount, count);
                if (count > 0)
                {
                    dirtEndProperties.SetVectorArray(DirtJunctions, upload);
                    dirtEndProperties.SetVectorArray(DirtJunctionAxes, axes);
                }
                road.Renderer.SetPropertyBlock(dirtEndProperties);
            }
        }

        private static void FindDirtJunctions(DistrictRoadRecord road, IReadOnlyList<Vector2> line,
            DistrictRoadRecord other, IReadOnlyList<Vector2> otherLine, Vector4[] output, Vector4[] axes, ref int count)
        {
            int segments = road.closed ? line.Count : line.Count - 1;
            int otherSegments = other.closed ? otherLine.Count : otherLine.Count - 1;
            float inner = Mathf.Max(road.width, other.width) * .5f;
            for (int i = 0; i < segments && count < output.Length; i++)
            {
                var a = line[i];
                var b = line[(i + 1) % line.Count];
                var ab = b - a;
                for (int j = 0; j < otherSegments && count < output.Length; j++)
                {
                    var c = otherLine[j];
                    var d = otherLine[(j + 1) % otherLine.Count];
                    var cd = d - c;
                    float cross = Cross(ab, cd);
                    if (Mathf.Abs(cross) < .20f * ab.magnitude * cd.magnitude || Mathf.Abs(cross) < .0001f)
                        continue;

                    float t = Cross(c - a, cd) / cross;
                    float u = Cross(c - a, ab) / cross;
                    if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
                        AddDirtJunction(a + ab * t, inner,
                            TurnAxis(ab, t, i, segments, road),
                            TurnAxis(cd, u, j, otherSegments, other), output, axes, ref count);
                    else
                    {
                        // A branch may stop at the edge of the other ribbon rather than its centerline.
                        if (!road.closed)
                        {
                            if (i == 0) TryBranchEnd(a, ab, c, cd, road.width, other.width, inner, output, axes, ref count);
                            if (i == segments - 1) TryBranchEnd(b, -ab, c, cd, road.width, other.width, inner, output, axes, ref count);
                        }
                        if (!other.closed)
                        {
                            if (j == 0) TryBranchEnd(c, cd, a, ab, other.width, road.width, inner, output, axes, ref count);
                            if (j == otherSegments - 1) TryBranchEnd(d, -cd, a, ab, other.width, road.width, inner, output, axes, ref count);
                        }
                    }
                }
            }

            static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        }

        // Axis length encodes road width in hundredths above the through (1) or terminal (.5) base.
        private static Vector2 TurnAxis(Vector2 segment, float t, int index, int segments, DistrictRoadRecord road)
        {
            var axis = segment.normalized;
            float width = road.width * .01f;
            if (!road.closed && index == 0 && t < .02f) return axis * (.5f + width);
            if (!road.closed && index == segments - 1 && t > .98f) return -axis * (.5f + width);
            return axis * (1 + width);
        }

        private static void TryBranchEnd(Vector2 end, Vector2 inward, Vector2 start, Vector2 segment, int branchWidth, int throughWidth,
            float inner, Vector4[] output, Vector4[] axes, ref int count)
        {
            float t = Mathf.Clamp01(Vector2.Dot(end - start, segment) / segment.sqrMagnitude);
            var projected = start + segment * t;
            float reach = throughWidth * .5f + .20f;
            if ((projected - end).sqrMagnitude <= reach * reach)
                AddDirtJunction((end + projected) * .5f, inner, inward.normalized * (.5f + branchWidth * .01f),
                    segment.normalized * (1 + throughWidth * .01f), output, axes, ref count);
        }

        private static void AddDirtJunction(Vector2 point, float inner, Vector2 a, Vector2 b,
            Vector4[] output, Vector4[] axes, ref int count)
        {
            if (count == output.Length) return;
            for (int i = 0; i < count; i++)
                if ((new Vector2(output[i].x, output[i].y) - point).sqrMagnitude < .25f) return;
            // Start the fade on the approach while covering the overlap of wider roads.
            axes[count] = new Vector4(a.x, a.y, b.x, b.y);
            output[count++] = new Vector4(point.x, point.y, inner * .30f, inner + .60f);
        }
    }
}
