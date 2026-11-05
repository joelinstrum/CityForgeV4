#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace CityForge.Editor
{
    public static class CoastalSharkAssetBuilder
    {
        const string Root="Assets/CityForge/Resources/CoastalWildlife/GreatWhite/";
        [MenuItem("CityForge/Wildlife/Prepare supplied great white")]
        public static void Build()
        {
            if(UnityEngine.Object.FindFirstObjectByType<World.PrototypeRegionView>())throw new InvalidOperationException("Return to the menu before preparing shark assets.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var model=AssetImporter.GetAtPath(Root+"GreatWhite.fbx") as ModelImporter;
            if(!model)throw new InvalidOperationException("Export the supplied licensed shark to the private GreatWhite resource directory first.");
            model.materialImportMode=ModelImporterMaterialImportMode.None;model.importAnimation=true;model.animationType=ModelImporterAnimationType.Generic;model.importCameras=false;model.importLights=false;model.SaveAndReimport();
            var clips=model.defaultClipAnimations;foreach(var clip in clips){clip.loopTime=true;clip.loopPose=true;clip.lockRootPositionXZ=true;clip.lockRootRotation=true;}model.clipAnimations=clips;model.SaveAndReimport();
            foreach(string name in new[]{"COLORMAP.png","NORMAL.jpg"}){
                var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"Textures/"+name);importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=2;importer.sRGBTexture=name=="COLORMAP.png";
                if(name=="NORMAL.jpg")importer.textureType=TextureImporterType.NormalMap;importer.SaveAndReimport();
            }
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"GreatWhite.mat");
            if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,Root+"GreatWhite.mat");}
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/COLORMAP.png"));material.SetColor("_BaseColor",Color.white);material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/NORMAL.jpg"));material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.45f);material.SetFloat("_Smoothness",.3f);material.SetFloat("_Metallic",0);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Root+"GreatWhite.controller");
            if(!controller){controller=AnimatorController.CreateAnimatorControllerAtPath(Root+"GreatWhite.controller");var state=controller.layers[0].stateMachine.AddState("Swim");state.motion=AssetDatabase.LoadAllAssetsAtPath(Root+"GreatWhite.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));controller.layers[0].stateMachine.defaultState=state;}
            var root=new GameObject("GreatWhiteShark");
            try{
                var body=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"GreatWhite.fbx"),root.transform);body.name="Supplied great white";
                foreach(var renderer in body.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();
                var head=body.GetComponentsInChildren<Transform>().First(t=>t.name=="Bone.015");var forward=head.position;forward.y=0;
                forward=Mathf.Abs(forward.x)>Mathf.Abs(forward.z)?Vector3.right*Mathf.Sign(forward.x):Vector3.forward*Mathf.Sign(forward.z);
                body.transform.rotation=Quaternion.FromToRotation(forward,Vector3.forward);
                var renderers=body.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                float scale=5/bounds.size.z;body.transform.localScale*=scale;body.transform.localPosition-=bounds.center*scale;
                var animator=body.GetComponentInChildren<Animator>();if(!animator)animator=body.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
                PrefabUtility.SaveAsPrefabAsset(root,Root+"GreatWhiteShark.prefab");EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
                Debug.Log("Prepared private 5m great white. Source bounds="+bounds+", scale="+scale+"; clips="+clips.Length);
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        [MenuItem("CityForge/Wildlife/Find coastal shark")]
        public static void Find()=>UnityEngine.Object.FindFirstObjectByType<World.CoastalSharkPopulation>()?.FindShark();
    }
}
#endif
