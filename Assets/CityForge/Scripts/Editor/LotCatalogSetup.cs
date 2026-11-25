using System.Collections.Generic;
using System.IO;
using System.Linq;
using CityForge.Lots;
using CityForge.UI;
using CityForge.World;
using UnityEditor.Animations;
using UnityEditor;
using UnityEngine;

internal static class LotCatalogSetup
{
    private const string DirectoryPath = "Assets/CityForge/Resources/Lots";
    private const string CatalogPath = DirectoryPath + "/LocalCatalog.asset";

    [InitializeOnLoadMethod]
    private static void QueueSetup() => EditorApplication.delayCall += EnsureCatalog;

    [MenuItem("CityForge/Lots/Prepare local catalog")]
    private static void EnsureCatalog()
    {
        foreach (var profile in VehicleProfile.All.Where(profile => !string.IsNullOrEmpty(profile.motionFolder))
            .GroupBy(profile => profile.motionFolder).Select(group => group.First()))
            EnsureVehicleMotion(profile);
        const string meadow = "Assets/CityForge/Art/Environment/MeadowNativeURP/";
        const string resources = "Assets/CityForge/Resources/";
        var house = AssetDatabase.LoadAssetAtPath<GameObject>(meadow +
            "Integration/Buildings/Farmhouse/tripo_convert_6e4dde22-3566-484b-a685-c971607adfc8.fbx");

        var farmhouse = Entry("farmhouse", "Farmhouse", "Buildings", house, new Vector2(12, 12), false, true);
        SetBuildingEra(farmhouse, "residential", "founders");
        farmhouse.distantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            DirectoryPath + "/V3Buildings/Farmhouse/Distant.fbx");
        farmhouse.materialOverride = AssetDatabase.LoadAssetAtPath<Material>(meadow +
            "Integration/Materials/Farmhouse tripo_mat_6e4dde22 LandscapeLit.mat");
        var palms = AssetDatabase.LoadAssetAtPath<Material>(resources + "TropicalCoconut/Palms.mat");

