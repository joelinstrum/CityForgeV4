using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    // These are mutation identities, not a clock: an unchanged world costs no geometry probes.
    internal readonly struct NavigationState : IEquatable<NavigationState>
    {
        private readonly int lots, count, roads, woodland;
        private readonly object trees, water, landscape;
        internal NavigationState(PrototypeRegionView view)
        {
            lots = view.LotPresentationBuilds; count = view.PlacedLots.Count;
            roads = view.FreightRoadRevision; woodland = view.WoodlandRevision;
            landscape = view.ApprovedDistrict;
            trees = view.ApprovedDistrict ? view.ApprovedDistrict.CanonicalTreeInstances : null;
            water = view.Data?.WaterBodies;
        }
        public bool Equals(NavigationState other) => lots == other.lots && count == other.count && roads == other.roads &&
            woodland == other.woodland && ReferenceEquals(trees, other.trees) && ReferenceEquals(water, other.water) && ReferenceEquals(landscape, other.landscape);
    }

    /// <summary>A certified path. After an edit, inspect only the one-metre sections being entered.
    /// Never rescan the remaining route just because something changed elsewhere.</summary>
    internal sealed class EstablishedRoute
    {
        private readonly Vector2[] points;
        private readonly NavigationState established;
        private readonly bool certified;
        private NavigationState inspected;
        private readonly HashSet<(int segment, int cell)> clear = new();
        internal int SectionChecks { get; private set; }
        internal EstablishedRoute(Vector2[] points, NavigationState state, bool certified = true)
        { this.points = points; established = inspected = state; this.certified = certified; }

        internal bool CanTravel(int segment, Vector2 from, Vector2 to, NavigationState state, Func<Vector2, Vector2, bool> available)
        {
            if (certified && state.Equals(established)) return true;
            if (!state.Equals(inspected)) { clear.Clear(); inspected = state; }
            var a = points[segment]; var b = points[(segment + 1) % points.Length];
            var edge = b - a; float length2 = edge.sqrMagnitude;
            int cells = Mathf.Max(1, Mathf.CeilToInt(edge.magnitude));
            float start = length2 > 0 ? Mathf.Clamp01(Vector2.Dot(from - a, edge) / length2) : 0;
            float end = length2 > 0 ? Mathf.Clamp01(Vector2.Dot(to - a, edge) / length2) : 0;
            int first = Mathf.Min(cells - 1, Mathf.FloorToInt(Mathf.Min(start, end) * cells));
            int last = Mathf.Min(cells - 1, Mathf.FloorToInt(Mathf.Max(start, end) * cells));
            for (int cell = first; cell <= last; cell++)
            {
                if (clear.Contains((segment, cell))) continue;
                SectionChecks++;
                if (!available(Vector2.Lerp(a, b, (float)cell / cells), Vector2.Lerp(a, b, (float)(cell + 1) / cells))) return false;
                clear.Add((segment, cell));
            }
            return true;
        }
    }

    public sealed partial class PrototypeRegionView
    {
        internal readonly struct RouteRoad
        {
            internal readonly Vector2[] points;
            internal readonly float width;
            internal readonly bool closed;
            internal RouteRoad(Vector2[] points, float width, bool closed) { this.points = points; this.width = width; this.closed = closed; }
        }
        private int routeRoadRevision = -1;
        private RouteRoad[] routeRoads;
        internal RouteRoad[] CaptureRouteRoads()
        {
            if (routeRoads != null && routeRoadRevision == FreightRoadRevision) return routeRoads;
            var roads = new List<RouteRoad>();
            foreach (var road in districtRoadMeshes.Keys)
                roads.Add(new RouteRoad(new List<Vector2>(RoadCenterline(road)).ToArray(), road.width, road.closed));
            routeRoadRevision = FreightRoadRevision;
            return routeRoads = roads.ToArray();
        }
        internal bool RouteRoadAvailable(Vector2 point, RouteRoad[] original, float shoulder = 1f) =>
            !OnRouteRoad(point, original, shoulder) || OnRouteRoad(point, CaptureRouteRoads(), shoulder);
        internal bool RouteWalkingSurfaceAvailable(Vector2 point, RouteRoad[] originalRoads, RouteRoad[] originalSidewalks) =>
            (!OnRouteRoad(point, originalRoads, 2f) && !OnRouteRoad(point, originalSidewalks, .2f)) ||
            OnRouteRoad(point, CaptureRouteRoads(), 2f) || OnRouteRoad(point, pedestrianSidewalkSnapshot, .2f);
        private static bool OnRouteRoad(Vector2 point, RouteRoad[] roads, float shoulder)
        {
            foreach (var road in roads)
            for (int i = 0; i < road.points.Length - (road.closed ? 0 : 1); i++)
            {
                var a = road.points[i]; var edge = road.points[(i + 1) % road.points.Length] - a;
                var closest = a + edge * (edge.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude) : 0);
                if ((point - closest).sqrMagnitude <= Mathf.Pow(road.width * .5f + shoulder, 2)) return true;
            }
            return false;
        }
    }
}
