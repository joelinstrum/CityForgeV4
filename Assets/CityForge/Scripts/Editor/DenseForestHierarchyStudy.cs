using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CityForge.World;
using CityForge.CameraSystem;
namespace CityForge.Editor
{
 [InitializeOnLoad] public static class DenseForestHierarchyStudy
 {
  public const string Folder="Assets/CityForge/Art/Trees/DenseForestTrees/HierarchyStudy/";
  public const string Output="/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs/HierarchyStudy/";
  static DenseForestHierarchyStudy(){EditorApplication.playModeStateChanged+=State;}
  static T Save<T>(T v,string p) where T:UnityEngine.Object {v.name=Path.GetFileNameWithoutExtension(p);var old=AssetDatabase.LoadAssetAtPath<T>(p);if(old){EditorUtility.CopySerialized(v,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(v);return old;}AssetDatabase.CreateAsset(v,p);return v;}
  public static BroadleafGameplayLODLibrary Build(){
   var close=AssetDatabase.LoadAssetAtPath<BroadleafGameplayLODLibrary>(DenseForestFamilyBuilder.Art+"CloseLOD0/A_Rounded_Summer/GameplayLOD.asset");
   var lib=UnityEngine.Object.Instantiate(close);lib.honorAuthoredBarkMaterial=true;lib.meshTiers=(GameObject[])close.meshTiers.Clone();
   var bark=new Material(AssetDatabase.LoadAssetAtPath<Material>(DenseForestFamilyBuilder.Art+"CloseLOD0/MatureHardwood.mat"));bark.SetColor("_EmissionColor",new Color(.21f,.19f,.165f));bark.EnableKeyword("_EMISSION");bark=Save(bark,Folder+"MutedHardwood.mat");
   for(int tier=0;tier<3;tier++){
    var go=UnityEngine.Object.Instantiate(close.meshTiers[tier]);
    try{foreach(var r in go.GetComponentsInChildren<MeshRenderer>()){
     var mats=r.sharedMaterials;
     for(int j=0;j<mats.Length;j++){
      if(r.name.ToLowerInvariant().Contains("foliage")){var m=new Material(mats[j]);m.shader=Shader.Find("CityForge/Trees/Soft Cluster Hierarchy");m.SetFloat("_LeafDetail",new[]{.22f,.12f,.05f}[tier]);m.SetFloat("_ClusterNormalStrength",new[]{.65f,.55f,.35f}[tier]);m.SetFloat("_ClusterContrast",new[]{.26f,.22f,.16f}[tier]);m.SetFloat("_DistanceCoverage",0);m.SetFloat("_DiffuseSkyFill",.52f);m.enableInstancing=true;mats[j]=Save(m,Folder+"Tier"+tier+"Leaf"+j+".mat");}
      else mats[j]=bark;
     }r.sharedMaterials=mats;
    }lib.meshTiers[tier]=PrefabUtility.SaveAsPrefabAsset(go,Folder+"LOD"+tier+".prefab");}finally{UnityEngine.Object.DestroyImmediate(go);}
   }
   lib=Save(lib,Folder+"GameplayLOD.asset");Save(UnityEngine.Object.Instantiate(lib),"Assets/CityForge/Resources/DenseForestHierarchyStudy.asset");AssetDatabase.SaveAssets();return lib;
  }
  [MenuItem("CityForge/Dense Forest Trees/Preview Single Tree Hierarchy Study")]
  public static void Run(){if(EditorApplication.isPlaying){SessionState.SetBool("HierarchyRestart",true);EditorApplication.isPlaying=false;return;}Build();SessionState.SetBool("HierarchyRun",true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;}
  static void State(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredEditMode && SessionState.GetBool("HierarchyRestart",false)){SessionState.SetBool("HierarchyRestart",false);EditorApplication.delayCall+=Run;}if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("HierarchyRun",false)){SessionState.SetBool("HierarchyRun",false);new GameObject("Single tree hierarchy study").AddComponent<DenseForestHierarchyRunner>();}}
 }
 public sealed class DenseForestHierarchyRunner:MonoBehaviour
 {
  CityForgeCameraController rig;Camera camera;Terrain terrain;BroadleafGameplayLOD renderer;GameObject stand;int stage;float next;bool ready;
  void Start(){Directory.CreateDirectory(DenseForestHierarchyStudy.Output);PrototypeRegionView.Open("Single Tree Hierarchy Study",null,730421,RegionSize.Medium);var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();view.EnterDistrict(3);rig=view.GetComponentInChildren<CityForgeCameraController>();camera=rig.GetComponent<Camera>();terrain=view.GetComponentInChildren<Terrain>();view.transform.Find("RandomTrees").gameObject.SetActive(false);view.GetComponent<DistantTreeRenderer>().enabled=false;foreach(var c in view.GetComponentsInChildren<Canvas>())c.gameObject.SetActive(false);view.enabled=false;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();next=Time.realtimeSinceStartup+2;}
  void Show(bool after,int zoom){if(stand)DestroyImmediate(stand);stand=new GameObject("One representative tree");var lib=AssetDatabase.LoadAssetAtPath<BroadleafGameplayLODLibrary>(after?DenseForestHierarchyStudy.Folder+"GameplayLOD.asset":DenseForestFamilyBuilder.Art+"CloseLOD0/A_Rounded_Summer/GameplayLOD.asset");var tree=Instantiate(lib.meshTiers[0],stand.transform);var p=new Vector3(632,0,160);p.y=terrain.SampleHeight(p);tree.transform.position=p;tree.transform.rotation=Quaternion.identity;tree.transform.localScale=Vector3.one;typeof(CityForgeCameraController).GetField("focus",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(rig,p+Vector3.up*8);rig.SetZoomLevel(zoom);rig.ApplyPose();renderer=stand.AddComponent<BroadleafGameplayLOD>();renderer.Initialize(rig,lib,null);}
  void Update(){if(stage>=6 || Time.realtimeSinceStartup<next || RenderPipelineManager.currentPipeline==null)return;
   if(!ready){Show(stage>=3,stage%3+1);ready=true;next=Time.realtimeSinceStartup+2;return;}
   Capture((stage>=3?"After":"Before")+"_Zoom"+(stage%3+1));stage++;ready=false;if(stage==6){Show(true,1);Debug.Log("SINGLE TREE HIERARCHY COMPLETE");}next=Time.realtimeSinceStartup+1;
  }
  void Capture(string name){renderer.RenderInstances();var rt=new RenderTexture(1920,1080,24);rt.Create();var old=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(DenseForestHierarchyStudy.Output+name+".png",image.EncodeToPNG());DestroyImmediate(image);}finally{RenderTexture.active=old;rt.Release();DestroyImmediate(rt);}}
  void OnGUI(){if(stage<6)return;GUI.Box(new Rect(20,Screen.height-90,500,70),"One representative tree - close hierarchy study");for(int z=1;z<=3;z++)if(GUI.Button(new Rect(30+(z-1)*95,Screen.height-50,90,25),"Zoom "+z))Show(true,z);if(GUI.Button(new Rect(320,Screen.height-50,90,25),"Previous"))Show(false,rig.ZoomLevel);}
 }
}
