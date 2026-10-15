using System.IO;
using CityForge.CameraSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CityForge.Editor
{
    public static class InitialProjectSetup
    {
        private const string Root = "Assets/CityForge";
        private const string ScenePath = Root + "/Scenes/Development/VisualTest.unity";

        [MenuItem("CityForge/Create Initial Visual Test")]
        public static void Create()
        {
            EnsureFolders();
            var pipeline = EnsurePipeline();
            pipeline.shadowDistance = 120f;
            EditorUtility.SetDirty(pipeline);
            GraphicsSettings.defaultRenderPipeline = pipeline;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                Debug.Log("CityForge: VisualTest already exists; existing scene was preserved.");
                AssetDatabase.SaveAssets();
                return;
            }

            var groundMaterial = CreateMaterial("M_Ground", new Color(0.33f, 0.40f, 0.29f), 0f, 0.25f);
            var buildingMaterial = CreateMaterial("M_Block_Stone", new Color(0.57f, 0.52f, 0.45f), 0f, 0.3f);
            var dryStone = CreateMaterial("M_Stone_Dry", new Color(0.40f, 0.38f, 0.33f), 0f, 0.15f);
            var wetStone = CreateMaterial("M_Stone_Wet", new Color(0.28f, 0.29f, 0.27f), 0f, 0.78f);
            var metal = CreateMaterial("M_Metal", new Color(0.48f, 0.51f, 0.54f), 0.85f, 0.7f);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "VisualTest";

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground_100m";
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "PF_MassStudy_Block";
            block.transform.localScale = new Vector3(8f, 4f, 6f);
            block.GetComponent<Renderer>().sharedMaterial = buildingMaterial;
            var prefab = PrefabUtility.SaveAsPrefabAsset(block, Root + "/Prefabs/PF_MassStudy_Block.prefab");
            Object.DestroyImmediate(block);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = new Vector3(0f, 2f, 0f);

            CreateSphere("Dry_Stone", new Vector3(-13f, 1.5f, 0f), dryStone);
            CreateSphere("Wet_Stone", new Vector3(-9f, 1.5f, 0f), wetStone);
            CreateSphere("Metal", new Vector3(-5f, 1.5f, 0f), metal);

            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.2f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);

            var cameraObject = new GameObject("CityForgeCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.fieldOfView = 50f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1500f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.70f, 0.77f, 0.81f);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<CityForgeCameraController>().ApplyPose();
            cameraObject.tag = "MainCamera";

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("CityForge: URP VisualTest scene created at " + ScenePath);
        }

        [MenuItem("CityForge/Validate Initial Scaffold")]
        public static void Validate()
        {
            if (!(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset))
                throw new System.InvalidOperationException("The default render pipeline is not URP.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/PF_MassStudy_Block.prefab") == null)
                throw new System.InvalidOperationException("The mass-study prefab is missing.");

            EditorSceneManager.OpenScene(ScenePath);
            var cameraObject = GameObject.Find("CityForgeCamera");
            if (cameraObject == null || cameraObject.GetComponent<CityForgeCameraController>() == null)
                throw new System.InvalidOperationException("The VisualTest camera rig is missing.");
            var camera = cameraObject.GetComponent<Camera>();
            if (!camera.orthographic)
                throw new System.InvalidOperationException("The default camera must be orthographic.");

            foreach (var name in new[] { "M_Ground", "M_Block_Stone", "M_Stone_Dry", "M_Stone_Wet", "M_Metal" })
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/" + name + ".mat");
                if (material == null || material.shader.name != "Universal Render Pipeline/Lit")
                    throw new System.InvalidOperationException(name + " is missing or is not a URP/Lit material.");
            }

            var capturePath = System.Environment.GetEnvironmentVariable("CITYFORGE_CAPTURE_PATH");
            if (!string.IsNullOrEmpty(capturePath)) Capture(camera, capturePath);
            Debug.Log("CityForge: initial scaffold validation passed.");
        }

        private static void Capture(Camera camera, string path)
        {
            var texture = new RenderTexture(1280, 720, 24);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                Debug.Log("CityForge: captured VisualTest at " + path);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                Object.DestroyImmediate(image);
                Object.DestroyImmediate(texture);
            }
        }

        private static UniversalRenderPipelineAsset EnsurePipeline()
        {
            const string pipelinePath = Root + "/Rendering/CityForge_URP.asset";
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
            if (pipeline != null) return pipeline;

            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            rendererData.name = "CityForge_UniversalRenderer";
            AssetDatabase.CreateAsset(rendererData, Root + "/Rendering/CityForge_UniversalRenderer.asset");
            pipeline = UniversalRenderPipelineAsset.Create(rendererData);
            pipeline.name = "CityForge_URP";
            AssetDatabase.CreateAsset(pipeline, pipelinePath);
            return pipeline;
        }

        private static Material CreateMaterial(string name, Color color, float metallic, float smoothness)
        {
            var path = Root + "/Materials/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new System.InvalidOperationException("URP Lit shader was not found.");
            var material = new Material(shader) { name = name, enableInstancing = true };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void CreateSphere(string name, Vector3 position, Material material)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "MaterialSample_" + name;
            sphere.transform.position = position;
            sphere.transform.localScale = Vector3.one * 3f;
            sphere.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void EnsureFolders()
        {
            foreach (var path in new[]
            {
                "Art", "Art/Buildings", "Art/Environment", "Art/Vegetation",
                "Materials", "Prefabs", "Rendering", "Scenes", "Scenes/Development",
                "Scenes/Gameplay", "Scripts", "Scripts/Camera", "Scripts/Editor", "Textures"
            })
            {
                var current = Root;
                foreach (var part in path.Split('/'))
                {
                    var child = current + "/" + part;
                    if (!AssetDatabase.IsValidFolder(child)) AssetDatabase.CreateFolder(current, part);
                    current = child;
                }
            }
        }
    }
}
