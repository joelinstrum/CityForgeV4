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
    public static class NpcStudyAssetBuilder
    {
        const string Root="Assets/CityForge/Resources/NpcStudy/";
        [MenuItem("CityForge/NPC/Prepare local resident")]
        public static void Build(){
            if(EditorApplication.isPlaying||File.Exists(Root+"ResidentDefinition.asset"))throw new InvalidOperationException("Prepare once in Edit mode; preserve prepared assets");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var model=(ModelImporter)AssetImporter.GetAtPath(Root+"Resident.fbx");
            model.importAnimation=true;model.animationType=ModelImporterAnimationType.Generic;model.materialImportMode=ModelImporterMaterialImportMode.None;model.importCameras=false;model.importLights=false;model.SaveAndReimport();
            var clips=model.defaultClipAnimations;
            foreach(var c in clips){c.loopTime=true;c.loopPose=true;c.lockRootRotation=true;c.lockRootPositionXZ=true;c.lockRootHeightY=true;c.keepOriginalOrientation=true;c.keepOriginalPositionXZ=true;c.keepOriginalPositionY=true;}
            model.clipAnimations=clips;model.SaveAndReimport();
            var imported=AssetDatabase.LoadAllAssetsAtPath(Root+"Resident.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
            var controller=AnimatorController.CreateAnimatorControllerAtPath(Root+"Resident.controller");
            foreach(var pair in new[]{("Idle","idle"),("Walk","walk"),("FoldArms","fold_arms")}){
                var clip=imported.First(c=>c.name.EndsWith(pair.Item2,StringComparison.OrdinalIgnoreCase));
                var state=controller.layers[0].stateMachine.AddState(pair.Item1);state.motion=clip;
                if(pair.Item1=="Idle")controller.layers[0].stateMachine.defaultState=state;
            }
            string texture=Directory.GetFiles(Root,"*_0.jpg",SearchOption.AllDirectories).Single();
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Resident supplied color"};
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texture));material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.18f);material.SetFloat("_Cull",0);AssetDatabase.CreateAsset(material,Root+"Resident.mat");
            var modelObject=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Resident.fbx"));modelObject.name="Supplied character";
            foreach(var r in modelObject.GetComponentsInChildren<Renderer>())r.sharedMaterial=material;
            var animator=modelObject.GetComponentInChildren<Animator>();if(!animator)animator=modelObject.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
            var root=new GameObject("StudyResident");modelObject.transform.SetParent(root.transform,false);
            Bounds bounds=modelObject.GetComponentsInChildren<Renderer>()[0].bounds;
            foreach(var r in modelObject.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);
            float scale=1.7f/bounds.size.y;modelObject.transform.localScale*=scale;modelObject.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"StudyResident.prefab");UnityEngine.Object.DestroyImmediate(root);
            var definition=ScriptableObject.CreateInstance<NpcDefinition>();definition.prefab=prefab;AssetDatabase.CreateAsset(definition,Root+"ResidentDefinition.asset");PrepareProfiles();AssetDatabase.SaveAssets();
            File.WriteAllText("Library/NpcAssetReport.txt",string.Join("\n",imported.Select(c=>$"{c.name}: {c.length:F2}s"))+"\nSource bounds "+bounds+"\nScale "+scale);
        }
        [MenuItem("CityForge/NPC/Prepare local behavior profiles")]
        public static void PrepareProfiles(){
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Prepare profiles in Edit mode");
            var definition=AssetDatabase.LoadAssetAtPath<NpcDefinition>(Root+"ResidentDefinition.asset");
            if(!definition)throw new InvalidOperationException("Prepare the local resident first");
            if(definition.profiles!=null&&definition.profiles.Length>0)return;
            var profiles=new NpcBehaviorProfile[2];
            for(int i=0;i<2;i++){
                string path=Root+(i==0?"ResidentProfile":"SocialProfile")+".asset";
                if(File.Exists(path))throw new InvalidOperationException("Existing unassigned profile requires manual selection: "+path);
                profiles[i]=ScriptableObject.CreateInstance<NpcBehaviorProfile>();
                if(i==1){profiles[i].talkWeight=6;profiles[i].wanderWeight=3;}
                AssetDatabase.CreateAsset(profiles[i],path);
            }
            definition.profiles=profiles;EditorUtility.SetDirty(definition);AssetDatabase.SaveAssets();
        }
        [MenuItem("CityForge/NPC/Start study in stream district")]
        public static void StartStudy(){
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play mode and open the accepted stream district");
            var land=UnityEngine.Object.FindFirstObjectByType<MeadowDistrictLandscape>();
            if(!land||land.IsRegionOverview||land.View.SavedRegionId!=StreamRockTestRegion.Id||land.DistrictIndex!=3)throw new InvalidOperationException("Open Stream Rock Study Pass17 northeast district first");
            if(land.GetComponentInChildren<NpcPopulation>())throw new InvalidOperationException("Study is already running");
            var go=new GameObject("NPC ambient life study");go.transform.SetParent(land.transform,false);
            var population=go.AddComponent<NpcPopulation>();
            try{population.Initialize(land,Resources.Load<NpcDefinition>("NpcStudy/ResidentDefinition"));}catch{UnityEngine.Object.Destroy(go);throw;}
            var rig=land.View.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>();rig.SetHeading(45);rig.ConfigureTerrainStudy(population.Center,24,0);population.SetDiagnostics(true);
        }
    }
}
#endif
