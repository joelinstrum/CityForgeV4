using System.Linq;
using UnityEngine;

namespace CityForge.Lots
{
    internal static class LotGrowableBuildings
    {
        // These imports face +Z at yaw zero. LotFrontage.South faces -Z.
        internal static float FrontageOffset(string assetId) => assetId switch
        {
            "v3-building-frontier-log-cabin" or "v3-building-small-colonial-cabin" => 180f,
            _ => 0f
        };

        internal static void CorrectLegacyCabinFacing(LotDefinition lot)
        {
            var layout = lot.zoningLayout;
            // Growth appends its chosen building after the authored objects.
            if (lot.type != LotType.Zoning || !layout.Configured || layout.hasEntrance || lot.objects.Count == 0) return;
            var building = lot.objects[lot.objects.Count - 1];
            float offset = FrontageOffset(building.assetId);
            float oldYaw = 90f * (int)layout.frontage;
            if (offset != 0f && building.position == layout.buildingCenter &&
                Mathf.Abs(Mathf.DeltaAngle(building.yaw, oldYaw)) < .01f)
                building.yaw = Mathf.Repeat(oldYaw + offset, 360f);
        }

        internal static LotObjectDefinition[] Candidates(LotDefinition lot, LotAssetCatalog catalog)
        {
            if (!catalog || !lot.zoningLayout.Configured) return System.Array.Empty<LotObjectDefinition>();
            var layout = lot.zoningLayout;
            if (layout.restrictGrowthBuildings && string.IsNullOrEmpty(lot.firstEraId))
                return System.Array.Empty<LotObjectDefinition>();
            return catalog.entries
                .Where(asset => asset != null && layout.AllowsGrowthBuilding(asset.id) && asset.CanPreviewGrowable(layout.zone, layout.wealth, lot.firstEraId))
                .Select(asset => PlaceInSlot(layout, asset))
                .Where(building => building != null &&
                    layout.BuildingFitProblem(lot, building, catalog.Find(building.assetId), catalog) == null)
                .ToArray();
        }

        internal static LotObjectDefinition PlaceInSlot(LotZoningLayout layout, LotAssetCatalogEntry asset)
        {
            var info = LotBuildingInfo.Load(asset.id);
            if (layout.hasEntrance && info == null) return null;
            float offset = info == null ? FrontageOffset(asset.id) : -90f * (int)info.facing;
            float yaw = Mathf.Repeat(90f * (int)layout.frontage + offset, 360f);
            var position = layout.buildingCenter;
            if (layout.hasEntrance)
            {
                var anchor = LotGroundOverlay.QuarterTurn(info.entrance, Mathf.RoundToInt(yaw / 90f));
                position = layout.entrance - anchor;
            }
            return new LotObjectDefinition { assetId = asset.id, position = position, yaw = yaw, scale = 1f };
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
