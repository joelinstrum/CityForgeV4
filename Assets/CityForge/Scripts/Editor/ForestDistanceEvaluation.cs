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
 [InitializeOnLoad] public static class ForestDistanceEvaluation
 {
  public const string Out="/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs/UnityDistancePass/";
  public const string Prior="/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs/UnityFrozenEvaluation/";
  static ForestDistanceEvaluation(){EditorApplication.playModeStateChanged+=State;EditorApplication.update+=Poll;}
  static void Poll(){var path=Path.Combine(Application.dataPath,"../Library/ForestDistance.request");if(!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists(path)){File.Delete(path);EditorApplication.delayCall+=Run;}}
  [MenuItem("CityForge/Frozen Broadleaf Evaluation/Compare Distance Rendering")]
  public static void Run()
  {
   if(EditorApplication.isPlaying){SessionState.SetBool("RestartDistanceEvaluation",true);EditorApplication.isPlaying=false;return;}
   Directory.CreateDirectory(Out);
   var shader=Shader.Find("CityForge/Evaluation/Frozen Blender Leaf");if(shader==null || ShaderUtil.ShaderHasError(shader))throw new Exception("Distance coverage shader must compile.");
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   SessionState.SetBool("DistanceEvaluationRunning",true);EditorApplication.isPlaying=true;
  }
  static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool("RestartDistanceEvaluation",false)){SessionState.SetBool("RestartDistanceEvaluation",false);EditorApplication.delayCall+=Run;}if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("DistanceEvaluationRunning",false)){SessionState.SetBool("DistanceEvaluationRunning",false);new GameObject("Distance rendering comparison").AddComponent<ForestDistanceEvaluationRunner>();}}
 }
 public sealed class ForestDistanceEvaluationRunner:MonoBehaviour
 {
  [Serializable] class Placement{public int index;public float x,y,z,yaw,scale;}
  [Serializable] class PlacementList{public Placement[] trees;}
  Camera cam;CityForgeCameraController rig;ForestDistancePresentation policy;Renderer[] renderers;Vector3 focus;
  readonly string[] labels={"Zoom1_Control","Zoom3_Control","Zoom4_Before","Zoom4_ShadowsOffOnly","Zoom4_CoverageOnly","Zoom4_After","Zoom5_Before","Zoom5_ShadowsOffOnly","Zoom5_CoverageOnly","Zoom5_After"};
  int stage=-1,frames;float start;double elapsedMs;long draws,tris;List<string> results=new();
  void Start(){try{
   PrototypeRegionView.Open("Distance Coverage Evaluation",null,730421,RegionSize.Medium);var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();view.EnterDistrict(3);rig=view.GetComponentInChildren<CityForgeCameraController>();cam=rig.GetComponent<Camera>();
   view.transform.Find("RandomTrees").gameObject.SetActive(false);view.GetComponent<DistantTreeRenderer>().enabled=false;foreach(var c in view.GetComponentsInChildren<Canvas>())c.gameObject.SetActive(false);
   var data=JsonUtility.FromJson<PlacementList>(File.ReadAllText(ForestDistanceEvaluation.Prior+"Forest150_Zoom3_placements.json"));if(data.trees.Length!=150)throw new Exception("Expected exact150-tree baseline placement.");
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(FrozenBroadleafEvaluation.Art+"FrozenBroadleaf.prefab");var stand=new GameObject("Frozen150-tree distance test");stand.transform.SetParent(view.transform);
   foreach(var p in data.trees){var t=Instantiate(prefab,stand.transform);t.name="FrozenTree_"+p.index;t.transform.position=new Vector3(p.x,p.y,p.z);t.transform.rotation=Quaternion.Euler(0,p.yaw,0);t.transform.localScale=Vector3.one*p.scale;}
   renderers=stand.GetComponentsInChildren<Renderer>();policy=stand.AddComponent<ForestDistancePresentation>();policy.Initialize(rig,renderers);
   focus=new Vector3(632.70f,11.44f+7,160.65f);
   // Recover exact camera focus used in the original test from the seed-generated original tree.
   var original=view.transform.Find("RandomTrees").Cast<Transform>().OrderBy(t=>(new Vector2(t.position.x-632.70f,t.position.z-160.65f)).sqrMagnitude).First();focus=original.position+Vector3.up*7;focus.y=view.GetComponentInChildren<Terrain>().SampleHeight(original.position)+7;
   typeof(CityForgeCameraController).GetField("focus",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(rig,focus);
   var count=prefab.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3);if(count!=111768)throw new Exception("Frozen geometry count changed");results.Add("150 instances;111768 triangles/tree; exact prior positions/rotations/scales; gameplay pitch35.264; unchanged source palette and lighting; full meshes every zoom.");
   EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();Next();
  }catch(Exception e){Fail(e);}}
  void Next(){stage++;if(stage==labels.Length){File.WriteAllLines(ForestDistanceEvaluation.Out+"measurements.txt",results);policy.SetDiagnosticPolicy(true,true);rig.SetZoomLevel(4);Debug.Log("DISTANCE RENDERING COMPARISON COMPLETE");enabled=false;return;}
   int level=stage==0?1:stage==1?3:stage<6?4:5;int mode=stage<2?3:(stage-2)%4;bool coverage=mode>=2,shadowsOff=mode==1 || mode==3;
   policy.SetDiagnosticPolicy(coverage,shadowsOff);rig.SetZoomLevel(level);
   bool far=level>=4;foreach(var mr in renderers){var block=new MaterialPropertyBlock();mr.GetPropertyBlock(block);if((mr.shadowCastingMode==ShadowCastingMode.Off)!=(far && shadowsOff))throw new Exception("Incorrect tree-only shadow policy");if((block.GetFloat("_DistanceCoverage")>.5)!=(far && coverage))throw new Exception("Incorrect coverage policy");}
   start=Time.realtimeSinceStartup;frames=0;elapsedMs=0;draws=tris=0;
  }
  void Update(){try{float elapsed=Time.realtimeSinceStartup-start;if(elapsed<2)return;frames++;elapsedMs+=Time.unscaledDeltaTime*1000;draws+=UnityStats.drawCalls;tris+=UnityStats.triangles;if(elapsed<7)return;
   results.Add(labels[stage]+": zoom="+rig.ZoomLevel+", orthoHalfHeight="+cam.orthographicSize+", FPS="+(1000*frames/elapsedMs).ToString("F2")+", ms="+(elapsedMs/frames).ToString("F2")+", draws="+(draws/(double)frames).ToString("F1")+", renderedTriangles="+(tris/(double)frames).ToString("F0")+", gameView="+Screen.width+"x"+Screen.height);
   Capture(labels[stage],1920,1080);if(stage==5 || stage==9)Pan();File.WriteAllLines(ForestDistanceEvaluation.Out+"measurements.txt",results);Next();
  }catch(Exception e){Fail(e);}}
  void Capture(string label,int w,int h){var rt=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32);rt.Create();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});var old=RenderTexture.active;RenderTexture.active=rt;var im=new Texture2D(w,h,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,w,h),0,0);im.Apply();File.WriteAllBytes(ForestDistanceEvaluation.Out+label+".png",im.EncodeToPNG());RenderTexture.active=old;DestroyImmediate(im);rt.Release();DestroyImmediate(rt);}
  void Pan(){var field=typeof(CityForgeCameraController).GetField("focus",BindingFlags.Instance|BindingFlags.NonPublic);for(int f=0;f<12;f++){field.SetValue(rig,focus+Vector3.right*(f*.15f));rig.ApplyPose();Capture(labels[stage]+"_Pan"+f.ToString("D2"),960,540);}field.SetValue(rig,focus);rig.ApplyPose();}
  void Fail(Exception e){Directory.CreateDirectory(ForestDistanceEvaluation.Out);File.WriteAllText(ForestDistanceEvaluation.Out+"error.txt",e.ToString());Debug.LogException(e);enabled=false;}
 }
}
