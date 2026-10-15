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
 [InitializeOnLoad] public static class ForestLightingEvaluation
 {
  public const string Out="/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs/UnityFoliageLighting/";
  public const string Prior="/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs/UnityFrozenEvaluation/";
  static ForestLightingEvaluation(){EditorApplication.playModeStateChanged+=State;EditorApplication.update+=Poll;}
  static void Poll(){var path=Path.Combine(Application.dataPath,"../Library/ForestLighting.request");if(!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists(path)){File.Delete(path);EditorApplication.delayCall+=Run;}}
  [MenuItem("CityForge/Frozen Broadleaf Evaluation/Compare Foliage Lighting")]
  public static void Run()
  {
   if(EditorApplication.isPlaying){SessionState.SetBool("RestartLightingEvaluation",true);EditorApplication.isPlaying=false;return;}
   Directory.CreateDirectory(Out);
   var shader=Shader.Find("CityForge/Evaluation/Frozen Blender Leaf");if(shader==null || ShaderUtil.ShaderHasError(shader))throw new Exception("Distance coverage shader must compile.");
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   SessionState.SetBool("LightingEvaluationRunning",true);EditorApplication.isPlaying=true;
  }
  static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool("RestartLightingEvaluation",false)){SessionState.SetBool("RestartLightingEvaluation",false);EditorApplication.delayCall+=Run;}if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("LightingEvaluationRunning",false)){SessionState.SetBool("LightingEvaluationRunning",false);new GameObject("Foliage lighting comparison").AddComponent<ForestLightingEvaluationRunner>();}}
 }
 public sealed class ForestLightingEvaluationRunner:MonoBehaviour
 {
  [Serializable] class Placement{public int index;public float x,y,z,yaw,scale;}
  [Serializable] class PlacementList{public Placement[] trees;}
  Camera cam;CityForgeCameraController rig;ForestDistancePresentation policy;Renderer[] renderers;Vector3 focus;FrozenCrownLighting lighting;
  readonly string[] labels={"Zoom1_Before","Zoom1_After","Zoom3_Before","Zoom3_After","Zoom4_Before","Zoom4_After","Zoom5_Before","Zoom5_After"};
  int stage=-1,frames;float start;double elapsedMs;long draws,tris;List<string> results=new();
  void Start(){try{
   PrototypeRegionView.Open("Distance Coverage Evaluation",null,730421,RegionSize.Medium);var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();view.EnterDistrict(3);rig=view.GetComponentInChildren<CityForgeCameraController>();cam=rig.GetComponent<Camera>();
   view.transform.Find("RandomTrees").gameObject.SetActive(false);view.GetComponent<DistantTreeRenderer>().enabled=false;foreach(var c in view.GetComponentsInChildren<Canvas>())c.gameObject.SetActive(false);
   var data=JsonUtility.FromJson<PlacementList>(File.ReadAllText(ForestLightingEvaluation.Prior+"Forest150_Zoom3_placements.json"));if(data.trees.Length!=150)throw new Exception("Expected exact150-tree baseline placement.");
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(FrozenBroadleafEvaluation.Art+"FrozenBroadleaf.prefab");var stand=new GameObject("Frozen150-tree distance test");stand.transform.SetParent(view.transform);
   foreach(var p in data.trees){var t=Instantiate(prefab,stand.transform);t.name="FrozenTree_"+p.index;t.transform.position=new Vector3(p.x,p.y,p.z);t.transform.rotation=Quaternion.Euler(0,p.yaw,0);t.transform.localScale=Vector3.one*p.scale;}
   renderers=stand.GetComponentsInChildren<Renderer>();policy=stand.AddComponent<ForestDistancePresentation>();policy.Initialize(rig,renderers);lighting=stand.AddComponent<FrozenCrownLighting>();lighting.Initialize(AssetDatabase.LoadAssetAtPath<TextAsset>(FrozenBroadleafEvaluation.Art+"Frozen_CrownLightingGuides.bytes"));
   focus=new Vector3(632.70f,11.44f+7,160.65f);
   // Recover exact camera focus used in the original test from the seed-generated original tree.
   var original=view.transform.Find("RandomTrees").Cast<Transform>().OrderBy(t=>(new Vector2(t.position.x-632.70f,t.position.z-160.65f)).sqrMagnitude).First();focus=original.position+Vector3.up*7;focus.y=view.GetComponentInChildren<Terrain>().SampleHeight(original.position)+7;
   typeof(CityForgeCameraController).GetField("focus",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(rig,focus);
   var count=prefab.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3);if(count!=111768)throw new Exception("Frozen geometry count changed");results.Add("150 instances;111768 triangles/tree; exact prior positions/rotations/scales; gameplay pitch35.264; unchanged source palette and region lighting; foliage shading comparison; full meshes every zoom.");
   EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();Next();
  }catch(Exception e){Fail(e);}}
  void Next(){stage++;if(stage==labels.Length){File.WriteAllLines(ForestLightingEvaluation.Out+"measurements.txt",results);policy.SetDiagnosticPolicy(true,true);rig.SetZoomLevel(3);lighting.SetProfile(true);Debug.Log("FOLIAGE LIGHTING COMPARISON COMPLETE");enabled=false;return;}
   int level=new[]{1,3,4,5}[stage/2];bool after=stage%2==1;
   policy.SetDiagnosticPolicy(true,true);rig.SetZoomLevel(level);lighting.SetProfile(after);
   bool far=level>=4;foreach(var mr in renderers){var block=new MaterialPropertyBlock();mr.GetPropertyBlock(block);if((mr.shadowCastingMode==ShadowCastingMode.Off)!=far)throw new Exception("Incorrect tree-only shadow policy");if((block.GetFloat("_DistanceCoverage")>.5)!=far)throw new Exception("Incorrect coverage policy");}
   start=Time.realtimeSinceStartup;frames=0;elapsedMs=0;draws=tris=0;
  }
  void Update(){try{float elapsed=Time.realtimeSinceStartup-start;if(elapsed<2)return;frames++;elapsedMs+=Time.unscaledDeltaTime*1000;draws+=UnityStats.drawCalls;tris+=UnityStats.triangles;if(elapsed<7)return;
   results.Add(labels[stage]+": zoom="+rig.ZoomLevel+", orthoHalfHeight="+cam.orthographicSize+", FPS="+(1000*frames/elapsedMs).ToString("F2")+", ms="+(elapsedMs/frames).ToString("F2")+", draws="+(draws/(double)frames).ToString("F1")+", renderedTriangles="+(tris/(double)frames).ToString("F0")+", gameView="+Screen.width+"x"+Screen.height);
   Capture(labels[stage],1920,1080);if(stage==3 || stage==5 || stage==7)Pan();File.WriteAllLines(ForestLightingEvaluation.Out+"measurements.txt",results);Next();
  }catch(Exception e){Fail(e);}}
  void Capture(string label,int w,int h){var rt=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32);rt.Create();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});var old=RenderTexture.active;RenderTexture.active=rt;var im=new Texture2D(w,h,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,w,h),0,0);im.Apply();File.WriteAllBytes(ForestLightingEvaluation.Out+label+".png",im.EncodeToPNG());RenderTexture.active=old;DestroyImmediate(im);rt.Release();DestroyImmediate(rt);}
  void Pan(){var field=typeof(CityForgeCameraController).GetField("focus",BindingFlags.Instance|BindingFlags.NonPublic);for(int f=0;f<12;f++){field.SetValue(rig,focus+Vector3.right*(f*.15f));rig.ApplyPose();Capture(labels[stage]+"_Pan"+f.ToString("D2"),960,540);}field.SetValue(rig,focus);rig.ApplyPose();}
  void Fail(Exception e){Directory.CreateDirectory(ForestLightingEvaluation.Out);File.WriteAllText(ForestLightingEvaluation.Out+"error.txt",e.ToString());Debug.LogException(e);enabled=false;}
 }
}
