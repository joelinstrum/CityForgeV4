using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.CameraSystem;
using CityForge.Lots;
using CityForge.UI;
using UnityEngine;

namespace CityForge.World
{
    [Serializable]
    public sealed class DistrictPlacedActor
    {
        public string assetId;
        public int district;
        public Vector2 position;
        public bool pedestrian;
        public bool tree;
        public LotObjectDefinition streetscape;

        internal static void Validate(DistrictPlacedActor[] actors, int districts, float mapSide)
        {
            if (actors == null) return;
            if (actors.Length > 512 || actors.Any(actor => actor == null ||
                string.IsNullOrWhiteSpace(actor.assetId) || actor.assetId.Length > 160 ||
                actor.district < 0 || actor.district >= districts ||
                (actor.streetscape != null && (actor.tree || actor.pedestrian ||
                    actor.streetscape.assetId != actor.assetId || !float.IsFinite(actor.streetscape.yaw) ||
                    !float.IsFinite(actor.streetscape.fenceLength) || actor.streetscape.fenceLength < 0 ||
                    actor.streetscape.fenceLength > 192f)) ||
                !float.IsFinite(actor.position.x) || !float.IsFinite(actor.position.y) ||
                Mathf.Abs(actor.position.x) > mapSide * .5f || Mathf.Abs(actor.position.y) > mapSide * .5f))
                throw new InvalidDataException("Invalid placed district population.");
        }
    }

    public sealed partial class PrototypeRegionView
    {
        private const int MaxPlacedActors = 512;
        internal const string FarmerAssetId = "v3-farmer";
        internal readonly List<DistrictPlacedActor> PlacedActors = new();
        private string armedDistrictActor;
        private bool armedPedestrian, armedTree;
        private Transform placedTreeRoot;
        private readonly Dictionary<DistrictPlacedActor, GameObject> placedTreeViews = new();
        private int armedActorCount;

        private sealed class Pedestrian
        {
            public Transform Body;
            public Animator Animator;
            public string IdleState, WalkState;
            public int IdleHash, WalkHash;
            public float Speed;
            public Vector2[] Route;
            public Transform[] Wheels;
            public Transform Carriage;
            public Vector3 CarriageRestPosition;
            public Quaternion CarriageRestRotation;
            public float HorseYaw, CarriageYaw, TurnFrom, TurnTarget;
            public float TurnProgress, CarriageTurnFrom, CarriageTurnTarget, CarriageTurnProgress;
            public bool HeadingInitialized;
            public Vector2 PassingOffset;
            public int Next, Direction = 1, TrafficRank;
            public bool Closed, Ambient, Vehicle, EagleCarried;
            public ActorSimulationCadence Cadence;
        }

        private readonly List<Pedestrian> pedestrians = new();
        private readonly List<(Vector2[] Points, bool Closed, bool Sidewalk)> pedestrianRoutes = new();
        private Transform pedestrianRoot;
        private CityForgeCameraController pedestrianCamera;
        private bool pedestrianRoutesDirty;
        private float pedestrianTick;
        private int ambientTrafficCount;
        private RegionTimeOfDay? trafficVisibilityPhase;

        internal void ArmDistrictActor(string id, int count, bool pedestrian, bool tree = false)
        {
            if (pedestrian && id == FarmerAssetId) return;
            armedDistrictActor = id;
            armedActorCount = count;
            armedPedestrian = pedestrian;
            armedTree = tree;
            if (tree)
            {
                ShowDistrictMessage("Click dry ground to plant the tree · Esc to cancel");
                return;
            }
            ShowDistrictMessage(pedestrian ? "Click dry ground near a sidewalk or road · Esc to cancel"
                : "Click suitable terrain to place wildlife · Esc to cancel");
        }

        internal void CancelDistrictActor() => armedDistrictActor = null;

