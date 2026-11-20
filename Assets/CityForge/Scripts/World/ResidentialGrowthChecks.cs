#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.Editor
{
    public static class ResidentialGrowthChecks
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
        private static void Require(bool value, string message) { if (!value) throw new Exception(message); }

        public static void Run()
        {
            var type = typeof(World.PrototypeRegionView);
            var root = new GameObject("Disposable residential growth checks");
            var view = root.AddComponent<World.PrototypeRegionView>();
            T Field<T>(string name) => (T)type.GetField(name, Instance).GetValue(view);
            object Call(string name, params object[] args) => type.GetMethod(name, Instance).Invoke(view, args);
            var zones = Field<Dictionary<Vector2Int, World.TownZone>>("townZones");
            var wealth = Field<Dictionary<Vector2Int, LotWealth>>("zoneWealth");
            var facing = Field<Dictionary<Vector2Int, World.TownZoneFacing>>("residentialFacing");
            var sizes = Field<Dictionary<Vector2Int, Vector2Int>>("zoneLotSizes");
            var anchors = Field<Dictionary<Vector2Int, Vector2Int>>("zoneLotAnchors");
            try
            {
                foreach (var zone in new[] { World.TownZone.Residential1, World.TownZone.Residential2, World.TownZone.Residential3 })
                {
                    Require((bool)type.GetMethod("SupportsLotGrowth", Static).Invoke(null, new object[] { zone }), zone + " excluded from growth");
                    zones.Clear(); wealth.Clear(); facing.Clear(); sizes.Clear(); anchors.Clear();
                    var rect = new RectInt(2, 3, 3, 3);
                    foreach (var cell in rect.allPositionsWithin)
                    {
                        zones[cell] = zone; wealth[cell] = LotWealth.Low; facing[cell] = World.TownZoneFacing.South;
                        Call("SetZonedLotSize", cell, rect.min, World.TownZoneFacing.South, new Vector2Int(3, 3));
                    }
                    bool Matches() => (bool)Call("HasGrowthZoning", rect, zone, World.TownZoneFacing.South, LotWealth.Low, new Vector2Int(3, 3));
                    Require(Matches(), zone + " rejected uniform parcel");
                    foreach (var smaller in new[] { new Vector2Int(2, 2), new Vector2Int(2, 3), Vector2Int.one })
                        Require(!(bool)Call("HasGrowthZoning", new RectInt(rect.min, smaller), zone,
                            World.TownZoneFacing.South, LotWealth.Low, new Vector2Int(3, 3)), zone + " accepted a smaller layout inside a fixed parcel");
                    wealth[rect.min] = LotWealth.High;
                    Require(!Matches(), zone + " grew across wealth boundary"); wealth[rect.min] = LotWealth.Low;
                    zones[rect.min] = World.TownZone.Commercial1;
                    Require(!Matches(), zone + " grew across zone boundary"); zones[rect.min] = zone;
                    anchors[rect.min] = rect.min + Vector2Int.one;
                    Require(!Matches(), zone + " crossed exact parcel anchor"); anchors[rect.min] = rect.min;
                    var captured = view.CaptureTownZones();
                    World.PrototypeRegionView.ValidateTownZones(captured, 1024, World.PrototypeRegionView.TownZoneGridVersion);
                    zones.Clear(); wealth.Clear(); facing.Clear(); sizes.Clear(); anchors.Clear();
                    Call("RestoreTownZones", captured, World.PrototypeRegionView.TownZoneGridVersion);
                    Require(Matches(), zone + " lost parcel, wealth or frontage on restore");
                    Field<HashSet<Vector2Int>>("localGrowthCells").Clear();
                    Call("QueueZoningGrowth", rect);
                    Require(Field<HashSet<Vector2Int>>("localGrowthCells").Count == 9, zone + " did not queue zoned cells");
                    Require(float.IsFinite(Field<float>("nextLotGrowth")), zone + " did not schedule growth");
                }
                Require(!(bool)type.GetMethod("SupportsLotGrowth", Static).Invoke(null, new object[] { World.TownZone.Industrial1 }), "Industrial growth changed");
                Require((bool)type.GetMethod("SupportsLotGrowth", Static).Invoke(null, new object[] { World.TownZone.Commercial1 }), "C1 growth regressed");
                Require((bool)type.GetMethod("SupportsLotGrowth",Static).Invoke(null,new object[]{World.TownZone.Commercial2}),"C2 row growth excluded");
                Require((bool)type.GetMethod("SupportsLotGrowth",Static).Invoke(null,new object[]{World.TownZone.Commercial3}),"C3 row growth excluded");
                var catalog = LotAssetCatalog.Load();
                var building = catalog.entries.First(a => a.growableZone == World.TownZone.Residential2 && a.growableWealth == LotWealth.Low && a.prefab);
                var layout = new LotDefinition { type = LotType.Zoning, width = 3, length = 3, firstEraId = "founders",
                    zoningLayout = new LotZoningLayout { zone = World.TownZone.Residential2, wealth = LotWealth.Low,
                        buildingSize = new Vector2(6, 6), overrideBuildingFit = true, restrictGrowthBuildings = true,
                        growthBuildingIds = new[] { building.id } } };
                Require(LotGrowableBuildings.Candidates(layout, catalog).Single().assetId == building.id, "R2 import not selected for matching lot");
                layout.zoningLayout.zone = World.TownZone.Residential3;
                Require(LotGrowableBuildings.Candidates(layout, catalog).Length == 0, "R3 borrowed an R2 building");
                // Early-era R3 excludes low wealth; medium-wealth tags exercise the shared candidate path.
                var original = building.growableZone;var originalWealth=building.growableWealth;
                try
                {
                    building.growableZone = World.TownZone.Residential3;
                    Require(LotGrowableBuildings.Candidates(layout,catalog).Length==0,"Founders R3 accepted low wealth");
                    building.growableWealth=LotWealth.Medium;layout.zoningLayout.wealth=LotWealth.Medium;
                    Require(LotGrowableBuildings.Candidates(layout, catalog).Single().assetId == building.id, "R3 matching content cannot grow");
                }
                finally { building.growableZone = original;building.growableWealth=originalWealth; }
                Debug.Log("RESIDENTIAL GROWTH CHECKS PASS: R1/R2/R3 scheduling, parcel/wealth/density boundaries, save restoration, R2/R3 building selection, C1 preserved.");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
#endif
