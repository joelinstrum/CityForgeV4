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

        public string id;
        public string name;
        public Vector2 position;
        public int establishedYear;
        public bool capital;
        public bool awaitingFoundersBuilding;
        public TownDevelopmentState development;

        public int DistrictIndex(int grid) => (int)(position.x * grid) + (int)(position.y * grid) * grid;

        internal static bool ValidYear(int year) => year >= 1 && year <= 9999;
        internal static bool ValidName(string name) => !string.IsNullOrWhiteSpace(name) &&
            name.Length <= NameLimit && !name.Any(char.IsControl);

        internal static void Validate(RegionTown[] towns)
        {
            if (towns == null) return;
            if (towns.Length > CountLimit) throw new InvalidDataException("Too many towns.");
            bool hasCapital = false;
            var ids = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (var town in towns)
            {
                if (town == null || !ValidName(town.name) || !ValidYear(town.establishedYear) ||
                    !new Rect(0, 0, 1, 1).Contains(town.position) || (town.capital && hasCapital) ||
                    (!string.IsNullOrEmpty(town.id) && !ids.Add(town.id)))
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

        internal Vector3 TownMarkerPoint(RegionTown town)
        {
            if (SelectedDistrict < 0) return Relief.SurfacePoint(town.position);
            var point = (town.position - Vector2.one * .5f) * MapSide;
            return new Vector3(point.x, LotHeight(point), point.y);
        }

        private bool CanEditTowns => SelectedDistrict < 0 && !DistrictLoading && !IsSaving && !returning && !RemakeBlocksInput;

        private bool HasNearbyNamesake(string name, Vector2 position, RegionTown except = null)
        {
            const float separation = 64f;
            return Towns.Any(town => town != except && string.Equals(town.name, name, StringComparison.OrdinalIgnoreCase) &&
                ((town.position - position) * MapSide).sqrMagnitude < separation * separation);
        }

        internal string MoveTown(RegionTown town, Vector2 position)
        {
            if (!CanEditTowns || !Towns.Contains(town)) return "This town cannot be moved right now.";
            if (!IsTownLand(position)) return "Choose a spot on land.";
            if (HasNearbyNamesake(town.name, position, town)) return "A town with this name is already nearby.";
            town.position = position;
            Towns = (RegionTown[])Towns.Clone();
            return null;
        }

        internal string DeleteTown(RegionTown town)
        {
            if (!CanEditTowns || !Towns.Contains(town)) return "This town cannot be deleted right now.";
            Towns = Towns.Where(item => item != town).ToArray();
            if (town.capital && Towns.Length > 0)
            {
                var successor = Towns.OrderBy(item => item.establishedYear).First();
                successor.capital = true;
                successor.awaitingFoundersBuilding = town.awaitingFoundersBuilding;
            }
            if (Development.connections != null)
                Development.connections = Development.connections.Where(connection => connection != null &&
                    connection.fromTownId != town.id && connection.toTownId != town.id).ToArray();
            RefreshRemakeButton();
            return null;
        }

        internal string AddTown(Vector2 position, string name, int year)
        {
            if (!CanEditTowns)
                return "Open the region map to establish a town.";
            if (!IsTownLand(position)) return "Choose a spot on land.";
            if (!RegionTown.ValidName(name)) return "Enter a town name.";
            if (HasNearbyNamesake(name.Trim(), position)) return "A town with this name is already nearby. Use Manage Towns to move it.";
            if (!RegionTown.ValidYear(year)) return "Enter a year from 1 to 9999.";
            if (Towns.Length >= RegionTown.CountLimit) return "This region has reached its town limit.";
            Towns = Towns.Append(new RegionTown
            {
                id = Guid.NewGuid().ToString("N"), name = name.Trim(), position = position,
                establishedYear = year, capital = Towns.Length == 0, awaitingFoundersBuilding = Towns.Length == 0
            }).ToArray();
            RefreshRemakeButton();
            return null;
        }
    }
}