        private bool TryPlaceDistrictActor(Vector2 screen)
        {
            if (armedDistrictActor == null) return false;
            if (TownConstructionVisible || DistrictLoading || IsSaving || SelectedDistrict < 0) return true;
            if (!TryRoadGround(screen, out var point) || !InActiveDistrict(point)) return true;
            if (PlacedActors.Count + armedActorCount > MaxPlacedActors)
            {
                ShowDistrictMessage("The region has reached its placed population limit.");
                return true;
            }

            if (armedTree)
            {
                var asset = LotAssetCatalog.Load()?.Find(armedDistrictActor);
                var water = DistrictLotWater();
                bool wet = water != null ? water.Wet(point, out _, out _) : !IsTownLand(point / MapSide + Vector2.one * .5f);
                if (asset == null || !asset.IsSeasonalTree || wet || PickDistrictRoad(point) != null ||
                    PlacedLots.Any(lot => new DistrictLotFootprint(lot).Contains(point)) ||
                    PlacedActors.Any(actor => actor.tree && (actor.position - point).sqrMagnitude < 4f))
                {
                    ShowDistrictMessage("Plant on open, dry ground.");
                    return true;
                }
                var record = NewActor(point);
                PlacedActors.Add(record);
                PresentPlacedTree(record, asset);
                ShowDistrictMessage($"{asset.label} planted.");
            }
            else if (armedPedestrian)
            {
                var asset = LotAssetCatalog.Load()?.Find(armedDistrictActor);
                if (asset == null || !asset.prefab || !asset.IsCharacter) return true;
                var water = DistrictLotWater();
                if (water != null ? water.Wet(point, out _, out _) : !IsTownLand(point / MapSide + Vector2.one * .5f))
                {
                    ShowDistrictMessage("Place pedestrians on dry ground.");
                    return true;
                }
                var record = NewActor(point);
                PlacedActors.Add(record);
                var walker = CreatePedestrian(record, asset);
                if (pedestrianRoutesDirty)
                {
                    RebuildPedestrianRoutes();
                    foreach (var existing in pedestrians)
                        if (!existing.Ambient && existing != walker) SetPedestrianRoute(existing);
                    RebuildAmbientTraffic(LotAssetCatalog.Load());
                }
                SetPedestrianRoute(walker);
                ShowDistrictMessage(walker.Route.Length > 1 ? "Pedestrian placed." : "Character placed. Build a sidewalk or road nearby to start walking.");
            }
            else
            {
                if (!ApprovedDistrict) { ShowDistrictMessage("Wildlife placement requires a landscape district."); return true; }
                var library = Resources.Load<WildlifeLibrary>("Wildlife/TemperateWildlife");
                var species = library ? Array.Find(library.species, animal => animal != null && animal.id == armedDistrictActor) : null;
                if (species == null || !species.prefab) return true;
                var wildlife = ApprovedDistrict.EnsurePlacedWildlife();
                var sites = new List<Vector2> { point };
                for (int i = 1; i < armedActorCount; i++)
                {
                    float angle = i * 2.39996f;
                    sites.Add(point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (1.8f * Mathf.Sqrt(i)));
                }
                if (sites.Any(site => !InActiveDistrict(site) || !wildlife.Suitable(species, site, out _)))
                {
                    ShowDistrictMessage("Choose a spot with suitable habitat for the whole group.");
                    return true;
                }
                foreach (var site in sites)
                {
                    wildlife.AddPlacedAnimal(species, site);
                    PlacedActors.Add(NewActor(site));
                }
                ShowDistrictMessage(armedActorCount == 1 ? "Animal placed." : $"Placed {armedActorCount} animals.");
            }
            return true;

            DistrictPlacedActor NewActor(Vector2 position) => new DistrictPlacedActor
            {
                assetId = armedDistrictActor, pedestrian = armedPedestrian, tree = armedTree, position = position,
                district = DistrictAtWorld(new Vector3(position.x, 0f, position.y))
            };
        }

        private void RebuildPlacedPedestrians()
        {
            CancelDistrictActor();
            RebuildPlacedTrees();
            RebuildStreetscape();
            if (pedestrianRoot) { pedestrianRoot.gameObject.SetActive(false); Destroy(pedestrianRoot.gameObject); }
            pedestrianRoot = null;
            pedestrians.Clear();
            pedestrianRoutes.Clear();
            pedestrianRoutesDirty = true;
            if (SelectedDistrict < 0) return;
            var catalog = LotAssetCatalog.Load();
            if (!catalog) return;
            RebuildPedestrianRoutes();
            var active = new HashSet<int>(ActiveDistrictMembers);
            foreach (var record in PlacedActors)
                if (record.pedestrian && record.assetId != FarmerAssetId && active.Contains(record.district) && catalog.Find(record.assetId) is { } asset &&
                    asset.prefab && asset.IsCharacter)
                    SetPedestrianRoute(CreatePedestrian(record, asset));
            RebuildAmbientTraffic(catalog);
        }

        private void RebuildPlacedTrees()
        {
            placedTreeViews.Clear();
            if (placedTreeRoot)
            {
                placedTreeRoot.gameObject.SetActive(false);
                Destroy(placedTreeRoot.gameObject);
                placedTreeRoot = null;
            }

            if (SelectedDistrict < 0 || !PlacedActors.Any(actor => actor.tree)) return;
            var catalog = LotAssetCatalog.Load();
            if (!catalog) return;
            var active = new HashSet<int>(ActiveDistrictMembers);
            foreach (var record in PlacedActors)
                if (record.tree && active.Contains(record.district) && catalog.Find(record.assetId) is { } asset &&
                    asset.prefab && asset.category == "Trees")
                    PresentPlacedTree(record, asset);
        }

        private void PresentPlacedTree(DistrictPlacedActor record, LotAssetCatalogEntry asset)
        {
            if (!placedTreeRoot)
            {
                placedTreeRoot = new GameObject("Individually planted trees").transform;
                placedTreeRoot.SetParent(transform, false);
            }
            var instance = LotRuntimeLoader.InstantiateObject(new LotObjectDefinition { assetId = asset.id, scale = 1f },
                asset, placedTreeRoot, VisualSeason);
            instance.transform.position = new Vector3(record.position.x, LotHeight(record.position), record.position.y);
            placedTreeViews.Add(record, instance);
        }

