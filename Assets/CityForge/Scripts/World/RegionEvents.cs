using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CityForge.World
{
    [Serializable] internal sealed class RegionEventDefinition
    {
        public string id, name, trigger, sourceAssetId, wealth, arrivalMessage;
        public int firstYear, lastYear, maxOccurrences, immigrantsPerArrival;
        public int populationPerRepresentative = 1;
        public string[] destinationCategories, actorAssetIds;
    }

    internal static class RegionEventCatalog
    {
        [Serializable] private sealed class File { public RegionEventDefinition[] events; }
        private static RegionEventDefinition[] definitions;
        internal static RegionEventDefinition[] Definitions => definitions ??= Load();
        private static RegionEventDefinition[] Load()
        {
            var json = Resources.Load<TextAsset>("Config/RegionEvents");
            if (!json) throw new InvalidDataException("Missing regional event definitions.");
            var result = JsonUtility.FromJson<File>(json.text)?.events;
            if (result == null || result.Any(e => e == null || string.IsNullOrEmpty(e.id) ||
                e.trigger != "galleon-docked" || string.IsNullOrEmpty(e.sourceAssetId) || e.wealth != "low" ||
                e.firstYear < 1 || e.lastYear < e.firstYear || e.maxOccurrences < 0 || e.maxOccurrences > 100 ||
                e.populationPerRepresentative < 1 || e.populationPerRepresentative > 1000 ||
                e.immigrantsPerArrival < 1 || e.immigrantsPerArrival > 36 ||
                e.actorAssetIds == null || e.actorAssetIds.Length == 0 ||
                e.destinationCategories == null || e.destinationCategories.Length == 0 || string.IsNullOrEmpty(e.arrivalMessage)) ||
                result.Select(e => e.id).Distinct().Count() != result.Length)
                throw new InvalidDataException("Invalid regional event definitions.");
            return result;
        }
    }

    [Serializable] public sealed class EventImmigrant
    {
        public string id, assetId, destinationLotId;
        public int district;
        public long representedPopulation;
        internal long Population => representedPopulation > 0 ? representedPopulation : 1;
        public Vector2 position, destination, arrivalPoint;
        public bool hasDestination, settled, hasArrivalPoint;
        public bool targetIsZone, arrivedAtZone;
        public bool awaitingHousing;
        public Vector2Int destinationZoneCell;
    }

    [Serializable] public sealed class ImmigrantSettlement
    {
        public string lotId;
        public long population;
    }

    [Serializable] public sealed class RegionEventState
    {
        public string eventId, sourceLotId;
        public long occurrences, populationArrived, settledPopulation;
        public List<ImmigrantSettlement> moveIns = new();
        public bool cancelled;
        public List<EventImmigrant> immigrants = new();

        internal static void Validate(RegionEventState[] states, int districts, float side)
        {
            if (states == null) return;
            var keys = new HashSet<string>();
            var people = new HashSet<string>();
            if (states.Length > 4096) throw new InvalidDataException("Too many regional events.");
            foreach (var s in states)
            {
                if (s == null || string.IsNullOrEmpty(s.eventId) || string.IsNullOrEmpty(s.sourceLotId) ||
                    !keys.Add(s.eventId + ":" + s.sourceLotId) || s.occurrences < 0 || s.populationArrived < 0 || s.settledPopulation < 0 ||
                    (s.moveIns != null && (s.moveIns.Count > 65536 || s.moveIns.Any(m => m == null || string.IsNullOrEmpty(m.lotId) || m.population < 0))) ||
                    s.immigrants == null || s.immigrants.Count > 3600)
                    throw new InvalidDataException("Invalid regional event progress.");
                foreach (var p in s.immigrants)
                    if (p == null || string.IsNullOrEmpty(p.id) || !people.Add(p.id) || string.IsNullOrEmpty(p.assetId) ||
                        p.representedPopulation < 0 || p.district < 0 || p.district >= districts || !Point(p.position) || !Point(p.destination) || (p.hasArrivalPoint && !Point(p.arrivalPoint)))
                        throw new InvalidDataException("Invalid immigrant state.");
            }
            bool Point(Vector2 p) => float.IsFinite(p.x) && float.IsFinite(p.y) && Mathf.Abs(p.x) <= side / 2 && Mathf.Abs(p.y) <= side / 2;
        }
    }
}
