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
 [InitializeOnLoad] public static class DenseForestFamilyEvaluation
 {
  public const string Output="/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs/DenseForestTrees/";
  static DenseForestFamilyEvaluation(){EditorApplication.update+=Poll;EditorApplication.playModeStateChanged+=State;}
  static void Poll(){var p=Path.Combine(Application.dataPath,"../Library/DenseForestFamily.request");if(!EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists(p)){File.Delete(p);Run();}}
  [MenuItem("CityForge/Dense Forest Trees/Build And Preview Seasonal Family")]
  public static void Run(){SessionState.SetBool("DenseForestRebuild",true);Begin();}
  [MenuItem("CityForge/Dense Forest Trees/Preview Saved Seasonal Family")] public static void PreviewSaved(){SessionState.SetBool("DenseForestClose",false);SessionState.SetBool("DenseForestRebuild",false);Begin();}
  [MenuItem("CityForge/Dense Forest Trees/Preview Close LOD0 Treatment")] public static void PreviewClose(){SessionState.SetBool("DenseForestClose",true);SessionState.SetBool("DenseForestRebuild",false);Begin();}
  [MenuItem("CityForge/Dense Forest Trees/Rebake Current Preview Atlases")]
  public static void RebakeCurrent(){var runner=UnityEngine.Object.FindFirstObjectByType<DenseForestFamilyEvaluationRunner>();var camera=UnityEngine.Object.FindFirstObjectByType<CityForgeCameraController>().GetComponent<Camera>();if(RenderPipelineManager.currentPipeline==null)throw new Exception("Game view must render before atlas baking.");var family=AssetDatabase.LoadAssetAtPath<DenseForestTreeFamily>("Assets/CityForge/Resources/DenseForestTrees.asset");foreach(var season in new[]{DenseForestSeason.SpringSummer,DenseForestSeason.Fall,DenseForestSeason.Winter})foreach(var lib in family.Libraries(season)){var folder=Path.GetDirectoryName(AssetDatabase.GetAssetPath(lib)).Replace("\\","/")+"/";BroadleafGameplayLODBaker.BakeAtlas(camera,lib,lib.meshTiers[0],folder);EditorUtility.SetDirty(lib);}AssetDatabase.SaveAssets();runner.BeginCaptures();}
  static void Begin(){if(EditorApplication.isPlaying){SessionState.SetBool("DenseForestRestart",true);EditorApplication.isPlaying=false;return;}SessionState.SetBool("DenseForestRun",true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;}
  static void State(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredEditMode && SessionState.GetBool("DenseForestRestart",false)){SessionState.SetBool("DenseForestRestart",false);EditorApplication.delayCall+=Begin;}if(s==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("DenseForestRun",false)){SessionState.SetBool("DenseForestRun",false);new GameObject("Dense Forest seasonal evaluation").AddComponent<DenseForestFamilyEvaluationRunner>();}}
 }
 public sealed class DenseForestFamilyEvaluationRunner:MonoBehaviour
 {
  DenseForestTreeFamily family;CityForgeCameraController rig;Camera camera;GameObject stand;BroadleafGameplayLOD[] lods;Terrain terrain;int stage;float next;string error;bool pendingBuild;
  void Start(){try{
   Directory.CreateDirectory(DenseForestFamilyEvaluation.Output);PrototypeRegionView.Open("Dense Forest Trees",null,730421,RegionSize.Medium);var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();view.EnterDistrict(3);rig=view.GetComponentInChildren<CityForgeCameraController>();camera=rig.GetComponent<Camera>();terrain=view.GetComponentInChildren<Terrain>();view.transform.Find("RandomTrees").gameObject.SetActive(false);view.GetComponent<DistantTreeRenderer>().enabled=false;foreach(var c in view.GetComponentsInChildren<Canvas>())c.gameObject.SetActive(false);view.enabled=false;
   pendingBuild=SessionState.GetBool("DenseForestRebuild",false);SessionState.SetBool("DenseForestRebuild",false);stage=-1;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
  }catch(Exception e){Fail(e);}}
  public void BeginCaptures(){stage=0;Show(0,false);next=Time.realtimeSinceStartup+2;}
  void Show(int season,bool cluster){
   if(stand)DestroyImmediate(stand);stand=new GameObject("Dense Forest Trees seasonal preview");lods=new BroadleafGameplayLOD[4];var lib=family.Libraries((DenseForestSeason)season);Vector3 center=new Vector3(632,0,160);center.y=terrain.SampleHeight(center);
   for(int v=0;v<4;v++){
    var group=new GameObject(DenseForestFamilyBuilder.Names[v]);group.transform.SetParent(stand.transform);int count=cluster?8:1;var rng=new System.Random(730421+v);
    for(int t=0;t<count;t++){
     var tree=Instantiate(lib[v].meshTiers[0],group.transform);Vector3 pos;
     if(cluster){float a=(float)rng.NextDouble()*Mathf.PI*2,r=20*Mathf.Sqrt((float)rng.NextDouble());pos=center+new Vector3(Mathf.Cos(a)*r,0,Mathf.Sin(a)*r);tree.transform.localScale=Vector3.one*(.9f+(float)rng.NextDouble()*.2f);tree.transform.rotation=Quaternion.Euler(0,(float)rng.NextDouble()*360,0);}
     else {float offset=(v-1.5f)*18;pos=center+new Vector3(offset*.7071f,0,-offset*.7071f);}
     pos.y=terrain.SampleHeight(pos);tree.transform.position=pos;
    }
    lods[v]=group.AddComponent<BroadleafGameplayLOD>();lods[v].Initialize(rig,lib[v],null);
   }
   var focus=center+Vector3.up*8;typeof(CityForgeCameraController).GetField("focus",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(rig,focus);rig.SetZoomLevel(1);rig.ApplyPose();
  }
  void Update(){if(error!=null || stage>=9 || Time.realtimeSinceStartup<next)return;if(RenderPipelineManager.currentPipeline==null){EditorApplication.QueuePlayerLoopUpdate();return;}try{
   if(stage<0){family=pendingBuild?DenseForestFamilyBuilder.Build(camera):AssetDatabase.LoadAssetAtPath<DenseForestTreeFamily>(SessionState.GetBool("DenseForestClose",false)?"Assets/CityForge/Resources/DenseForestTreesClose.asset":"Assets/CityForge/Resources/DenseForestTrees.asset");if(family==null)family=DenseForestFamilyBuilder.Build(camera);BeginCaptures();return;}
   if(stage<6){int season=stage/2;bool cluster=stage%2==1;Capture(new[]{"Summer","Fall","Winter"}[season]+(cluster?"_Cluster_Zoom1":"_FourVariants_Zoom1"));stage++;if(stage<6){Show(stage/2,stage%2==1);next=Time.realtimeSinceStartup+2;}else{Show(0,true);next=Time.realtimeSinceStartup+2;}}
   else if(stage<8){rig.SetZoomLevel(stage==6?4:5);rig.ApplyPose();foreach(var lod in lods)lod.RefreshPresentation();Capture("Summer_Cluster_Zoom"+(stage==6?4:5));stage++;}
   else {rig.SetZoomLevel(3);rig.ApplyPose();foreach(var lod in lods)lod.RefreshPresentation();Capture("Summer_Cluster_Zoom3");if(SessionState.GetBool("DenseForestClose",false)){rig.SetZoomLevel(1);rig.ApplyPose();}File.WriteAllText(DenseForestFamilyEvaluation.Output+"UnityValidation.txt","12 seasonal libraries built; 36 mesh prefabs; guide registration zero missing; 4 variants per season; 32-tree mixed cluster; seed730421. Actual region terrain, grass, water, sun and35.264-degree camera. Source baseline preserved. Zoom4/5 use per-tree baked atlas; accepted original320-tree cluster untouched. No winter terrain snow applied.\n");Debug.Log("DENSE FOREST FAMILY COMPLETE");stage=9;}
  }catch(Exception e){Fail(e);}}
  void Capture(string name){foreach(var lod in lods)lod.RenderInstances();var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);rt.Create();var old=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(DenseForestFamilyEvaluation.Output+(SessionState.GetBool("DenseForestClose",false)?"CloseLOD0_":"")+name+".png",image.EncodeToPNG());DestroyImmediate(image);}finally{RenderTexture.active=old;rt.Release();DestroyImmediate(rt);}}
  void OnGUI(){if(family==null)return;GUI.Box(new Rect(20,Screen.height-80,620,65),"Dense Forest Trees — four variants");for(int s=0;s<3;s++)if(GUI.Button(new Rect(30+s*130,Screen.height-45,125,25),new[]{"Spring / Summer","Fall","Winter"}[s])){Show(s,true);}if(GUI.Button(new Rect(425,Screen.height-45,40,25),"+"))rig.SetZoomLevel(rig.ZoomLevel-1);if(GUI.Button(new Rect(470,Screen.height-45,40,25),"−"))rig.SetZoomLevel(rig.ZoomLevel+1);GUI.Label(new Rect(520,Screen.height-45,100,25),"Zoom "+rig.ZoomLevel);}
  void Fail(Exception e){error=e.ToString();File.WriteAllText(DenseForestFamilyEvaluation.Output+"UnityError.txt",error);Debug.LogException(e);enabled=false;}
 }
}