        private Pedestrian CreatePedestrian(DistrictPlacedActor record, LotAssetCatalogEntry asset)
        {
            if (!pedestrianRoot)
            {
                pedestrianRoot = new GameObject("District pedestrians").transform;
                pedestrianRoot.SetParent(transform, false);
                pedestrianCamera = GetComponentInChildren<CityForgeCameraController>();
            }
            var body = LotRuntimeLoader.InstantiateObject(new LotObjectDefinition { assetId = asset.id, scale = 1f },
                asset, pedestrianRoot).transform;
            body.position = PedestrianGround(record.position);
            var native = Resources.Load<NativeCampAssets>("NativeCamps/Assets");
            NpcDefinition definition = native ? asset.id switch
            {
                "native-chief" => native.chief,
                "native-warrior" => native.warrior,
                "native-resident" => native.resident,
                _ => null
            } : null;
            var animator = body.GetComponentInChildren<Animator>();
            if (animator)
            {
                animator.applyRootMotion = false;
                animator.keepAnimatorStateOnDisable = true;
                animator.cullingMode = AnimatorCullingMode.CullCompletely;
            }
            var walker = new Pedestrian
            {
                Body = body, Animator = animator,
                IdleState = definition ? definition.idleState : string.IsNullOrEmpty(asset.idleState) ? "Idle" : asset.idleState,
                WalkState = definition ? definition.walkState : "Walk",
                Speed = definition ? definition.walkSpeed : 1.15f
            };
            pedestrians.Add(walker);
            return walker;
        }

        private void RebuildAmbientTraffic(LotAssetCatalog catalog)
        {
            for (int i = pedestrians.Count - 1; i >= 0; i--)
            {
                if (!pedestrians[i].Ambient) continue;
                pedestrians[i].Body.gameObject.SetActive(false);
                Destroy(pedestrians[i].Body.gameObject);
                pedestrians.RemoveAt(i);
            }

            ambientTrafficCount = 0;
            trafficVisibilityPhase = null;
            var buildings = districtLotRoots.Keys.Where(lot =>
                lot.definition.objects.Any(item => catalog && catalog.Find(item.assetId)?.category == "Buildings"))
                .OrderBy(lot => lot.center.y).ThenBy(lot => lot.center.x).ToArray();
            if (!catalog || buildings.Length == 0) return;
            var blocks = ResidentialBlockRoutes();
            var pools = new Dictionary<LotWealth, LotAssetCatalogEntry[]>
            {
                [LotWealth.Low] = Pool("v3-hooligan", "v3-axeman"),
                [LotWealth.Medium] = Pool("v3-dock-worker"),
                [LotWealth.High] = Pool("v3-victorian-gentleman")
            };
            var foundersLow = Pool("v3-founders-cook", "v3-founders-stout-townsperson",
                "v3-founders-townsperson", "v3-founders-privateer");
            var foundersHigh = Pool("v3-founders-gentleman-black", "v3-founders-gentleman-blue",
                "v3-founders-lady-gray", "v3-founders-lady-red");
            int districtYear = TownYear(SelectedDistrict);
            string vehicleEra = CityForgeTimeline.Eras.FirstOrDefault(era =>
                districtYear >= era.FirstYear && districtYear <= era.LastYear)?.id;
            var vehicleGroups = VehicleProfile.All.Where(profile => profile.spawnEvery > 0 && profile.AvailableIn(vehicleEra))
                .Select(profile => (profile, asset: catalog.Find(profile.assetId)))
                .Where(entry => entry.asset != null && entry.asset.prefab)
                .GroupBy(entry => (group: string.IsNullOrEmpty(entry.profile.trafficGroup) ? entry.profile.assetId :
                    entry.profile.trafficGroup, every: entry.profile.spawnEvery, parked: entry.profile.parked))
                .Select(group => (group.Key, choices: group.ToArray())).ToArray();
            var vehicleCounts = new int[vehicleGroups.Length];
            var vehicleRoutes = districtRoadMeshes.Keys.Select(road =>
                (Points: RoadCenterline(road).ToArray(), Closed: road.closed)).Where(route => route.Points.Length > 1).ToArray();
            var random = new System.Random(SelectedDistrict);
            var walking = blocks.Select(route => (Points: route, Closed: true))
                .Concat(pedestrianRoutes.Select(route => (route.Points, route.Closed))).ToArray();
            foreach (var lot in buildings)
            {
                var wealth = lot.definition.zoningLayout.Configured
                    ? lot.definition.zoningLayout.wealth : lot.definition.wealth;
                var zone = lot.definition.zoningLayout.zone;
                if (zone == TownZone.None && LotClassification.FindCategory(lot.definition.category)?.id == "commercial")
                    zone = TownZone.Commercial1;
                var eligibleVehicles = vehicleGroups.Select(group => group.choices
                    .Where(entry => entry.profile.AllowsTraffic(vehicleEra, zone, wealth)).ToArray()).ToArray();
                var era = ResidentialGround.LotEra(lot.definition);
                var pool = pools.GetValueOrDefault(wealth) ?? pools[LotWealth.Low];
                if (era == "founders")
                {
                    if (wealth == LotWealth.High && foundersHigh.Length > 0) pool = foundersHigh;
                    if (wealth == LotWealth.Low && foundersLow.Length > 0 &&
                        lot.definition.zoningLayout.zone is TownZone.Residential1 or TownZone.Commercial1)
                        pool = foundersLow;
                }
                if (pool.Length == 0) pool = pools.Values.SelectMany(entries => entries).ToArray();
                var root = districtLotRoots[lot];
                foreach (var item in lot.definition.objects.Where(item => catalog.Find(item.assetId)?.category == "Buildings"))
                {
                    var position = root.TransformPoint(new Vector3(item.position.x, 0, item.position.y));
                    var origin = new Vector2(position.x, position.z);
                    if (pool.Length > 0)
                    {
                        var route = FindTrafficRoute(origin, walking);
                        if (route.points == null)
                        {
                            var frontage = lot.definition.LandBounds;
                            var local = new Vector2(frontage.center.x, frontage.yMin + .5f);
                            var facing = (int)lot.definition.zoningLayout.frontage;
                            local = facing == 0 ? local : LotGroundOverlay.QuarterTurn(local - frontage.center, facing) + frontage.center;
                            var outside = root.TransformPoint(new Vector3(local.x, 0, local.y));
                            route.start = new Vector2(outside.x, outside.z);
                        }
                        SpawnTraffic(pool[random.Next(pool.Length)], route.start, route.points, route.next, route.closed, random);
                    }
                    for (int i = 0; i < vehicleGroups.Length; i++)
                    {
                        var eligible = eligibleVehicles[i];
                        var group = vehicleGroups[i].Key;
                        if (eligible.Length == 0 || ++vehicleCounts[i] % group.every != 0) continue;
                        var choice = eligible[random.Next(eligible.Length)].asset;
                        if (!group.parked)
                        {
                            var route = FindTrafficRoute(origin, vehicleRoutes);
                            if (route.points != null)
                                SpawnTraffic(choice, route.start, route.points, route.next, route.closed, random, true);
                            continue;
                        }
                        var bounds = lot.definition.LandBounds;
                        var local = new Vector3(bounds.xMax - .7f, 0, bounds.yMin + 1.5f);
                        var world = root.TransformPoint(local);
                        var point = new Vector2(world.x, world.z);
                        if (StreetscapePlacementProblem(new LotObjectDefinition(), point, DistrictLotWater()) == null)
                        {
                            var vehicle = SpawnTraffic(choice, point, null, 0, false, random, true);
                            vehicle.Body.rotation = root.rotation;
                        }
                    }
                }
            }

            var traffic = pedestrians.Where(actor => actor.Ambient).ToArray();
            ambientTrafficCount = traffic.Length;
            for (int i = traffic.Length - 1; i > 0; i--)
            {
                int swap = random.Next(i + 1);
                (traffic[i], traffic[swap]) = (traffic[swap], traffic[i]);
            }
            for (int i = 0; i < traffic.Length; i++) traffic[i].TrafficRank = i;
            RefreshTrafficVisibility();

            LotAssetCatalogEntry[] Pool(params string[] ids) => ids.Select(catalog.Find).Where(asset =>
                asset != null && asset.prefab && asset.prefab.GetComponentInChildren<Animator>() is { } animator &&
                animator.runtimeAnimatorController && animator.runtimeAnimatorController.animationClips.Any(clip =>
                    clip.name.Contains("walk", StringComparison.OrdinalIgnoreCase))).ToArray();
        }

