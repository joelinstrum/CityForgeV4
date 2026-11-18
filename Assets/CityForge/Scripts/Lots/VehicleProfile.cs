using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.World;
using CityForge.UI;
using UnityEngine;

namespace CityForge.Lots
{
    [Serializable]
    public sealed class VehicleProfile
    {
        public string assetId, label, modelFolder, colorExtension;
        public string motionFolder, idleState, travelState;
        public int[] eraNumbers;
        public string[] eras, zones, wealthTiers;
        public float length, speed, speedVariation, teamHorseHeight;
        public string trafficGroup;
        public string emptyCargoMesh, cargoMeshPart;
        public Vector3 cargoCenter, cargoSize, cargoCutCenter, cargoCutSize, cargoKeepCenter, cargoKeepSize;
        public bool enclosedCargo;
        public int spawnEvery;
        public bool parked, team, rollWheels, supportsLumberFreight, supportsResourceFreight;

        public IEnumerable<string> EraIds()
        {
            if (eraNumbers == null || eraNumbers.Length == 0) return eras ?? Array.Empty<string>();
            var timeline = CityForgeTimeline.Eras;
            return eraNumbers.Where(number => number >= 1 && number <= timeline.Length)
                .Select(number => timeline[number - 1].id);
        }

        internal bool AvailableIn(string era)
        {
            if (eraNumbers is { Length: > 0 })
                return EraIds().Contains(era, StringComparer.OrdinalIgnoreCase);
            return string.IsNullOrEmpty(era) || Matches(eras, era);
        }

        internal bool AllowsTraffic(string era, TownZone zone, LotWealth wealth) =>
            spawnEvery > 0 && AvailableIn(era) && Matches(zones, zone.ToString()) && Matches(wealthTiers, wealth.ToString());

        private static bool Matches(string[] filter, string value) =>
            filter == null || filter.Length == 0 || filter.Contains(value, StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, VehicleProfile> profiles;
        public static IEnumerable<VehicleProfile> All => (profiles ??= Load()).Values;
        internal static VehicleProfile Find(string id) => id == null ? null : (profiles ??= Load()).GetValueOrDefault(id);

        private static Dictionary<string, VehicleProfile> Load()
        {
            var result = new Dictionary<string, VehicleProfile>(StringComparer.Ordinal);
            foreach (var file in Resources.LoadAll<TextAsset>("Config/Vehicles"))
            {
                try
                {
                    var profile = JsonUtility.FromJson<VehicleProfile>(file.text);
                    if (profile == null || string.IsNullOrWhiteSpace(profile.assetId) ||
                        !float.IsFinite(profile.speed) || profile.speed < 0 || !float.IsFinite(profile.length) ||
                        !float.IsFinite(profile.speedVariation) || !result.TryAdd(profile.assetId, profile))
                        Debug.LogWarning($"Invalid or duplicate vehicle profile {file.name}.");
                }
                catch (ArgumentException error)
                {
                    Debug.LogWarning($"Could not read vehicle profile {file.name}: {error.Message}");
                }
            }
            return result;
        }
    }
}
