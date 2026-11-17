using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CityForge.EditorTools
{
    internal static class EagleImport
    {
        private const string Folder = "Assets/CityForge/Resources/Wildlife/Eagle/";
        private static readonly string[] States = { "Flap", "Glide", "Reach", "Carry", "Release", "Perch" };

        [InitializeOnLoadMethod]
        private static void QueuePrepare() => EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Model.fbx") &&
                (!AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Eagle.prefab") ||
                 AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Folder + "Flight.controller") is { } flight &&
                 !flight.animationClips.Any(clip => clip.name == "Perch")))
                Prepare();
        };

        [MenuItem("CityForge/Animals/Prepare Local Eagle")]
        internal static void Prepare()
        {
            string modelPath = Folder + "Model.fbx";
            if (AssetImporter.GetAtPath(modelPath) is not ModelImporter importer) return;
            if (importer.animationType != ModelImporterAnimationType.Generic || importer.clipAnimations.Length < States.Length)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                var clips = importer.defaultClipAnimations;
                foreach (var clip in clips)
                    clip.loopTime = States.Take(2).Append("Carry").Append("Perch").Any(state => clip.name.EndsWith(state, StringComparison.Ordinal));
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
            var motions = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder + "Flight.controller");
            if (!controller) controller = AnimatorController.CreateAnimatorControllerAtPath(Folder + "Flight.controller");
            var machine = controller.layers[0].stateMachine;
            foreach (string state in States)
            {
                var clip = motions.FirstOrDefault(motion => motion.name.EndsWith(state, StringComparison.Ordinal));
                if (!clip) throw new InvalidOperationException($"Missing eagle animation {state}.");
                var node = machine.states.FirstOrDefault(entry => entry.state.name == state).state;
                if (!node) node = machine.AddState(state);
                node.motion = clip;
                if (state == "Flap") machine.defaultState = node;
                EditorUtility.SetDirty(node);
            }

            var normalPath = Folder + "Normal.jpg";
            if (AssetImporter.GetAtPath(normalPath) is TextureImporter normal && normal.textureType != TextureImporterType.NormalMap)
            {
                normal.textureType = TextureImporterType.NormalMap;
                normal.SaveAndReimport();
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "Feathers.mat");
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, Folder + "Feathers.mat");
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "Color.jpg"));
            var bump = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (bump)
            {
                material.SetTexture("_BumpMap", bump);
                material.EnableKeyword("_NORMALMAP");
            }
            material.SetFloat("_Smoothness", .17f);
            var root = new GameObject("Bald Eagle");
            try
            {
                var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath), root.transform, false);
                var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
                renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();
                var bounds = renderer.bounds;
                float scale = 2.15f / bounds.size.x;
                model.transform.localScale *= scale;
                model.transform.localPosition -= bounds.center * scale;
                var animator = model.GetComponentInChildren<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                PrefabUtility.SaveAsPrefabAsset(root, Folder + "Eagle.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            EditorUtility.SetDirty(material);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(machine);
            AssetDatabase.SaveAssets();
        }
    }
}
