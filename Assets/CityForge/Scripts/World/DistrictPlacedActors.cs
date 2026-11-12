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

        internal static void Validate(DistrictPlacedActor[] actors, int districts, float mapSide)
        {
            if (actors == null) return;
            if (actors.Length > 512 || actors.Any(actor => actor == null ||
                string.IsNullOrWhiteSpace(actor.assetId) || actor.assetId.Length > 160 ||
                actor.district < 0 || actor.district >= districts ||
                !float.IsFinite(actor.position.x) || !float.IsFinite(actor.position.y) ||
                Mathf.Abs(actor.position.x) > mapSide * .5f || Mathf.Abs(actor.position.y) > mapSide * .5f))
                throw new InvalidDataException("Invalid placed district population.");
        }
    }

    public sealed partial class PrototypeRegionView
    {
        private const int MaxPlacedActors = 512;
        internal readonly List<DistrictPlacedActor> PlacedActors = new();
        private string armedDistrictActor;
        private bool armedPedestrian, armedTree;
        private Transform placedTreeRoot;
        private int armedActorCount;

        private sealed class Pedestrian
        {
            public Transform Body;
            public Animator Animator;
            public string IdleState, WalkState;
            public int IdleHash, WalkHash;
            public float Speed;
            public Vector2[] Route;
            public int Next, Direction = 1;
            public bool Closed;
        }

        private readonly List<Pedestrian> pedestrians = new();
        private readonly List<(Vector2[] Points, bool Closed, bool Sidewalk)> pedestrianRoutes = new();
        private Transform pedestrianRoot;
        private CityForgeCameraController pedestrianCamera;
        private bool pedestrianRoutesDirty;
        private float pedestrianTick;

        internal void ArmDistrictActor(string id, int count, bool pedestrian, bool tree = false)
        {
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
                if (pedestrianRoutesDirty) RebuildPedestrianRoutes();
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
            if (pedestrianRoot) { pedestrianRoot.gameObject.SetActive(false); Destroy(pedestrianRoot.gameObject); }
            pedestrianRoot = null;
            pedestrians.Clear();
            pedestrianRoutes.Clear();
            pedestrianRoutesDirty = true;
            if (SelectedDistrict < 0) return;
            var catalog = LotAssetCatalog.Load();
            if (!catalog) return;
            if (!PlacedActors.Any(actor => actor.pedestrian)) return;
            RebuildPedestrianRoutes();
            var active = new HashSet<int>(ActiveDistrictMembers);
            foreach (var record in PlacedActors)
                if (record.pedestrian && active.Contains(record.district) && catalog.Find(record.assetId) is { } asset &&
                    asset.prefab && asset.IsCharacter)
                    SetPedestrianRoute(CreatePedestrian(record, asset));
        }

        private void RebuildPlacedTrees()
        {
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
                    asset.prefab && asset.IsSeasonalTree)
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

        private void TickPlacedPedestrians()
        {
            if (!pedestrianRoot || SelectedDistrict < 0 || DistrictLoading || IsSaving) return;
            bool visible = pedestrianCamera && pedestrianCamera.ZoomLevel <= 2;
            if (pedestrianRoot.gameObject.activeSelf != visible) pedestrianRoot.gameObject.SetActive(visible);
            if (!visible) return;
            if (pedestrianRoutesDirty)
            {
                RebuildPedestrianRoutes();
                foreach (var walker in pedestrians) SetPedestrianRoute(walker);
            }
            pedestrianTick += Mathf.Min(Time.deltaTime, .1f);
            if (pedestrianTick < (pedestrianCamera.ZoomLevel < 2 ? 1f / 30f : .1f)) return;
            float elapsed = pedestrianTick;
            pedestrianTick = 0f;
            foreach (var walker in pedestrians)
            {
                // On reload or after visibility changes the Animator can still be in its default state.
                // Confirm locomotion before translating so an idle character cannot slide.
                if (walker.Route.Length < 2 || !SetPedestrianAnimation(walker, true)) continue;
                var p = walker.Body.position;
                var current = new Vector2(p.x, p.z);
                var target = walker.Route[walker.Next];
                var next = Vector2.MoveTowards(current, target, elapsed * walker.Speed);
                var delta = next - current;
                if (delta.sqrMagnitude > .000001f)
                {
                    walker.Body.position = PedestrianGround(next);
                    walker.Body.rotation = Quaternion.LookRotation(new Vector3(delta.x, 0, delta.y));
                }
                if (next != target) continue;
                int index = walker.Next + walker.Direction;
                if (walker.Closed) index = (index + walker.Route.Length) % walker.Route.Length;
                else if (index < 0 || index >= walker.Route.Length)
                {
                    walker.Direction = -walker.Direction;
                    index = walker.Next + walker.Direction;
                }
                walker.Next = index;
            }
        }
    }
}
