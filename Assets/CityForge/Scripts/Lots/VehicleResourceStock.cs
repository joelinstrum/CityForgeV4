using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForge.Lots
{
    public enum VehicleTaskCategory { Resource, Emergency, Event }
    public enum VehicleDestination { TownCenter }
    public enum VehicleResource { Timber, Lumber, Bricks, Stone, Iron, Coal, Clay, Fish, FertileSoil, Pasture, FreshWater, NaturalHarbor, ScenicBeauty }

    [Serializable] public sealed class LotResourceAmount
    {
        public VehicleResource resource;
        public int amount;
    }

    internal static class VehicleResources
    {
        internal static readonly VehicleResource[] All = (VehicleResource[])Enum.GetValues(typeof(VehicleResource));
        internal static string Label(VehicleResource resource) => resource switch
        {
            VehicleResource.FertileSoil => "Fertile Soil", VehicleResource.FreshWater => "Fresh Water",
            VehicleResource.NaturalHarbor => "Natural Harbor", VehicleResource.ScenicBeauty => "Scenic Beauty", _ => resource.ToString()
        };
        internal static int Stored(World.DistrictLotRecord lot, VehicleResource resource) => resource == VehicleResource.Timber
            ? lot.storedTimber : lot.storedResources?.Find(item => item.resource == resource)?.amount ?? 0;
        internal static int Received(World.DistrictLotRecord lot, VehicleResource resource) => resource == VehicleResource.Timber
            ? lot.receivedTimber : lot.receivedResources?.Find(item => item.resource == resource)?.amount ?? 0;
        internal static void Store(World.DistrictLotRecord lot, VehicleResource resource, int amount)
        {
            if (resource == VehicleResource.Timber) { lot.storedTimber = amount; return; }
            lot.storedResources ??= new List<LotResourceAmount>(); Set(lot.storedResources, resource, amount);
        }
        internal static void Receive(World.DistrictLotRecord lot, VehicleResource resource, int amount)
        {
            if (resource == VehicleResource.Timber) { lot.receivedTimber = amount; return; }
            lot.receivedResources ??= new List<LotResourceAmount>(); Set(lot.receivedResources, resource, amount);
        }
        private static void Set(List<LotResourceAmount> stock, VehicleResource resource, int amount)
        {
            var entry = stock.Find(item => item.resource == resource);
            if (entry == null) { entry = new LotResourceAmount { resource = resource }; stock.Add(entry); }
            entry.amount = amount;
        }
    }
}