        var candidates = new[]
        {
            farmhouse,
            HitchedHorse(),
            HorseDrawnVehicle("covered-wagon", "Horse-drawn covered wagon", "CoveredWagon", 2.7773728f, 2.45f, 2),
            HorseDrawnVehicle("food-wagon", "Horse-drawn food wagon", "FoodWagon", 2.301941f, 2.02f),
            HorseDrawnVehicle("horse-and-buggy", "Horse and buggy", "Buggy", 2.644676f, 2.05f),
            HorseDrawnVehicle("lumber-wagon", "Horse-drawn lumber wagon", "LumberWagon", 2.4001557f, 2.12f),
            FoundersBuilding("timber-council-hall", "Timber Council Hall", "TimberCouncilHall", 12f),
            FoundersBuilding("stone-courthouse", "Stone Courthouse", "StoneCourthouse", 12f),
            FoundersBuilding("brick-state-house", "Brick State House", "BrickStateHouse", 12f),
            FoundersBuilding("harbor-council-house", "Harbor Council House", "HarborCouncilHouse", 12f),
            FoundersBuilding("agrarian-assembly-hall", "Agrarian Assembly Hall", "AgrarianAssemblyHall", 12f),
            MediumWealthStoneHouse(),
            MediumWealthHome("medium-wealth-old-yeller", "Old Yeller", "MediumWealthOldYeller", 10.6f),
            MediumWealthHome("medium-wealth-deep-blue", "Deep Blue House", "MediumWealthDeepBlue", 10.3f),
            MediumWealthHome("medium-wealth-colonial", "Colonial House", "MediumWealthColonial", 10.3f),
            MediumWealthHome("medium-wealth-brick-colonial", "Brick Colonial House", "MediumWealthBrickColonial", 10.3f),
            MediumWealthHome("medium-wealth-red-house", "Red House", "MediumWealthRedHouse", 9.5f),
            MediumWealthHome("medium-wealth-gray-colonial", "Gray Colonial House", "MediumWealthGrayColonial", 10.3f),
            FoundersResidentialHome("high-wealth-grand-colonial", "Grand Colonial Entryway",
                "HighWealthGrandColonial", 12f, LotWealth.High),
            FoundersResidentialHome("high-wealth-federal-mansion", "Federal Style Mansion",
                "HighWealthFederalMansion", 13f, LotWealth.High),
            FoundersResidentialHome("high-wealth-grand-georgian", "Grand Georgian Mansion",
                "HighWealthGrandGeorgian", 14f, LotWealth.High),
            FoundersResidentialHome("high-wealth-red-brick-georgian", "Red Brick Georgian Mansion",
                "HighWealthRedBrickGeorgian", 14f, LotWealth.High),
            FoundersResidentialHome("high-wealth-vernon", "Georgian Greek Revival Vernon",
                "HighWealthVernon", 16.5f, LotWealth.High),
            FoundersResidentialHome("high-wealth-georgian-stone", "Georgian Stone Mansion Entrance",
                "HighWealthGeorgianStone", 13.5f, LotWealth.High),
            FoundersResidentialHome("high-wealth-georgian-stone-green-trim", "Georgian Stone Mansion Green Trim",
                "HighWealthGeorgianStoneGreenTrim", 13.5f, LotWealth.High),
            FoundersResidentialHome("high-wealth-grand-stucco-manor", "Grand Stucco Manor",
                "HighWealthGrandStuccoManor", 12.5f, LotWealth.High),
            FoundersResidentialHome("high-wealth-regency-southern-mansion", "Regency Southern Mansion",
                "HighWealthRegencySouthernMansion", 13f, LotWealth.High),
            FoundersResidentialHome("high-wealth-southern-mansion-slate", "Southern Mansion with Slate Accents",
                "HighWealthSouthernMansionSlate", 13f, LotWealth.High),
            FoundersResidentialHome("high-wealth-grand-southern-greek-revival", "Grand Southern Greek Revival Mansion",
                "HighWealthGrandSouthernGreekRevival", 15f, LotWealth.High),
            FoundersResidentialHome("high-wealth-mediterranean-villa", "Mediterranean Villa with Burnt Orange Shutters",
                "HighWealthMediterraneanVilla", 14f, LotWealth.High),
            FoundersResidentialHome("high-wealth-georgian-three-dormers", "Georgian Colonial Three Dormers",
                "HighWealthGeorgianThreeDormers", 12f, LotWealth.High),
            R2Townhouse("r3-mid-wealth-barn-roof", "Barn Roof", "R3MidWealthBarnRoof", 7.399569f),
            R2Townhouse("r3-mid-wealth-blue-townhouse", "Blue Townhouse", "R3MidWealthBlueTownhouse", 6.819484f),
            R2Townhouse("r3-mid-wealth-dutch-roof", "Dutch Roof", "R3MidWealthDutchRoof", 8.043133f),
            R2Townhouse("r3-mid-wealth-mansard-roof", "Mansard Roof", "R3MidWealthMansardRoof", 7.116240f),
            R2Townhouse("r3-mid-wealth-stone-dutch", "Stone Dutch", "R3MidWealthStoneDutch", 6.912975f),
            R2Townhouse("r3-mid-wealth-stone-house", "Stone House", "R3MidWealthStoneHouse", 7.001306f),
            R2Townhouse("r3-mid-wealth-3-window-brick-dormers", "3 Window Brick Dormers", "R3MidWealth3WindowBrickDormers", 7.277107f),
            R2Townhouse("r3-mid-wealth-3-window-brick", "3 Window Brick", "R3MidWealth3WindowBrick", 7.206851f),
            R2Townhouse("r3-mid-wealth-painted-white-brick", "Painted White Brick", "R3MidWealthPaintedWhiteBrick", 6.700951f),
            R2Townhouse("r3-mid-wealth-3-story-brick-2", "Three Story Brick 2", "R3MidWealth3StoryBrick2", 6.243569f),
            R2Townhouse("r3-mid-wealth-3-story-yellow", "Three Story Yellow", "R3MidWealth3StoryYellow", 7.142250f),
            R2Townhouse("r3-mid-wealth-3-story-brick", "Three Story Brick", "R3MidWealth3StoryBrick", 7.050409f),
            R2Townhouse("r3-mid-wealth-3-story-yellow-stucco", "Three Story Yellow Stucco", "R3MidWealth3StoryYellowStucco", 6.923958f),
            R2Townhouse("r3-mid-wealth-2-story-brick", "Two Story Brick", "R3MidWealth2StoryBrick", 6.562879f),
            R2Townhouse("r3-mid-wealth-2-story-tan-wood", "Two Story Tan Wood", "R3MidWealth2StoryTanWood", 6.335295f),
            R2Townhouse("r3-mid-wealth-2-story-yellow-house", "Two Story Yellow House", "R3MidWealth2StoryYellowHouse", 5.978082f),
            R2LowWealthHome("r2-four-story-low-wealth-stone-base", "Four Story Low Wealth Stone Base", "R2FourStoryLowWealthStoneBase", 10.818288f),
            R2LowWealthHome("r2-four-story-shanty", "Four Story Shanty", "R2FourStoryShanty", 12.607938f),
            R2LowWealthHome("r2-low-wealth-brick-apartment", "Low Wealth Brick Apartment", "R2LowWealthBrickApartment", 13.325564f),
            R2LowWealthHome("r2-low-wealth-double-entry", "Low Wealth Double Entry", "R2LowWealthDoubleEntry", 11.602993f),
            R2LowWealthHome("r2-low-wealth-duplex", "Low Wealth Duplex", "R2LowWealthDuplex", 13.081451f),
            R2LowWealthHome("r2-low-wealth-res-stone-apartment", "Low Wealth Res Stone Apartment", "R2LowWealthResStoneApartment", 11.640968f),
            R2LowWealthHome("r2-low-wealth-res-trio-2", "Low Wealth Res Trio 2", "R2LowWealthResTrio2", 16.719370f),
            R2LowWealthHome("r2-low-wealth-stone-house-2", "Low Wealth Stone House 2", "R2LowWealthStoneHouse2", 12.289818f),
            R2LowWealthHome("r2-low-wealth-trio", "Low Wealth Trio", "R2LowWealthTrio", 15.387814f),
            R2LowWealthHome("r2-low-wealth-apartment-collapse", "Low Wealth Apartment Collapse", "R2LowWealthApartmentCollapse", 14.357811f),
            ArtDecoTownhome("r2-art-deco-townhome-01", "Green Cornice Bay", "R2ArtDecoTownhome01", 17.8812650f, -3.609878f, 3.573470f, -8.940632f),
            ArtDecoTownhome("r2-art-deco-townhome-02", "Brownstone Bay", "R2ArtDecoTownhome02", 17.6031329f, -3.056328f, 3.056602f, -8.801566f),
            ArtDecoTownhome("r2-art-deco-townhome-03", "Brick Three Bay", "R2ArtDecoTownhome03", 17.5816773f, -3.046315f, 3.049321f, -8.790839f),
            ArtDecoTownhome("r2-art-deco-townhome-04", "Green Cornice Wide Bay", "R2ArtDecoTownhome04", 16.5014203f, -2.735046f, 2.942048f, -8.250710f),
            ArtDecoTownhome("r2-art-deco-townhome-05", "Brownstone Three Story", "R2ArtDecoTownhome05", 17.6730403f, -2.937979f, 2.859773f, -8.836520f),
            ArtDecoTownhome("r2-art-deco-townhome-06", "Limestone Shutters", "R2ArtDecoTownhome06", 14.9590225f, -2.468996f, 2.436817f, -7.479511f),
            ArtDecoTownhome("r2-art-deco-townhome-07", "Brick Paired Bays", "R2ArtDecoTownhome07", 15.3724633f, -2.560041f, 2.602777f, -7.686232f),
            ArtDecoTownhome("r2-art-deco-townhome-08", "Brick Three Bay Steps", "R2ArtDecoTownhome08", 16.6482557f, -2.827796f, 2.849298f, -8.324128f),
            ArtDecoTownhome("r2-art-deco-townhome-09", "Limestone Deco Crown", "R2ArtDecoTownhome09", 16.8477555f, -2.626389f, 2.630842f, -8.423878f),
            ArtDecoTownhome("r2-art-deco-townhome-10", "Brick Projecting Bay", "R2ArtDecoTownhome10", 13.9257337f, -2.815253f, 2.812207f, -6.962867f),
            ArtDecoTownhome("r2-art-deco-townhome-11", "Brick Entry Bay", "R2ArtDecoTownhome11", 12.1550867f, -2.476389f, 2.497515f, -6.077543f),
            ArtDecoTownhome("r2-art-deco-townhome-12", "Green Cornice Double Bay", "R2ArtDecoTownhome12", 13.7193686f, -2.984711f, 2.992190f, -6.859684f),
            ArtDecoCommercial("c2-art-deco-hotel", "Hotel", "C2ArtDecoHotel", 35.4270937f, -10.260852f, 10.193405f, -17.713547f),
            ArtDecoCommercial("c2-art-deco-theater", "Theater", "C2ArtDecoTheater", 17.6713855f, -6.130346f, 6.130346f, -6.926136f),
            ArtDecoCommercial("c2-art-deco-kingsley-department-store", "Kingsley Department Store", "C2ArtDecoKingsley", 17.1641594f, -8.190216f, 8.311699f, -7.870271f),
            ArtDecoCommercial("c2-art-deco-dry-goods", "Dry Goods", "C2ArtDecoDryGoods", 28.0867770f, -6.507189f, 6.656571f, -13.422217f),
            ArtDecoCommercial("c2-art-deco-cartier-corner", "Cartier Corner", "C2ArtDecoCartierCorner", 12.4979254f, -6.232958f, 5.910493f, -5.514523f),
            ArtDecoCommercial("c2-art-deco-metro-bank", "Metro Bank", "C2ArtDecoMetroBank", 23.5535981f, -5.429439f, 5.437563f, -11.776799f),
            ArtDecoCityCenter(),
            FrontierLogCabin(),
            SmallColonialCabin(),
            FoundersHome("low-wealth-front-gable", "Front Gable House", "LowWealthFrontGable"),
            FoundersHome("low-wealth-vertical-siding-front-gable", "Vertical Siding Front Gable House",
                "LowWealthVerticalSidingFrontGable", span: 9.6f, normalExtension: ".jpg"),
            FoundersHome("low-wealth-front-gable-cottage", "Front Gable Cottage",
                "LowWealthFrontGableCottage", span: 7.5f, normalExtension: ".jpg"),
            FoundersHome("low-wealth-side-gable", "Side Gable House", "LowWealthSideGable"),
            V3Building("wooden-cottage", "Wooden Cottage", "WoodenCottage", "residential", "founders", 8f),
            C2MediumWealthShop("c2-mid-wealth-ye-olde-inn", "Ye Olde Inn", "C2MidWealthYeOldeInn", 10.195834f),
            C2MediumWealthShop("c2-mid-wealth-kings-arms", "King’s Arms", "C2MidWealthKingsArms", 8.646178f),
            C2MediumWealthShop("c2-mid-wealth-apothecary-3-stories", "Apothecary · Three Stories", "C2MidWealthApothecary3Stories", 8.489448f),
            C2MediumWealthShop("c2-mid-wealth-apothecary", "Apothecary", "C2MidWealthApothecary", 11.888233f),
            C2MediumWealthShop("c2-mid-wealth-books", "Bookshop", "C2MidWealthBooks", 11.543073f),
            C2MediumWealthShop("c2-mid-wealth-ceramics", "Ceramics", "C2MidWealthCeramics", 7.510434f),
            C2MediumWealthShop("c2-mid-wealth-chevron-store", "Chevron Store", "C2MidWealthChevronStore", 6.717772f),
            C2MediumWealthShop("c2-mid-wealth-coffee", "Coffee Shop", "C2MidWealthCoffee", 7.340046f),
            C2MediumWealthShop("c2-mid-wealth-commerican-merchants-shop", "Merchants Shop", "C2MidWealthCommericanMerchantsShop", 13.357366f),
            C2MediumWealthShop("c2-mid-wealth-dry-goods", "Dry Goods", "C2MidWealthDryGoods", 10.986853f),
            C2MediumWealthShop("c2-mid-wealth-pottery-barn", "Pottery Barn", "C2MidWealthPotteryBarn", 6.146081f),
            C2MediumWealthShop("c2-mid-wealth-sea-urchin", "Sea Urchin", "C2MidWealthSeaUrchin", 7.075263f),
            FoundersShop("plymouth-store", "Plymouth Store", "PlymouthStore", 9.26f, ".jpeg"),
            FoundersShop("founders-bakery", "Bakery", "FoundersBakery", 8f),
            FoundersShop("founders-blacksmith", "Blacksmith", "FoundersBlacksmith", 8f),
            FoundersShop("founders-cooperage", "Cooperage", "FoundersOldBarn", 8.5f),
            FoundersShop("founders-tavern", "Tavern", "FoundersTavern", 9f),
            FoundersShop("founders-shop-blacksmith-2", "Blacksmith (False Front)", "FoundersBlacksmithTwo", 8.5f),
            FoundersShop("founders-shop-bootmaker", "Boot & Shoe Maker", "FoundersBootmaker", 6.5f),
            FoundersShop("founders-shop-apothecary", "Apothecary", "FoundersApothecary", 7.5f),
            FoundersShop("founders-shop-general-store", "General Store", "FoundersGeneralStoreTwo", 8.6f),
            FoundersShop("founders-shop-hardware", "Hardware Store", "FoundersHardware", 9f),
            FoundersShop("founders-shop-livery", "Livery Stable", "FoundersLivery", 9f),
            FoundersShop("founders-shop-printing", "Printing Office", "FoundersPrintingOffice", 8f),
            FoundersShop("founders-shop-saloon", "Saloon", "FoundersSaloonTwo", 8.6f),
            FoundersShop("founders-shop-wagon-works", "Wagon Works", "FoundersWagonWorks", 9.2f),
            FoundersShop("founders-shop-right-corner-cooperage", "Right Corner · Cooperage", "FoundersRightCornerCooperage", 7.3323f),
            FoundersShop("founders-shop-right-corner-dry-goods", "Right Corner · Dry Goods", "FoundersRightCornerDryGoods", 7.8836f),
            FoundersShop("founders-shop-right-corner-inn", "Right Corner · Inn", "FoundersRightCornerInn", 8.6f),
            FoundersCivic("founders-academy", "Academy", "FoundersAcademy", 17.2136713f, "education"),
            FoundersCivic("founders-courthouse", "Courthouse", "FoundersCourthouse", 15.4768819f, "government"),
            FoundersCivic("founders-infirmary", "Infirmary", "FoundersInfirmary", 10.2721535f, "healthcare"),
            FoundersCivic("founders-schoolhouse", "Schoolhouse", "FoundersSchoolhouse", 9.4058629f, "education"),
            FoundersCivic("founders-college", "College", "FoundersCollege", 34.4019896f, "education"),
            FoundersCivic("new-england-church", "New England Church", "NewEnglandChurch", 18.482987f, "culture"),
            FoundersCivic("chapel", "Chapel", "FoundersChapel", 13.479562f),
            FoundersCivic("constable-station", "Constable Station", "FoundersConstableStation", 13.100854f * 1.18f),
            V3Building("small-school", "Small School", "SmallSchool", "civics", "founders", 9f, ".jpg", ".png"),
            V3Building("lumber-mill", "Lumber Mill", "LumberMill", "industrial", "founders", 11.95f),
            V3Building("work-tent", "Lumber Camp Work Tent", "WorkTent", "industrial", "founders", 5f),
            V3Building("stone-quarry", "Stone Quarry", "StoneQuarry", "industrial", "founders", 16f),
            V3Building("queen-anne-house", "Queen Anne House", "QueenAnneHouse", "residential", "victorian", 12f),
            V3Fence("v3-white-picket", "White picket fence", "PicketFenceV01",
                "CF_Prop_PicketFence_Straight_v01", "", 2.4f, repeatable: true, iron: false),
            V3Fence("v3-wrought-iron", "Wrought iron fence", "WroughtIronFenceV01",
                "CF_WroughtIronFence_Straight_LOD0_v01", "", 6.4f, repeatable: true),
            V3Fence("stone-column-wrought-iron-section", "Wrought iron fence with stone columns",
                "StoneColumnWroughtIronSection", "Model", "", 1.5f, byHeight: true, extension: ".jpg"),
            V3Fence("beige-stone-wrought-iron-section", "Beige stone wrought iron fence section",
                "BeigeStoneWroughtIronSection", "Model", "", 1.5f, byHeight: true, extension: ".jpg"),
            V3Fence("beige-stone-wrought-iron-partial", "Beige stone column with wrought iron partial",
                "BeigeStoneWroughtIronPartial", "Model", "", 1.5f, byHeight: true, extension: ".jpg"),
            V3Fence("v3-wrought-iron-corner", "Wrought iron corner", "WroughtIronFenceV01",
                "CF_WroughtIronFence_Corner_LShape_LOD0_v02", "corner-", 2.55f),
            V3Fence("v3-decorative-iron", "Decorative iron garden fence", "WroughtIronVariationsV01",
                "CF_Prop_DecorativeIronFence_v01", "decorative-", 1.6f, byHeight: true, extension: ".jpg"),
            V3Fence("v3-ornate-iron-gate", "Ornate iron gate", "WroughtIronVariationsV01",
                "CF_Prop_OrnateIronGate_v01", "gate-", 2.2f, byHeight: true, extension: ".jpg"),
            StreetFurniture("ornate-bench", "Ornate park bench", "Seating", 1.8f),
            StreetFurniture("simple-street-lamppost", "Simple street lamppost", "Lighting", 3.6f, byHeight: true),
            StreetFurniture("three-lantern-lamppost", "Three-lantern lamppost", "Lighting", 4.5f, byHeight: true),
            // The district Streetscape picker also reads these shared catalog entries.
            StreetFurniture("18th-century-lamppost", "18th-century lamppost", "Lighting", 3.6f, byHeight: true),
            V3Fence("founders-fence-panel", "Founders fence panel", "FoundersFence",
                "Model", "", 2.4f, repeatable: true, iron: false, extension: ".jpg", normalExtension: ".png"),
            V3Fence("weathered-picket-section", "Weathered picket fence section", "WeatheredPicketSection",
                "Model", "", 2.4f, repeatable: true, iron: false, extension: ".jpg"),
            V3Fence("weathered-picket-partial", "Weathered picket fence partial", "WeatheredPicketPartial",
                "Model", "", 2.4f, iron: false, extension: ".jpg"),
            V3Fence("flowered-picket-section", "Picket fence section with flowers", "FloweredPicketSection",
                "Model", "", 2.4f, repeatable: true, iron: false, extension: ".jpg"),
            V3Fence("flowered-picket-partial", "Partial picket fence with flowers", "FloweredPicketPartial",
                "Model", "", 2.4f, iron: false, extension: ".jpg"),
            Fence("wood-fence", "Wood fence", "01"),
            V3Fence("wood-fence-section", "Rustic Fence", "RusticFence",
                "Model", "", 2.4f, repeatable: true, iron: false),
            V3Fence("rustic-fence-half", "Rustic Fence Half Section", "RusticFenceHalf",
                "Model", "", 1.2f, repeatable: true, iron: false),
            Fence("meadow-fence-02", "Meadow wood fence 02", "02"),
            Fence("meadow-fence-03", "Meadow wood fence 03", "03"),
            Fence("meadow-fence-04", "Meadow wood fence 04", "04"),
            Fence("meadow-fence-05", "Meadow wood fence 05", "05"),
            Tree("poplar", "Poplar", "Deciduous", meadow + "Showcase/Flora/prefab_Baked_01_Poplar.prefab", 3),
            LondonPlane("a"),
            LondonPlane("b"),
            Tree("silver-maple", "Silver maple", "Deciduous", resources + "Trees/ForestBroadleaf/SilverMapleA/Summer.prefab", 5),
            Tree("siberian-elm", "Siberian elm", "Deciduous", resources + "Trees/SiberianElm/SiberianElmA.prefab", 5),
            Tree("balsam-fir", "Balsam fir", "Evergreen", resources + "Trees/MountainEvergreen/BalsamFir.prefab", 4),
            Tree("fraser-fir", "Fraser fir", "Evergreen", resources + "Trees/MountainEvergreen/FraserFir.prefab", 4),
            Tree("cilician-fir", "Cilician fir", "Evergreen", resources + "Trees/MountainEvergreen/CilicianFir.prefab", 4),
            Tree("coconut-palm", "Coconut palm", "Tropical", resources + "TropicalCoconut/Palm0_LOD0.fbx", 4, palms),
            Tree("coconut-palm-leaning", "Coconut palm (variant 2)", "Tropical", resources + "TropicalCoconut/Palm3_LOD0.fbx", 4, palms),
            Tree("coconut-palm-variant-3", "Coconut palm (variant 3)", "Tropical", resources + "TropicalCoconut/Palm6_LOD0.fbx", 4, palms)
        };

