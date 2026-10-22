#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using CityForge.World;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class WildlifeImporter
    {
        const string Pack="Assets/Animal pack deluxe/";
        const string Output="Assets/CityForge/Resources/Wildlife/";
        static readonly Dictionary<Material,Material> materials=new();
        static WildlifeImporter(){EditorApplication.update+=Poll;}
        static void Poll(){const string request="Library/WildlifeImport.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
            if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}File.Delete(request);
            try{Build();File.WriteAllText("Library/WildlifeImport.result","PASS: curated temperate mammals, fish and Living Birds; local URP materials; idle/walk-only looping controllers; original licensed assets unchanged.");}
            catch(Exception e){File.WriteAllText("Library/WildlifeImport.result","FAIL: "+e);Debug.LogException(e);}}
        [MenuItem("CityForge/Animals/Build Local Temperate Wildlife")]
        public static void Build(){
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop play before building wildlife.");
            if(!Directory.Exists(Pack))throw new FileNotFoundException("Import Animal Pack Deluxe first.");
            Directory.CreateDirectory(Output);AssetDatabase.Refresh();materials.Clear();
            var entries=new List<WildlifeSpecies>();
            Add("deer","Deer_prefab","Deer_Idle.fbx","Deer_Walk.fbx",WildlifeHabitat.Woodland,8,1.6f,.8f);
            Add("rabbit","Wild_rabbit_prefab","WildRabbit_Idle.fbx","WildRabbit_Walk.fbx",WildlifeHabitat.WoodlandEdge,10,.38f,.45f);
            Add("boar","Wild_boar_prefab","WildBoar_Idle.fbx","WildBoar_Walk.fbx",WildlifeHabitat.Woodland,4,.85f,.5f);
            Add("wolf","Wolf_prefab","Wolf_IdleA.fbx","Wolf_Walk.fbx",WildlifeHabitat.Woodland,1,.85f,.6f);
            Add("bear","Brown_bear_prefab","Bear_Idle.fbx","Bear_Walk.fbx",WildlifeHabitat.Woodland,1,1.25f,.35f);
            Add("frog","Common_frog_prefab","common_frog_idle_anim.FBX","common_frog_walk_anim.FBX",WildlifeHabitat.FreshwaterBank,4,.12f,0);
            Add("crab","Crab_prefab","crab_idle_anim.FBX","crab_walk_anim.FBX",WildlifeHabitat.RiverBankShallows,1,.15f,.22f);
            Add("perch","Perch_fish_prefab","perch_fish_swim_anim.FBX","perch_fish_swim_anim.FBX",WildlifeHabitat.Freshwater,5,.4f,.25f,true);
            Add("pike","Pike_prefab","pike_swim_anim.FBX","pike_swim_anim.FBX",WildlifeHabitat.Freshwater,2,.75f,.3f,true);
            Add("salmon","Salmon_prefab","salmon_swim_anim.FBX","salmon_swim_anim.FBX",WildlifeHabitat.River,2,.65f,.4f,true);
            if(Directory.Exists("Assets/living birds")){
                foreach(var bird in new[]{"sparrow","chickadee","robin","goldFinch","cardinal","blueJay","crow"}){
                    bool crow=bird=="crow";
                    Add("bird-"+bird,"lb_"+bird,"bird@watch01.FBX","bird@hopForward.FBX",WildlifeHabitat.WoodlandBird,crow?1:4,crow?.42f:.22f,.3f,false,"Assets/living birds/",true);
                    Add("bird-"+bird+"-flight","lb_"+bird,"bird@flyStraight.FBX","bird@flyStraight.FBX",WildlifeHabitat.FlyingBird,crow?1:4,crow?.9f:.42f,3f,true,"Assets/living birds/",true);
                }
            }
            var library=AssetDatabase.LoadAssetAtPath<WildlifeLibrary>(Output+"TemperateWildlife.asset");if(!library){library=ScriptableObject.CreateInstance<WildlifeLibrary>();AssetDatabase.CreateAsset(library,Output+"TemperateWildlife.asset");}
            File.WriteAllText(Output+"Available.txt","Temperate wildlife assets installed locally.\n");
            foreach(var entry in entries)AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(entry.prefab),ImportAssetOptions.ForceUpdate);
            library.species=entries.ToArray();EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            void Add(string id,string prefab,string idle,string walk,WildlifeHabitat habitat,int weight,float size,float speed,bool length=false,string pack=Pack,bool bird=false){
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(pack+(bird?"resources/":"Prefabs/")+prefab+".prefab");if(!source)throw new FileNotFoundException(prefab);
                var instance=UnityEngine.Object.Instantiate(source);instance.name=id;
                try{
                    foreach(var script in instance.GetComponentsInChildren<MonoBehaviour>(true))UnityEngine.Object.DestroyImmediate(script);
                    foreach(var collider in instance.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
                    foreach(var body in instance.GetComponentsInChildren<Rigidbody>(true))UnityEngine.Object.DestroyImmediate(body);
                    foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterials=renderer.sharedMaterials.Select(ConvertMaterial).ToArray();
                    var animator=instance.GetComponentInChildren<Animator>();if(!animator)throw new Exception(id+" lacks Animator");
                    string controllerPath=Output+id+".controller";
                    var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                    if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                    controller.parameters=Array.Empty<AnimatorControllerParameter>();controller.AddParameter("Moving",AnimatorControllerParameterType.Bool);
                    var machine=controller.layers[0].stateMachine;foreach(var state in machine.states)machine.RemoveState(state.state);
                    EditorUtility.SetDirty(controller);var still=machine.AddState("Idle");var moving=machine.AddState("Walk");
                    still.motion=Clip(id+"-idle",idle,pack);moving.motion=Clip(id+"-walk",walk,pack);machine.defaultState=still;
                    var start=still.AddTransition(moving);start.hasExitTime=false;start.duration=.2f;start.AddCondition(AnimatorConditionMode.If,0,"Moving");
                    var stop=moving.AddTransition(still);stop.hasExitTime=false;stop.duration=.2f;stop.AddCondition(AnimatorConditionMode.IfNot,0,"Moving");
                    animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;
                    var renderers=instance.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)throw new Exception(id+" has no renderer");
                    var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    float dimension=length?Mathf.Max(bounds.size.x,bounds.size.z):bounds.size.y;
                    if(dimension<.001f)throw new Exception(id+" empty bounds");
                    float scale=size/dimension;
                    var wrapper=new GameObject(id);instance.transform.SetParent(wrapper.transform,false);instance.transform.localScale*=scale;
                    instance.transform.localPosition-=Vector3.up*(length?bounds.center.y:bounds.min.y)*scale;
                    try{var saved=PrefabUtility.SaveAsPrefabAsset(wrapper,Output+id+".prefab");entries.Add(new WildlifeSpecies{id=id,prefab=saved,habitat=habitat,weight=weight,speed=speed});}
                    finally{UnityEngine.Object.DestroyImmediate(wrapper);instance=null;}
                }finally{if(instance)UnityEngine.Object.DestroyImmediate(instance);}
            }
        }
        static AnimationClip Clip(string name,string file,string pack){
            var source=AssetDatabase.LoadAllAssetsAtPath(pack+(pack==Pack?"Animations/":"animations/")+file).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview__"));
            if(!source)throw new FileNotFoundException(file);var clip=UnityEngine.Object.Instantiate(source);clip.name=name;AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());
            var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.keepOriginalPositionXZ=true;settings.keepOriginalPositionY=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
            string path=Output+name+".anim";var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(old){EditorUtility.CopySerialized(clip,old);UnityEngine.Object.DestroyImmediate(clip);return old;}AssetDatabase.CreateAsset(clip,path);return clip;
        }
        static Material ConvertMaterial(Material source){
            if(!source)throw new Exception("Animal has an empty material slot");if(materials.TryGetValue(source,out var cached))return cached;
            string guid=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));string path=Output+"Material-"+guid+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
            material.name="Wildlife "+source.name;material.SetTexture("_BaseMap",source.mainTexture);material.SetColor("_BaseColor",source.HasProperty("_Color")?source.color:Color.white);
            material.SetFloat("_Smoothness",.15f);material.SetFloat("_Metallic",0);material.enableInstancing=true;
            if(source.HasProperty("_BumpMap")&&source.GetTexture("_BumpMap")){material.SetTexture("_BumpMap",source.GetTexture("_BumpMap"));material.EnableKeyword("_NORMALMAP");}
            if(source.HasProperty("_Mode")&&source.GetFloat("_Mode")==1){material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",source.GetFloat("_Cutoff"));material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=2450;}
            EditorUtility.SetDirty(material);materials[source]=material;return material;
        }
    }
}
#endif
