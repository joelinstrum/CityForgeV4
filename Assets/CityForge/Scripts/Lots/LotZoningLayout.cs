using System;
using CityForge.World;
using UnityEngine;

namespace CityForge.Lots
{
    public enum LotWealth { None, Low, Medium, High }

    [Serializable]
    public struct LotZoningLayout
    {
        public TownZone zone;
        public LotWealth wealth;
        public Vector2 buildingCenter;
        public Vector2 buildingSize;

        public bool Configured => zone != TownZone.None;
        public Rect BuildingSlot => new Rect(buildingCenter - buildingSize * .5f, buildingSize);

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
            float.IsFinite(buildingCenter.x) && float.IsFinite(buildingCenter.y) &&
            buildingSize.x > 0 && buildingSize.x <= LotDefinition.MaxTiles * LotDefinition.TileMeters &&
            buildingSize.y > 0 && buildingSize.y <= LotDefinition.MaxTiles * LotDefinition.TileMeters);

        internal bool Fits(LotDefinition lot)
        {
            var slot = BuildingSlot;
            var bounds = lot.LandBounds;
            const float tolerance = .001f;
            return slot.xMin >= bounds.xMin - tolerance && slot.yMin >= bounds.yMin - tolerance &&
                slot.xMax <= bounds.xMax + tolerance && slot.yMax <= bounds.yMax + tolerance;
        }
    }
}