        private List<Vector2[]> ResidentialBlockRoutes()
        {
            // Split road edges at crossings, including a road ending on another edge.
            var segments = new List<(Vector2 A, Vector2 B, float Width, List<float> Cuts)>();
            foreach (var road in districtRoadMeshes.Keys)
            for (int i = 0; i < road.points.Length - (road.closed ? 0 : 1); i++)
            {
                var a = road.points[i];
                var b = road.points[(i + 1) % road.points.Length];
                if ((b - a).sqrMagnitude > .01f)
                    segments.Add((a, b, road.width, new List<float> { 0f, 1f }));
            }

            for (int i = 0; i < segments.Count; i++)
            for (int j = i + 1; j < segments.Count; j++)
            {
                var a = segments[i];
                var b = segments[j];
                var ab = a.B - a.A;
                var cd = b.B - b.A;
                float cross = Cross(ab, cd);
                if (Mathf.Abs(cross) < .0001f) continue;
                float t = Cross(b.A - a.A, cd) / cross;
                float u = Cross(b.A - a.A, ab) / cross;
                if (t < 0f || t > 1f || u < 0f || u > 1f) continue;
                a.Cuts.Add(t);
                b.Cuts.Add(u);
            }

            var graph = new Dictionary<Vector2, Dictionary<Vector2, float>>();
            foreach (var segment in segments)
            {
                segment.Cuts.Sort();
                for (int i = 1; i < segment.Cuts.Count; i++)
                {
                    var a = Quantize(Vector2.Lerp(segment.A, segment.B, segment.Cuts[i - 1]));
                    var b = Quantize(Vector2.Lerp(segment.A, segment.B, segment.Cuts[i]));
                    if (a == b) continue;
                    Connect(a, b, segment.Width);
                    Connect(b, a, segment.Width);
                }
            }

            // Dead ends cannot belong to a block. Remove them before choosing turns.
            var deadEnds = new Queue<Vector2>(graph.Where(node => node.Value.Count < 2).Select(node => node.Key));
            while (deadEnds.Count > 0)
            {
                var end = deadEnds.Dequeue();
                if (!graph.Remove(end, out var neighbors)) continue;
                foreach (var neighbor in neighbors.Keys)
                {
                    graph[neighbor].Remove(end);
                    if (graph[neighbor].Count < 2) deadEnds.Enqueue(neighbor);
                }
            }

            var routes = new List<Vector2[]>();
            var visited = new HashSet<(Vector2 from, Vector2 to)>();
            foreach (var (start, neighbors) in graph)
            foreach (var first in neighbors.Keys)
            {
                var edge = (from: start, to: first);
                var ring = new List<Vector2>();
                while (visited.Add(edge))
                {
                    ring.Add(edge.from);
                    var incoming = edge.to - edge.from;
                    var next = edge.from;
                    float turn = 181f;
                    foreach (var candidate in graph[edge.to].Keys)
                    {
                        if (candidate == edge.from) continue;
                        float angle = Vector2.SignedAngle(incoming, candidate - edge.to);
                        if (angle >= turn) continue;
                        turn = angle;
                        next = candidate;
                    }
                    edge = (edge.to, next);
                }
                if (edge != (start, first) || ring.Count < 3) continue;
                float area = 0f;
                for (int i = 0; i < ring.Count; i++)
                    area += Cross(ring[i] - start, ring[(i + 1) % ring.Count] - start);
                if (area >= -1f) continue;

                var shoulder = new Vector2[ring.Count];
                for (int i = 0; i < ring.Count; i++)
                {
                    var at = ring[i];
                    var previous = ring[(i + ring.Count - 1) % ring.Count];
                    var next = ring[(i + 1) % ring.Count];
                    var incoming = Right((at - previous).normalized);
                    var outgoing = Right((next - at).normalized);
                    var corner = (incoming + outgoing).normalized;
                    float setback = Mathf.Max(graph[at][previous], graph[at][next]) * .5f + .45f;
                    shoulder[i] = at + corner * (setback / Mathf.Max(.5f, Vector2.Dot(corner, outgoing)));
                }
                routes.Add(shoulder);
            }
            return routes;

            void Connect(Vector2 a, Vector2 b, float width)
            {
                if (!graph.TryGetValue(a, out var neighbors)) graph[a] = neighbors = new Dictionary<Vector2, float>();
                neighbors[b] = Mathf.Max(width, neighbors.GetValueOrDefault(b));
            }
            static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
            static Vector2 Right(Vector2 direction) => new Vector2(direction.y, -direction.x);
            static Vector2 Quantize(Vector2 point) => (Vector2)Vector2Int.RoundToInt(point * 100f) * .01f;
        }

