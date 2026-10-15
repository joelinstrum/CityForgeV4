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
 [InitializeOnLoad] public static class DenseForestRestoredCrownStudy
 {
  public const string Folder="Assets/CityForge/Art/Trees/DenseForestTrees/RestoredCrownStudy/";
  public const string Output="/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs/RestoredCrownStudy/";
  static DenseForestRestoredCrownStudy(){EditorApplication.playModeStateChanged+=State;}
  static T Save<T>(T v,string p) where T:UnityEngine.Object {v.name=Path.GetFileNameWithoutExtension(p);var old=AssetDatabase.LoadAssetAtPath<T>(p);if(old){EditorUtility.CopySerialized(v,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(v);return old;}AssetDatabase.CreateAsset(v,p);return v;}
  public static BroadleafGameplayLODLibrary Build(){
   AssetDatabase.Refresh();
   foreach(var name in new[]{"Hardwood_Albedo.jpg","Hardwood_Normal.jpg","LayeredLeaf_Albedo.png"}){var imp=(TextureImporter)AssetImporter.GetAtPath(Folder+name);imp.textureType=name.Contains("Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;imp.sRGBTexture=!name.Contains("Normal");imp.mipmapEnabled=true;imp.anisoLevel=8;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();}
   var close=AssetDatabase.LoadAssetAtPath<BroadleafGameplayLODLibrary>(DenseForestFamilyBuilder.Art+"CloseLOD0/A_Rounded_Summer/GameplayLOD.asset");var lib=UnityEngine.Object.Instantiate(close);lib.honorAuthoredBarkMaterial=true;lib.meshTiers=(GameObject[])close.meshTiers.Clone();
   var bark=new Material(AssetDatabase.LoadAssetAtPath<Material>(DenseForestFamilyBuilder.Art+"CloseLOD0/MatureHardwood.mat"));bark.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"Hardwood_Albedo.jpg"));bark.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"Hardwood_Normal.jpg"));bark.SetTexture("_EmissionMap",bark.GetTexture("_BaseMap"));bark.SetColor("_BaseColor",Color.white);bark.SetColor("_EmissionColor",new Color(.90f,.82f,.72f));bark.SetFloat("_BumpScale",.60f);bark.SetTextureScale("_BaseMap",new Vector2(.55f,.32f));bark.EnableKeyword("_NORMALMAP");bark.EnableKeyword("_EMISSION");bark=Save(bark,Folder+"NaturalHardwood.mat");
   var tree=UnityEngine.Object.Instantiate(close.meshTiers[0]);
   try{foreach(var r in tree.GetComponentsInChildren<MeshRenderer>()){
    var mats=r.sharedMaterials;for(int j=0;j<mats.Length;j++){
     if(r.name.ToLowerInvariant().Contains("foliage")){var m=new Material(mats[j]);m.shader=Shader.Find("CityForge/Trees/Dense Layered Crown");m.SetTexture("_CoverageMap",m.GetTexture("_BaseMap"));m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"LayeredLeaf_Albedo.png"));m.SetFloat("_LeafDetail",.85f);m.SetFloat("_ClusterNormalStrength",.20f);m.SetFloat("_ClusterContrast",.12f);m.SetFloat("_LobeNormalBlend",.60f);m.SetFloat("_DistanceCoverage",0);m.SetFloat("_DiffuseSkyFill",.55f);m.enableInstancing=true;mats[j]=Save(m,Folder+"Leaf"+j+".mat");}else mats[j]=bark;
    }r.sharedMaterials=mats;
   }
   lib.meshTiers[0]=PrefabUtility.SaveAsPrefabAsset(tree,Folder+"LOD0.prefab");
   for(int k=0;k<tree.GetComponentsInChildren<MeshFilter>().Length;k++){var a=tree.GetComponentsInChildren<MeshFilter>()[k];var b=close.meshTiers[0].GetComponentsInChildren<MeshFilter>()[k];if(a.sharedMesh!=b.sharedMesh)throw new Exception("Restored crown mesh changed");}
   }finally{UnityEngine.Object.DestroyImmediate(tree);}
   lib=Save(lib,Folder+"GameplayLOD.asset");Save(UnityEngine.Object.Instantiate(lib),"Assets/CityForge/Resources/DenseForestRestoredCrownStudy.asset");AssetDatabase.SaveAssets();Debug.Log("Restored crown: all mesh references exactly match the dense source; only LOD0 materials replaced.");return lib;
  }
  [MenuItem("CityForge/Dense Forest Trees/Preview Restored Dense Crown")]
  public static void Run(){if(EditorApplication.isPlaying){SessionState.SetBool("RestoredCrownRestart",true);EditorApplication.isPlaying=false;return;}Build();SessionState.SetBool("RestoredCrownRun",true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;}
  static void State(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredEditMode && SessionState.GetBool("RestoredCrownRestart",false)){SessionState.SetBool("RestoredCrownRestart",false);EditorApplication.delayCall+=Run;}if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("RestoredCrownRun",false)){SessionState.SetBool("RestoredCrownRun",false);new GameObject("Single tree hierarchy study").AddComponent<DenseForestRestoredCrownRunner>();}}
 }
 public sealed class DenseForestRestoredCrownRunner:MonoBehaviour
 {
  CityForgeCameraController rig;Camera camera;Terrain terrain;BroadleafGameplayLOD renderer;GameObject stand;int stage;float next;bool ready;
  void Start(){Directory.CreateDirectory(DenseForestRestoredCrownStudy.Output);PrototypeRegionView.Open("Restored Dense Crown",null,730421,RegionSize.Medium);var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();view.EnterDistrict(3);rig=view.GetComponentInChildren<CityForgeCameraController>();camera=rig.GetComponent<Camera>();terrain=view.GetComponentInChildren<Terrain>();view.transform.Find("RandomTrees").gameObject.SetActive(false);view.GetComponent<DistantTreeRenderer>().enabled=false;foreach(var c in view.GetComponentsInChildren<Canvas>())c.gameObject.SetActive(false);view.enabled=false;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();next=Time.realtimeSinceStartup+2;}
  void Show(bool after,int zoom){if(stand)DestroyImmediate(stand);stand=new GameObject("One representative tree");var lib=AssetDatabase.LoadAssetAtPath<BroadleafGameplayLODLibrary>(after?DenseForestRestoredCrownStudy.Folder+"GameplayLOD.asset":DenseForestFamilyBuilder.Art+"CloseLOD0/A_Rounded_Summer/GameplayLOD.asset");var tree=Instantiate(lib.meshTiers[0],stand.transform);var p=new Vector3(632,0,160);p.y=terrain.SampleHeight(p);tree.transform.position=p;tree.transform.rotation=Quaternion.identity;tree.transform.localScale=Vector3.one;typeof(CityForgeCameraController).GetField("focus",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(rig,p+Vector3.up*8);rig.SetZoomLevel(zoom);rig.ApplyPose();renderer=stand.AddComponent<BroadleafGameplayLOD>();renderer.Initialize(rig,lib,null);}
  void Update(){if(stage>=6 || Time.realtimeSinceStartup<next || RenderPipelineManager.currentPipeline==null)return;
   if(!ready){Show(stage>=3,stage%3+1);ready=true;next=Time.realtimeSinceStartup+2;return;}
   Capture((stage>=3?"After":"Before")+"_Zoom"+(stage%3+1));stage++;ready=false;if(stage==6){Show(true,1);Debug.Log("RESTORED DENSE CROWN COMPLETE");}next=Time.realtimeSinceStartup+1;
  }
  void Capture(string name){CaptureSized(name,1920,1080);if(name=="After_Zoom1")CaptureSized(name+"_4K",3840,2160);}
  void CaptureSized(string name,int width,int height){renderer.RenderInstances();var rt=new RenderTexture(width,height,24);rt.Create();var old=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(DenseForestRestoredCrownStudy.Output+name+".png",image.EncodeToPNG());DestroyImmediate(image);}finally{RenderTexture.active=old;rt.Release();DestroyImmediate(rt);}}
  void OnGUI(){if(stage<6)return;GUI.Box(new Rect(20,Screen.height-90,500,70),"One representative tree - close hierarchy study");for(int z=1;z<=3;z++)if(GUI.Button(new Rect(30+(z-1)*95,Screen.height-50,90,25),"Zoom "+z))Show(true,z);if(GUI.Button(new Rect(320,Screen.height-50,90,25),"Previous"))Show(false,rig.ZoomLevel);}
 }
}