        var meadowPoplars = Enumerable.Range(2, 7).Select(variant => Tree(
            $"meadow-poplar-{variant:00}", $"Meadow poplar {variant:00}", "Deciduous",
            meadow + $"Showcase/Flora/prefab_Baked_{variant:00}_Poplar.prefab", 3));

        var foundersHomes = Enumerable.Range(1, 9).Select(variant => FoundersHome(
            $"low-wealth-home-{variant:00}", $"Founder's Home {variant:00}", $"LowWealthHome{variant:00}",
            span: variant == 6 ? 9.375f : 7.5f));

        // Approximate door leaf heights from equal-scale orthographic renders, excluding trim and steps.
        // House 04 supplies the original 9 m reference before per-house adjustments.
        var mediumHomeDoorHeights = new[] { 71f, 71f, 51f, 83f, 105f, 87f, 59f, 65f, 59f };
        var mediumHomeScaleAdjustments = new[] { 1.15f, 1.20f, 1f, 1.10f, 1.10f, 1.10f, 1f, .85f, 1f };
        var mediumHomes = Enumerable.Range(1, mediumHomeDoorHeights.Length).Select(variant =>
        {
            int index = variant - 1;
            float span = 9f * mediumHomeDoorHeights[3] / mediumHomeDoorHeights[index] * mediumHomeScaleAdjustments[index];
            return MediumWealthHome($"medium-wealth-home-{variant:00}", $"Founders House · $$ {variant:00}",
                $"MediumWealthHome{variant:00}", span, ".png");
        });

        var catalog = AssetDatabase.LoadAssetAtPath<LotAssetCatalog>(CatalogPath);
        var entries = (catalog ? catalog.entries : System.Array.Empty<LotAssetCatalogEntry>()).ToList();
        bool changed = false;
        foreach (var entry in candidates.Concat(CityForge.EditorTools.RebuiltOfficeCatalog.CatalogEntries()).Concat(ConfiguredVehicles()).Concat(PlaceableSeasonalTrees.CatalogEntries()).Concat(CityForge.EditorTools.FoxOakPreparation.OakEntries()).Concat(PurchasedTreeCatalog.CatalogEntries()).Concat(CityForge.EditorTools.PlaneTreePreparation.CatalogEntries()).Concat(CityForge.EditorTools.CityTreePreparation.CatalogEntries()).Concat(foundersHomes).Concat(mediumHomes).Concat(meadowPoplars).Concat(ChildrenCharacterSetup.Entries()).Concat(Characters()).Concat(CityForge.EditorTools.ArtDecoCharacterPreparation.CatalogEntries()).Concat(Landscaping()).Concat(Corn()).Concat(Wheat()).Where(item => item.prefab))
        {
            if (entry.category != "Buildings" && !entry.IsCharacter)
                entry.distantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{DirectoryPath}/PropLOD/{entry.id}.fbx");
            if (entry.category == "Buildings" && entry.group == "civics" && entry.eraId == "founders")
                entry.additionalEraIds = new[] { "industrial" };
            entry.PrepareFenceSpan();
            var existing = entries.Find(item => item != null && item.id == entry.id);
            if (existing == null)
            {
                entries.Add(entry);
                changed = true;
                continue;
            }
            // Prepared buildings carry their classification and prefab references in the package.
            // User property overrides are applied separately by LotBuildingPropertiesStore.
            if (entry.prefab.GetComponent<CityForge.World.AuthoredBuildingLighting>())
            {
                if (JsonUtility.ToJson(existing) != JsonUtility.ToJson(entry))
                { entries[entries.IndexOf(existing)] = entry; changed = true; }
                continue;
            }
            if (entry.id is "oak-pilot-a" or "oak-pilot-b")
            {
                if (existing.prefab != entry.prefab || existing.preserveTreeMaterials != entry.preserveTreeMaterials)
                { entries[entries.IndexOf(existing)] = entry; changed = true; }
                continue;
            }
            if (entry.preserveTreeMaterials)
            {
                if (!existing.preserveTreeMaterials || existing.prefab != entry.prefab ||
                    existing.autumnPrefab != entry.autumnPrefab || existing.winterPrefab != entry.winterPrefab ||
                    existing.label != entry.label || existing.footprint != entry.footprint)
                {
                    entries[entries.IndexOf(existing)] = entry;
                    changed = true;
                }
                continue;
            }
            if(existing.preserveEmissionColors!=entry.preserveEmissionColors){existing.preserveEmissionColors=entry.preserveEmissionColors;changed=true;}
            if (existing.nightWindowGlass != entry.nightWindowGlass)
            {
                existing.nightWindowGlass = entry.nightWindowGlass;
                changed = true;
            }
            if(entry.rowGeometry!=null&&JsonUtility.ToJson(existing.rowGeometry)!=JsonUtility.ToJson(entry.rowGeometry))
            {
                existing.rowGeometry=entry.rowGeometry;changed=true;
            }
            if (entry.id == "v3-building-constable-station" &&
                (existing.footprint != entry.footprint || existing.buildingDimensionsMeters != entry.buildingDimensionsMeters))
            {
                existing.footprint = entry.footprint;
                existing.buildingDimensionsMeters = entry.buildingDimensionsMeters;
                changed = true;
            }
            if (existing.distantPrefab != entry.distantPrefab)
            {
                existing.distantPrefab = entry.distantPrefab;
                changed = true;
            }
            if (entry.autumnPrefab && entry.winterPrefab && (existing.prefab != entry.prefab ||
                existing.autumnPrefab != entry.autumnPrefab || existing.winterPrefab != entry.winterPrefab))
            {
                existing.prefab = entry.prefab;
                existing.autumnPrefab = entry.autumnPrefab;
                existing.winterPrefab = entry.winterPrefab;
                existing.label = entry.label;
                existing.footprint = entry.footprint;
                changed = true;
            }
            else if (((entry.category == "Landscaping" && (entry.group == "Agriculture" || entry.id == "boxwood-curved-02")) ||
                      (entry.category == LotAssetCatalog.StreetFurniture && entry.group == "Lighting") ||
                      entry.id.StartsWith("v3-building-medium-wealth-home-", System.StringComparison.Ordinal) ||
                      entry.id is "v3-building-r2-low-wealth-res-trio-2" or
                          "v3-building-low-wealth-vertical-siding-front-gable" or
                          "v3-building-low-wealth-front-gable-cottage" or
                          "v3-building-founders-shop-right-corner-cooperage" or
                          "v3-building-founders-shop-right-corner-dry-goods") &&
                (existing.prefab != entry.prefab || existing.footprint != entry.footprint ||
                 existing.materialOverride != entry.materialOverride))
            {
                entries[entries.IndexOf(existing)] = entry;
                changed = true;
            }
            else if (entry.id == "wood-fence-section" && existing.prefab != entry.prefab)
            {
                entries[entries.IndexOf(existing)] = entry;
                changed = true;
            }
            else if ((existing.fenceSpan <= 0 && entry.fenceSpan > 0) || (!existing.fencePost && entry.fencePost))
            {
                existing.fenceSpan = entry.fenceSpan;
                existing.fenceDepth = entry.fenceDepth;
                existing.fenceCenter = entry.fenceCenter;
                existing.fenceAlongZ = entry.fenceAlongZ;
                existing.fencePost = entry.fencePost;
                existing.fencePostCenter = entry.fencePostCenter;
                existing.fencePostWidth = entry.fencePostWidth;
                existing.fenceRailLift = entry.fenceRailLift;
                changed = true;
            }
            else if (entry.subcategory == "founders-buildings" &&
                (existing.prefab != entry.prefab || existing.materialOverride != entry.materialOverride ||
                    existing.footprint != entry.footprint || existing.subcategory != entry.subcategory))
            {
                entries[entries.IndexOf(existing)] = entry;
                changed = true;
            }
            else if (entry.category == LotAssetCatalog.Transport &&
                VehicleProfile.All.Any(profile => profile.assetId == entry.id && profile.team) &&
                (existing.prefab != entry.prefab || existing.footprint != entry.footprint || existing.fitModel != entry.fitModel))
            {
                existing.prefab = entry.prefab;
                existing.footprint = entry.footprint;
                existing.fitModel = entry.fitModel;
                changed = true;
            }
            else if ((entry.category == "Buildings" || entry.IsCharacter) &&
                (existing.group != entry.group || existing.eraId != entry.eraId ||
                    !(existing.additionalEraIds ?? System.Array.Empty<string>()).SequenceEqual(entry.additionalEraIds ?? System.Array.Empty<string>()) ||
                    existing.fromYear != entry.fromYear || existing.toYear != entry.toYear ||
                    existing.growableZone != entry.growableZone || existing.growableWealth != entry.growableWealth))
            {
                existing.group = entry.group;
                existing.eraId = entry.eraId;
                existing.additionalEraIds = entry.additionalEraIds;
                existing.fromYear = entry.fromYear;
                existing.toYear = entry.toYear;
                existing.growableZone = entry.growableZone;
                existing.growableWealth = entry.growableWealth;
                changed = true;
            }
            else if (string.IsNullOrEmpty(existing.group) && !string.IsNullOrEmpty(entry.group))
            {
                existing.group = entry.group;
                changed = true;
            }
        }
        foreach (var retired in entries.Where(item => item != null && LotAssetCatalog.IsRetiredTree(item.id)))
            if (retired.playerPlaceable) { retired.playerPlaceable = false; changed = true; }
        DeliveryCargoAssetSetup.Prepare();
        if (!changed) return;
        if (!catalog)
        {
            Directory.CreateDirectory(DirectoryPath);
            AssetDatabase.Refresh();
            catalog = ScriptableObject.CreateInstance<LotAssetCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        catalog.entries = entries.ToArray();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }

