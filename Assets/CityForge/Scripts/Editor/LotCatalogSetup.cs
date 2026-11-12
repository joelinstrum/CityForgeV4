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
        const string meadow = "Assets/CityForge/Art/Environment/MeadowNativeURP/";
        const string resources = "Assets/CityForge/Resources/";
        var house = AssetDatabase.LoadAssetAtPath<GameObject>(meadow +
            "Integration/Buildings/Farmhouse/tripo_convert_6e4dde22-3566-484b-a685-c971607adfc8.fbx");

        var farmhouse = Entry("farmhouse", "Farmhouse", "Buildings", house, new Vector2(12, 12), false, true);
        SetBuildingEra(farmhouse, "residential", "founders");
        farmhouse.materialOverride = AssetDatabase.LoadAssetAtPath<Material>(meadow +
            "Integration/Materials/Farmhouse tripo_mat_6e4dde22 LandscapeLit.mat");
        var palms = AssetDatabase.LoadAssetAtPath<Material>(resources + "TropicalCoconut/Palms.mat");

        var candidates = new[]
        {
            farmhouse,
            FoundersBuilding("timber-council-hall", "Timber Council Hall", "TimberCouncilHall", 12f),
            FoundersBuilding("stone-courthouse", "Stone Courthouse", "StoneCourthouse", 12f),
            FoundersBuilding("brick-state-house", "Brick State House", "BrickStateHouse", 12f),
            FoundersBuilding("harbor-council-house", "Harbor Council House", "HarborCouncilHouse", 12f),
            FoundersBuilding("agrarian-assembly-hall", "Agrarian Assembly Hall", "AgrarianAssemblyHall", 12f),
            FrontierLogCabin(),
            SmallColonialCabin(),
            FoundersHome("low-wealth-front-gable", "Front Gable House", "LowWealthFrontGable"),
            FoundersHome("low-wealth-side-gable", "Side Gable House", "LowWealthSideGable"),
            V3Building("wooden-cottage", "Wooden Cottage", "WoodenCottage", "residential", "founders", 8f),
            V3Building("plymouth-store", "Plymouth Store", "PlymouthStore", "commercial", "founders", 9.26f, ".jpeg", ".jpeg"),
            V3Building("small-school", "Small School", "SmallSchool", "civics", "founders", 9f, ".jpg", ".png"),
            V3Building("lumber-mill", "Lumber Mill", "LumberMill", "industrial", "industrial", 11.95f),
            V3Building("queen-anne-house", "Queen Anne House", "QueenAnneHouse", "residential", "victorian", 12f),
            V3Fence("v3-white-picket", "White picket fence", "PicketFenceV01",
                "CF_Prop_PicketFence_Straight_v01", "", 2.4f, repeatable: true, iron: false),
            V3Fence("v3-wrought-iron", "Wrought iron fence", "WroughtIronFenceV01",
                "CF_WroughtIronFence_Straight_LOD0_v01", "", 6.4f, repeatable: true),
            V3Fence("v3-wrought-iron-corner", "Wrought iron corner", "WroughtIronFenceV01",
                "CF_WroughtIronFence_Corner_LShape_LOD0_v02", "corner-", 2.55f),
            V3Fence("v3-decorative-iron", "Decorative iron garden fence", "WroughtIronVariationsV01",
                "CF_Prop_DecorativeIronFence_v01", "decorative-", 1.6f, byHeight: true, extension: ".jpg"),
            V3Fence("v3-ornate-iron-gate", "Ornate iron gate", "WroughtIronVariationsV01",
                "CF_Prop_OrnateIronGate_v01", "gate-", 2.2f, byHeight: true, extension: ".jpg"),
            StreetFurniture("ornate-bench", "Ornate park bench", "Seating", 1.8f),
            StreetFurniture("simple-street-lamppost", "Simple street lamppost", "Lighting", 3.6f, byHeight: true),
            StreetFurniture("three-lantern-lamppost", "Three-lantern lamppost", "Lighting", .9f),
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

        var catalog = AssetDatabase.LoadAssetAtPath<LotAssetCatalog>(CatalogPath);
        var entries = (catalog ? catalog.entries : System.Array.Empty<LotAssetCatalogEntry>()).ToList();
        bool changed = false;
        foreach (var entry in candidates.Concat(PlaceableSeasonalTrees.CatalogEntries()).Concat(foundersHomes).Concat(meadowPoplars).Concat(Characters()).Concat(Landscaping()).Concat(Corn()).Concat(Wheat()).Where(item => item.prefab))
        {
            var existing = entries.Find(item => item != null && item.id == entry.id);
            if (existing == null)
            {
                entries.Add(entry);
                changed = true;
            }
            else if (entry.autumnPrefab && entry.winterPrefab && (existing.prefab != entry.prefab ||
                existing.autumnPrefab != entry.autumnPrefab || existing.winterPrefab != entry.winterPrefab))
            {
                existing.prefab = entry.prefab;
                existing.autumnPrefab = entry.autumnPrefab;
                existing.winterPrefab = entry.winterPrefab;
                existing.label = entry.label;
                existing.footprint = entry.footprint;
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
            else if ((entry.category == "Buildings" || entry.IsCharacter) &&
                (existing.group != entry.group || existing.eraId != entry.eraId ||
                    existing.fromYear != entry.fromYear || existing.toYear != entry.toYear ||
                    existing.growableZone != entry.growableZone || existing.growableWealth != entry.growableWealth))
            {
                existing.group = entry.group;
                existing.eraId = entry.eraId;
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
            material.SetFloat("_Smoothness", 1);
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

    private static LotAssetCatalogEntry FoundersHome(string id, string label, string folder, float span = 7.5f)
    {
        var home = V3Building(id, label, folder, "residential", "founders", span, ".jpg", ".png");
        home.growableZone = TownZone.Residential1;
        home.growableWealth = LotWealth.Low;
        return home;
    }

    private static LotAssetCatalogEntry V3Building(string id, string label, string folder, string category,
        string era, float span, string colorExtension = ".jpg", string normalExtension = ".jpg")
    {
        string root = DirectoryPath + "/V3Buildings/" + folder + "/";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Model.fbx");
        var entry = Entry("v3-building-" + id, label, "Buildings", model, Vector2.one * span, false, true);
        SetBuildingEra(entry, category, era);
        if (!model) return entry;

        var bounds = FenceBounds(model).size;
        float widest = Mathf.Max(bounds.x, bounds.z);
        if (widest > .001f) entry.footprint = new Vector2(bounds.x, bounds.z) * (span / widest);
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
        GardenModel("boxwood-hedge", "Clipped boxwood hedge", "Hedges", "Boxwood", 3f, "Color.png"),
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
            material ??= GardenMaterial(root + "Corn.mat", color, root + "Corn_Normal.tga", cutout: true);
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
            material ??= GardenMaterial(root + "Wheat.mat", color, root + "Wheat_Normal.tga", cutout: true);
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
        entry.materialOverride = GardenMaterial(root + "Prop.mat", color, normalPath, cutout: false);
        return entry;
    }

    private static LotAssetCatalogEntry GardenModel(string id, string label, string group, string folder,
        float span, string colorFile, string normalFile = null, string modelFile = "Model.fbx")
    {
        string root = DirectoryPath + "/Landscaping/" + folder + "/";
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(root + modelFile);
        var entry = Entry(id, label, "Landscaping", model, Vector2.one * span, true, true);
        entry.group = group;
        if (!model) return entry;
        var bounds = FenceBounds(model).size;
        float widest = Mathf.Max(bounds.x, bounds.z);
        if (widest > .001f) entry.footprint = new Vector2(bounds.x, bounds.z) * (span / widest);
        var color = AssetDatabase.LoadAssetAtPath<Texture2D>(root + colorFile);
        entry.materialOverride = GardenMaterial(root + "Garden.mat", color, normalFile == null ? null : root + normalFile,
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
            var surface = GardenMaterial(root + shape + ".mat", plan, null, cutout: true);
            var flowers = GardenMaterial(root + "Flowers.mat", blooms, null, cutout: true);
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

    private static Material GardenMaterial(string path, Texture2D color, string normalPath, bool cutout)
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
            V3("farmer", "Farmer", "Farmer", "founders"),
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
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Character.prefab");
        if (prefab)
        {
            AddCharacterWalk(root, modelPath);
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

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = folder + " Color" };
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(root + "Color.jpg"));
            material.SetFloat("_Smoothness", .18f);
            AssetDatabase.CreateAsset(material, root + "Character.mat");
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
            AddCharacterWalk(root, modelPath);
            return result;
        }
        finally { Object.DestroyImmediate(wrapper); }
    }

    private static void AddCharacterWalk(string root, string modelPath)
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(root + "Idle.controller");
        if (!controller) return;
        var machine = controller.layers[0].stateMachine;
        if (machine.states.Any(state => state.state.name == "Walk")) return;
        if (!(AssetImporter.GetAtPath(modelPath) is ModelImporter importer)) return;
        var clips = importer.clipAnimations;
        var walk = clips.FirstOrDefault(clip => clip.name.IndexOf("walk", System.StringComparison.OrdinalIgnoreCase) >= 0);
        if (walk != null && !walk.loopTime)
        {
            walk.loopTime = true;
            walk.loopPose = true;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
        var motion = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__") &&
                clip.name.IndexOf("walk", System.StringComparison.OrdinalIgnoreCase) >= 0);
        if (!motion) return;
        machine.AddState("Walk").motion = motion;
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
        string extension = ".png")
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

        var normalPath = path + prefix + "normal" + extension;
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
