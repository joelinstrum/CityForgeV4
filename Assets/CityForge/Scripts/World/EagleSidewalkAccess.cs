using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    internal static class EagleSidewalkGeometry
    {
        internal static bool OuterEdge(Rect bounds, Vector2 point)
        {
            // The outer two metres are the walkable sidewalk band for ordinary lots.
            if (!bounds.Contains(point)) return false;
            return Mathf.Min(point.x - bounds.xMin, bounds.xMax - point.x,
                point.y - bounds.yMin, bounds.yMax - point.y) <= 2f;
        }
    }

    public sealed partial class PrototypeRegionView
    {
        private bool EaglePickupSidewalk(Vector3 position)
        {
            var point = new Vector2(position.x, position.z);
            foreach (var (lot, root) in districtLotRoots)
            {
                if (!root) continue;
                var local3 = root.InverseTransformPoint(position);
                var local = new Vector2(local3.x, local3.z);
                if (EagleSidewalkGeometry.OuterEdge(lot.definition.LandBounds, local)) return true;
                foreach (var overlay in lot.definition.overlays)
                    if (overlay.texture == "worn-sidewalk" && overlay.Bounds.Contains(local)) return true;
            }
            foreach (var road in districtRoadMeshes.Keys)
            {
                var line = RoadCenterline(road);
                for (int i = 0; i < line.Count - (road.closed ? 0 : 1); i++)
                {
                    var a = line[i]; var b = line[(i + 1) % line.Count]; var edge = b - a;
                    if (edge.sqrMagnitude < .001f) continue;
                    var nearest = a + edge * Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude);
                    if ((nearest - point).sqrMagnitude <= Mathf.Pow(road.width * .5f + 1.8f, 2)) return true;
                }
            }
            return false;
        }

        private bool EagleSidewalkSegment(Vector3 from, Vector3 to)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(new Vector2(from.x,from.z), new Vector2(to.x,to.z)) / .5f));
            for (int i = 0; i <= steps; i++) if (!EaglePickupSidewalk(Vector3.Lerp(from,to,(float)i/steps))) return false;
            return true;
        }
    }
}