    private static void SetBuildingEra(LotAssetCatalogEntry entry, string category, string eraId)
    {
        entry.group = category;
        SetEra(entry, eraId);
    }

    private static LotAssetCatalogEntry FoundersBuilding(string id, string label, string folder, float span)
    {
        string root = DirectoryPath + "/FoundersBuildings/" + folder + "/";
        string modelPath = root + "Model.fbx";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var entry = Entry(id, label, "Buildings", model, Vector2.one * span, false, true);
        SetBuildingEra(entry, "civics", "founders");
        entry.distantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Distant.fbx");
        entry.subcategory = "founders-buildings";
        if (!model) return entry;

        if (AssetImporter.GetAtPath(modelPath) is ModelImporter modelImporter &&
            (modelImporter.materialImportMode != ModelImporterMaterialImportMode.None ||
                modelImporter.importAnimation || modelImporter.importCameras || modelImporter.importLights))
        {
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
            modelImporter.importAnimation = modelImporter.importCameras = modelImporter.importLights = false;
            modelImporter.SaveAndReimport();
            entry.prefab = model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        }

        var bounds = FenceBounds(model).size;
        float widest = Mathf.Max(bounds.x, bounds.z);
        if (widest > .001f) entry.footprint = new Vector2(bounds.x, bounds.z) * (span / widest);

        string materialPath = root + "Building.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (!material)
        {
            var color = ImportTexture("Color.jpg", TextureImporterType.Default, true);
            var normal = ImportTexture("Normal.png", TextureImporterType.NormalMap, false);
            var surface = ImportTexture("MetallicSmoothness.png", TextureImporterType.Default, false);
            if (!color || !normal || !surface) return entry;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = entry.label };
            material.SetTexture("_BaseMap", color);
            material.SetTexture("_BumpMap", normal);
            material.SetTexture("_MetallicGlossMap", surface);
            material.SetFloat("_Smoothness", .35f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            AssetDatabase.CreateAsset(material, materialPath);
        }
        entry.materialOverride = material;
        return entry;

        Texture2D ImportTexture(string file, TextureImporterType type, bool srgb)
        {
            var path = root + file;
            if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
                (importer.textureType != type || importer.sRGBTexture != srgb || importer.maxTextureSize != 2048))
            {
                importer.textureType = type;
                importer.sRGBTexture = srgb;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }

    private static LotAssetCatalogEntry SmallColonialCabin()
    {
        var cabin = V3Building("small-colonial-cabin", "Small Colonial Cabin", "SmallColonialCabin",
            "residential", "founders", 7.5f, ".jpg", ".png");
        cabin.growableZone = TownZone.Residential1;
        cabin.growableWealth = LotWealth.Low;
        return cabin;
    }

    private static LotAssetCatalogEntry FrontierLogCabin()
    {
        var cabin = V3Building("frontier-log-cabin", "Frontier Log Cabin", "FrontierLogCabin",
            "residential", "founders", 7.462412f);
        cabin.growableZone = TownZone.Residential1;
        cabin.growableWealth = LotWealth.Low;
        return cabin;
    }

    private static LotAssetCatalogEntry FoundersHome(string id, string label, string folder, float span = 7.5f,
        string normalExtension = ".png")
    {
        var home = V3Building(id, label, folder, "residential", "founders", span, ".jpg", normalExtension);
        home.growableZone = TownZone.Residential1;
        home.growableWealth = LotWealth.Low;
        return home;
    }

    private static IEnumerable<LotAssetCatalogEntry> ConfiguredVehicles() => VehicleProfile.All
        .Where(profile => !string.IsNullOrEmpty(profile.modelFolder) && profile.length > 0 &&
            profile.assetId.StartsWith("v3-vehicle-", System.StringComparison.Ordinal))
        .Select(profile => LotVehicle(profile.assetId.Substring("v3-vehicle-".Length), profile.label,
            profile.modelFolder, profile.EraIds().FirstOrDefault(), profile.length, profile.colorExtension ?? ".jpg"));

    private static LotAssetCatalogEntry LotVehicle(string id, string label, string folder, string era,
        float length, string colorExtension = ".jpg")
    {
        var entry = V3Building(id, label, folder, "", era, length, colorExtension, modelCategory: LotAssetCatalog.Transport);
        entry.id = "v3-vehicle-" + id;
        entry.playerPlaceable = true;
        return entry;
    }

    private static LotAssetCatalogEntry HorseDrawnVehicle(string id, string label, string folder, float height, float hitch, int horseCount = 1)
    {
        var wagon = LotVehicle(id, label, folder, "founders", 1f);
        var horse = LotVehicle("horse", "Horse", "Horse", "founders", 1f);
        if (!wagon.prefab || !horse.prefab) return Entry(wagon.id, label, LotAssetCatalog.Transport, null, Vector2.zero, true, false);
        string path = $"{DirectoryPath}/V3Vehicles/{folder}/Team.prefab";
        var team = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        const string geometryVersion = "CityForgeHorseTeamGeometryV5";
        if (!team || !AssetDatabase.GetLabels(team).Contains(geometryVersion))
        {
            var root = new GameObject(label);
            try
            {
                AddModel(wagon, height, -hitch);
                float horseHeight = VehicleProfile.All.FirstOrDefault(profile => profile.assetId == wagon.id)?.teamHorseHeight ?? 0f;
                for (int i = 0; i < horseCount; i++)
                    AddModel(horse, horseHeight > 0f ? horseHeight : 1.75f, 0f, i - (horseCount - 1) * .5f);
                team = PrefabUtility.SaveAsPrefabAsset(root, path);
                AssetDatabase.SetLabels(team, AssetDatabase.GetLabels(team).Append(geometryVersion).Distinct().ToArray());
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }

            void AddModel(LotAssetCatalogEntry entry, float targetHeight, float z, float x = 0f)
            {
                // Keep fitted dimensions outside the animated FBX root, whose clips may restore its source scale.
                var model = new GameObject(entry.label);
                model.transform.SetParent(root.transform, false);
                var visual = UnityEngine.Object.Instantiate(entry.prefab, model.transform, false);
                if (entry.id == horse.id)
                {
                    var controller = Resources.Load<RuntimeAnimatorController>("Lots/V3Vehicles/Horse/Motion");
                    var animator = model.GetComponentInChildren<Animator>();
                    if (!animator) animator = visual.AddComponent<Animator>();
                    animator.avatar = Resources.LoadAll<Avatar>("Lots/V3Vehicles/Horse/Model").FirstOrDefault();
                    animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false;
                }
                foreach (var animator in model.GetComponentsInChildren<Animator>())
                    if (animator.runtimeAnimatorController) { animator.Rebind(); animator.Update(0f); }
                var bounds = PosedVehicleBounds(model);
                if (bounds.size.y > .001f) model.transform.localScale *= targetHeight / bounds.size.y;
                var fitted = PosedVehicleBounds(model);
                model.transform.localPosition = new Vector3(x - fitted.center.x, -fitted.min.y, z);
                foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => entry.materialOverride).ToArray();
            }
        }
        if (!team) return Entry(wagon.id, label, LotAssetCatalog.Transport, null, Vector2.zero, true, false);
        var size = PosedVehicleBounds(team).size;
        // Team geometry is authored in metres around its hitch; renderer bounds must not refit an animated horse.
        var result = Entry(wagon.id, label, LotAssetCatalog.Transport, team, new Vector2(size.x, size.z), true, false);
        SetEra(result, "founders");
        return result;
    }

