using System;
using System.Linq;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    public sealed partial class PrototypeRegionView
    {
        private const string DistrictTownCenterId = "6c54a4bdc61a47b09bbc4bf17f5d5420";

        private static LotDefinition DistrictTownCenter() => new()
        {
            id = DistrictTownCenterId, name = "Town Center", type = LotType.Playable,
            category = "civics", subcategory = "government", firstEraId = "founders",
            width = 4, length = 4, deliveryRole = LotDeliveryRole.TownCenter,
            deliveryRoleConfigured = true,
            objects = new() { new LotObjectDefinition { assetId = "timber-council-hall" } }
        };

        private void RegisterDistrictTownCenter(DistrictLotRecord record)
        {
            if (record.definition.id != DistrictTownCenterId ||
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
