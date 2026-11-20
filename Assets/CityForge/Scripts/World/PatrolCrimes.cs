using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private readonly List<DistrictHarass> patrolCrimes = new();
        private readonly List<DistrictPatrol> patrolOfficers = new();
        private PatrolStreetGraph patrolGraph;
        private NavigationState patrolGraphState;
        private float patrolDispatchAt;
        internal int PatrolGraphBuilds { get; private set; }
        internal void RegisterPatrol(DistrictPatrol officer) { if (!patrolOfficers.Contains(officer)) patrolOfficers.Add(officer); }
        internal void UnregisterPatrol(DistrictPatrol officer) => patrolOfficers.Remove(officer);
        internal void ReportHarass(DistrictHarass crime) { if (!patrolCrimes.Contains(crime)) patrolCrimes.Add(crime); }
        internal void ForgetHarass(DistrictHarass crime) => patrolCrimes.Remove(crime);
        internal PatrolStreetGraph PatrolGraph
        {
            get
            {
                var state = new NavigationState(this);
                if (patrolGraph != null && patrolGraphState.Equals(state)) return patrolGraph;
                patrolGraphState = state; PatrolGraphBuilds++;
                var paths = new List<(Vector2[] Points, bool Closed)>();
                foreach (var road in districtRoads)
                {
                    if (!districtRoadMeshes.ContainsKey(road)) continue;
                    var line = RoadCenterline(road);
                    if (line.Count < 2) continue;
                    var left = new Vector2[line.Count]; var right = new Vector2[line.Count];
                    for (int i = 0; i < line.Count; i++)
                    {
                        var previous = line[i == 0 ? (road.closed ? line.Count - 1 : 0) : i - 1];
                        var next = line[i == line.Count - 1 ? (road.closed ? 0 : i) : i + 1];
                        var direction = (next - previous).normalized;
                        var offset = new Vector2(-direction.y, direction.x) * (road.width * .5f + .45f);
                        left[i] = line[i] + offset; right[i] = line[i] - offset;
                    }
                    paths.Add((left, road.closed)); paths.Add((right, road.closed));
                    if (!road.closed)
                    {
                        paths.Add((new[] { left[0], right[0] }, false));
                        paths.Add((new[] { left[^1], right[^1] }, false));
                    }
                }
                // Crossing links join the two sidewalks at explicit street junctions.
                // All final links still require the ordinary dry, structure-safe certificate.
                foreach (var node in streetJunctionNodes)
                foreach (var arm in node.Arms)
                {
                    var offset = new Vector2(-arm.Direction.y, arm.Direction.x) * (arm.Road.width * .5f + .45f);
                    paths.Add((new[] { node.Point - offset, node.Point + offset }, false));
                }
                return patrolGraph = new PatrolStreetGraph(paths, PatrolClear);
            }
        }
        internal bool PatrolClear(Vector2 from, Vector2 to)
        {
            if (SelectedDistrict >= 0)
            {
                int count = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / 2));
                for (int i = 0; i <= count; i++) if (!InActiveDistrict(Vector2.Lerp(from, to, (float)i / count))) return false;
            }
            return ActorRouteClear(from, to, .25f, true);
        }
        internal void DispatchPatrolCrimes(float now)
        {
            if (now < patrolDispatchAt || patrolCrimes.Count == 0 || patrolOfficers.Count == 0) return;
            patrolDispatchAt = now + .5f;
            for (int i = patrolCrimes.Count - 1; i >= 0; i--)
                if (!patrolCrimes[i] || !patrolCrimes[i].IncidentActive) patrolCrimes.RemoveAt(i);
            foreach (var crime in patrolCrimes)
            {
                if (!crime || crime.RespondingOfficer || !crime.IncidentActive) continue;
                var point = new Vector2(crime.transform.position.x, crime.transform.position.z);
                var candidates = new List<(DistrictPatrol Officer, float Distance, int Order)>();
                for (int i = 0; i < patrolOfficers.Count; i++)
                {
                    var officer = patrolOfficers[i];
                    if (officer && officer.CanRespond) candidates.Add((officer, (officer.Point - point).sqrMagnitude, i));
                }
                candidates.Sort((a, b) => { int near = a.Distance.CompareTo(b.Distance); return near != 0 ? near : a.Order.CompareTo(b.Order); });
                foreach (var candidate in candidates)
                {
                    if (!PatrolGraph.TryRoute(candidate.Officer.Point, point, out var path)) continue;
                    if (crime.Reserve(candidate.Officer, now)) candidate.Officer.Respond(crime, path, now);
                    break;
                }
            }
        }
    }
}
