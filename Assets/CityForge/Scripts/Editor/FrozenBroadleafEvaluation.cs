using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using CityForge.World;
using CityForge.CameraSystem;

namespace CityForge.Editor {
 [InitializeOnLoad] public static class FrozenBroadleafEvaluation {
  public const string Art="Assets/CityForge/Art/Trees/FrozenBroadleafEvaluation/";
  public const string Out="/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs/UnityFrozenEvaluation/";
  static FrozenBroadleafEvaluation(){EditorApplication.playModeStateChanged+=State; EditorApplication.update+=Poll;}
  static void Poll(){if(!EditorApplication.isPlaying && !EditorApplication.isCompiling && !EditorApplication.isUpdating && File.Exists("Library/FrozenEvaluation.request")){File.Delete("Library/FrozenEvaluation.request");EditorApplication.delayCall+=Run;}}
  [MenuItem("CityForge/Frozen Broadleaf Evaluation/Run Frozen Tests")]
  public static void Run(){
   Directory.CreateDirectory(Out); AssetDatabase.Refresh();
   var mi=(ModelImporter)AssetImporter.GetAtPath(Art+"FrozenBroadleaf.fbx");mi.importCameras=false;mi.importLights=false;mi.importAnimation=false;mi.isReadable=true;mi.meshCompression=ModelImporterMeshCompression.Off;mi.importNormals=ModelImporterNormals.Import;mi.importTangents=ModelImporterTangents.CalculateMikk;mi.SaveAndReimport();
   var ti=(TextureImporter)AssetImporter.GetAtPath(Art+"Frozen_Leaf.png");ti.alphaIsTransparency=true;ti.mipmapEnabled=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.maxTextureSize=2048;ti.SaveAndReimport();
   var ni=(TextureImporter)AssetImporter.GetAtPath(Art+"Frozen_Bark_Normal.png");ni.textureType=TextureImporterType.NormalMap;ni.textureCompression=TextureImporterCompression.Uncompressed;ni.SaveAndReimport();
   var shader=Shader.Find("CityForge/Evaluation/Frozen Blender Leaf");if(shader==null || ShaderUtil.ShaderHasError(shader))throw new Exception("Frozen evaluation leaf shader compile failure");
   var colors=new[]{new Color(.36f,.46f,.30f,1),new Color(.28f,.37f,.245f,1),new Color(.19f,.27f,.18f,1)};
   var mats=new Material[3];for(int i=0;i<3;i++){var m=new Material(shader);m.name=new[]{"Summer outer foliage","Summer middle foliage","Summer interior foliage"}[i];m.SetVector("_BaseColor",colors[i]);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Frozen_Leaf.png"));m.SetFloat("_Smoothness",.07f);m.enableInstancing=false;AssetDatabase.CreateAsset(m,Art+"FrozenLeaf"+i+".mat");mats[i]=m;}
   var bark=new Material(Shader.Find("Universal Render Pipeline/Lit"));bark.name="Irregular gray brown bark";bark.SetColor("_BaseColor",new Color(.26f,.235f,.205f,1).gamma);bark.SetFloat("_Smoothness",.05f);bark.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"Frozen_Bark_Normal.png"));bark.SetFloat("_BumpScale",1);bark.EnableKeyword("_NORMALMAP");bark.EnableKeyword("_EMISSION");bark.SetColor("_EmissionColor",new Color(.10f,.092f,.082f,1));bark.enableInstancing=false;AssetDatabase.CreateAsset(bark,Art+"FrozenBark.mat");
   var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"FrozenBroadleaf.fbx"));
   foreach(var mr in root.GetComponentsInChildren<MeshRenderer>()) {var slots=mr.sharedMaterials;for(int i=0;i<slots.Length;i++){var name=slots[i].name.ToLowerInvariant();slots[i]=name.Contains("outer")?mats[0]:name.Contains("middle")?mats[1]:name.Contains("interior")?mats[2]:bark;}mr.sharedMaterials=slots;mr.shadowCastingMode=ShadowCastingMode.TwoSided;}
   root.name="FrozenBroadleaf_ExactRefinement01";PrefabUtility.SaveAsPrefabAsset(root,Art+"FrozenBroadleaf.prefab");UnityEngine.Object.DestroyImmediate(root);AssetDatabase.SaveAssets();
   EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool("FrozenEvaluationRunning",true);EditorApplication.isPlaying=true;
  }
  static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("FrozenEvaluationRunning",false)){SessionState.SetBool("FrozenEvaluationRunning",false);new GameObject("Frozen Evaluation Runner").AddComponent<FrozenEvaluationRunner>();}}
 }
 public class FrozenEvaluationRunner:MonoBehaviour {
  PrototypeRegionView view; CityForgeCameraController rig; Terrain terrain; Camera cam; GameObject stand;GameObject prefab; Vector3 center;int stage=-1;float start;int frames;double ms;long draws,tris,batches;List<string> lines=new();List<object> placements=new();
  readonly string[] labels={"Single_Zoom1","Cluster25_Zoom1","Cluster25_Zoom3","Forest150_Zoom1","Forest150_Zoom3","Forest150_Zoom5","EnvironmentOnly_Zoom3"};
  void Start(){try{
   PrototypeRegionView.Open("Frozen Crown Evaluation",null,730421,RegionSize.Medium);view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();terrain=view.GetComponentInChildren<Terrain>();rig=view.GetComponentInChildren<CityForgeCameraController>();cam=rig.GetComponent<Camera>();view.EnterDistrict(3);
   view.transform.Find("RandomTrees").gameObject.SetActive(false);view.GetComponent<DistantTreeRenderer>().enabled=false;
   foreach(var canvas in view.GetComponentsInChildren<Canvas>())canvas.gameObject.SetActive(false);
   // Existing region terrain and grass remain; evaluate stand at an existing dry NE woodland location.
   var originals=view.transform.Find("RandomTrees").Cast<Transform>().Where(t=>t.position.x>100 && t.position.z>100).ToArray();
   float best=float.MaxValue;foreach(var t in originals){float min=99999,max=-99999;bool valid=true;for(int x=-6;x<=6;x+=2)for(int z=-6;z<=6;z+=2){var p=t.position+new Vector3(x*6.5f,0,z*6.5f);float h=terrain.SampleHeight(p);float u=p.x/view.MapSide+.5f,v=p.z/view.MapSide+.5f;if(h<view.SeaLevel+2 || terrain.terrainData.GetSteepness(u,v)>18){valid=false;break;}min=Mathf.Min(min,h);max=Mathf.Max(max,h);}if(valid && max-min<best){best=max-min;center=t.position;}}
   if(best==float.MaxValue)throw new Exception("No suitable terrain stand location");center.y=terrain.SampleHeight(center);
   prefab=AssetDatabase.LoadAssetAtPath<GameObject>(FrozenBroadleafEvaluation.Art+"FrozenBroadleaf.prefab");
   var meshes=prefab.GetComponentsInChildren<MeshFilter>();var triangleCount=meshes.Sum(m=>m.sharedMesh.triangles.Length/3);lines.Add("Full tree triangles="+triangleCount+"; no LOD or impostor; seed=730421; center="+center+"; terrain height range="+best);if(triangleCount!=111768)throw new Exception("Imported triangle count differs: "+triangleCount);
   var window=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.Show();window.Focus();
   Next();
  }catch(Exception e){Fail(e);}}
  void Next(){stage++;if(stage>=labels.Length){File.WriteAllLines(FrozenBroadleafEvaluation.Out+"measurements.txt",lines);File.WriteAllText(FrozenBroadleafEvaluation.Out+"placements.json",JsonUtility.ToJson(new PlacementList{trees=records.ToArray()},true));Debug.Log("FROZEN EVALUATION COMPLETE");stage=3;Next();enabled=false;return;}
   if(stand!=null)DestroyImmediate(stand);stand=new GameObject("Temporary frozen tree stand");stand.transform.SetParent(view.transform);var random=new System.Random(730421);
   int count=stage==0?1:stage<3?25:stage<6?150:0;int cols=count==25?5:13;float spacing=count==25?11f:6.5f;records.Clear();
   for(int i=0;i<count;i++){int row=i/cols,col=i%cols;float x=count==1?0:(col-(cols-1)*.5f)*spacing+((float)random.NextDouble()-.5f)*spacing*.30f;float z=count==1?0:(row-((count-1)/cols)*.5f)*spacing+((float)random.NextDouble()-.5f)*spacing*.30f;if(count==25){for(int attempt=0;attempt<2000;attempt++){float a=(float)random.NextDouble()*Mathf.PI*2,rad=Mathf.Sqrt((float)random.NextDouble())*28;x=Mathf.Cos(a)*rad;z=Mathf.Sin(a)*rad;if(!records.Any(t=>new Vector2(t.x-center.x-x,t.z-center.z-z).sqrMagnitude<8.5f*8.5f))break;}}var p=center+new Vector3(x,0,z);p.y=terrain.SampleHeight(p)-.08f;float yaw=count==1?0:(float)random.NextDouble()*360;float scale=count==1?1:.90f+.20f*(float)random.NextDouble();var tree=Instantiate(prefab,stand.transform);tree.transform.position=p;tree.transform.rotation=Quaternion.Euler(0,yaw,0);tree.transform.localScale=Vector3.one*scale;tree.name="FrozenTree_"+i;records.Add(new Placement{index=i,x=p.x,y=p.y,z=p.z,yaw=yaw,scale=scale});}
   // Only focus is translated to the stand. Preserve actual district zoom calibration and gameplay pitch/yaw.
   typeof(CityForgeCameraController).GetField("focus",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(rig,center+Vector3.up*7);
   rig.SetZoomLevel(stage==2 || stage==4 || stage==6?3:stage==5?5:1);
   start=Time.realtimeSinceStartup;frames=0;ms=0;draws=tris=batches=0;
  }
  [Serializable]public class Placement{public int index;public float x,y,z,yaw,scale;}
  [Serializable]public class PlacementList{public Placement[] trees;}
  List<Placement> records=new();
  void Update(){try{float elapsed=Time.realtimeSinceStartup-start;if(elapsed<3)return;
   frames++;ms+=Time.unscaledDeltaTime*1000;draws+=UnityStats.drawCalls;tris+=UnityStats.triangles;batches+=UnityStats.batches;
   if(elapsed<9)return;
   lines.Add(labels[stage]+": instances="+stand.transform.childCount+", zoom="+rig.ZoomLevel+", pitch="+cam.transform.eulerAngles.x+", yaw="+cam.transform.eulerAngles.y+", orthoHalfHeight="+cam.orthographicSize+", FPS="+(1000*frames/ms).ToString("F2")+", ms="+(ms/frames).ToString("F2")+", averageDrawCalls="+(draws/(double)frames).ToString("F1")+", batches="+(batches/(double)frames).ToString("F1")+", renderedTriangles="+(tris/(double)frames).ToString("F0")+", resolution="+Screen.width+"x"+Screen.height);
   File.WriteAllLines(FrozenBroadleafEvaluation.Out+"measurements.txt",lines);File.WriteAllText(FrozenBroadleafEvaluation.Out+labels[stage]+"_placements.json",JsonUtility.ToJson(new PlacementList{trees=records.ToArray()},true));
   // Render actual gameplay camera through URP at a consistent capture resolution, no scene/lighting changes.
   var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);rt.Create();var req=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};RenderPipeline.SubmitRenderRequest(cam,req);var old=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(FrozenBroadleafEvaluation.Out+labels[stage]+".png",image.EncodeToPNG());RenderTexture.active=old;DestroyImmediate(image);rt.Release();DestroyImmediate(rt);if(stage==4 || stage==5) CapturePan();Next();
  }catch(Exception e){Fail(e);}}
  void CapturePan(){
   var field=typeof(CityForgeCameraController).GetField("focus",BindingFlags.Instance|BindingFlags.NonPublic);var original=(Vector3)field.GetValue(rig);
   for(int f=0;f<12;f++){field.SetValue(rig,original+Vector3.right*(f*.15f));rig.ApplyPose();var rt=new RenderTexture(960,540,24,RenderTextureFormat.ARGB32);rt.Create();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});var old=RenderTexture.active;RenderTexture.active=rt;var im=new Texture2D(960,540,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,960,540),0,0);im.Apply();File.WriteAllBytes(FrozenBroadleafEvaluation.Out+labels[stage]+"_Pan"+f.ToString("D2")+".png",im.EncodeToPNG());RenderTexture.active=old;DestroyImmediate(im);rt.Release();DestroyImmediate(rt);}field.SetValue(rig,original);rig.ApplyPose();
  }
  void Fail(Exception e){Directory.CreateDirectory(FrozenBroadleafEvaluation.Out);File.WriteAllText(FrozenBroadleafEvaluation.Out+"error.txt",e.ToString());Debug.LogException(e);enabled=false;}
 }
}

// Refresh after initial editor startup.
