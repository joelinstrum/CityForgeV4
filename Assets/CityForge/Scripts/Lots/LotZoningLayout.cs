using System;
using CityForge.World;
using UnityEngine;

namespace CityForge.Lots
{
    public enum LotWealth { None, Low, Medium, High }

    public enum LotFrontage { South, West, North, East }

    [Serializable]
    public struct LotZoningLayout
    {
        public TownZone zone;
        public LotWealth wealth;
        public string previewBuildingId;
        public bool faceAwayFromRoad;
        public int buildingQuarterTurns;
        public bool autoPaths;
        public float buildingScale;
        internal float BuildingScale => buildingScale == 0f ? 1f : buildingScale;
        public bool restrictGrowthBuildings;
        public string[] growthBuildingIds;

        internal bool AllowsGrowthBuilding(string id) => !restrictGrowthBuildings ||
            (growthBuildingIds != null && Array.IndexOf(growthBuildingIds, id) >= 0);
        public Vector2 buildingCenter;
        public Vector2 buildingSize;
        public LotFrontage frontage;
        public bool hasEntrance;
        public Vector2 entrance;
        public bool hasDriveway;
        public Vector2 driveway;

        public bool Configured => zone != TownZone.None;
        public Rect BuildingSlot => new Rect(buildingCenter - buildingSize * .5f, buildingSize);

        internal LotZoningLayout SeedBuilding(LotObjectDefinition building, LotAssetCatalogEntry asset)
        {
            var next = this;
            var bounds = LotRuntimeLoader.Footprint(building, asset);
            if (hasEntrance) next.entrance += bounds.center - buildingCenter;
            next.buildingCenter = bounds.center;
            next.buildingSize = bounds.size;
            next.previewBuildingId = asset.id;
            return next;
        }

        public string Label
        {
            get
            {
                if (!Configured) return "";
                int index = (int)zone - 1;
                char use = index < 3 ? 'R' : index < 6 ? 'C' : 'I';
                return $"{use}-{index % 3 + 1} {new string('$', (int)wealth)}";
            }
        }

        internal bool Valid => !Configured || (zone >= TownZone.Residential1 && zone <= TownZone.Industrial3 &&
            wealth >= LotWealth.Low && wealth <= LotWealth.High &&
            BuildingScale >= .25f && BuildingScale <= 3f &&
            buildingQuarterTurns >= 0 && buildingQuarterTurns < 4 &&
            frontage >= LotFrontage.South && frontage <= LotFrontage.East &&
            (!hasEntrance || Finite(entrance)) && (!hasDriveway || Finite(driveway)) &&
            float.IsFinite(buildingCenter.x) && float.IsFinite(buildingCenter.y) &&
            buildingSize.x > 0 && buildingSize.x <= LotDefinition.MaxTiles * LotDefinition.TileMeters &&
            buildingSize.y > 0 && buildingSize.y <= LotDefinition.MaxTiles * LotDefinition.TileMeters);

        internal string BuildingFitProblem(LotDefinition lot, Rect bounds, LotAssetCatalog catalog)
        {
            if (!Contains(lot.LandBounds, bounds.min) || !Contains(lot.LandBounds, bounds.max))
                return "Keep the building inside the dry area of the lot.";
            foreach (var item in lot.objects)
            {
                var other = catalog ? catalog.Find(item.assetId) : null;
                if (other != null && bounds.Overlaps(LotRuntimeLoader.Footprint(item, other)))
                    return "Keep the building clear of other lot assets.";
            }
            return null;
        }

        internal bool Fits(LotDefinition lot) => FitProblem(lot) == null;

        internal string FitProblem(LotDefinition lot)
        {
            var slot = BuildingSlot;
            var land = lot.LandBounds;
            if (!Contains(land, slot.min) || !Contains(land, slot.max))
                return "Fit the building slot inside the dry area of the lot.";
            if (hasEntrance && !Contains(slot, entrance))
                return "Keep the house entrance inside the building slot.";
            if (hasDriveway)
            {
                var bounds = new Rect(-lot.Size * .5f, lot.Size);
                var frontBounds = LotGroundOverlay.QuarterTurn(bounds, -(int)frontage);
                var point = LotGroundOverlay.QuarterTurn(driveway, -(int)frontage);
                if (!Contains(land, driveway) || Mathf.Abs(point.y - frontBounds.yMin) > .001f)
                    return "Put the driveway entrance on the dry frontage edge of the lot.";
            }
            return null;
        }

        private static bool Finite(Vector2 point) => float.IsFinite(point.x) && float.IsFinite(point.y);

        private static bool Contains(Rect bounds, Vector2 point)
        {
            const float tolerance = .001f;
            return point.x >= bounds.xMin - tolerance && point.y >= bounds.yMin - tolerance &&
                point.x <= bounds.xMax + tolerance && point.y <= bounds.yMax + tolerance;
        }
    }
}
