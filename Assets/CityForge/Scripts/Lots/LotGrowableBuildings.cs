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
            string id when id.StartsWith("v3-building-low-wealth-", System.StringComparison.Ordinal) => 180f,
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
            const float maxUpscale = 1.2f;
            var layout = lot.zoningLayout;
            var info = LotBuildingInfo.Load(asset.id);
            var entry = info != null && !info.dimensionsOnly ? info : null;
            building = null;
            if (layout.hasEntrance && entry == null) return "Missing entry point";

            float offset = entry == null ? FrontageOffset(asset.id) : -90f * (int)entry.facing;
            float yaw = Mathf.Repeat(90f * (int)layout.frontage + offset + (layout.faceAwayFromRoad ? 180f : 0f), 360f);
            var anchor = layout.hasEntrance
                ? LotGroundOverlay.QuarterTurn(entry.entrance, Mathf.RoundToInt(yaw / 90f)) : Vector2.zero;
            var footprint = info != null && info.HasDimensions
                ? new Vector2(info.dimensions.x, info.dimensions.z) : asset.footprint;
            var placement = new LotObjectDefinition { assetId = asset.id, yaw = yaw };
            SetScale(layout.BuildingScale);
            var problem = layout.BuildingFitProblem(lot, LotRuntimeLoader.Footprint(placement, asset, footprint), catalog);
            if (problem != null) return problem;

            // The rendered footprint can be wider than the authored estimate.
            var expansionFootprint = Vector2.Max(asset.footprint, footprint);
            float low = placement.scale;
            float high = Mathf.Min(3f, low * maxUpscale);
            if (high > low && FitsAt(low))
            {
                if (FitsAt(high)) low = high;
                else
                {
                    for (int i = 0; i < 10; i++)
                    {
                        float middle = (low + high) * .5f;
                        if (FitsAt(middle)) low = middle;
                        else high = middle;
                    }
                }
            }
            SetScale(low);
            building = placement;
            return null;

            void SetScale(float scale)
            {
                placement.scale = scale;
                placement.position = layout.hasEntrance ? layout.entrance - anchor * scale : layout.buildingCenter;
            }

            bool FitsAt(float scale)
            {
                SetScale(scale);
                var bounds = LotRuntimeLoader.Footprint(placement, asset, expansionFootprint);
                return layout.BuildingFitProblem(lot, bounds, catalog, withinSlot: true) == null;
            }
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