        private Pedestrian SpawnTraffic(LotAssetCatalogEntry asset, Vector2 start, Vector2[] route,
            int next, bool closed, System.Random random, bool vehicle = false)
        {
            if (route is { Length: > 1 })
            {
                int previous = (next - 1 + route.Length) % route.Length;
                var edge = route[next] - route[previous];
                float length = edge.magnitude;
                if (length > .001f)
                {
                    float spread = vehicle ? 9f : 4f;
                    float distance = Vector2.Distance(route[previous], start);
                    float randomized = Mathf.Lerp(Mathf.Max(0f, distance - spread),
                        Mathf.Min(length, distance + spread), (float)random.NextDouble());
                    start = route[previous] + edge * (randomized / length);
                }
            }
            var actor = CreatePedestrian(new DistrictPlacedActor { position = start }, asset);
            actor.Ambient = true;
            actor.Vehicle = vehicle;
            actor.Route = route ?? Array.Empty<Vector2>();
            actor.Next = next;
            actor.Closed = closed;
            if (vehicle)
            {
                var profile = VehicleProfile.Find(asset.id);
                float variation = Mathf.Clamp01(profile?.speedVariation ?? 0f);
                actor.Speed = (profile?.speed ?? 0f) * Mathf.Lerp(1f - variation, 1f + variation, (float)random.NextDouble());
                SetVehicleMotion(actor.Body, asset.id, actor.Route.Length > 1);
                foreach (var animator in actor.Body.GetComponentsInChildren<Animator>())
                    animator.cullingMode = AnimatorCullingMode.CullCompletely;
                if (profile?.team == true && actor.Body.childCount > 0 && actor.Body.GetChild(0).childCount > 1)
                {
                    actor.Carriage = actor.Body.GetChild(0).GetChild(0);
                    actor.CarriageRestPosition = actor.Carriage.localPosition;
                    actor.CarriageRestRotation = actor.Carriage.localRotation;
                }
                if (profile?.rollWheels == true)
                    actor.Wheels = actor.Body.GetComponentsInChildren<Transform>().Where(part =>
                        part.name is "Carriage_Front_Left" or "Carriage_Front_Right" or
                        "Carriage_Rear_Left" or "Carriage_Rear_Right").ToArray();
            }
            else actor.Speed *= Mathf.Lerp(.83f, 1.17f, (float)random.NextDouble());
            return actor;
        }

        private static (Vector2 start, Vector2[] points, int next, bool closed) FindTrafficRoute(Vector2 origin,
            IEnumerable<(Vector2[] Points, bool Closed)> routes)
        {
            (Vector2 start, Vector2[] points, int next, bool closed) best = (origin, null, 0, false);
            float nearest = float.PositiveInfinity;
            foreach (var (points, closed) in routes)
                for (int i = 0; i < points.Length - (closed ? 0 : 1); i++)
                {
                    int end = (i + 1) % points.Length;
                    var edge = points[end] - points[i];
                    if (edge.sqrMagnitude < .01f) continue;
                    var point = points[i] + edge * Mathf.Clamp01(Vector2.Dot(origin - points[i], edge) / edge.sqrMagnitude);
                    float distance = (point - origin).sqrMagnitude;
                    if (distance >= nearest) continue;
                    nearest = distance;
                    best = (point, points, end, closed);
                }
            return best;
        }

