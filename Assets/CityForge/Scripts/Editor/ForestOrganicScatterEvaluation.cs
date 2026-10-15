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
 [InitializeOnLoad] public static class ForestOrganicScatterEvaluation
 {
  public const string Out="/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs/UnityOrganicScatter/";
  public const string Prior="/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs/UnityFrozenEvaluation/";
  static ForestOrganicScatterEvaluation(){EditorApplication.playModeStateChanged+=State;EditorApplication.update+=Poll;}
  static void Poll(){var path=Path.Combine(Application.dataPath,"../Library/ForestOrganicScatter.request");if(!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists(path)){File.Delete(path);EditorApplication.delayCall+=Run;}}
  [MenuItem("CityForge/Frozen Broadleaf Evaluation/Preview Organic320 Scatter")]
  public static void Run()
  {
   if(EditorApplication.isPlaying){SessionState.SetBool("RestartOrganicScatterEvaluation",true);EditorApplication.isPlaying=false;return;}
   Directory.CreateDirectory(Out);
   var shader=Shader.Find("CityForge/Evaluation/Frozen Blender Leaf");if(shader==null || ShaderUtil.ShaderHasError(shader))throw new Exception("Distance coverage shader must compile.");
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   SessionState.SetBool("OrganicScatterEvaluationRunning",true);EditorApplication.isPlaying=true;
  }
  static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool("RestartOrganicScatterEvaluation",false)){SessionState.SetBool("RestartOrganicScatterEvaluation",false);EditorApplication.delayCall+=Run;}if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("OrganicScatterEvaluationRunning",false)){SessionState.SetBool("OrganicScatterEvaluationRunning",false);new GameObject("Foliage lighting comparison").AddComponent<ForestOrganicScatterEvaluationRunner>();}}
 }
 public sealed class ForestOrganicScatterEvaluationRunner:MonoBehaviour
 {
  [Serializable] class Placement{public int index;public float x,y,z,yaw,scale;}
  [Serializable] class PlacementList{public Placement[] trees;}
  Camera cam;CityForgeCameraController rig;ForestDistancePresentation policy;Renderer[] renderers;Vector3 focus;FrozenCrownLighting lighting;
  readonly string[] labels={"Organic320_Zoom1","Organic320_Zoom3","Organic320_Zoom5"};
  int stage=-1,frames;float start;double elapsedMs;long draws,tris;List<string> results=new();
  void Start(){try{
   PrototypeRegionView.Open("Distance Coverage Evaluation",null,730421,RegionSize.Medium);var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();view.EnterDistrict(3);rig=view.GetComponentInChildren<CityForgeCameraController>();cam=rig.GetComponent<Camera>();
   view.transform.Find("RandomTrees").gameObject.SetActive(false);view.GetComponent<DistantTreeRenderer>().enabled=false;foreach(var c in view.GetComponentsInChildren<Canvas>())c.gameObject.SetActive(false);
   var data=Scatter(view.GetComponentInChildren<Terrain>());File.WriteAllText(ForestOrganicScatterEvaluation.Out+"placements.json",JsonUtility.ToJson(data,true));
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(FrozenBroadleafEvaluation.Art+"FrozenBroadleaf.prefab");var stand=new GameObject("Organic320 unchanged tree test");stand.transform.SetParent(view.transform);
   foreach(var p in data.trees){var t=Instantiate(prefab,stand.transform);t.name="FrozenTree_"+p.index;t.transform.position=new Vector3(p.x,p.y,p.z);t.transform.rotation=Quaternion.Euler(0,p.yaw,0);t.transform.localScale=Vector3.one*p.scale;}
   renderers=stand.GetComponentsInChildren<Renderer>();foreach(var r in renderers)foreach(var m in r.sharedMaterials)if(m.shader.name=="Universal Render Pipeline/Lit")m.EnableKeyword("_EMISSION");policy=stand.AddComponent<ForestDistancePresentation>();policy.Initialize(rig,renderers);lighting=stand.AddComponent<FrozenCrownLighting>();lighting.Initialize(AssetDatabase.LoadAssetAtPath<TextAsset>(FrozenBroadleafEvaluation.Art+"Frozen_CrownLightingGuides.bytes"));
   focus=new Vector3(632.70f,11.44f+7,160.65f);
   // Recover exact camera focus used in the original test from the seed-generated original tree.
   var original=view.transform.Find("RandomTrees").Cast<Transform>().OrderBy(t=>(new Vector2(t.position.x-632.70f,t.position.z-160.65f)).sqrMagnitude).First();focus=original.position+Vector3.up*7;focus.y=view.GetComponentInChildren<Terrain>().SampleHeight(original.position)+7;
   typeof(CityForgeCameraController).GetField("focus",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(rig,focus);
   var count=prefab.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3);if(count!=111768)throw new Exception("Frozen geometry count changed");results.Add("320 instances;111768 triangles/tree; organic clustered scatter; yaw0–360 and scale.90–1.10; gameplay pitch35.264; unchanged source palette and region lighting; foliage shading comparison; full meshes every zoom.");
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
  void Next(){stage++;if(stage==labels.Length){File.WriteAllLines(ForestOrganicScatterEvaluation.Out+"measurements.txt",results);policy.SetDiagnosticPolicy(true,true);rig.SetZoomLevel(3);lighting.SetProfile(true);foreach(var r in renderers){if(r.sharedMaterials.Any(m=>m.shader.name=="Universal Render Pipeline/Lit")){var b=new MaterialPropertyBlock();r.GetPropertyBlock(b);b.SetColor("_BaseColor",new Color(.26f,.235f,.205f,1).gamma);b.SetColor("_EmissionColor",new Color(.10f,.092f,.082f,1));r.SetPropertyBlock(b);}}Debug.Log("ORGANIC320 SCATTER COMPLETE");enabled=false;return;}
   int level=new[]{1,3,5}[stage];bool after=true;
   policy.SetDiagnosticPolicy(true,true);rig.SetZoomLevel(level);lighting.SetProfile(true);
   foreach(var renderer in renderers){var b=new MaterialPropertyBlock();renderer.GetPropertyBlock(b);b.SetFloat("_FarCanopyPresentation",level>=4?1:0);if(renderer.sharedMaterials.Any(m=>m.shader.name=="Universal Render Pipeline/Lit"))b.SetColor("_BaseColor",after?new Color(.26f,.235f,.205f,1).gamma:new Color(.115f,.091f,.067f,1).gamma);if(renderer.sharedMaterials.Any(m=>m.shader.name=="Universal Render Pipeline/Lit"))b.SetColor("_EmissionColor",after?new Color(.10f,.092f,.082f,1):Color.black);renderer.SetPropertyBlock(b);}
   bool far=level>=4;foreach(var mr in renderers){var block=new MaterialPropertyBlock();mr.GetPropertyBlock(block);if((mr.shadowCastingMode==ShadowCastingMode.Off)!=far)throw new Exception("Incorrect tree-only shadow policy");if((block.GetFloat("_DistanceCoverage")>.5)!=far)throw new Exception("Incorrect coverage policy");}
   start=Time.realtimeSinceStartup;frames=0;elapsedMs=0;draws=tris=0;
  }
  void Update(){try{float elapsed=Time.realtimeSinceStartup-start;if(elapsed<2)return;frames++;elapsedMs+=Time.unscaledDeltaTime*1000;draws+=UnityStats.drawCalls;tris+=UnityStats.triangles;if(elapsed<7)return;
   results.Add(labels[stage]+": zoom="+rig.ZoomLevel+", orthoHalfHeight="+cam.orthographicSize+", FPS="+(1000*frames/elapsedMs).ToString("F2")+", ms="+(elapsedMs/frames).ToString("F2")+", draws="+(draws/(double)frames).ToString("F1")+", renderedTriangles="+(tris/(double)frames).ToString("F0")+", gameView="+Screen.width+"x"+Screen.height);
   Capture(labels[stage],1920,1080);if(stage==1 || stage==2)Pan();File.WriteAllLines(ForestOrganicScatterEvaluation.Out+"measurements.txt",results);Next();
  }catch(Exception e){Fail(e);}}
  void Capture(string label,int w,int h){var rt=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32);rt.Create();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});var old=RenderTexture.active;RenderTexture.active=rt;var im=new Texture2D(w,h,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,w,h),0,0);im.Apply();File.WriteAllBytes(ForestOrganicScatterEvaluation.Out+label+".png",im.EncodeToPNG());RenderTexture.active=old;DestroyImmediate(im);rt.Release();DestroyImmediate(rt);}
  void Pan(){var field=typeof(CityForgeCameraController).GetField("focus",BindingFlags.Instance|BindingFlags.NonPublic);for(int f=0;f<12;f++){field.SetValue(rig,focus+Vector3.right*(f*.15f));rig.ApplyPose();Capture(labels[stage]+"_Pan"+f.ToString("D2"),960,540);}field.SetValue(rig,focus);rig.ApplyPose();}
  void Fail(Exception e){Directory.CreateDirectory(ForestOrganicScatterEvaluation.Out);File.WriteAllText(ForestOrganicScatterEvaluation.Out+"error.txt",e.ToString());Debug.LogException(e);enabled=false;}
 }
}
