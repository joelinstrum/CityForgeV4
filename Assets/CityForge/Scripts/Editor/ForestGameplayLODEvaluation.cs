using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CityForge.World;
using CityForge.CameraSystem;
namespace CityForge.Editor
{
 [InitializeOnLoad] public static class ForestGameplayLODEvaluation
 {
  public const string Out="/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs/UnityGameplayLOD/";
  public const string Prior="/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs/UnityFrozenEvaluation/";
  static ForestGameplayLODEvaluation(){EditorApplication.playModeStateChanged+=State;EditorApplication.update+=Poll;}
  static void Poll(){var path=Path.Combine(Application.dataPath,"../Library/ForestGameplayLOD.request");if(!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists(path)){File.Delete(path);EditorApplication.delayCall+=Run;}}
  [MenuItem("CityForge/Frozen Broadleaf Evaluation/Preview Optimized Forest")]
  public static void Preview(){SessionState.SetBool("GameplayLODInteractive",true);Run();}
  [MenuItem("CityForge/Frozen Broadleaf Evaluation/Evaluate Gameplay LODs")]
  public static void Run()
  {
   if(EditorApplication.isPlaying){SessionState.SetBool("RestartGameplayLODEvaluation",true);EditorApplication.isPlaying=false;return;}
   Directory.CreateDirectory(Out);
   var shader=Shader.Find("CityForge/Evaluation/Frozen Blender Leaf");if(shader==null || ShaderUtil.ShaderHasError(shader))throw new Exception("Distance coverage shader must compile.");
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   SessionState.SetBool("GameplayLODEvaluationRunning",true);EditorApplication.isPlaying=true;
  }
  static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool("RestartGameplayLODEvaluation",false)){SessionState.SetBool("RestartGameplayLODEvaluation",false);EditorApplication.delayCall+=Run;}if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("GameplayLODEvaluationRunning",false)){SessionState.SetBool("GameplayLODEvaluationRunning",false);{var runner=new GameObject("Gameplay forest LOD preview").AddComponent<ForestGameplayLODEvaluationRunner>();runner.InteractiveOnly=SessionState.GetBool("GameplayLODInteractive",false);SessionState.SetBool("GameplayLODInteractive",false);}}}
 }
 public sealed class ForestGameplayLODEvaluationRunner:MonoBehaviour
 {
  public bool InteractiveOnly;
  [Serializable] class Placement{public int index;public float x,y,z,yaw,scale;}
  [Serializable] class PlacementList{public Placement[] trees;}
  Camera cam;CityForgeCameraController rig;ForestDistancePresentation policy;Renderer[] renderers;Vector3 focus;FrozenCrownLighting lighting;BroadleafGameplayLOD gameplayLOD;
  readonly string[] labels={"Zoom1_Before","Zoom1_After","Zoom2_Before","Zoom2_After","Zoom3_Before","Zoom3_After","Zoom4_Before","Zoom4_After","Zoom5_Before","Zoom5_After"};
  int stage=-1,frames;float start;double elapsedMs;long draws,tris;List<string> results=new();
  void Start(){try{
   PrototypeRegionView.Open("Distance Coverage Evaluation",null,730421,RegionSize.Medium);var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();view.EnterDistrict(3);rig=view.GetComponentInChildren<CityForgeCameraController>();cam=rig.GetComponent<Camera>();
   view.transform.Find("RandomTrees").gameObject.SetActive(false);view.GetComponent<DistantTreeRenderer>().enabled=false;foreach(var c in view.GetComponentsInChildren<Canvas>())c.gameObject.SetActive(false);
   var data=JsonUtility.FromJson<PlacementList>(File.ReadAllText("/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs/UnityOrganicScatter/placements.json"));File.WriteAllText(ForestGameplayLODEvaluation.Out+"placements.json",JsonUtility.ToJson(data,true));
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(FrozenBroadleafEvaluation.Art+"FrozenBroadleaf.prefab");var stand=new GameObject("Organic320 unchanged tree test");stand.transform.SetParent(view.transform);
   foreach(var p in data.trees){var t=Instantiate(prefab,stand.transform);t.name="FrozenTree_"+p.index;t.transform.position=new Vector3(p.x,p.y,p.z);t.transform.rotation=Quaternion.Euler(0,p.yaw,0);t.transform.localScale=Vector3.one*p.scale;}
   renderers=stand.GetComponentsInChildren<Renderer>();foreach(var r in renderers)foreach(var m in r.sharedMaterials)if(m.shader.name=="Universal Render Pipeline/Lit"){m.SetColor("_EmissionColor",new Color(.10f,.092f,.082f,1));m.EnableKeyword("_EMISSION");}policy=stand.AddComponent<ForestDistancePresentation>();policy.Initialize(rig,renderers);lighting=stand.AddComponent<FrozenCrownLighting>();lighting.Initialize(AssetDatabase.LoadAssetAtPath<TextAsset>(FrozenBroadleafEvaluation.Art+"Frozen_CrownLightingGuides.bytes"));
   focus=new Vector3(632.70f,11.44f+7,160.65f);
   // Recover exact camera focus used in the original test from the seed-generated original tree.
   var original=view.transform.Find("RandomTrees").Cast<Transform>().OrderBy(t=>(new Vector2(t.position.x-632.70f,t.position.z-160.65f)).sqrMagnitude).First();focus=original.position+Vector3.up*7;focus.y=view.GetComponentInChildren<Terrain>().SampleHeight(original.position)+7;
   typeof(CityForgeCameraController).GetField("focus",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(rig,focus);
   var count=prefab.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3);if(count!=111768)throw new Exception("Frozen geometry count changed");results.Add("320 instances;111768 triangles/tree; organic clustered scatter; yaw0–360 and scale.90–1.10; gameplay pitch35.264; unchanged source palette and region lighting; foliage shading comparison; source controls versus zoom-selected derived LODs.");
   var library=AssetDatabase.LoadAssetAtPath<BroadleafGameplayLODLibrary>("Assets/CityForge/Resources/BroadleafGameplayLOD.asset") ?? BroadleafGameplayLODBaker.Build(cam);gameplayLOD=stand.AddComponent<BroadleafGameplayLOD>();var cluster=AssetDatabase.LoadAssetAtPath<BroadleafCanopyCluster>("Assets/CityForge/Resources/Organic320Canopy.asset") ?? BroadleafGameplayLODBaker.BuildCluster(cam,renderers);gameplayLOD.Initialize(rig,library,cluster);if(InteractiveOnly){view.enabled=false;gameplayLOD.ShowPreviewControls=true;rig.SetZoomLevel(3);lighting.SetProfile(true);gameplayLOD.SetLODEnabled(true);enabled=false;return;}
   EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();Next();
  }catch(Exception e){Fail(e);}}
  PlacementList Scatter(Terrain terrain)
  {
   var rng=new System.Random(730421);var points=new List<Vector2>();var parents=new List<Vector2>();
   System.Func<float> unit=()=> (float)rng.NextDouble();
   System.Func<Vector2> uniform=()=>{float a=unit()*Mathf.PI*2,r=Mathf.Sqrt(unit());return new Vector2(Mathf.Cos(a)*72*r,Mathf.Sin(a)*60*r);};
   for(int i=0;i<14;i++)parents.Add(uniform()*.82f);
   var entries=new List<Placement>();int attempts=0;
   while(entries.Count<320 && attempts++<200000)
   {
    Vector2 q;if(unit()<.58f){var c=parents[rng.Next(parents.Count)];float a=unit()*Mathf.PI*2;float r=Mathf.Sqrt(-2*Mathf.Log(Mathf.Max(.00001f,unit())))*9;q=c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;}else q=uniform();
    float angle=Mathf.Atan2(q.y/60,q.x/72);float edge=1+.08f*Mathf.Sin(angle*3+.7f)+.055f*Mathf.Cos(angle*5-1.2f);
    if(q.x*q.x/(72*72)+q.y*q.y/(60*60)>edge*edge)continue;
    if((q-new Vector2(-22,9)).sqrMagnitude<81 || (q-new Vector2(27,-18)).sqrMagnitude<49)continue;
    if(points.Any(v=>(v-q).sqrMagnitude<2.8f*2.8f))continue;
    var pos=new Vector3(631.8388f+q.x,0,159.2643f+q.y);pos.y=terrain.SampleHeight(pos)+terrain.transform.position.y;
    points.Add(q);entries.Add(new Placement{index=entries.Count,x=pos.x,y=pos.y,z=pos.z,yaw=unit()*360,scale=.90f+unit()*.20f});
   }
   if(entries.Count!=320)throw new Exception("Organic scatter did not reach320 trees");return new PlacementList{trees=entries.ToArray()};
  }
  void Next(){stage++;if(stage==labels.Length){Verify();File.WriteAllLines(ForestGameplayLODEvaluation.Out+"measurements.txt",results);policy.SetDiagnosticPolicy(true,true);rig.SetZoomLevel(3);lighting.SetProfile(true);gameplayLOD.SetLODEnabled(true);foreach(var r in renderers){if(r.sharedMaterials.Any(m=>m.shader.name=="Universal Render Pipeline/Lit")){var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);b.SetColor("_BaseColor",new Color(.26f,.235f,.205f,1).gamma);b.SetColor("_EmissionColor",new Color(.10f,.092f,.082f,1));r.SetPropertyBlock(b);}}Debug.Log("GAMEPLAY LOD COMPARISON COMPLETE");enabled=false;return;}
   int level=new[]{1,2,3,4,5}[stage/2];bool after=stage%2==1;
   policy.SetDiagnosticPolicy(true,true);rig.SetZoomLevel(level);lighting.SetProfile(true);
   foreach(var renderer in renderers){var b=new MaterialPropertyBlock();renderer.GetPropertyBlock(b);b.SetFloat("_FarCanopyPresentation",level>=4?1:0);if(renderer.sharedMaterials.Any(m=>m.shader.name=="Universal Render Pipeline/Lit"))b.SetColor("_BaseColor",new Color(.26f,.235f,.205f,1).gamma);if(renderer.sharedMaterials.Any(m=>m.shader.name=="Universal Render Pipeline/Lit"))b.SetColor("_EmissionColor",new Color(.10f,.092f,.082f,1));renderer.SetPropertyBlock(b);}
   bool far=level>=4;foreach(var mr in renderers){var block=new MaterialPropertyBlock();mr.GetPropertyBlock(block);if((mr.shadowCastingMode==ShadowCastingMode.Off)!=far)throw new Exception("Incorrect tree-only shadow policy");if((block.GetFloat("_DistanceCoverage")>.5)!=far)throw new Exception("Incorrect coverage policy");}
   foreach(var r in renderers)foreach(var m in r.sharedMaterials)if(m.shader.name=="Universal Render Pipeline/Lit"){m.SetColor("_EmissionColor",new Color(.10f,.092f,.082f,1));m.EnableKeyword("_EMISSION");}
   gameplayLOD.SetLODEnabled(after);
   start=Time.realtimeSinceStartup;frames=0;elapsedMs=0;draws=tris=0;
  }
  void Verify()
  {
   var checks=new List<string>();gameplayLOD.SetLODEnabled(true);
   for(int pass=0;pass<3;pass++)for(int level=1;level<=5;level++){rig.SetZoomLevel(level);if(gameplayLOD.ActiveTriangles<=0)throw new Exception("Empty LOD atzoom"+level);if(level==4 && gameplayLOD.ActiveTriangles!=640)throw new Exception("Unexpected far batch geometry");if(level==5 && gameplayLOD.ActiveTriangles!=2)throw new Exception("Cluster did not replace far trees");checks.Add("pass="+pass+",zoom="+level+",switchMs="+gameplayLOD.LastSwitchMs.ToString("F3")+",treeTriangles="+gameplayLOD.ActiveTriangles);}
   var field=typeof(CityForgeCameraController).GetField("yaw",BindingFlags.Instance|BindingFlags.NonPublic);float yaw=(float)field.GetValue(rig);
   try{foreach(var angle in new[]{135,225,315}){field.SetValue(rig,(float)angle);rig.ApplyPose();gameplayLOD.RefreshPresentation();Capture("Validation_Zoom5_Yaw"+angle,1920,1080);}}
   finally{field.SetValue(rig,yaw);rig.ApplyPose();gameplayLOD.RefreshPresentation();}
   File.WriteAllLines(ForestGameplayLODEvaluation.Out+"switch_verification.txt",checks);
  }
  void Update(){try{float elapsed=Time.realtimeSinceStartup-start;if(elapsed<2)return;frames++;elapsedMs+=Time.unscaledDeltaTime*1000;draws+=UnityStats.drawCalls;tris+=UnityStats.triangles;if(elapsed<7)return;
   results.Add(labels[stage]+": zoom="+rig.ZoomLevel+", orthoHalfHeight="+cam.orthographicSize+", FPS="+(1000*frames/elapsedMs).ToString("F2")+", ms="+(elapsedMs/frames).ToString("F2")+", draws="+(draws/(double)frames).ToString("F1")+", renderedTriangles="+(tris/(double)frames).ToString("F0")+", switchMs="+gameplayLOD.LastSwitchMs.ToString("F3")+", activeTreeTriangles="+gameplayLOD.ActiveTriangles+", gameView="+Screen.width+"x"+Screen.height);
   Capture(labels[stage],1920,1080);if(stage==1 || stage==3 || stage==5 || stage==7 || stage==9)Pan();File.WriteAllLines(ForestGameplayLODEvaluation.Out+"measurements.txt",results);Next();
  }catch(Exception e){Fail(e);}}
  void Capture(string label,int w,int h){gameplayLOD.RenderInstances();var rt=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32);rt.Create();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});var old=RenderTexture.active;RenderTexture.active=rt;var im=new Texture2D(w,h,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,w,h),0,0);im.Apply();File.WriteAllBytes(ForestGameplayLODEvaluation.Out+label+".png",im.EncodeToPNG());RenderTexture.active=old;DestroyImmediate(im);rt.Release();DestroyImmediate(rt);}
  void Pan(){var field=typeof(CityForgeCameraController).GetField("focus",BindingFlags.Instance|BindingFlags.NonPublic);for(int f=0;f<12;f++){field.SetValue(rig,focus+Vector3.right*(f*.15f));rig.ApplyPose();Capture(labels[stage]+"_Pan"+f.ToString("D2"),960,540);}field.SetValue(rig,focus);rig.ApplyPose();}
  void Fail(Exception e){Directory.CreateDirectory(ForestGameplayLODEvaluation.Out);File.WriteAllText(ForestGameplayLODEvaluation.Out+"error.txt",e.ToString());Debug.LogException(e);enabled=false;}
 }
}
