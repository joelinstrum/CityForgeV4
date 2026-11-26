using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        internal void AttachVehicleScript(Transform instance, LotObjectDefinition item, LotAssetCatalogEntry asset, DistrictLotRecord lot, Camera camera)
        {
            // Behavior is selected on the saved object; models and lot names never activate jobs.
            var profile = VehicleProfile.Find(asset.id);
            bool legacy = item.vehicleScript.behavior == LotVehicleBehavior.LumberWagon && profile?.supportsLumberFreight == true;
            bool resource = item.vehicleScript.behavior == LotVehicleBehavior.ResourceDelivery && item.vehicleScript.ResourceTask && profile?.supportsResourceFreight == true;
            if ((legacy || resource) && ApprovedDistrict)
                instance.gameObject.AddComponent<LotResourceVehicle>().Initialize(this, lot, item, camera);
        }

        internal Action<Vector2, float, float> FreightVehicleMotion(Transform body, string assetId)
        {
            var vehicle = new Pedestrian { Body = body, Vehicle = true };
            ConfigureVehicleMotion(vehicle, VehicleProfile.Find(assetId));
            return (heading, distance, delta) =>
            {
                if (vehicle.Carriage) AdvanceHorseTeam(vehicle, heading, distance, delta);
                else if (heading.sqrMagnitude > .0001f)
                {
                    body.rotation = Quaternion.LookRotation(new Vector3(heading.x, 0, heading.y));
                    if (vehicle.Wheels != null) foreach (var wheel in vehicle.Wheels)
                        wheel.Rotate(wheel.InverseTransformDirection(body.right), distance * Mathf.Rad2Deg / .48f, Space.Self);
                }
            };
        }

        internal bool TryFreightDestination(DistrictLotRecord camp, Vector2 from, string preferredId,
            out DistrictLotRecord destination, out Vector2[] route, bool townCenterOnly = false)
        {
            destination = null; route = null;
            // Explicit receiver roles take precedence over all catalog labels and building models.
            foreach (var role in townCenterOnly ? new[] { LotDeliveryRole.TownCenter } : new[] { LotDeliveryRole.LumberMill, LotDeliveryRole.TownCenter })
            {
                float shortest = float.PositiveInfinity;
                foreach (var candidate in PlacedLots.Where(lot => lot != camp && InActiveDistrict(lot.center) && lot.definition.DeliveryRole == role)
                    .OrderBy(lot => lot.id == preferredId ? 0 : 1))
                {
                    float reach = candidate.definition.Size.magnitude * .5f + 16f;
                    if (!FreightRoute(from, candidate.center, 24f, reach, false, out var path)) continue;
                    float distance = 0;
                    for (int i = 1; i < path.Length; i++) distance += Vector2.Distance(path[i - 1], path[i]);
                    if (distance >= shortest) continue;
                    shortest = distance; destination = candidate; route = path;
                }
                if (destination != null) return true;
            }
            return false;
        }

        internal int FreightRoutePlans { get; private set; }
        internal bool FreightRoute(Vector2 from, Vector2 to, float fromAccess, float toAccess, bool returnHome, out Vector2[] route)
        {
            FreightRoutePlans++;
            var roads = TransportRoadRoutes();
            if (!FreightRoadGraph.TryRoute(roads, from, to, fromAccess, toAccess, out var path)) { route = null; return false; }
            var rounded = new List<Vector2>();
            LotPathSurface.RoundCorners(path, null, false, .8f, rounded);
            if (rounded.Count == 0) rounded.AddRange(path);
            if (returnHome && (rounded[^1] - to).sqrMagnitude > .001f) rounded.Add(to);
            route = rounded.ToArray();
            for (int i = 1; i < route.Length; i++) if (!FreightClear(route[i - 1], route[i])) { route = null; return false; }
            return route.Length > 0;
        }

        private WildlifeWaterAccess freightWaterAccess;
        private MeadowDistrictLandscape freightWaterLandscape;
        internal bool FreightClear(Vector2 from, Vector2 to, float clearance = .9f) => ActorRouteClear(from, to, clearance, false);
        private bool ActorRouteClear(Vector2 from, Vector2 to, float clearance, bool orientedObjects)
        {
            var land = ApprovedDistrict;
            if (!land || !land.DisplayTerrain) return false;
            if (freightWaterLandscape != land) { freightWaterLandscape = land; freightWaterAccess = new WildlifeWaterAccess(land); }
            var water = freightWaterAccess;
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(from, to) / Mathf.Min(.75f, Mathf.Max(.15f, clearance))));
            float previous = TransportHeight(from);
            for (int i = 0; i <= steps; i++)
            {
                var p = Vector2.Lerp(from, to, (float)i / steps);
                float height = TransportHeight(p);
                if (TryBridgeSurface(p,clearance,out _))
                {
                    if (Mathf.Abs(height-previous)>.35f) return false;
                    previous=height;continue;
                }
                // Shared grading can create valid dry streets only centimetres above the
                // regional sea datum. Actual water footprints remain authoritative there.
                bool gradedGround=TryZoneGroundHeight(p,out _);
                if (!water.Dry(p, clearance) || (!gradedGround && height < SeaLevel + .1f) || Mathf.Abs(height - previous) > .35f || (orientedObjects ? OrientedWorkObstacle(p, clearance) : LumberjackObstacle(p, clearance))) return false;
                previous = height;
            }
            return land.TimberRouteClear(from, to);
        }
    }

    internal static class LotFreightLedger
    {
        internal static bool Load(DistrictLotRecord camp, LotVehicleJobState job, int capacity, string destination, VehicleResource resource = VehicleResource.Timber)
        {
            if (capacity < 1 || capacity > 100 || !Enum.IsDefined(typeof(VehicleResource), resource) || job.cargo != 0 || job.phase != LotFreightPhase.Waiting || VehicleResources.Stored(camp, resource) < capacity) return false;
            VehicleResources.Store(camp, resource, VehicleResources.Stored(camp, resource) - capacity); job.cargo = capacity;
            job.cargoResource = resource;
            job.destinationLotId = destination; job.phase = LotFreightPhase.Outbound;
            return true;
        }
        internal static int LoadSome(DistrictLotRecord source, LotVehicleJobState job, int count, int capacity, VehicleResource resource)
        {
            if ((job.phase != LotFreightPhase.Waiting && job.phase != LotFreightPhase.Loading) || count <= 0 || capacity < 1 || capacity > 100 ||
                (job.cargo > 0 && job.cargoResource != resource)) return 0;
            int amount = Mathf.Min(count, capacity - job.cargo, VehicleResources.Stored(source, resource));
            if (amount <= 0) return 0;
            VehicleResources.Store(source, resource, VehicleResources.Stored(source, resource) - amount);
            job.cargoResource = resource; job.cargo += amount; job.phase = LotFreightPhase.Loading;
            return amount;
        }
        internal static bool Dispatch(LotVehicleJobState job, int capacity, string destination)
        {
            if (job.phase != LotFreightPhase.Loading || job.cargo < capacity || string.IsNullOrEmpty(destination)) return false;
            job.destinationLotId = destination; job.phase = LotFreightPhase.Outbound; job.handlingProgress = 0;
            return true;
        }
        internal static int UnloadSome(DistrictLotRecord receiver, LotVehicleJobState job, int count)
        {
            if (job.phase != LotFreightPhase.Unloading || job.destinationLotId != receiver.id || receiver.definition.DeliveryRole == LotDeliveryRole.None) return 0;
            int amount = Mathf.Min(count, job.cargo, LotFreightValidation.StockLimit - VehicleResources.Received(receiver, job.cargoResource));
            if (amount <= 0) return 0;
            VehicleResources.Receive(receiver, job.cargoResource, VehicleResources.Received(receiver, job.cargoResource) + amount);
            job.cargo -= amount;
            if (job.cargo == 0) { job.phase = LotFreightPhase.Returning; job.destinationLotId = null; job.handlingProgress = 0; }
            return amount;
        }
        internal static bool Deliver(DistrictLotRecord receiver, LotVehicleJobState job)
        {
            if (job.phase != LotFreightPhase.Outbound || job.cargo <= 0 || receiver.id != job.destinationLotId ||
                receiver.definition.DeliveryRole == LotDeliveryRole.None || VehicleResources.Received(receiver, job.cargoResource) > LotFreightValidation.StockLimit - job.cargo) return false;
            VehicleResources.Receive(receiver, job.cargoResource, VehicleResources.Received(receiver, job.cargoResource) + job.cargo);
            job.cargo = 0; job.phase = LotFreightPhase.Returning; job.destinationLotId = null;
            return true;
        }
    }
}
