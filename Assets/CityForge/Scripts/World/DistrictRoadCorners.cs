using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CityForge.World
{
    internal static class ConnectedRoadCorners
    {
        internal static HashSet<int> Find(DistrictRoadRecord road, IEnumerable<DistrictRoadRecord> roads)
        {
            var result = new HashSet<int>();
            if (!road.curved) return result;
            int count = road.points.Length;
            for (int i = road.closed ? 0 : 1; i < count - (road.closed ? 0 : 1); i++)
            {
                var corner = road.points[i];
                var before = (road.points[(i + count - 1) % count] - corner).normalized;
                var after = (road.points[(i + 1) % count] - corner).normalized;
                foreach (var other in roads)
                {
                    if (other == road) continue;
                    for (int j = 0; j < other.points.Length - (other.closed ? 0 : 1); j++)
                    {
                        var a = other.points[j]; var b = other.points[(j + 1) % other.points.Length];
                        var delta = b - a;
                        float t = Mathf.Clamp01(Vector2.Dot(corner - a, delta) / Mathf.Max(.000001f, delta.sqrMagnitude));
                        var join = a + delta * t;
                        // Snapping to a sampled curve can land slightly inside its authored corner.
                        float reach = Mathf.Max(.15f, road.width * .5f);
                        if ((corner - join).sqrMagnitude > reach * reach) continue;
                        if (Extra(a - join) || Extra(b - join)) { result.Add(i); break; }
                    }
                    if (result.Contains(i)) break;
                }
                bool Extra(Vector2 leg) => leg.sqrMagnitude > .01f &&
                    Vector2.Dot(leg.normalized, before) < .995f && Vector2.Dot(leg.normalized, after) < .995f;
            }
            return result;
        }
    }

    public sealed partial class PrototypeRegionView
    {
        private void RefreshConnectedRoadCorners()
        {
            foreach (var road in districtRoads)
            {
                var next = ConnectedRoadCorners.Find(road, districtRoads);
                bool changed = road.junctionCorners == null ? next.Count > 0 : !road.junctionCorners.SetEquals(next);
                road.junctionCorners = next;
                if (!changed && next.Count == 0 || !districtRoadMeshes.TryGetValue(road, out var view) || !view) continue;
                var filter = view.GetComponent<MeshFilter>();
                var old = filter.sharedMesh;
                var mesh = BuildDistrictRoadMesh(road);
                filter.sharedMesh = mesh;
                roadPresentationAssets.Remove(old); roadPresentationAssets.Add(mesh);
                Destroy(old);
                if (showRoadPaths) RefreshRoadPathGuide(road);
            }
        }
    }
}
