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
            Fence("wood-fence", "Wood fence", "01"),
            Fence("meadow-fence-02", "Meadow wood fence 02", "02"),
            Fence("meadow-fence-03", "Meadow wood fence 03", "03"),
            Fence("meadow-fence-04", "Meadow wood fence 04", "04"),
            Fence("meadow-fence-05", "Meadow wood fence 05", "05"),
            Tree("poplar", "Poplar", "Deciduous", meadow + "Showcase/Flora/prefab_Baked_01_Poplar.prefab", 3),
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

        var catalog = AssetDatabase.LoadAssetAtPath<LotAssetCatalog>(CatalogPath);
        var entries = (catalog ? catalog.entries : System.Array.Empty<LotAssetCatalogEntry>()).ToList();
        bool changed = false;
        foreach (var entry in candidates.Concat(meadowPoplars).Concat(Characters()).Where(item => item.prefab))
        {
            var existing = entries.Find(item => item != null && item.id == entry.id);
            if (existing == null)
            {
                entries.Add(entry);
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
            else if ((entry.category == "Buildings" || entry.IsCharacter) &&
                (existing.group != entry.group || existing.eraId != entry.eraId ||
                    existing.fromYear != entry.fromYear || existing.toYear != entry.toYear))
            {
                existing.group = entry.group;
                existing.eraId = entry.eraId;
                existing.fromYear = entry.fromYear;
                existing.toYear = entry.toYear;
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
