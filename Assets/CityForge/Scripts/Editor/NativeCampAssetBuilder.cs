#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using CityForge.World;
namespace CityForge.Editor
{
    public static class NativeCampAssetBuilder
    {
        const string Root="Assets/CityForge/Resources/NativeCamps/";
        [MenuItem("CityForge/NPC/Prepare native camp assets")]
        public static void Build(){
            if(EditorApplication.isPlaying||File.Exists(Root+"Assets.asset"))throw new InvalidOperationException("Prepare once in Edit mode; do not overwrite prepared camps");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureDirtTexture();
            var assets=ScriptableObject.CreateInstance<NativeCampAssets>();
            var normal=(TextureImporter)AssetImporter.GetAtPath(Root+"Teepee-Normal.png");normal.textureType=TextureImporterType.NormalMap;normal.maxTextureSize=2048;normal.SaveAndReimport();
            assets.teepeeMaterials=new Material[4];int index=0;
            foreach(string color in new[]{"Plain","Red","Blue","Brown"}){
                var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Teepee "+color};material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Teepee-"+color+".png"));
                material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Teepee-Normal.png"));material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.65f);material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.12f);material.SetFloat("_Cull",0);
                AssetDatabase.CreateAsset(material,Root+"Teepee-"+color+".mat");assets.teepeeMaterials[index++]=material;
            }
            assets.teepee=StaticPrefab("Teepee",assets.teepeeMaterials[0]);
            var wood=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Totem supplied vertex-color bake"};wood.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Totem-Color.png"));wood.SetColor("_BaseColor",new Color(.42f,.27f,.14f,1));wood.SetFloat("_Metallic",0);wood.SetFloat("_Smoothness",.16f);AssetDatabase.CreateAsset(wood,Root+"Totem.mat");assets.totem=StaticPrefab("Totem",wood);
            var resident=Resources.Load<NpcDefinition>("NpcStudy/ResidentDefinition");if(!resident)throw new InvalidOperationException("Prepare resident study assets first");
            assets.resident=resident;assets.chief=Character("Chief",resident.profiles);assets.warrior=Character("Warrior",resident.profiles);
            AssetDatabase.CreateAsset(assets,Root+"Assets.asset");AssetDatabase.SaveAssets();
        }
        [MenuItem("CityForge/NPC/Configure local dirt texture")]
        public static void ConfigureDirtTexture(){
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required");
            var importer=AssetImporter.GetAtPath(Root+"Courtyard-Dirt.png") as TextureImporter;if(importer==null)return;
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.alphaSource=TextureImporterAlphaSource.None;
            importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;importer.mipmapEnabled=true;
            importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.npotScale=TextureImporterNPOTScale.None;importer.SaveAndReimport();
        }
        static GameObject StaticPrefab(string name,Material material){
            var importer=(ModelImporter)AssetImporter.GetAtPath(Root+name+".fbx");importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+name+".fbx"));go.name=name;
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=material;
            if(name=="Totem"){
                var wrapper=new GameObject("Totem");go.transform.SetParent(wrapper.transform,false);
                // Preserve the FBX axis-conversion rotation on a child; camp yaw belongs to the wrapper.
                var bounds=go.GetComponentsInChildren<Renderer>()[0].bounds;foreach(var r in go.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
                float scale=5/bounds.size.y;go.transform.localScale*=scale;go.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;go=wrapper;
            }
            var prefab=PrefabUtility.SaveAsPrefabAsset(go,Root+name+".prefab");UnityEngine.Object.DestroyImmediate(go);return prefab;
        }
        public static void RepairTotemOrientation(){
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit mode required");
            var assets=AssetDatabase.LoadAssetAtPath<NativeCampAssets>(Root+"Assets.asset");
            assets.totem=StaticPrefab("Totem",AssetDatabase.LoadAssetAtPath<Material>(Root+"Totem.mat"));EditorUtility.SetDirty(assets);AssetDatabase.SaveAssets();
        }
        static NpcDefinition Character(string role,NpcBehaviorProfile[] profiles){
            string folder=Root+role+"/",path=folder+"Model.fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.importAnimation=true;importer.animationType=ModelImporterAnimationType.Generic;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importCameras=false;importer.importLights=false;importer.SaveAndReimport();
            var clips=importer.defaultClipAnimations;foreach(var c in clips){c.loopTime=true;c.loopPose=true;c.lockRootRotation=true;c.lockRootPositionXZ=true;c.lockRootHeightY=true;c.keepOriginalOrientation=true;c.keepOriginalPositionXZ=true;c.keepOriginalPositionY=true;}importer.clipAnimations=clips;importer.SaveAndReimport();
            var motions=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
            var controller=AnimatorController.CreateAnimatorControllerAtPath(folder+"Character.controller");
            foreach(string name in new[]{"Idle","Walk","FoldArms"}){
                string suffix=name=="FoldArms"?"fold_arms":name.ToLowerInvariant();var clip=motions.FirstOrDefault(c=>c.name.EndsWith(suffix,StringComparison.OrdinalIgnoreCase))??motions.First(c=>c.name.EndsWith("idle",StringComparison.OrdinalIgnoreCase));
                var state=controller.layers[0].stateMachine.AddState(name);state.motion=clip;if(name=="Idle")controller.layers[0].stateMachine.defaultState=state;
            }
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=role+" supplied color"};string texture=Directory.GetFiles(folder,"*_0.jpg",SearchOption.AllDirectories).Single();material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.18f);material.SetFloat("_Cull",0);AssetDatabase.CreateAsset(material,folder+"Character.mat");
            var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));go.name=role;foreach(var renderer in go.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=material;
            var animator=go.GetComponentInChildren<Animator>();if(!animator)animator=go.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
            var wrapper=new GameObject(role+" NPC");go.transform.SetParent(wrapper.transform,false);var bounds=go.GetComponentsInChildren<Renderer>()[0].bounds;foreach(var r in go.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);float scale=1.7f/bounds.size.y;go.transform.localScale*=scale;go.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
            var prefab=PrefabUtility.SaveAsPrefabAsset(wrapper,folder+"Character.prefab");UnityEngine.Object.DestroyImmediate(wrapper);
            var definition=ScriptableObject.CreateInstance<NpcDefinition>();definition.prefab=prefab;definition.profiles=profiles;AssetDatabase.CreateAsset(definition,folder+"Definition.asset");return definition;
        }
    }
}
#endif
