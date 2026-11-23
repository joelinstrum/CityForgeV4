using System;
using System.Linq;
using CityForge.Lots;
using CityForge.UI;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private static bool IsTownCenterLot(LotDefinition lot) => CivicBuildingChoices.IsTownCenter(lot);
        private static bool CityCenterEraAllows(LotDefinition lot,string era) => IsTownCenterLot(lot)
            ? (LotClassification.Era(lot)?.FirstYear ?? lot.firstEraYear) <= (CityForgeTimeline.Find(era)?.LastYear ?? 0)
            : CivicBuildingChoices.Era(lot)==era;

        private void RegisterDistrictTownCenter(DistrictLotRecord record)
        {
            if (!IsTownCenterLot(record.definition) ||
                Towns.Any(town => ActiveDistrictMembers.Contains(town.DistrictIndex(GridSide)))) return;
            var town = new RegionTown
            {
                id = Guid.NewGuid().ToString("N"), name = "Town " + (record.district + 1),
                position = record.center / MapSide + Vector2.one * .5f,
                establishedYear = TownYear(record.district)
            };
            Towns = Towns.Append(town).ToArray();
            RefreshTownConstruction();
        }
    }
}