    private static Bounds PosedVehicleBounds(GameObject model)
    {
        var result = new Bounds(); bool first = true;
        void Include(Vector3 point)
        {
            if (first) { result = new Bounds(point, Vector3.zero); first = false; }
            else result.Encapsulate(point);
        }
        foreach (var renderer in model.GetComponentsInChildren<Renderer>())
        {
            if (renderer is SkinnedMeshRenderer skin && skin.sharedMesh)
            {
                var mesh = skin.sharedMesh; var vertices = mesh.vertices; var weights = mesh.boneWeights;
                var poses = mesh.bindposes; var bones = skin.bones;
                var matrices = bones.Select((bone, index) => bone.localToWorldMatrix * poses[index]).ToArray();
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (weights.Length != vertices.Length) { Include(renderer.transform.TransformPoint(vertices[i])); continue; }
                    var w = weights[i]; var v = vertices[i]; var point = Vector3.zero;
                    if (w.weight0 > 0) point += matrices[w.boneIndex0].MultiplyPoint3x4(v) * w.weight0;
                    if (w.weight1 > 0) point += matrices[w.boneIndex1].MultiplyPoint3x4(v) * w.weight1;
                    if (w.weight2 > 0) point += matrices[w.boneIndex2].MultiplyPoint3x4(v) * w.weight2;
                    if (w.weight3 > 0) point += matrices[w.boneIndex3].MultiplyPoint3x4(v) * w.weight3;
                    Include(point);
                }
                continue;
            }
            var bounds = renderer.localBounds;
            for (int i = 0; i < 8; i++) Include(renderer.transform.TransformPoint(new Vector3(
                (i & 1) == 0 ? bounds.min.x : bounds.max.x, (i & 2) == 0 ? bounds.min.y : bounds.max.y,
                (i & 4) == 0 ? bounds.min.z : bounds.max.z)));
        }
        return result;
    }

    private static LotAssetCatalogEntry HitchedHorse()
    {
        var horse = LotVehicle("horse", "Horse", "Horse", "founders", 2.3f);
        if (!horse.prefab) return Entry("v3-vehicle-hitched-horse", "Hitched horse", LotAssetCatalog.Transport,
            null, Vector2.zero, true, false);
        string folder = $"{DirectoryPath}/V3Vehicles/Horse/";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "Hitched.prefab");
        if (!prefab)
        {
            var root = new GameObject("Hitched horse");
            try
            {
                LotRuntimeLoader.InstantiateObject(new LotObjectDefinition { assetId = horse.id }, horse, root.transform);
                var wood = AssetDatabase.LoadAssetAtPath<Material>(folder + "HitchingPost.mat");
                if (!wood)
                {
                    wood = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    wood.color = new Color(.26f, .17f, .10f);
                    AssetDatabase.CreateAsset(wood, folder + "HitchingPost.mat");
                }
                var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = "Hitching post";
                post.transform.SetParent(root.transform, false);
                post.transform.localPosition = new Vector3(.55f, .53f, 1.38f);
                post.transform.localScale = new Vector3(.14f, 1.06f, .14f);
                post.GetComponent<Renderer>().sharedMaterial = wood;
                UnityEngine.Object.DestroyImmediate(post.GetComponent<Collider>());
                var tether = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tether.name = "Tether";
                tether.transform.SetParent(root.transform, false);
                var halter = new Vector3(0f, 1.24f, .95f);
                var tie = new Vector3(.55f, 1f, 1.38f);
                tether.transform.localPosition = (halter + tie) * .5f;
                tether.transform.localRotation = Quaternion.LookRotation(tie - halter);
                tether.transform.localScale = new Vector3(.024f, .024f, Vector3.Distance(tie, halter));
                tether.GetComponent<Renderer>().sharedMaterial = wood;
                UnityEngine.Object.DestroyImmediate(tether.GetComponent<Collider>());
                prefab = PrefabUtility.SaveAsPrefabAsset(root, folder + "Hitched.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        var size = FenceBounds(prefab).size;
        var entry = Entry("v3-vehicle-hitched-horse", "Hitched horse", LotAssetCatalog.Transport,
            prefab, new Vector2(size.x, size.z), true, false);
        SetEra(entry, "founders");
        return entry;
    }

    private static void EnsureVehicleMotion(VehicleProfile profile)
    {
        string root = $"{DirectoryPath}/V3Vehicles/{profile.motionFolder}/";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(root + "Motion.controller");
        if (AssetImporter.GetAtPath(root + "Model.fbx") is ModelImporter importer &&
            (importer.animationType != ModelImporterAnimationType.Generic || importer.clipAnimations.Length == 0))
        {
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            var clipsToImport = importer.defaultClipAnimations;
            foreach (var clip in clipsToImport) clip.loopTime = true;
            importer.clipAnimations = clipsToImport;
            importer.SaveAndReimport();
        }
        var clips = AssetDatabase.LoadAllAssetsAtPath(root + "Model.fbx").OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__")).ToArray();
        var idle = FindClip(profile.idleState);
        var travel = FindClip(profile.travelState);
        if (!idle || !travel) return;
        if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(root + "Motion.controller");
        var states = controller.layers[0].stateMachine;
        var idleState = State(profile.idleState, idle);
        if (states.defaultState != idleState)
        {
            states.defaultState = idleState;
            EditorUtility.SetDirty(states);
        }
        State(profile.travelState, travel);
        AssetDatabase.SaveAssetIfDirty(controller);

        AnimationClip FindClip(string name) => string.IsNullOrEmpty(name) ? null :
            clips.FirstOrDefault(clip => clip.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0);

        AnimatorState State(string name, AnimationClip clip)
        {
            var state = states.states.FirstOrDefault(entry => entry.state.name == name).state;
            if (!state) state = states.AddState(name);
            if (state.motion != clip)
            {
                state.motion = clip;
                EditorUtility.SetDirty(state);
                EditorUtility.SetDirty(states);
            }
            return state;
        }
    }

    private static LotAssetCatalogEntry MediumWealthStoneHouse() =>
        MediumWealthHome("medium-wealth-stone-house", "Stone House", "MediumWealthStoneHouse", 9f);

    private static LotAssetCatalogEntry MediumWealthHome(string id, string label, string folder,
        float span, string normalExtension = ".jpg") =>
        FoundersResidentialHome(id, label, folder, span, LotWealth.Medium, normalExtension);

    private static LotAssetCatalogEntry R2Townhouse(string id, string label, string folder, float span)
    {
        var home = FoundersResidentialHome(id, label, folder, span, LotWealth.Medium);
        // Preserve legacy asset IDs and art paths so existing lots keep their models.
        home.growableZone = TownZone.Residential2;
        home.rowGeometry = id switch
        {
            "r3-mid-wealth-barn-roof" => new LotRowGeometry {leftWall=-2.95274f,rightWall=2.99400f,frontStep=-3.67522f},
            "r3-mid-wealth-blue-townhouse" => new LotRowGeometry {leftWall=-2.71537f,rightWall=2.73299f,frontStep=-3.39141f},
            "r3-mid-wealth-dutch-roof" => new LotRowGeometry {leftWall=-2.42541f,rightWall=2.39480f,frontStep=-4.00116f},
            "r3-mid-wealth-mansard-roof" => new LotRowGeometry {leftWall=-2.76323f,rightWall=2.75494f,frontStep=-3.53494f},
            "r3-mid-wealth-stone-dutch" => new LotRowGeometry {leftWall=-2.70670f,rightWall=2.67975f,frontStep=-3.43065f},
            "r3-mid-wealth-stone-house" => new LotRowGeometry {leftWall=-2.54080f,rightWall=2.61172f,frontStep=-3.48247f},
            "r3-mid-wealth-3-window-brick-dormers" => new LotRowGeometry {leftWall=-2.86458f,rightWall=2.88997f,frontStep=-3.61563f},
            "r3-mid-wealth-3-window-brick" => new LotRowGeometry {leftWall=-2.69101f,rightWall=2.63932f,frontStep=-3.58508f},
            "r3-mid-wealth-painted-white-brick" => new LotRowGeometry {leftWall=-2.41166f,rightWall=2.46760f,frontStep=-3.33648f},
            "r3-mid-wealth-3-story-brick-2" => new LotRowGeometry {leftWall=-2.02353f,rightWall=2.10623f,frontStep=-3.10862f},
            "r3-mid-wealth-3-story-yellow" => new LotRowGeometry {leftWall=-2.57612f,rightWall=2.70351f,frontStep=-3.55775f},
            "r3-mid-wealth-3-story-brick" => new LotRowGeometry {leftWall=-3.11843f,rightWall=3.12660f,frontStep=-3.50779f},
            "r3-mid-wealth-3-story-yellow-stucco" => new LotRowGeometry {leftWall=-3.03225f,rightWall=3.12142f,frontStep=-3.44952f},
            "r3-mid-wealth-2-story-brick" => new LotRowGeometry {leftWall=-3.15382f,rightWall=3.13296f,frontStep=-2.93022f},
            "r3-mid-wealth-2-story-tan-wood" => new LotRowGeometry {leftWall=-2.68913f,rightWall=2.69194f,frontStep=-3.14983f},
            "r3-mid-wealth-2-story-yellow-house" => new LotRowGeometry {leftWall=-2.62055f,rightWall=2.62106f,frontStep=-2.97729f},
            _ => null
        };
        return home;
    }

    private static LotAssetCatalogEntry ArtDecoTownhome(string id, string label, string folder,
        float span, float leftWall, float rightWall, float frontStep)
    {
        var home = V3Building(id, label, folder, "residential", "modern", span);
        home.growableZone = TownZone.Residential2;
        home.growableWealth = LotWealth.Medium;
        home.rowGeometry = new LotRowGeometry { leftWall = leftWall, rightWall = rightWall, frontStep = frontStep };
        ConfigureBuildingWindowMask(home, $"{DirectoryPath}/V3Buildings/{folder}/Windows.png");
        ConfigureCivicSurface(home, $"{DirectoryPath}/V3Buildings/{folder}/MetallicSmoothness.png");
        return home;
    }

    private static LotAssetCatalogEntry ArtDecoCommercial(string id, string label, string folder,
        float span, float leftWall, float rightWall, float frontStep)
    {
        var shop = V3Building(id, label, folder, "commercial", "modern", span);
        shop.growableZone = TownZone.Commercial2;
        shop.growableWealth = LotWealth.Medium;
        shop.preserveEmissionColors = true;
        shop.rowGeometry = new LotRowGeometry { leftWall = leftWall, rightWall = rightWall, frontStep = frontStep };
        ConfigureBuildingWindowMask(shop, $"{DirectoryPath}/V3Buildings/{folder}/Windows.png");
        ConfigureCivicSurface(shop, $"{DirectoryPath}/V3Buildings/{folder}/MetallicSmoothness.png");
        return shop;
    }

    private static LotAssetCatalogEntry ArtDecoCityCenter()
    {
        var center = V3Building("art-deco-city-center", "Art Deco City Center", "ArtDecoCityCenter",
            "civics", "modern", 29.0573551f);
        center.subcategory = "government";
        center.additionalEraIds = new[]{"postwar","contemporary"};
        center.preserveEmissionColors = true;
        ConfigureBuildingWindowMask(center, $"{DirectoryPath}/V3Buildings/ArtDecoCityCenter/Windows.png");
        ConfigureCivicSurface(center, $"{DirectoryPath}/V3Buildings/ArtDecoCityCenter/MetallicSmoothness.png");
        return center;
    }

    private static LotAssetCatalogEntry R2LowWealthHome(string id, string label, string folder, float span)
    {
        var home = FoundersResidentialHome(id, label, folder, span, LotWealth.Low);
        home.growableZone = TownZone.Residential2;
        return home;
    }

    private static LotAssetCatalogEntry FoundersCivic(string id, string label, string folder, float span, string subcategory = null)
    {
        var civic = V3Building(id, label, folder, "civics", "founders", span);
        if (!string.IsNullOrEmpty(subcategory))
        {
            civic.subcategory = subcategory;
            ConfigureCivicSurface(civic, $"{DirectoryPath}/V3Buildings/{folder}/MetallicSmoothness.png");
        }
        ConfigureBuildingWindowMask(civic, $"{DirectoryPath}/V3Buildings/{folder}/Windows.png");
        return civic;
    }

    private static void ConfigureCivicSurface(LotAssetCatalogEntry building, string path)
    {
        if (!building.materialOverride || !File.Exists(path)) return;
        if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.sRGBTexture)
        {
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
        }
        var map = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var material = building.materialOverride;
        if (!map || (material.GetTexture("_MetallicGlossMap") == map &&
            material.IsKeywordEnabled("_METALLICSPECGLOSSMAP") && material.GetFloat("_Smoothness") == .35f)) return;
        material.SetTexture("_MetallicGlossMap", map);
        material.SetFloat("_Metallic", 1);
        material.SetFloat("_Smoothness", .35f);
        material.SetFloat("_SmoothnessTextureChannel", 0);
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
    }

    private static LotAssetCatalogEntry C2MediumWealthShop(string id,string label,string folder,float span)
    {
        var shop=FoundersShop(id,label,folder,span);
        shop.growableZone=TownZone.Commercial2;
        shop.growableWealth=LotWealth.Medium;
        shop.rowGeometry=id switch
        {
            "c2-mid-wealth-ye-olde-inn" => new LotRowGeometry {leftWall=-4.88359f,rightWall=4.23539f,frontStep=-4.18531f},
            "c2-mid-wealth-kings-arms" => new LotRowGeometry {leftWall=-3.96520f,rightWall=3.96688f,frontStep=-3.20125f},
            "c2-mid-wealth-apothecary-3-stories" => new LotRowGeometry {leftWall=-2.27996f,rightWall=2.32612f,frontStep=-3.29143f},
            "c2-mid-wealth-apothecary" => new LotRowGeometry {leftWall=-2.87519f,rightWall=2.87519f,frontStep=-4.85184f},
            "c2-mid-wealth-books" => new LotRowGeometry {leftWall=-4.47501f,rightWall=4.45703f,frontStep=-3.86112f},
            "c2-mid-wealth-ceramics" => new LotRowGeometry {leftWall=-2.83764f,rightWall=2.81378f,frontStep=-2.45122f},
            "c2-mid-wealth-chevron-store" => new LotRowGeometry {leftWall=-3.09095f,rightWall=3.08898f,frontStep=-2.44835f},
            "c2-mid-wealth-coffee" => new LotRowGeometry {leftWall=-1.93372f,rightWall=1.93610f,frontStep=-2.98350f},
            "c2-mid-wealth-commerican-merchants-shop" => new LotRowGeometry {leftWall=-3.16776f,rightWall=3.14923f,frontStep=-5.19055f},
            "c2-mid-wealth-dry-goods" => new LotRowGeometry {leftWall=-3.03168f,rightWall=3.03168f,frontStep=-4.18795f},
            "c2-mid-wealth-pottery-barn" => new LotRowGeometry {leftWall=-2.87763f,rightWall=2.87089f,frontStep=-2.07684f},
            "c2-mid-wealth-sea-urchin" => new LotRowGeometry {leftWall=-2.75282f,rightWall=2.73371f,frontStep=-2.73563f},
            _=>null
        };
        return shop;
    }

    private static LotAssetCatalogEntry FoundersShop(string id, string label, string folder,
        float span, string extension = ".jpg")
    {
        var shop = V3Building(id, label, folder, "commercial", "founders", span, extension, extension);
        shop.growableZone = TownZone.Commercial1;
        shop.growableWealth = LotWealth.Low;
        ConfigureBuildingWindowMask(shop, $"{DirectoryPath}/V3Buildings/{folder}/Windows.png");
        return shop;
    }

    private static LotAssetCatalogEntry FoundersResidentialHome(string id, string label, string folder,
        float span, LotWealth wealth, string normalExtension = ".jpg")
    {
        var home = V3Building(id, label, folder, "residential", "founders", span, ".jpg", normalExtension);
        home.growableZone = TownZone.Residential1;
        home.growableWealth = wealth;
        string maskPath = $"{DirectoryPath}/V3Buildings/{folder}/Windows.png";
        string paneMaskPath = $"{DirectoryPath}/V3Buildings/{folder}/WindowPanes.png";
        if (wealth == LotWealth.High && File.Exists(paneMaskPath)) maskPath = paneMaskPath;
        ConfigureBuildingWindowMask(home, maskPath);
        return home;
    }

    private static void ConfigureBuildingWindowMask(LotAssetCatalogEntry building, string maskPath)
    {
        building.nightWindowGlass = AssetDatabase.LoadAssetAtPath<Mesh>(
            maskPath.Substring(0, maskPath.LastIndexOf('/')) + "/NightWindowGlass.asset");
        if (AssetImporter.GetAtPath(maskPath) is TextureImporter maskImporter && maskImporter.sRGBTexture)
        {
            maskImporter.sRGBTexture = false;
            maskImporter.SaveAndReimport();
        }
        if (building.materialOverride && AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath) is { } mask &&
            (building.materialOverride.GetTexture("_EmissionMap") != mask || !building.materialOverride.IsKeywordEnabled("_EMISSION") ||
             building.materialOverride.globalIlluminationFlags != MaterialGlobalIlluminationFlags.BakedEmissive))
        {
            building.materialOverride.SetTexture("_EmissionMap", mask);
            // Keep the Lit emission variant enabled when Unity validates the material.
            building.materialOverride.SetColor("_EmissionColor", new Color(.001f, .001f, .001f, 1f));
            building.materialOverride.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            building.materialOverride.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(building.materialOverride);
            AssetDatabase.SaveAssetIfDirty(building.materialOverride);
        }
    }

    private static LotAssetCatalogEntry V3Building(string id, string label, string folder, string category,
        string era, float span, string colorExtension = ".jpg", string normalExtension = ".jpg",
        string modelCategory = "Buildings")
    {
        string root = DirectoryPath + (modelCategory == LotAssetCatalog.Transport ? "/V3Vehicles/" : "/V3Buildings/") + folder + "/";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Model.fbx");
        var entry = Entry("v3-building-" + id, label, modelCategory, model, Vector2.one * span, false, true);
        SetBuildingEra(entry, category, era);
        if (modelCategory == "Buildings")
            entry.distantPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Distant.fbx");
        if (!model) return entry;

        var bounds = FenceBounds(model).size;
        float widest = Mathf.Max(bounds.x, bounds.z);
        if (widest > .001f)
        {
            entry.footprint = new Vector2(bounds.x, bounds.z) * (span / widest);
            entry.buildingDimensionsMeters = bounds * (span / widest);
        }
        string normalPath = root + "Normal" + normalExtension;
        if (AssetImporter.GetAtPath(normalPath) is TextureImporter importer &&
            importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }

        var materialPath = root + "Building.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (!material)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = label };
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(root + "Color" + colorExtension));
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normal)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            material.SetFloat("_Smoothness", .18f);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        entry.materialOverride = material;
        return entry;
    }

    private static LotAssetCatalogEntry[] Landscaping() => new[]
    {
        GardenModel("wheat-garden", "Wheat Garden", "Agriculture", "WheatGarden", 3.5f),
        GardenModel("corn-garden", "Corn Garden", "Agriculture", "CornGarden", 3.5f, "Color.jpg", "Normal.jpg"),
        GardenModel("boxwood-hedge", "Clipped boxwood hedge", "Hedges", "Boxwood", 3f, "Color.png"),
        GardenModel("boxwood-hedge-half", "Clipped boxwood hedge · Half", "Hedges", "Boxwood", 1.5f,
            "Color.png", modelFile: "Half.fbx"),
        Boxwood("boxwood-corner-02", "Boxwood corner", "Corner02", 3f),
        Boxwood("boxwood-curved-02", "Curved boxwood hedge", "Curved02", 3f),
        Boxwood("boxwood-rectangular-03", "Rectangular boxwood hedge", "Rectangular03", 3f),
        Boxwood("boxwood-round-02", "Round boxwood", "Round02", 1.5f),
        Cypress("pencil-cypress-small", "Pencil cypress · Small", "PencilSmall"),
        Cypress("pencil-cypress-medium", "Pencil cypress · Medium", "PencilMedium"),
        Cypress("pencil-cypress-tall", "Pencil cypress · Tall", "PencilTall"),
        Cypress("mediterranean-cypress-compact", "Mediterranean cypress · Compact", "MediterraneanCompact"),
        Cypress("mediterranean-cypress-tall", "Mediterranean cypress · Tall", "MediterraneanTall"),
        FlowerBed("square-flower-bed", "Square flower bed", "square", new Vector2(4, 4)),
        FlowerBed("rectangular-flower-bed", "Rectangular flower bed", "rectangle", new Vector2(6, 3)),
        GardenModel("ornate-garden-bench", "Ornate garden bench", "Benches", "GardenBench", 1.8f, "Color.jpg", "Normal.jpg"),
        GardenModel("blue-gazebo", "Blue Gazebo", "Gazebos", "BlueGazebo", 4f, "Color.jpg", "Normal.jpg"),
        GardenModel("mediterranean-gazebo", "Mediterranean Gazebo", "Gazebos", "MediterraneanGazebo", 4f, "Color.jpg", "Normal.jpg"),
        GardenModel("rustic-gazebo", "Rustic Gazebo", "Gazebos", "RusticGazebo", 4f, "Color.jpg", "Normal.jpg"),
        GardenModel("white-gazebo", "White Gazebo", "Gazebos", "WhiteGazebo", 4f, "Color.jpg", "Normal.jpg"),
        GardenModel("stone-garden-fountain", "Stone garden fountain", "Fountains", "StoneFountain", 3f, "Color.jpg", "Normal.png")
    };

    private static System.Collections.Generic.IEnumerable<LotAssetCatalogEntry> Corn()
    {
        const string root = DirectoryPath + "/Landscaping/Vegetables/Corn/";
        var color = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "Corn_Base_Color.tga");
        if (!color) yield break;
        var labels = new[] { "Corn · Mature", "Corn · Medium", "Corn · Short", "Corn · Young", "Corn · Seedling" };
        Material material = null;
        for (int i = 0; i < labels.Length; i++)
        {
            int variant = i + 2;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(root + $"Corn_V{variant}.fbx");
            if (!model) continue;
            var size = FenceBounds(model).size;
            if (size.x <= .001f || size.y <= .001f || size.z <= .001f) continue;
            material ??= TexturedLotMaterial(root + "Corn.mat", color, root + "Corn_Normal.tga", cutout: true);
            var entry = Entry($"corn-v{variant}", labels[i], "Landscaping", model,
                new Vector2(size.x, size.z), true, true);
            entry.group = "Vegetables";
            entry.materialOverride = material;
            yield return entry;
        }
    }

    private static System.Collections.Generic.IEnumerable<LotAssetCatalogEntry> Wheat()
    {
        const string root = DirectoryPath + "/Landscaping/Vegetables/Wheat/";
        var color = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "Wheat_Base_Color.tga");
        if (!color) yield break;
        var variants = new[]
        {
            ("Wheat_V1", "Wheat · Plant 1"),
            ("Wheat_V2", "Wheat · Plant 2"),
            ("Wheat_V3", "Wheat · Plant 3"),
            ("Wheat_Group_V1", "Wheat · Clump"),
            ("Wheat_dry_V1", "Wheat · Dry plant 1"),
            ("Wheat_dry_V2", "Wheat · Dry plant 2"),
            ("Wheat_dry_V3", "Wheat · Dry plant 3"),
            ("Wheat_dry_V4", "Wheat · Dry plant 4"),
            ("Wheat_dry_Group_V1", "Wheat · Dry clump 1"),
            ("Wheat_dry_Group_V2", "Wheat · Dry clump 2")
        };
        Material material = null;
        foreach (var (modelName, label) in variants)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(root + modelName + ".fbx");
            if (!model) continue;
            var size = FenceBounds(model).size;
            if (size.x <= .001f || size.y <= .001f || size.z <= .001f) continue;
            material ??= TexturedLotMaterial(root + "Wheat.mat", color, root + "Wheat_Normal.tga", cutout: true);
            var entry = Entry(modelName.ToLowerInvariant().Replace('_', '-'), label, "Landscaping", model,
                new Vector2(size.x, size.z), true, true);
            entry.group = "Vegetables";
            entry.materialOverride = material;
            yield return entry;
        }
    }

    private static LotAssetCatalogEntry Cypress(string id, string label, string model)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DirectoryPath + "/Landscaping/Cypress/" + model + ".prefab");
        var size = prefab ? FenceBounds(prefab).size : Vector3.zero;
        var entry = Entry(id, label, "Landscaping", prefab, new Vector2(size.x, size.z), true, false);
        entry.group = "Trees";
        return entry;
    }

    private static LotAssetCatalogEntry Boxwood(string id, string label, string shape, float span)
    {
        const string root = DirectoryPath + "/Landscaping/BoxwoodCollection/";
        var entry = GardenModel(id, label, "Hedges", "BoxwoodCollection", span, "Color.png", "Normal.png",
            "Boxwood_" + shape + ".fbx");
        var material = entry.materialOverride;
        if (!material || material.GetTexture("_MetallicGlossMap")) return entry;

        const string surfacePath = root + "Surface.png";
        if (AssetImporter.GetAtPath(surfacePath) is TextureImporter importer && importer.sRGBTexture)
        {
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
        }
        var surface = AssetDatabase.LoadAssetAtPath<Texture2D>(surfacePath);
        if (!surface) return entry;
        material.SetTexture("_MetallicGlossMap", surface);
        material.SetTexture("_OcclusionMap", surface);
        material.SetFloat("_Smoothness", 1f);
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
        material.EnableKeyword("_OCCLUSIONMAP");
        EditorUtility.SetDirty(material);
        return entry;
    }

    private static LotAssetCatalogEntry StreetFurniture(string id, string label, string group, float size,
        bool byHeight = false)
    {
        string root = DirectoryPath + "/V3StreetFurniture/" + id + "/";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Model.fbx");
        var entry = Entry("v3-" + id, label, LotAssetCatalog.StreetFurniture, model, Vector2.zero, true, true);
        entry.group = group;
        if (!model) return entry;

        var bounds = FenceBounds(model).size;
        float dimension = byHeight ? bounds.y : Mathf.Max(bounds.x, bounds.z);
        if (dimension <= .001f)
        {
            entry.prefab = null;
            return entry;
        }

        entry.footprint = new Vector2(bounds.x, bounds.z) * (size / dimension);
        var color = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "Color.png");
        if (!color) color = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "Color.jpg");
        string normalPath = File.Exists(root + "Normal.png") ? root + "Normal.png" : root + "Normal.jpg";
        entry.materialOverride = TexturedLotMaterial(root + "Prop.mat", color, normalPath, cutout: false);
        if (group == "Lighting")
        {
            string glowPath = root + "LanternGlow.png";
            if (File.Exists(glowPath) && AssetImporter.GetAtPath(glowPath) is TextureImporter glowImporter)
            {
                if (glowImporter.sRGBTexture)
                {
                    glowImporter.sRGBTexture = false;
                    glowImporter.SaveAndReimport();
                }
                var glow = AssetDatabase.LoadAssetAtPath<Texture2D>(glowPath);
                if (glow && (entry.materialOverride.GetTexture("_EmissionMap") != glow ||
                    !entry.materialOverride.IsKeywordEnabled("_EMISSION") ||
                    entry.materialOverride.GetColor("_EmissionColor").maxColorComponent < .5f))
                {
                    entry.materialOverride.SetTexture("_EmissionMap", glow);
                    // Runtime LamppostLight supplies day/night color; keep the authored
                    // keyword alive through URP material validation and player builds.
                    entry.materialOverride.SetColor("_EmissionColor", Color.white);
                    entry.materialOverride.EnableKeyword("_EMISSION");
                    EditorUtility.SetDirty(entry.materialOverride);
                    AssetDatabase.SaveAssetIfDirty(entry.materialOverride);
                }
            }
        }
        return entry;
    }

    private static LotAssetCatalogEntry GardenModel(string id, string label, string group, string folder,
        float span, string colorFile = null, string normalFile = null, string modelFile = "Model.fbx")
    {
        string root = DirectoryPath + "/Landscaping/" + folder + "/";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(root + modelFile);
        var entry = Entry(id, label, "Landscaping", model, Vector2.one * span, true, true);
        entry.group = group;
        if (!model) return entry;
        var bounds = FenceBounds(model).size;
        float widest = Mathf.Max(bounds.x, bounds.z);
        if (widest > .001f) entry.footprint = new Vector2(bounds.x, bounds.z) * (span / widest);
        if (colorFile == null) return entry;
        var color = AssetDatabase.LoadAssetAtPath<Texture2D>(root + colorFile);
        entry.materialOverride = TexturedLotMaterial(root + "Garden.mat", color, normalFile == null ? null : root + normalFile,
            cutout: group == "Hedges");
        return entry;
    }

    private static LotAssetCatalogEntry FlowerBed(string id, string label, string shape, Vector2 size)
    {
        const string root = DirectoryPath + "/Landscaping/Beds/";
        string prefabPath = root + shape + ".prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var plan = AssetDatabase.LoadAssetAtPath<Texture2D>(root + shape + ".png");
        var blooms = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "Flowers.png");
        if (!prefab && plan && blooms)
        {
            var garden = new GameObject(label);
            var surface = TexturedLotMaterial(root + shape + ".mat", plan, null, cutout: true);
            var flowers = TexturedLotMaterial(root + "Flowers.mat", blooms, null, cutout: true);
            AddQuad("Planting plan", new Vector3(0, .045f, 0), Quaternion.Euler(90, 0, 0), size, surface);
            for (int row = -1; row <= 1; row += 2)
            for (int x = -1; x <= 1; x++)
            {
                var position = new Vector3(x * size.x * .26f, .21f, row * size.y * .24f);
                AddQuad("Flowers", position, Quaternion.identity, new Vector2(.9f, .42f), flowers);
                AddQuad("Flowers", position, Quaternion.Euler(0, 90, 0), new Vector2(.9f, .42f), flowers);
            }
            prefab = PrefabUtility.SaveAsPrefabAsset(garden, prefabPath);
            Object.DestroyImmediate(garden);

            void AddQuad(string name, Vector3 position, Quaternion rotation, Vector2 scale, Material material)
            {
                var card = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Object.DestroyImmediate(card.GetComponent<Collider>());
                card.name = name;
                card.transform.SetParent(garden.transform, false);
                card.transform.SetLocalPositionAndRotation(position, rotation);
                card.transform.localScale = new Vector3(scale.x, scale.y, 1);
                card.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
        }
        var entry = Entry(id, label, "Landscaping", prefab, size, true, false);
        entry.group = "Flower Beds";
        return entry;
    }

    private static Material TexturedLotMaterial(string path, Texture2D color, string normalPath, bool cutout)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetTexture("_BaseMap", color);
        material.SetFloat("_Smoothness", .16f);
        if (normalPath != null && AssetImporter.GetAtPath(normalPath) is TextureImporter normalImporter)
        {
            if (normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
            material.EnableKeyword("_NORMALMAP");
        }
        if (cutout)
        {
            material.SetFloat("_AlphaClip", 1);
            material.SetFloat("_Cutoff", .42f);
            material.SetFloat("_Cull", 0);
            material.EnableKeyword("_ALPHATEST_ON");
            material.SetOverrideTag("RenderType", "TransparentCutout");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static LotAssetCatalogEntry[] Characters()
    {
        var native = Resources.Load<NativeCampAssets>("NativeCamps/Assets");
        return new[]
        {
            Native("native-chief", "Indigenous Chief", native ? native.chief : null),
            Native("native-warrior", "Indigenous Warrior", native ? native.warrior : null),
            Native("native-resident", "Indigenous Resident", native ? native.resident : null),
            V3("founders-gentleman-black", "Founders Gentleman in Black", "FoundersGentlemanBlack", "founders"),
            V3("founders-gentleman-blue", "Founders Gentleman in Blue", "FoundersGentlemanBlue", "founders"),
            V3("founders-lady-gray", "Founders Lady in Gray", "FoundersLadyGray", "founders"),
            V3("founders-lady-red", "Founders Lady in Red", "FoundersLadyRed", "founders"),
            V3("founders-cook", "Founders Cook", "FoundersCook", "founders"),
            V3("founders-stout-townsperson", "Founders Stout Townsperson", "FoundersStoutTownsperson", "founders"),
            V3("founders-townsperson", "Founders Townsperson", "FoundersTownsperson", "founders"),
            V3("founders-constable", "Constable", "FoundersConstable", "founders"),
            V3("founders-privateer", "Founders Privateer", "FoundersPrivateer", "founders"),
            V3("farmer", "Farmer", "Farmer", "founders"),
            V3("axeman", "Axeman (Lumberjack)", "Axeman", "founders"),
            V3("musketman", "Revolutionary-era Musketman", "Musketman", "founders"),
            V3("dock-worker", "Dock Worker", "DockWorker", "industrial"),
            V3("historic-policeman", "Historic Policeman", "Policeman", "victorian"),
            V3("victorian-gentleman", "Victorian Gentleman", "Gentleman", "victorian"),
            V3("hooligan", "Street Hooligan", "Hooligan", "victorian")
        };

        LotAssetCatalogEntry Native(string id, string label, NpcDefinition definition)
        {
            return Character(id, label, definition ? definition.prefab : null, "founders",
                definition ? definition.idleState : "Idle", definition ? definition.radius : .35f);
        }

        LotAssetCatalogEntry V3(string id, string label, string folder, string era) =>
            Character("v3-" + id, label, PrepareV3Character(folder), era, "Idle", .35f);
    }

    private static LotAssetCatalogEntry Character(string id, string label, GameObject prefab, string eraId,
        string idle, float radius)
    {
        var entry = Entry(id, label, "Characters", prefab, Vector2.one * radius * 2, false, false);
        SetEra(entry, eraId);
        entry.group = CityForgeTimeline.Find(eraId)?.Name;
        entry.idleState = idle;
        return entry;
    }

    private static void SetEra(LotAssetCatalogEntry entry, string eraId)
    {
        var era = CityForgeTimeline.Find(eraId);
        entry.eraId = eraId;
        entry.fromYear = era?.FirstYear ?? 0;
        entry.toYear = era?.LastYear ?? 0;
    }

    private static GameObject PrepareV3Character(string folder)
    {
        string root = DirectoryPath + "/V3Characters/" + folder + "/";
        string modelPath = root + "Model.fbx";
        if (folder == "Hooligan") HooliganHarassSetup.Prepare();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Character.prefab");
        if (prefab)
        {
            AddCharacterModelState(root, modelPath, "Walk");
            if (folder == "FoundersConstable")
            {
                AddCharacterModelState(root, modelPath, "Run");
                AddCharacterModelState(root, modelPath, "FoldArms");
            }
            if (folder == "Axeman") AddCharacterModelState(root, modelPath, "Chop");
            if (folder == "Farmer") AddCharacterModelState(root, modelPath, "Hoe");
            AddCharacterFoldArms(root);
            return prefab;
        }
        if (!(AssetImporter.GetAtPath(modelPath) is ModelImporter importer)) return null;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.importAnimation = true;
        importer.importCameras = false;
        importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.SaveAndReimport();
        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips.Where(clip => clip.name.IndexOf("idle", System.StringComparison.OrdinalIgnoreCase) >= 0))
        {
            clip.loopTime = true;
            clip.loopPose = true;
        }
        if (clips.Length > 0)
        {
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        var idle = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__") &&
                clip.name.IndexOf("idle", System.StringComparison.OrdinalIgnoreCase) >= 0);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (!model) return null;
        var wrapper = new GameObject(folder);
        try
        {
            var child = Object.Instantiate(model, wrapper.transform, false);
            var renderers = child.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return null;
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            if (bounds.size.y <= .001f) return null;
            float scale = 1.72f / bounds.size.y;
            child.transform.localScale *= scale;
            child.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * scale;

            var material = TexturedLotMaterial(root + "Character.mat",
                AssetDatabase.LoadAssetAtPath<Texture2D>(root + "Color.jpg"),
                File.Exists(root + "Normal.jpg") ? root + "Normal.jpg" : null, false);
            material.SetFloat("_Smoothness", .18f);
            foreach (var renderer in renderers)
                renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();

            if (idle)
            {
                var controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(root + "Idle.controller", idle);
                controller.layers[0].stateMachine.defaultState.name = "Idle";
                var animator = child.GetComponentInChildren<Animator>() ?? child.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
            }
            var result = PrefabUtility.SaveAsPrefabAsset(wrapper, root + "Character.prefab");
            AddCharacterModelState(root, modelPath, "Walk");
            if (folder == "FoundersConstable")
            {
                AddCharacterModelState(root, modelPath, "Run");
                AddCharacterModelState(root, modelPath, "FoldArms");
            }
            if (folder == "Axeman") AddCharacterModelState(root, modelPath, "Chop");
            if (folder == "Farmer") AddCharacterModelState(root, modelPath, "Hoe");
            AddCharacterFoldArms(root);
            return result;
        }
        finally { Object.DestroyImmediate(wrapper); }
    }

    private static void AddCharacterFoldArms(string root)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(root + "FoldArms.anim");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(root + "Idle.controller");
        if (!clip || !controller) return;
        var machine = controller.layers[0].stateMachine;
        var fold = machine.states.FirstOrDefault(state => state.state.name == "FoldArms").state;
        if (!fold) fold = machine.AddState("FoldArms");
        if (fold.motion == clip) return;
        fold.motion = clip;
        EditorUtility.SetDirty(fold);
        EditorUtility.SetDirty(machine);
        AssetDatabase.SaveAssets();
    }

    private static void AddCharacterModelState(string root, string modelPath, string stateName)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(root + "Idle.controller");
        if (!controller) return;
        var machine = controller.layers[0].stateMachine;
        var state = machine.states.FirstOrDefault(entry => entry.state.name == stateName).state;
        var motion = state ? state.motion as AnimationClip : null;
        motion ??= AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__") &&
                clip.name.IndexOf(stateName, System.StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(clip => clip.name.Equals(stateName, System.StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(clip => clip.name, System.StringComparer.Ordinal).FirstOrDefault();
        if (!motion) return;
        if (!(AssetImporter.GetAtPath(modelPath) is ModelImporter importer)) return;
        var clips = importer.clipAnimations;
        // Repair the clip actually bound to the state, including existing controllers.
        // Sources can contain multiple walk takes; looping a different take leaves the
        // active walk frozen at its final frame while navigation keeps moving.
        string motionName = motion.name;
        var importedClip = clips.FirstOrDefault(clip => clip.name == motionName);
        if (importedClip != null && !importedClip.loopTime)
        {
            importedClip.loopTime = true;
            importedClip.loopPose = true;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
            motion = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                .FirstOrDefault(clip => clip.name == motionName);
        }
        if (!state) state = machine.AddState(stateName);
        if (state.motion == motion) return;
        state.motion = motion;
        EditorUtility.SetDirty(state);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
    }

    private static LotAssetCatalogEntry Fence(string id, string label, string variant)
    {
        const string path = "Assets/CityForge/Art/Environment/MeadowNativeURP/" +
            "Meadow Environment Dynamic Nature/Fence/Fence System Prefabs/";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path + $"prefab_wood_fence_type_{variant}_A_span.prefab");
        var entry = Entry(id, label, "Fences", prefab, new Vector2(4, 1), true, false);
        if (!prefab) return entry;
        var bounds = FenceBounds(prefab);
        entry.fenceAlongZ = bounds.size.z > bounds.size.x;
        entry.fenceSpan = Mathf.Max(bounds.size.x, bounds.size.z);
        entry.fenceDepth = Mathf.Max(.05f, Mathf.Min(bounds.size.x, bounds.size.z));
        entry.fenceCenter = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        if (id != "wood-fence") entry.footprint = new Vector2(bounds.size.x, bounds.size.z);
        var post = AssetDatabase.LoadAssetAtPath<GameObject>(path + $"prefab_wood_fence_type_{variant}_A_post.prefab");
        if (post)
        {
            var postBounds = FenceBounds(post);
            entry.fencePost = post;
            entry.fencePostCenter = new Vector3(postBounds.center.x, postBounds.min.y, postBounds.center.z);
            entry.fencePostWidth = Mathf.Max(postBounds.size.x, postBounds.size.z);
            entry.fenceRailLift = Mathf.Max(0, (postBounds.size.y - bounds.size.y) * .65f);
        }
        return entry;
    }

    private static LotAssetCatalogEntry V3Fence(string id, string label, string folder, string model,
        string prefix, float size, bool repeatable = false, bool iron = true, bool byHeight = false,
        string extension = ".png", string normalExtension = null)
    {
        var path = DirectoryPath + "/V3Fences/" + folder + "/";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path + model + ".fbx");
        if (!prefab) return Entry(id, label, "Fences", null, Vector2.zero, true, true);
        var bounds = FenceBounds(prefab);
        float longest = Mathf.Max(bounds.size.x, bounds.size.z);
        if (longest <= 0 || byHeight && bounds.size.y <= 0)
            return Entry(id, label, "Fences", null, Vector2.zero, true, true);
        float scale = size / (byHeight ? bounds.size.y : longest);
        var entry = Entry(id, label, "Fences", prefab,
            new Vector2(bounds.size.x, bounds.size.z) * scale, true, true);
        if (repeatable)
        {
            entry.fenceAlongZ = bounds.size.z > bounds.size.x;
            entry.fenceSpan = longest * scale;
            entry.fenceDepth = Mathf.Min(bounds.size.x, bounds.size.z) * scale;
        }

        var normalPath = path + prefix + "normal" + (normalExtension ?? extension);
        if (AssetImporter.GetAtPath(normalPath) is TextureImporter importer &&
            (importer.textureType != TextureImporterType.NormalMap || importer.convertToNormalmap || importer.sRGBTexture))
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.convertToNormalmap = false;
            importer.sRGBTexture = false;
            importer.SaveAndReimport();
        }

        var materialPath = path + id + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (!material)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = label };
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path + prefix + "base-color" + extension));
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normal)
            {
                material.SetTexture("_BumpMap", normal);
                material.EnableKeyword("_NORMALMAP");
            }
            var metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(path + prefix + "metallic-smoothness.png");
            if (iron && metallic)
            {
                material.SetTexture("_MetallicGlossMap", metallic);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            material.SetFloat("_Metallic", iron ? 1f : 0f);
            material.SetFloat("_Smoothness", iron ? .72f : .18f);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        entry.materialOverride = material;
        return entry;
    }

    private static Bounds FenceBounds(GameObject prefab)
    {
        var lod = prefab.GetComponentInChildren<LODGroup>();
        var renderers = lod && lod.lodCount > 0 ? lod.GetLODs()[0].renderers : prefab.GetComponentsInChildren<Renderer>();
        var bounds = new Bounds();
        bool initialized = false;
        // Measure the rendered panel relative to the wrapper that will seat it.
        foreach (var renderer in renderers.Where(renderer => renderer))
        {
            var local = renderer.localBounds;
            for (int corner = 0; corner < 8; corner++)
            {
                var offset = Vector3.Scale(local.extents, new Vector3(
                    (corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                var point = renderer.transform.TransformPoint(local.center + offset) - prefab.transform.position;
                if (initialized) bounds.Encapsulate(point);
                else { bounds = new Bounds(point, Vector3.zero); initialized = true; }
            }
        }
        return bounds;
    }

    private static LotAssetCatalogEntry LondonPlane(string variant)
    {
        string root = DirectoryPath + $"/V3LondonPlanes/Tree-{variant}-";
        var entry = Tree($"london-plane-{variant}", $"London Plane {variant.ToUpperInvariant()}",
            "Deciduous", root + "summer.prefab", 6f);
        entry.autumnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + "autumn.prefab");
        entry.winterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + "winter.prefab");
        return entry;
    }

    private static LotAssetCatalogEntry Tree(string id, string label, string group, string path, float footprint,
        Material material = null)
    {
        var entry = Entry(id, label, "Trees", AssetDatabase.LoadAssetAtPath<GameObject>(path),
            Vector2.one * footprint, true, false);
        entry.group = group;
        entry.materialOverride = material;
        return entry;
    }

    private static LotAssetCatalogEntry Entry(string id, string label, string category, GameObject prefab,
        Vector2 footprint, bool cosmetic, bool fit)
    {
        return new LotAssetCatalogEntry
        {
            id = id, label = label, category = category, prefab = prefab,
            footprint = footprint, playerPlaceable = cosmetic, fitModel = fit
        };
    }
}
