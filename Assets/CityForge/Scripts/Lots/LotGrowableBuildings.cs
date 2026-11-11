using System.Linq;
using UnityEngine;

namespace CityForge.Lots
{
    internal static class LotGrowableBuildings
    {
        internal static LotObjectDefinition[] Candidates(LotDefinition lot, LotAssetCatalog catalog)
        {
            if (!catalog || !lot.zoningLayout.Configured) return System.Array.Empty<LotObjectDefinition>();
            var layout = lot.zoningLayout;
            return catalog.entries
                .Where(asset => asset != null && asset.CanPreviewGrowable(layout.zone, layout.wealth, lot.firstEraId))
                .Select(asset => new LotObjectDefinition
                {
                    assetId = asset.id, position = layout.buildingCenter,
                    yaw = 90f * (int)layout.frontage, scale = 1f
                })
                .Where(building => layout.BuildingFitProblem(lot, building, catalog.Find(building.assetId), catalog) == null)
                .ToArray();
        }

        internal static LotDefinition Materialize(LotDefinition layout, LotObjectDefinition building)
        {
            var grown = LotDefinition.Parse(JsonUtility.ToJson(layout));
            grown.objects.Add(new LotObjectDefinition
            {
                assetId = building.assetId, position = building.position, yaw = building.yaw, scale = building.scale
            });
            return grown;
        }
    }
}
