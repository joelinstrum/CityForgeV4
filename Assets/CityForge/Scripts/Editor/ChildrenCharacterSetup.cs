using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using CityForge.Lots;
using CityForge.UI;
using Object = UnityEngine.Object;

internal static class ChildrenCharacterSetup
{
    internal const string Root = "Assets/CityForge/Resources/Lots/V3Characters";
    internal static readonly string[] Folders = { "ChildGirlBlue", "ChildGirlRed", "ChildBoyBlue", "ChildBoyRed" };
    internal static readonly string[] Ids = { "v3-child-girl-blue", "v3-child-girl-red", "v3-child-boy-blue", "v3-child-boy-red" };
    internal static readonly string[] Labels = { "Child · Blue Dress", "Child · Red Dress", "Child · Blue Shirt", "Child · Red Vest" };
    internal static IEnumerable<LotAssetCatalogEntry> Entries()
    {
        for (int i = 0; i < Folders.Length; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/{Folders[i]}/Character.prefab");
            if (!prefab) continue;
            yield return new LotAssetCatalogEntry { id = Ids[i], label = Labels[i], category = "Characters",
                group = "Founders Era", subcategory = "Children", eraId = "founders", fromYear = CityForgeTimeline.Find("founders").FirstYear, toYear = CityForgeTimeline.Find("founders").LastYear,
                prefab = prefab, footprint = Vector2.one * .5f, idleState = "Idle",
                description = "Schoolyard child. Defaults to Children at play: random available animations, at least 50% idle." };
        }
    }

    [MenuItem("CityForge/Import/Prepare schoolyard children")]
    public static void Prepare()
    {
        for (int i = 0; i < Folders.Length; i++) PrepareOne(Folders[i]);
        AssetDatabase.SaveAssets();
        typeof(LotCatalogSetup).GetMethod("EnsureCatalog", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
        Debug.Log("CHILDREN IMPORT COMPLETE: four animated children, reduced skinned LODs and all supplied animations.");
    }

    private static void PrepareOne(string folder)
    {
        string root = $"{Root}/{folder}/";
        foreach (string file in new[] { "Model.fbx", "Distant.fbx" })
        {
            var importer = AssetImporter.GetAtPath(root + file) as ModelImporter;
            if (!importer) throw new InvalidOperationException("Missing child model: " + root + file);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = file == "Model.fbx";
            importer.importCameras = false; importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            if (!importer.importAnimation) continue;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = true; clip.loopPose = true;
                clip.lockRootRotation = true; clip.lockRootPositionXZ = true;
                clip.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips; importer.SaveAndReimport();
        }
        var normalImport = (TextureImporter)AssetImporter.GetAtPath(root + "Normal.jpg");
        normalImport.textureType = TextureImporterType.NormalMap; normalImport.SaveAndReimport();
        string matPath = root + "Character.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(root + "Color.jpg"));
        mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(root + "Normal.jpg"));
        mat.EnableKeyword("_NORMALMAP"); mat.SetFloat("_Smoothness", .18f); mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        string controllerPath = root + "Play.controller";
        var old = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (old) AssetDatabase.DeleteAsset(controllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine = controller.layers[0].stateMachine;
        foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(root + "Model.fbx").OfType<AnimationClip>().Where(c => !c.name.StartsWith("__")))
        {
            var state = machine.AddState(ChildrenAtPlay.StateName(clip.name)); state.motion = clip;
            if (state.name == "Idle") machine.defaultState = state;
        }
        var wrapper = new GameObject(folder);
        try
        {
            var visual = new GameObject("Child visual scale").transform;
            visual.SetParent(wrapper.transform, false);
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(root + "Model.fbx"), visual);
            var near = model.GetComponentInChildren<SkinnedMeshRenderer>();
            if (!near) throw new InvalidOperationException("Missing skinned child mesh");
            near.sharedMaterial = mat;
            var animator = model.GetComponentInChildren<Animator>() ?? model.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullCompletely;
            animator.Rebind(); animator.Update(0f);
            var idleClip = controller.animationClips.First(clip => ChildrenAtPlay.StateName(clip.name) == "Idle");
            idleClip.SampleAnimation(animator.gameObject, .5f);
            var baked = new Mesh(); near.BakeMesh(baked);
            var bounds = new Bounds(near.transform.TransformPoint(baked.bounds.center), Vector3.zero);
            for (int corner = 0; corner < 8; corner++)
                bounds.Encapsulate(near.transform.TransformPoint(baked.bounds.center + Vector3.Scale(baked.bounds.extents,
                    new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1))));
            Object.DestroyImmediate(baked);
            float scale = 1.25f / bounds.size.y;
            visual.localScale = Vector3.one * scale;
            visual.localPosition = -new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * scale;
            var sourceFar = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Distant.fbx").GetComponentInChildren<SkinnedMeshRenderer>();
            var farGo = new GameObject("Reduced child mesh"); farGo.transform.SetParent(near.transform.parent, false);
            farGo.transform.localPosition = near.transform.localPosition; farGo.transform.localRotation = near.transform.localRotation; farGo.transform.localScale = near.transform.localScale;
            var far = farGo.AddComponent<SkinnedMeshRenderer>();

            var bones = model.GetComponentsInChildren<Transform>().ToDictionary(t => t.name, t => t);
            far.bones = sourceFar.bones.Select(b => bones[b.name]).ToArray();
            // FBX round-tripping changes source centimeters to meters. Normalize the
            // raw vertex units and reuse the original skeleton's inverse bind poses.
            var reduced = Object.Instantiate(sourceFar.sharedMesh);
            Bounds VertexBounds(Mesh mesh)
            {
                var points = mesh.vertices; var value = new Bounds(points[0], Vector3.zero);
                foreach (var point in points) value.Encapsulate(point);
                return value;
            }
            float unitRatio = VertexBounds(near.sharedMesh).size.magnitude / VertexBounds(reduced).size.magnitude;
            float units = Mathf.Pow(10f, Mathf.Round(Mathf.Log10(unitRatio)));
            reduced.vertices = reduced.vertices.Select(point => point * units).ToArray();
            reduced.bindposes = far.bones.Select(bone => near.sharedMesh.bindposes[Array.IndexOf(near.bones, bone)]).ToArray();
            reduced.RecalculateBounds();
            string meshPath = root + "DistantMesh.asset";
            var previousMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (previousMesh) { EditorUtility.CopySerialized(reduced, previousMesh); Object.DestroyImmediate(reduced); EditorUtility.SetDirty(previousMesh); }
            else { AssetDatabase.CreateAsset(reduced, meshPath); previousMesh = reduced; }
            far.sharedMesh = previousMesh;
            far.rootBone = sourceFar.rootBone && bones.TryGetValue(sourceFar.rootBone.name, out var rootBone) ? rootBone : near.rootBone;
            far.sharedMaterial = mat; far.localBounds = near.localBounds;
            var lod = wrapper.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(.08f, new Renderer[] { near }), new LOD(.001f, new Renderer[] { far }) });
            lod.RecalculateBounds();
            PrefabUtility.SaveAsPrefabAsset(wrapper, root + "Character.prefab");
        }
        finally { Object.DestroyImmediate(wrapper); }
    }
}
