using System;
using System.IO;
using System.Linq;
using CityForge.UI;
using UnityEngine;

namespace CityForge.World
{
    [Serializable]
    public sealed class RegionTown
    {
        public const int NameLimit = 64;
        internal const int CountLimit = 1024;

        public string name;
        public Vector2 position;
        public int establishedYear;
        public bool capital;

        internal static bool ValidYear(int year) => year >= 1 && year <= 9999;
        internal static bool ValidName(string name) => !string.IsNullOrWhiteSpace(name) &&
            name.Length <= NameLimit && !name.Any(char.IsControl);

        internal static void Validate(RegionTown[] towns)
        {
            if (towns == null) return;
            if (towns.Length > CountLimit) throw new InvalidDataException("Too many towns.");
            bool hasCapital = false;
            foreach (var town in towns)
            {
                if (town == null || !ValidName(town.name) || !ValidYear(town.establishedYear) ||
                    !new Rect(0, 0, 1, 1).Contains(town.position) || (town.capital && hasCapital))
                    throw new InvalidDataException("Invalid town metadata.");
                hasCapital |= town.capital;
            }
        }
    }

    public sealed partial class PrototypeRegionView
    {
        public RegionTown[] Towns { get; private set; } = Array.Empty<RegionTown>();

        internal int TownYear(int district)
        {
            var info = Districts?.FirstOrDefault(item => item.index == district);
            if (info != null && RegionTown.ValidYear(info.currentYear)) return info.currentYear;
            foreach (var era in CityForgeTimeline.Eras)
                foreach (var period in era.Periods)
                    if (period.Name == CreationSettings?.period) return period.FirstYear;
            return CityForgeTimeline.Eras[0].FirstYear;
        }

        internal bool IsTownLand(Vector2 position)
        {
            if (!new Rect(0, 0, 1, 1).Contains(position)) return false;
            if (Waterless) return true;
            foreach (var body in Data.WaterBodies)
                if (body.Bounds.Contains(position) && body.Contains(position)) return false;

            if (RegionalRiverNetwork.HasContinuousChannels(Data))
                return RegionalRiverNetwork.SignedDistance(Data, position) >= 0;
            if (SmoothRiverSurface.Sample(Data.WaterDistance, position.x, position.y) < 0) return false;
            return CreationSettings?.version >= 5 ||
                Data.SmoothHeight(position.x, position.y) * VerticalHeight >= SeaLevel + .1f;
        }

        internal string AddTown(Vector2 position, string name, int year, bool capital)
        {
            if (SelectedDistrict >= 0 || DistrictLoading || IsSaving || returning)
                return "Open the region map to establish a town.";
            if (!IsTownLand(position)) return "Choose a spot on land.";
            if (!RegionTown.ValidName(name)) return "Enter a town name.";
            if (!RegionTown.ValidYear(year)) return "Enter a year from 1 to 9999.";
            if (Towns.Length >= RegionTown.CountLimit) return "This region has reached its town limit.";
            // Copy a displaced capital so any captured snapshot retains its original metadata.
            Towns = Towns.Select(town => capital && town.capital
                ? new RegionTown { name = town.name, position = town.position, establishedYear = town.establishedYear }
                : town).Append(new RegionTown {
                    name = name.Trim(), position = position, establishedYear = year, capital = capital
                }).ToArray();
            return null;
        }
    }
}
