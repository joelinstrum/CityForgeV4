using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    internal enum HarborVesselKind { Galleon, CargoBarge, PirateShip }

    internal sealed class HarborVesselVisits : MonoBehaviour
    {
        private readonly struct VesselProfile
        {
            internal readonly HarborVesselKind Kind;
            internal readonly GameObject Prefab;
            internal readonly float Clearance, HalfLength, HalfBeam, Draft, Speed;

            internal VesselProfile(HarborVesselKind kind, GameObject prefab, float clearance, float halfLength, float halfBeam, float draft, float speed)
            {
                Kind = kind;
                Prefab = prefab;
                Clearance = clearance;
                HalfLength = halfLength;
                HalfBeam = halfBeam;
                Draft = draft;
                Speed = speed;
            }
        }
        private readonly struct DockingBerth
        {
            internal readonly Vector2 Approach, Position, Alongside;

            internal DockingBerth(Vector2 approach, Vector2 position, Vector2 alongside)
            {
                Approach = approach;
                Position = position;
                Alongside = alongside;
            }

            internal bool HasManeuver => Position != Approach;
        }

        private static readonly Vector2Int[] Neighbors =
            { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

        private MeadowDistrictLandscape land;
        private WildlifeWaterAccess water;
        private readonly List<VesselProfile> vessels = new();
        private GameObject vessel;
        private Material vesselMaterial;
        private Coroutine visits;
        private int nextPort, nextVessel;

        internal void Initialize(MeadowDistrictLandscape landscape)
        {
            land = landscape;
            water = new WildlifeWaterAccess(land);
            AddVessel(HarborVesselKind.Galleon, "Galleon/GalleonVisit", 12f, 9f, 3.15f, 2.2f, 8f);
            AddVessel(HarborVesselKind.CargoBarge, "WoodenBargeV01", 5.25f, 5f, 1.65f, .65f, 10f);
            AddVessel(HarborVesselKind.PirateShip, "PirateShip/PirateShipVisit", 11f, 8.5f, 2.65f, 1.8f, 9f);
            if (vessels.Count > 0 && isActiveAndEnabled) visits = StartCoroutine(VisitPorts());
        }

        private void AddVessel(HarborVesselKind kind, string resource, float clearance, float halfLength, float halfBeam,
            float draft, float speed)
        {
            var prefab = Resources.Load<GameObject>("Lots/HarborTraffic/" + resource);
            if (prefab) vessels.Add(new VesselProfile(kind, prefab, clearance, halfLength, halfBeam, draft, speed));
        }

        private void OnEnable()
        {
            if (vessels.Count > 0 && visits == null) visits = StartCoroutine(VisitPorts());
        }

        private void OnDisable()
        {
            if (visits != null) StopCoroutine(visits);
            visits = null;
            RemoveVessel();
        }

        private void OnDestroy()
        {
            if (vesselMaterial) Destroy(vesselMaterial);
        }

        private IEnumerator VisitPorts()
        {
            yield return new WaitForSeconds(2);
            var ports = new List<DistrictLotRecord>();
            var route = new List<Vector2>();
            while (true)
            {
                ports.Clear();
                foreach (var lot in land.View.PlacedLots)
                    if (land.Contains(lot.center) && lot.definition.AttractsVessels) ports.Add(lot);

                if (ports.Count == 0)
                {
                    yield return new WaitForSeconds(8);
                    continue;
                }

                var port = ports[nextPort++ % ports.Count];
                Vector2 center = port.center;
                int rotation = port.RotationSteps;
                var bodies = land.View.Data.WaterBodies;
                route.Clear();
                var profile = vessels[nextVessel];
                DockingBerth berth = default;
                for (int attempt = 0; attempt < vessels.Count && PortStillValid(); attempt++)
                {
                    profile = vessels[(nextVessel + attempt) % vessels.Count];
                    if (!TryFindBerth(port, profile, out berth)) continue;
                    yield return FindRoute(berth.Approach, profile, route);
                    if (route.Count > 1) break;
                }
                nextVessel = (nextVessel + 1) % vessels.Count;

                if (route.Count > 1 && PortStillValid())
                {
                    SpawnVessel(profile);
                    Vector2 first = route[0];
                    Vector2 heading = route[1] - first;
                    water.Wet(first, out _, out float level);
                    vessel.transform.SetPositionAndRotation(new Vector3(first.x, level, first.y), Heading(heading));
                    yield return Sail(route, profile.Speed, PortStillValid);
                    if (berth.HasManeuver && PortStillValid())
                    {
                        var arrival = route[^1] - route[^2];
                        var alongside = Vector2.Dot(arrival, berth.Alongside) < 0 ? -berth.Alongside : berth.Alongside;
                        yield return Dock(berth.Approach, berth.Position, alongside, PortStillValid);
                    }

                    var position = vessel.transform.position;
                    bool berthed = PortStillValid() &&
                        Vector2.Distance(new Vector2(position.x, position.z), berth.Position) < .1f;
                    var landing = berthed ? land.View.HarborLandingFor(port) : null;
                    var gangway = landing ? landing.Connect(vessel.transform, profile.Kind) : null;
                    var crew = berthed ? HarborDockWorkers.Begin(vessel.transform, land.View, port, profile.Kind) : null;
                    float leaveAt = Time.time + (crew ? Mathf.Max(10f, crew.VisitSeconds) : 10f);
                    while (Time.time < leaveAt && PortStillValid()) yield return null;
                    if (crew)
                    {
                        crew.gameObject.SetActive(false);
                        Destroy(crew.gameObject);
                    }
                    if (gangway)
                    {
                        gangway.SetActive(false);
                        Destroy(gangway);
                    }
                    if (PortStillValid())
                    {
                        if (berth.HasManeuver)
                        {
                            var forward = vessel.transform.forward;
                            yield return Dock(berth.Position, berth.Approach, new Vector2(forward.x, forward.z), PortStillValid);
                        }
                        route.Reverse();
                        yield return Sail(route, profile.Speed, PortStillValid);
                    }
                    RemoveVessel();
                }
                yield return new WaitForSeconds(5);

                bool PortStillValid() => land.View.Data.WaterBodies == bodies && port.center == center &&
                    port.RotationSteps == rotation && port.definition.AttractsVessels &&
                    land.View.PlacedLots.Contains(port);
            }
        }

        private bool TryFindBerth(DistrictLotRecord port, VesselProfile profile, out DockingBerth berth)
        {
            var landing = land.View.HarborLandingFor(port);
            if (landing && landing.TryGetPorchContact(out var contact, out var porchOutward) &&
                TryPorchBerth(port, profile, contact, porchOutward, out berth))
                return true;

            Vector2 half = port.definition.WaterfrontSize * .5f;
            var frontage = PrototypeRegionView.WaterfrontWorldPoint(port, new Vector2(0, half.y));
            var outward = (frontage - port.center).normalized;
            var tangent = new Vector2(outward.y, -outward.x);
            for (float reach = profile.Clearance + .75f; reach <= 42; reach += 3)
            {
                DockingBerth nearest = default;
                float bestScore = float.PositiveInfinity;
                for (int column = 0; column < 3; column++)
                {
                    float lateral = column == 0 ? 0 : (column == 1 ? -.75f : .75f) * half.x;
                    var edge = frontage + tangent * lateral;
                    var approach = edge + outward * reach;
                    if (!Navigable(approach, profile)) continue;
                    if (float.IsPositiveInfinity(bestScore))
                    {
                        nearest = new DockingBerth(approach, approach, tangent);
                        bestScore = reach;
                    }

                    // Turn in open water before easing in. Check the lower hull's
                    // shape at the berth so the bowsprit and rigging don't set the gap.
                    foreach (float angle in DockingAngles)
                    {
                        var axis = (tangent + outward * angle).normalized;
                        if (!DockingHullFits(approach, axis, profile)) continue;
                        for (float distance = reach - .5f; distance >= profile.HalfBeam + .55f; distance -= .5f)
                        {
                            var point = edge + outward * distance;
                            if (!DockingHullFits(point, axis, profile)) break;
                            float score = distance + Mathf.Abs(lateral) * .15f;
                            if (score >= bestScore) continue;
                            nearest = new DockingBerth(approach, point, axis);
                            bestScore = score;
                        }
                    }
                }
                if (!float.IsPositiveInfinity(bestScore))
                {
                    berth = nearest;
                    return true;
                }
            }
            berth = default;
            return false;
        }

        private bool TryPorchBerth(DistrictLotRecord port, VesselProfile profile, Vector2 contact,
            Vector2 outward, out DockingBerth berth)
        {
            const float overlap = .4f;
            var alongside = new Vector2(outward.y, -outward.x);
            var position = contact + outward * (profile.HalfBeam - overlap);
            berth = default;
            if (!DockingHullFits(position, alongside, profile, port)) return false;

            // Give the hull room to turn offshore before the final sideways approach.
            for (float reach = profile.Clearance + 3f; reach <= 45f; reach += 3f)
            {
                var approach = contact + outward * reach;
                if (!Navigable(approach, profile)) continue;
                int samples = Mathf.CeilToInt(Vector2.Distance(approach, position) / .75f);
                bool clear = true;
                for (int i = 0; i < samples && clear; i++)
                    clear = DockingHullFits(Vector2.Lerp(approach, position, i / (float)samples), alongside, profile, port);
                if (!clear) return false;
                berth = new DockingBerth(approach, position, alongside);
                return true;
            }
            return false;
        }

        // Small heading offsets let the hull follow a shoreline that is angled
        // relative to the lot grid.
        private static readonly float[] DockingAngles = { 0, -.27f, .27f, -.58f, .58f, -1f, 1f };

        private bool DockingHullFits(Vector2 center, Vector2 forward, VesselProfile profile, DistrictLotRecord porch = null)
        {
            var cross = new Vector2(-forward.y, forward.x);
            int stations = Mathf.CeilToInt(profile.HalfLength);
            for (int i = 0; i <= stations; i++)
            {
                float fraction = i * 2f / stations - 1f;
                var station = center + forward * (fraction * profile.HalfLength);
                float beam = profile.Kind == HarborVesselKind.CargoBarge ? profile.HalfBeam :
                    profile.HalfBeam * Mathf.Lerp(1f, .45f, Mathf.InverseLerp(.60f, 1f, Mathf.Abs(fraction)));
                for (int side = -1; side <= 1; side++)
                {
                    var sample = station + cross * (side * beam);
                    if (!DeepEnough(sample, profile.Draft) || land.View.HarborStoneOverlapsLot(sample, .55f, porch))
                        return false;
                }
            }
            return true;
        }

        private IEnumerator Dock(Vector2 from, Vector2 to, Vector2 alongside, System.Func<bool> portStillValid)
        {
            var rotation = Heading(alongside);
            while (portStillValid() && Quaternion.Angle(vessel.transform.rotation, rotation) > .5f)
            {
                vessel.transform.rotation = Quaternion.RotateTowards(vessel.transform.rotation, rotation, 50f * Time.deltaTime);
                yield return null;
            }

            float length = Vector2.Distance(from, to);
            for (float distance = 0; distance < length && portStillValid();)
            {
                distance += Time.deltaTime * 2f;
                var point = Vector2.Lerp(from, to, distance / length);
                if (!water.Wet(point, out _, out float level)) yield break;
                vessel.transform.SetPositionAndRotation(new Vector3(point.x, level, point.y), rotation);
                yield return null;
            }
        }

        private IEnumerator FindRoute(Vector2 berth, VesselProfile profile, List<Vector2> route)
        {
            // Search outward from the berth so the vessel enters at the nearest reachable
            // district edge. Limit the work per frame and simplify only across clear water.
            int side = Mathf.Clamp(Mathf.CeilToInt(land.Side / 7f), 8, 128);
            float step = land.Side / side;
            var parent = new int[side * side];
            System.Array.Fill(parent, -2);
            var open = new Queue<int>();
            Vector2 local = (berth - land.Origin) / step;
            int seedX = Mathf.FloorToInt(local.x), seedY = Mathf.FloorToInt(local.y);
            for (int y = Mathf.Max(0, seedY - 1); y <= Mathf.Min(side - 1, seedY + 1); y++)
            for (int x = Mathf.Max(0, seedX - 1); x <= Mathf.Min(side - 1, seedX + 1); x++)
            {
                int index = y * side + x;
                if (!ClearSegment(berth, Point(index), profile)) continue;
                parent[index] = -1;
                open.Enqueue(index);
            }

            int visited = 0;
            while (open.Count > 0)
            {
                int index = open.Dequeue();
                Vector2 point = Point(index);
                int x = index % side, y = index / side;
                foreach (var direction in Neighbors)
                {
                    int nx = x + direction.x, ny = y + direction.y;
                    if (nx < 0 || ny < 0 || nx >= side || ny >= side || !land.Contains(Point(ny * side + nx)))
                    {
                        if (Vector2.Distance(point, berth) < 16f) continue;
                        var path = new List<Vector2>();
                        for (int node = index; node >= 0; node = parent[node]) path.Add(Point(node));
                        path.Add(berth);
                        for (int node = 0; node < path.Count - 1;)
                        {
                            route.Add(path[node]);
                            int next = Mathf.Min(node + 12, path.Count - 1);
                            while (next > node + 1 && !ClearSegment(path[node], path[next], profile)) next--;
                            node = next;
                        }
                        route.Add(berth);
                        yield break;
                    }
                    int neighbor = ny * side + nx;
                    if (parent[neighbor] != -2 || !ClearSegment(point, Point(neighbor), profile)) continue;
                    parent[neighbor] = index;
                    open.Enqueue(neighbor);
                }
                if (++visited % 64 == 0) yield return null;
            }

            Vector2 Point(int index) =>
                land.Origin + new Vector2(index % side + .5f, index / side + .5f) * step;
        }

        private bool ClearSegment(Vector2 from, Vector2 to, VesselProfile profile)
        {
            int samples = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / 3f));
            for (int i = 1; i <= samples; i++)
                if (!Navigable(Vector2.Lerp(from, to, i / (float)samples), profile)) return false;
            return true;
        }

        private bool Navigable(Vector2 point, VesselProfile profile)
        {
            if (land.View.HarborStoneOverlapsLot(point, profile.Clearance) || !DeepEnough(point, profile.Draft)) return false;
            foreach (var direction in Neighbors)
                if (!DeepEnough(point + (Vector2)direction * profile.Clearance, profile.Draft)) return false;
            return true;
        }

        private bool DeepEnough(Vector2 point, float draft)
        {
            // Hull clearance can straddle the district boundary at entry. Terrain is
            // available on the rendered side, so use the boundary sample there.
            var local = point - land.Origin;
            var sample = land.Origin + new Vector2(Mathf.Clamp(local.x, .01f, land.Side - .01f),
                Mathf.Clamp(local.y, .01f, land.Side - .01f));
            if (!land.Contains(sample)) return false;
            float ground = land.Height(sample);
            return water.Wet(sample, ground, out var body, out float level) &&
                (body.kind == WaterBodyKind.Ocean || (body.kind == WaterBodyKind.River && !body.isStream)) &&
                level - ground >= draft;
        }

        private IEnumerator Sail(List<Vector2> route, float speed, System.Func<bool> portStillValid)
        {
            for (int i = 1; i < route.Count && portStillValid(); i++)
            {
                Vector2 from = route[i - 1], to = route[i];
                Quaternion heading = Heading(to - from);
                float length = Vector2.Distance(from, to);
                for (float travelled = 0; travelled < length && portStillValid();)
                {
                    travelled += Time.deltaTime * speed;
                    Vector2 point = Vector2.Lerp(from, to, travelled / length);
                    if (!water.Wet(point, out _, out float level)) yield break;
                    vessel.transform.SetPositionAndRotation(new Vector3(point.x, level, point.y),
                        Quaternion.RotateTowards(vessel.transform.rotation, heading, 85f * Time.deltaTime));
                    yield return null;
                }
            }
        }

        private static Quaternion Heading(Vector2 direction) =>
            Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y));

        private void SpawnVessel(VesselProfile profile)
        {
            vessel = Instantiate(profile.Prefab, transform);
            vessel.name = profile.Kind switch
            {
                HarborVesselKind.PirateShip => "Visiting pirate ship",
                HarborVesselKind.Galleon => "Visiting galleon",
                _ => "Visiting wooden cargo barge"
            };
            if (profile.Kind != HarborVesselKind.CargoBarge) return;
            foreach (var renderer in vessel.GetComponentsInChildren<MeshRenderer>())
            {
                var source = renderer.sharedMaterial;
                if (!source || source.shader.name != "Standard") continue;
                if (!vesselMaterial)
                {
                    // V3's material uses the Built-in pipeline. Retain its imported maps in URP.
                    vesselMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    vesselMaterial.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
                    vesselMaterial.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
                    vesselMaterial.EnableKeyword("_NORMALMAP");
                    vesselMaterial.SetFloat("_Smoothness", .25f);
                }
                if (vesselMaterial) renderer.sharedMaterial = vesselMaterial;
            }
        }

        private void RemoveVessel()
        {
            if (vessel)
            {
                vessel.SetActive(false);
                Destroy(vessel);
            }
            vessel = null;
        }
    }
}