        internal static void SetVehicleMotion(Transform body, string assetId, bool travelling)
        {
            var profile = VehicleProfile.Find(assetId);
            if (profile == null || string.IsNullOrEmpty(profile.motionFolder) || body.childCount == 0) return;
            string folder = $"Lots/V3Vehicles/{profile.motionFolder}/";
            var controller = Resources.Load<RuntimeAnimatorController>(folder + "Motion");
            if (!controller) return;
            var model = body.GetChild(0);
            if (profile.team)
            {
                for (int i = 1; i < model.childCount; i++) Animate(model.GetChild(i));
            }
            else Animate(model);

            void Animate(Transform part)
            {
                var animators = part.GetComponentsInChildren<Animator>();
                if (animators.Length == 0 && profile.team)
                {
                    var avatar = Resources.LoadAll<Avatar>(folder + "Model").FirstOrDefault();
                    if (!avatar) return;
                    var animator = part.gameObject.AddComponent<Animator>();
                    animator.avatar = avatar;
                    animators = new[] { animator };
                }
                foreach (var animator in animators)
                {
                    animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false;
                    animator.keepAnimatorStateOnDisable = true;
                    string state = travelling ? profile.travelState : profile.idleState;
                    if (!string.IsNullOrEmpty(state)) animator.Play(state);
                }
            }
        }

        private Vector3 PedestrianGround(Vector2 point) => new Vector3(point.x, LotHeight(point) + .04f, point.y);

        private void SetPedestrianRoute(Pedestrian walker)
        {
            var at = walker.Body.position;
            var point = new Vector2(at.x, at.z);
            Vector2[] route = null;
            Vector2 nearest = point;
            int next = 0;
            bool closed = false;
            float best = 8f * 8f;

            foreach (var candidate in pedestrianRoutes)
                if (candidate.Sidewalk) Consider(candidate.Points, candidate.Closed);
            if (route == null)
            {
                best = 12f * 12f;
                foreach (var candidate in pedestrianRoutes)
                    if (!candidate.Sidewalk) Consider(candidate.Points, candidate.Closed);
            }

            walker.Route = route ?? Array.Empty<Vector2>();
            walker.Next = next;
            walker.Closed = closed;
            walker.Direction = 1;
            if (route != null) walker.Body.position = PedestrianGround(nearest);
            SetPedestrianAnimation(walker, route != null);

            void Consider(Vector2[] candidate, bool loop)
            {
                for (int i = 0; i < candidate.Length - (loop ? 0 : 1); i++)
                {
                    var start = candidate[i];
                    var end = (i + 1) % candidate.Length;
                    var edge = candidate[end] - start;
                    if (edge.sqrMagnitude < .01f) continue;
                    var projected = start + edge * Mathf.Clamp01(Vector2.Dot(point - start, edge) / edge.sqrMagnitude);
                    float distance = (point - projected).sqrMagnitude;
                    if (distance >= best) continue;
                    best = distance;
                    nearest = projected;
                    next = end;
                    route = candidate;
                    closed = loop;
                }
            }
        }

        private static bool SetPedestrianAnimation(Pedestrian walker, bool walking)
        {
            var animator = walker.Animator;
            if (!animator || !animator.isActiveAndEnabled || !animator.isInitialized || animator.layerCount == 0)
                return false;
            if (walker.WalkHash == 0)
            {
                var prefix = animator.GetLayerName(0) + ".";
                walker.WalkHash = Animator.StringToHash(prefix + walker.WalkState);
                walker.IdleHash = Animator.StringToHash(prefix + walker.IdleState);
            }
            int state = walking ? walker.WalkHash : walker.IdleHash;
            if (animator.GetCurrentAnimatorStateInfo(0).fullPathHash == state && !animator.IsInTransition(0))
                return true;
            if (!animator.HasState(0, state)) return false;
            animator.Play(state, 0, 0f);
            animator.Update(0f);
            return animator.GetCurrentAnimatorStateInfo(0).fullPathHash == state;
        }

        private void RebuildPedestrianRoutes()
        {
            pedestrianRoutes.Clear();
            pedestrianRoutesDirty = false;
            foreach (var (lot, root) in districtLotRoots)
            {
                foreach (var overlay in lot.definition.overlays)
                    if (overlay.texture == "worn-sidewalk") Sidewalk(overlay.Bounds, root);
                if (lot.definition.overlayTexture == "worn-sidewalk")
                    Sidewalk(lot.definition.LandBounds, root);
            }

            foreach (var road in districtRoadMeshes.Keys)
            {
                var line = RoadCenterline(road);
                if (line.Count < 2) continue;
                for (int side = -1; side <= 1; side += 2)
                {
                    var shoulder = new Vector2[line.Count];
                    for (int i = 0; i < line.Count; i++)
                    {
                        var previous = line[i == 0 ? (road.closed ? line.Count - 1 : 0) : i - 1];
                        var following = line[i == line.Count - 1 ? (road.closed ? 0 : i) : i + 1];
                        var direction = (following - previous).normalized;
                        shoulder[i] = line[i] + new Vector2(-direction.y, direction.x) * (side * (road.width * .5f + .45f));
                    }
                    pedestrianRoutes.Add((shoulder, road.closed, false));
                }
            }
            void Sidewalk(Rect bounds, Transform root)
            {
                if (Mathf.Min(bounds.width, bounds.height) > 5f) return;
                var direction = bounds.width >= bounds.height ? Vector2.right * bounds.width : Vector2.up * bounds.height;
                pedestrianRoutes.Add((new[] { ToWorld(bounds.center - direction * .5f),
                    ToWorld(bounds.center + direction * .5f) }, false, true));
                Vector2 ToWorld(Vector2 local)
                {
                    var world = root.TransformPoint(new Vector3(local.x, 0, local.y));
                    return new Vector2(world.x, world.z);
                }
            }

        }

