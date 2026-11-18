using System;
using System.Collections.Generic;
using System.IO;
using CityForge.World;
using UnityEngine;

namespace CityForge.Lots
{
    public enum LotDeliveryRole { None, LumberMill, TownCenter }
    public enum LotVehicleBehavior { None, LumberWagon, ResourceDelivery }
    public enum LotFreightPhase { Waiting, Outbound, Returning, Loading, Unloading }

    [Serializable]
    public struct LotVehicleScript
    {
        public LotVehicleBehavior behavior;
        public int capacity, handlingSeconds;
        public bool configured, enabled;
        public VehicleTaskCategory category;
        public VehicleResource resource;
        public RegionTimeOfDay when;
        public VehicleDestination destination;
        internal bool ResourceTask => !configured || (enabled && category == VehicleTaskCategory.Resource);
        internal int Capacity => capacity == 0 ? 4 : capacity;
        internal int HandlingSeconds => handlingSeconds == 0 ? 4 : handlingSeconds;
        internal bool IsValid => Enum.IsDefined(typeof(LotVehicleBehavior), behavior) && capacity >= 0 && capacity <= 100 &&
            handlingSeconds >= 0 && handlingSeconds <= 120 &&
            Enum.IsDefined(typeof(VehicleTaskCategory), category) && Enum.IsDefined(typeof(VehicleResource), resource) &&
            Enum.IsDefined(typeof(RegionTimeOfDay), when) && Enum.IsDefined(typeof(VehicleDestination), destination);
    }

    [Serializable]
    public sealed class LotVehicleJobState
    {
        public string objectId, destinationLotId;
        public LotFreightPhase phase;
        public int cargo;
        public float handlingProgress;
        public VehicleResource cargoResource;
        public bool departedThisWindow;
        public bool hasPosition;
        public Vector2 position;
    }

    internal static class LotFreightValidation
    {
        internal const int StockLimit = 1000000;
        private static void ValidateStock(List<LotResourceAmount> stock)
        {
            if (stock == null) return;
            var seen = new HashSet<VehicleResource>();
            if (stock.Count > VehicleResources.All.Length) throw new InvalidDataException("Too many resource stocks.");
            foreach (var item in stock)
                if (item == null || !Enum.IsDefined(typeof(VehicleResource), item.resource) || !seen.Add(item.resource) ||
                    item.resource == VehicleResource.Timber || item.amount < 0 || item.amount > StockLimit)
                    throw new InvalidDataException("Invalid lot resource stock.");
        }
        internal static void Validate(DistrictLotRecord lot)
        {
            if (lot.storedTimber < 0 || lot.storedTimber > StockLimit || lot.receivedTimber < 0 || lot.receivedTimber > StockLimit)
                throw new InvalidDataException("Invalid lot timber stock.");
            ValidateStock(lot.storedResources); ValidateStock(lot.receivedResources);
            lot.vehicleJobs ??= new List<LotVehicleJobState>();
            if (lot.vehicleJobs.Count > 512) throw new InvalidDataException("Too many lot vehicle jobs.");
            var ids = new HashSet<string>();
            foreach (var job in lot.vehicleJobs)
                if (job == null || string.IsNullOrEmpty(job.objectId) || !ids.Add(job.objectId) ||
                    !lot.definition.objects.Exists(item => item.id == job.objectId) ||
                    !Enum.IsDefined(typeof(LotFreightPhase), job.phase) || !Enum.IsDefined(typeof(VehicleResource), job.cargoResource) || job.cargo < 0 || job.cargo > 100 ||
                    !float.IsFinite(job.position.x) || !float.IsFinite(job.position.y) ||
                    !float.IsFinite(job.handlingProgress) || job.handlingProgress < 0 || job.handlingProgress >= 1.001f ||
                    ((job.phase == LotFreightPhase.Waiting || job.phase == LotFreightPhase.Returning) && job.cargo != 0))
                    throw new InvalidDataException("Invalid lot vehicle job.");
        }
    }
}
