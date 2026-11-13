using System.Linq;
using UnityEngine;

namespace CityForge.Lots
{
    internal static class LotGrowableBuildings
    {
        // These imports face +Z at yaw zero. LotFrontage.South faces -Z.
        internal static float FrontageOffset(string assetId) => assetId switch
        {
            // The vertical-siding model's gabled entrance is on its +X wall.
            "v3-building-low-wealth-vertical-siding-front-gable" or "v3-building-low-wealth-front-gable-cottage" => 90f,
            "v3-building-frontier-log-cabin" or "v3-building-small-colonial-cabin" => 180f,
            string id when id.StartsWith("v3-building-low-wealth-", System.StringComparison.Ordinal) ||
                id.StartsWith("v3-building-medium-wealth-", System.StringComparison.Ordinal) => 180f,
            _ => 0f
        };

        internal static void CorrectLegacyCabinFacing(LotDefinition lot)
        {
            var layout = lot.zoningLayout;
            // Growth appends its chosen building after the authored objects.
            if (lot.type != LotType.Zoning || !layout.Configured || layout.hasEntrance || layout.faceAwayFromRoad || lot.objects.Count == 0) return;
            var building = lot.objects[lot.objects.Count - 1];
            if (building.assetId is not ("v3-building-frontier-log-cabin" or "v3-building-small-colonial-cabin")) return;
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
                .Select(asset => PlacementProblem(lot, asset, catalog, out var building) == null ? building : null)
                .Where(building => building != null)
                .ToArray();
        }

        internal static string PlacementProblem(LotDefinition lot, LotAssetCatalogEntry asset, LotAssetCatalog catalog,
            out LotObjectDefinition building)
        {
            var layout = lot.zoningLayout;
            var info = LotBuildingInfo.Load(asset.id);
            var entry = info != null && !info.dimensionsOnly ? info : null;
            building = null;
            if (layout.hasEntrance && entry == null) return "Missing entry point";

            float offset = entry == null ? FrontageOffset(asset.id) : -90f * (int)entry.facing;
            float yaw = Mathf.Repeat(90f * (int)layout.frontage + offset + 90f * layout.buildingQuarterTurns + (layout.faceAwayFromRoad ? 180f : 0f), 360f);
            var anchor = layout.hasEntrance
                ? LotGroundOverlay.QuarterTurn(entry.entrance, Mathf.RoundToInt(yaw / 90f)) : Vector2.zero;
            var footprint = info != null && info.HasDimensions
                ? new Vector2(info.dimensions.x, info.dimensions.z) : asset.footprint;
            var placement = new LotObjectDefinition
            {
                assetId = asset.id,
                yaw = yaw,
                position = layout.hasEntrance ? layout.entrance - anchor : layout.buildingCenter
            };
            var bounds = LotRuntimeLoader.Footprint(placement, asset, Vector2.Max(asset.footprint, footprint));
            var problem = layout.BuildingFitProblem(lot, bounds, catalog);
            if (problem != null) return problem;
            building = placement;
            return null;
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
