using System;
using System.Linq;
using UnityEngine;

namespace CityForge.World
{
    public enum TownSpecialization { Agriculture, Industry, Commerce, Maritime, Culture, Residential }

    [Serializable]
    public sealed class SpecializationScore
    {
        public TownSpecialization category;
        public float score;
    }

    [Serializable]
    public sealed class TownDevelopmentState
    {
        public int population;
    }

    public interface ITownSpecializationCalculator
    {
        SpecializationScore[] Potential(SettlementSuitability site);
        SpecializationScore[] Actual(RegionTown town, SettlementSuitability site, int year);
    }

    [Serializable]
    public sealed class TownConnection
    {
        public string fromTownId, toTownId, infrastructureId;
        public float capacity;
    }

    public interface IRegionalBenefitsCalculator
    {
        float Evaluate(string benefitId, RegionTown recipient, RegionTown[] towns, TownConnection[] connections, int year);
    }

    [Serializable]
    public sealed class LegacyPathDefinition
    {
        public string id;
        // Ordered achievement identifiers. The final entry is the path's highest rank.
        public string[] rankIds;
    }

    [Serializable]
    public sealed class LegacyAchievement
    {
        public string pathId, rankId;
        public int earnedYear;
    }

    [Serializable]
    public sealed class RegionDevelopmentState
    {
        public TownConnection[] connections;
        public LegacyAchievement[] achievements;
    }

    public sealed partial class PrototypeRegionView
    {
        public ResourceMap RegionalResources { get; private set; } = new ResourceMap(null);
        public RegionDevelopmentState Development { get; private set; } = new RegionDevelopmentState();
        public RegionTown Capital => Array.Find(Towns, town => town.capital);
        ResourceModifierRules resourceRules;

        void RestoreSettlementData(SavedRegionInfo info)
        {
            RegionalResources = new ResourceMap(info.resources);
            Development = info.development ?? new RegionDevelopmentState();
            Towns = info.towns ?? Array.Empty<RegionTown>();
            bool needsCapital = Towns.Length > 0 && Capital == null;
            Towns = Towns.Select((town, i) => new RegionTown
            {
                id = string.IsNullOrEmpty(town.id) ? $"legacy-town-{i}" : town.id,
                name = town.name, position = town.position, establishedYear = town.establishedYear,
                capital = town.capital || (needsCapital && i == 0),
                awaitingFoundersBuilding = town.awaitingFoundersBuilding && town.capital,
                development = town.development
            }).ToArray();
        }

        public SettlementSuitability EvaluateSettlement(Vector2 worldPosition, int year,
            Func<string, bool> hasTechnology = null)
        {
            if (resourceRules == null)
            {
                var rules = UnityEngine.Resources.Load<TextAsset>("Config/RegionalResourceModifiers");
                resourceRules = rules ? JsonUtility.FromJson<ResourceModifierRules>(rules.text) : new ResourceModifierRules();
            }
            Vector2 position = worldPosition / MapSide + Vector2.one * .5f;
            return RegionalResources.Evaluate(position, year, resourceRules, hasTechnology);
        }

        public SettlementSuitability EvaluateTown(RegionTown town, int year, Func<string, bool> hasTechnology = null) =>
            EvaluateSettlement((town.position - Vector2.one * .5f) * MapSide, year, hasTechnology);

        public RegionTown[] TownsInDistrict(int district)
        {
            var members = DistrictMembers(district);
            return Towns.Where(town => members.Contains(town.DistrictIndex(GridSide))).ToArray();
        }
    }
}