        private void RefreshTrafficVisibility()
        {
            if (trafficVisibilityPhase == TimeOfDay) return;
            trafficVisibilityPhase = TimeOfDay;
            int limit = TimeOfDay switch
            {
                RegionTimeOfDay.Morning => (ambientTrafficCount + 2) / 4,
                RegionTimeOfDay.Evening => (ambientTrafficCount + 1) / 2,
                RegionTimeOfDay.Night => 0,
                _ => ambientTrafficCount
            };
            foreach (var actor in pedestrians)
            {
                bool active = !actor.Ambient || actor.TrafficRank < limit;
                if (actor.Body.gameObject.activeSelf != active) actor.Body.gameObject.SetActive(active);
            }
        }

        private readonly List<(Vector2 center, Vector2 forward)> wagonTraffic = new();

        private void TickPlacedPedestrians()
        {
            if (SelectedDistrict < 0 || DistrictLoading || IsSaving) return;
            if (pedestrianRoutesDirty)
            {
                RebuildPedestrianRoutes();
                foreach (var walker in pedestrians)
                    if (!walker.Ambient) SetPedestrianRoute(walker);
                RebuildAmbientTraffic(LotAssetCatalog.Load());
            }
            if (!pedestrianRoot) return;
            RefreshTrafficVisibility();
            bool visible = pedestrianCamera && pedestrianCamera.ZoomLevel <= 2;
            if (pedestrianRoot.gameObject.activeSelf != visible) pedestrianRoot.gameObject.SetActive(visible);
            if (!visible) return;
            pedestrianTick += Mathf.Min(Time.deltaTime, .1f);
            if (pedestrianTick < (pedestrianCamera.ZoomLevel < 2 ? 1f / 30f : .1f)) return;
            float elapsed = pedestrianTick;
            pedestrianTick = 0f;
            var camera = pedestrianCamera.GetComponent<Camera>();
            wagonTraffic.Clear();
            foreach (var wagon in pedestrians)
            {
                if (!wagon.Carriage || !wagon.Body.gameObject.activeSelf) continue;
                var center = wagon.Body.position - wagon.Body.forward * 1.3f;
                wagonTraffic.Add((new Vector2(center.x, center.z), new Vector2(wagon.Body.forward.x, wagon.Body.forward.z)));
            }
            foreach (var walker in pedestrians)
            {
                if (!walker.Body.gameObject.activeSelf || walker.EagleCarried || walker.Route.Length < 2) continue;
                var p = walker.Body.position;
                bool nearCamera = ActorSimulationVisibility.NearCamera(camera, p);
                float simulationTime = walker.Cadence.Step(elapsed, nearCamera, walker.Body.GetInstanceID());
                if (simulationTime <= 0f) continue;
                // A culled Animator may still report its previous state as it returns to view.
                if (nearCamera && !walker.Vehicle && !SetPedestrianAnimation(walker, true)) continue;
                var current = new Vector2(p.x, p.z) - walker.PassingOffset;
                float budget = simulationTime * walker.Speed;
                if (walker.Carriage && walker.HeadingInitialized)
                {
                    var nextHeading = (walker.Route[walker.Next] - current).normalized;
                    if (nextHeading != Vector2.zero &&
                        Mathf.Abs(Mathf.DeltaAngle(walker.HorseYaw, Mathf.Atan2(nextHeading.x, nextHeading.y) * Mathf.Rad2Deg)) > 30f)
                    {
                        TurnHorseTeam(walker, nextHeading, simulationTime);
                        continue;
                    }
                }
                if (walker.Vehicle && !walker.Carriage)
                    budget = SteerAroundWagons(walker, current, simulationTime, budget);
                var heading = Vector2.zero;
                float travelled = 0f;
                for (int segment = 0; segment < walker.Route.Length && budget > 0f; segment++)
                {
                    var target = walker.Route[walker.Next];
                    var next = Vector2.MoveTowards(current, target, budget);
                    var delta = next - current;
                    float distance = delta.magnitude;
                    if (distance > .0001f) heading = delta / distance;
                    budget -= distance;
                    travelled += distance;
                    current = next;
                    if (next != target) break;
                    int index = walker.Next + walker.Direction;
                    if (walker.Closed) index = (index + walker.Route.Length) % walker.Route.Length;
                    else if (index < 0 || index >= walker.Route.Length)
                    {
                        walker.Direction = -walker.Direction;
                        index = walker.Next + walker.Direction;
                    }
                    walker.Next = index;
                }
                var destination = current + walker.PassingOffset;
                walker.Body.position = PedestrianGround(destination);
                if (walker.Carriage)
                    TurnHorseTeam(walker, heading, simulationTime);
                else
                {
                    var motion = walker.Vehicle ? destination - new Vector2(p.x, p.z) : heading;
                    if (motion.sqrMagnitude > .00001f)
                        walker.Body.rotation = Quaternion.LookRotation(new Vector3(motion.x, 0, motion.y));
                }
                if (walker.Wheels != null)
                    foreach (var wheel in walker.Wheels)
                        wheel.Rotate(wheel.InverseTransformDirection(walker.Body.right),
                            travelled * Mathf.Rad2Deg / .48f, Space.Self);
            }
        }
        private float SteerAroundWagons(Pedestrian rider, Vector2 position, float delta, float budget)
        {
            var direction = (rider.Route[rider.Next] - position).normalized;
            if (direction == Vector2.zero) direction = new Vector2(rider.Body.forward.x, rider.Body.forward.z);
            var right = new Vector2(direction.y, -direction.x);
            Vector2 targetOffset = Vector2.zero;
            float nearest = float.PositiveInfinity;
            float blockingDistance = float.PositiveInfinity;
            foreach (var (center, forward) in wagonTraffic)
            {
                var relative = center - position;
                float forwardDistance = Vector2.Dot(relative, direction);
                float sideDistance = Vector2.Dot(relative, right);
                if (forwardDistance < -4.25f || forwardDistance > 13f ||
                    Mathf.Abs(sideDistance) > 2.6f || relative.sqrMagnitude >= nearest) continue;
                nearest = relative.sqrMagnitude;
                float side = sideDistance > .4f ? -1f : 1f;
                // Keep the selected side while alongside the team.
                if (rider.PassingOffset.sqrMagnitude > .2f)
                    side = Vector2.Dot(rider.PassingOffset, right) < 0 ? -1f : 1f;
                targetOffset = right * (side * 1.85f);
                float reach = Mathf.Lerp(2.3f, 4.1f, Mathf.Abs(Vector2.Dot(forward, direction)));
                if (forwardDistance > 0f) blockingDistance = Mathf.Max(0f, forwardDistance - reach);
            }
            rider.PassingOffset = Vector2.MoveTowards(rider.PassingOffset, targetOffset, 1.7f * delta);
            if (targetOffset != Vector2.zero && Vector2.Dot(rider.PassingOffset, targetOffset.normalized) < 1.65f)
                budget = Mathf.Min(budget, blockingDistance);
            return budget;
        }

