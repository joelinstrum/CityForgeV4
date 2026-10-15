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
 [InitializeOnLoad] public static class DenseForestBranchSprayStudy
 {
  public const string Folder="Assets/CityForge/Art/Trees/DenseForestTrees/BranchSprayStudy/";
  public const string Output="/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs/BranchSprayStudy/";
  static DenseForestBranchSprayStudy(){EditorApplication.playModeStateChanged+=State;}
  static T Save<T>(T v,string p) where T:UnityEngine.Object {v.name=Path.GetFileNameWithoutExtension(p);var old=AssetDatabase.LoadAssetAtPath<T>(p);if(old){EditorUtility.CopySerialized(v,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(v);return old;}AssetDatabase.CreateAsset(v,p);return v;}
  public static BroadleafGameplayLODLibrary Build(){
   AssetDatabase.Refresh();
   var textureImporter=(TextureImporter)AssetImporter.GetAtPath(Folder+"SummerSpray_Albedo.png");textureImporter.sRGBTexture=true;textureImporter.alphaIsTransparency=true;textureImporter.mipmapEnabled=true;textureImporter.mipMapsPreserveCoverage=true;textureImporter.alphaTestReferenceValue=.32f;textureImporter.anisoLevel=8;textureImporter.maxTextureSize=2048;textureImporter.textureCompression=TextureImporterCompression.Uncompressed;textureImporter.SaveAndReimport();
   var path=Folder+"LOD0.fbx";var imp=(ModelImporter)AssetImporter.GetAtPath(path);imp.isReadable=true;imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.importCameras=false;imp.importLights=false;imp.importAnimation=false;imp.SaveAndReimport();
   var close=AssetDatabase.LoadAssetAtPath<BroadleafGameplayLODLibrary>(DenseForestFamilyBuilder.Art+"CloseLOD0/A_Rounded_Summer/GameplayLOD.asset");var lib=UnityEngine.Object.Instantiate(close);lib.honorAuthoredBarkMaterial=true;lib.meshTiers=(GameObject[])close.meshTiers.Clone();
   var bark=new Material(AssetDatabase.LoadAssetAtPath<Material>(DenseForestFamilyBuilder.Art+"CloseLOD0/MatureHardwood.mat"));bark.SetTexture("_EmissionMap",bark.GetTexture("_BaseMap"));bark.SetColor("_EmissionColor",new Color(.45f,.405f,.35f));bark.SetTextureScale("_BaseMap",new Vector2(.33f,.7f));bark.SetTextureScale("_BumpMap",new Vector2(.33f,.7f));bark.SetTextureScale("_MetallicGlossMap",new Vector2(.33f,.7f));bark.SetTextureScale("_EmissionMap",new Vector2(.33f,.7f));bark.EnableKeyword("_EMISSION");bark=Save(bark,Folder+"MutedHardwood.mat");
   var stage=new GameObject("Branch spray import");var tree=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path),stage.transform);
   try{
    foreach(var r in tree.GetComponentsInChildren<MeshRenderer>()){
     var mats=r.sharedMaterials;bool leaves=r.name.ToLowerInvariant().Contains("foliage");
     for(int j=0;j<mats.Length;j++)mats[j]=leaves?AssetDatabase.LoadAssetAtPath<Material>(DenseForestFamilyBuilder.Art+"SummerLeaf"+Mathf.Min(j,2)+".mat"):bark;r.sharedMaterials=mats;
    }
    var guide=stage.AddComponent<FrozenCrownLighting>();guide.Initialize(AssetDatabase.LoadAssetAtPath<TextAsset>(Folder+"CrownGuides.bytes"));if(guide.MissingVertices!=0)throw new Exception("Branch spray crown guides unmatched");
    foreach(var r in tree.GetComponentsInChildren<MeshRenderer>())if(r.name.ToLowerInvariant().Contains("foliage")){
     var mats=r.sharedMaterials;for(int j=0;j<mats.Length;j++){var m=new Material(mats[j]);m.shader=Shader.Find("CityForge/Trees/Branch Spray Leaves");if(j<2){m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"SummerSpray_Albedo.png"));m.SetColor("_BaseColor",j==0?new Color(.92f,1f,.88f,1):new Color(.72f,.80f,.68f,1));}m.SetFloat("_LobeNormalBlend",.50f);m.SetFloat("_DiffuseSkyFill",.65f);m.SetFloat("_Smoothness",.025f);m.SetFloat("_DistanceCoverage",0);m.enableInstancing=true;mats[j]=Save(m,Folder+"Leaf"+j+".mat");}r.sharedMaterials=mats;
    }
    foreach(var f in tree.GetComponentsInChildren<MeshFilter>())f.sharedMesh=Save(UnityEngine.Object.Instantiate(f.sharedMesh),Folder+f.name+".asset");
    lib.meshTiers[0]=PrefabUtility.SaveAsPrefabAsset(tree,Folder+"LOD0.prefab");
   }finally{UnityEngine.Object.DestroyImmediate(stage);}
   // The new geometry is evaluated at Zoom1/2. Zoom3 returns to the approved lobe volume.
   lib.meshTiers[1]=lib.meshTiers[0];
   lib.meshTiers[2]=close.meshTiers[2];
   lib=Save(lib,Folder+"GameplayLOD.asset");Save(UnityEngine.Object.Instantiate(lib),"Assets/CityForge/Resources/DenseForestBranchSprayStudy.asset");AssetDatabase.SaveAssets();return lib;
  }
  [MenuItem("CityForge/Dense Forest Trees/Preview Single Tree Branch Spray Study")]
  public static void Run(){if(EditorApplication.isPlaying){SessionState.SetBool("BranchSprayRestart",true);EditorApplication.isPlaying=false;return;}Build();SessionState.SetBool("BranchSprayRun",true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;}
  static void State(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredEditMode && SessionState.GetBool("BranchSprayRestart",false)){SessionState.SetBool("BranchSprayRestart",false);EditorApplication.delayCall+=Run;}if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("BranchSprayRun",false)){SessionState.SetBool("BranchSprayRun",false);new GameObject("Single tree hierarchy study").AddComponent<DenseForestBranchSprayRunner>();}}
 }
 public sealed class DenseForestBranchSprayRunner:MonoBehaviour
 {
  CityForgeCameraController rig;Camera camera;Terrain terrain;BroadleafGameplayLOD renderer;GameObject stand;int stage;float next;bool ready;
  void Start(){Directory.CreateDirectory(DenseForestBranchSprayStudy.Output);PrototypeRegionView.Open("Single Tree Branch Spray Study",null,730421,RegionSize.Medium);var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();view.EnterDistrict(3);rig=view.GetComponentInChildren<CityForgeCameraController>();camera=rig.GetComponent<Camera>();terrain=view.GetComponentInChildren<Terrain>();view.transform.Find("RandomTrees").gameObject.SetActive(false);view.GetComponent<DistantTreeRenderer>().enabled=false;foreach(var c in view.GetComponentsInChildren<Canvas>())c.gameObject.SetActive(false);view.enabled=false;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();next=Time.realtimeSinceStartup+2;}
  void Show(bool after,int zoom){if(stand)DestroyImmediate(stand);stand=new GameObject("One representative tree");var lib=AssetDatabase.LoadAssetAtPath<BroadleafGameplayLODLibrary>(after?DenseForestBranchSprayStudy.Folder+"GameplayLOD.asset":DenseForestFamilyBuilder.Art+"CloseLOD0/A_Rounded_Summer/GameplayLOD.asset");var tree=Instantiate(lib.meshTiers[0],stand.transform);var p=new Vector3(632,0,160);p.y=terrain.SampleHeight(p);tree.transform.position=p;tree.transform.rotation=Quaternion.identity;tree.transform.localScale=Vector3.one;typeof(CityForgeCameraController).GetField("focus",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(rig,p+Vector3.up*8);rig.SetZoomLevel(zoom);rig.ApplyPose();renderer=stand.AddComponent<BroadleafGameplayLOD>();renderer.Initialize(rig,lib,null);}
  void Update(){if(stage>=6 || Time.realtimeSinceStartup<next || RenderPipelineManager.currentPipeline==null)return;
   if(!ready){Show(stage>=3,stage%3+1);ready=true;next=Time.realtimeSinceStartup+2;return;}
   Capture((stage>=3?"After":"Before")+"_Zoom"+(stage%3+1));stage++;ready=false;if(stage==6){Show(true,1);Debug.Log("SINGLE TREE HIERARCHY COMPLETE");}next=Time.realtimeSinceStartup+1;
  }
  void Capture(string name){CaptureSized(name,1920,1080);if(name=="After_Zoom1")CaptureSized(name+"_4K",3840,2160);}
  void CaptureSized(string name,int width,int height){renderer.RenderInstances();var rt=new RenderTexture(width,height,24);rt.Create();var old=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(DenseForestBranchSprayStudy.Output+name+".png",image.EncodeToPNG());DestroyImmediate(image);}finally{RenderTexture.active=old;rt.Release();DestroyImmediate(rt);}}
  void OnGUI(){if(stage<6)return;GUI.Box(new Rect(20,Screen.height-90,500,70),"One representative tree - close hierarchy study");for(int z=1;z<=3;z++)if(GUI.Button(new Rect(30+(z-1)*95,Screen.height-50,90,25),"Zoom "+z))Show(true,z);if(GUI.Button(new Rect(320,Screen.height-50,90,25),"Previous"))Show(false,rig.ZoomLevel);}
 }
}