        private static void TurnHorseTeam(Pedestrian team, Vector2 heading, float elapsed)
        {
            if (heading != Vector2.zero)
            {
                float yaw = Mathf.Atan2(heading.x, heading.y) * Mathf.Rad2Deg;
                if (!team.HeadingInitialized)
                {
                    team.HeadingInitialized = true;
                    team.HorseYaw = team.CarriageYaw = team.TurnFrom = team.TurnTarget = yaw;
                    team.TurnProgress = team.CarriageTurnProgress = 1f;
                    team.CarriageTurnFrom = team.CarriageTurnTarget = yaw;
                }
                else if (Mathf.Abs(Mathf.DeltaAngle(team.TurnTarget, yaw)) >
                    (team.TurnProgress < 1f ? 12f : .5f))
                {
                    team.TurnFrom = team.HorseYaw;
                    team.TurnTarget = yaw;
                    team.TurnProgress = 0f;
                }
            }
            if (!team.HeadingInitialized) return;
            float turn = Mathf.DeltaAngle(team.TurnFrom, team.TurnTarget);
            float duration = Mathf.Max(.15f, Mathf.Abs(turn) / 100f);
            team.TurnProgress = Mathf.Min(1f, team.TurnProgress + elapsed / duration);
            team.HorseYaw = StagedTurn(team.TurnFrom, turn, team.TurnProgress);
            if (team.TurnProgress >= 1f)
            {
                // The wagon starts its own turn only once the horse faces down the new street.
                if (Mathf.Abs(Mathf.DeltaAngle(team.CarriageTurnTarget, team.TurnTarget)) > .5f)
                {
                    team.CarriageTurnFrom = team.CarriageYaw;
                    team.CarriageTurnTarget = team.TurnTarget;
                    team.CarriageTurnProgress = 0f;
                }
                float carriageTurn = Mathf.DeltaAngle(team.CarriageTurnFrom, team.CarriageTurnTarget);
                team.CarriageTurnProgress = Mathf.Min(1f, team.CarriageTurnProgress +
                    elapsed / Mathf.Max(.15f, Mathf.Abs(carriageTurn) / 65f));
                team.CarriageYaw = StagedTurn(team.CarriageTurnFrom, carriageTurn, team.CarriageTurnProgress);
            }
            team.Body.rotation = Quaternion.Euler(0, team.HorseYaw, 0);
            var hinge = Quaternion.Euler(0, Mathf.DeltaAngle(team.HorseYaw, team.CarriageYaw), 0);
            team.Carriage.localRotation = hinge * team.CarriageRestRotation;
            team.Carriage.localPosition = hinge * team.CarriageRestPosition;
        }

        private static float StagedTurn(float from, float angle, float progress)
        {
            float half = Mathf.Min(45f, Mathf.Abs(angle) * .5f) * Mathf.Sign(angle);
            return from + (progress <= .5f
                ? Mathf.Lerp(0f, half, Mathf.SmoothStep(0f, 1f, progress * 2f))
                : Mathf.Lerp(half, angle, Mathf.SmoothStep(0f, 1f, (progress - .5f) * 2f)));
        }

    }
}
