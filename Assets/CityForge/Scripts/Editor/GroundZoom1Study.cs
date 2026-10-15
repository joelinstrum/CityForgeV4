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
namespace CityForge.Editor {
[InitializeOnLoad] public static class GroundZoom1Study {
 public const string Art="Assets/CityForge/Art/Environment/GroundZoom1Study/";
 public const string Output="/Users/joelinstrum/Documents/Codex/2026-10-08/tre/outputs/GroundZoom1/";
 public static readonly string[] Keys={"Forest1","Forest2","Forest3","Forest4","Mud"};
 static GroundZoom1Study(){EditorApplication.playModeStateChanged+=State;}
 [MenuItem("CityForge/Ground Study/Preview Purchased Materials Zoom 1")]
 public static void Run(){
  if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;EditorApplication.delayCall+=Run;return;}
  AssetDatabase.Refresh();
  foreach(var key in Keys){
   foreach(var kind in new[]{"Albedo","Normal","AO","Roughness","MetalSmooth"}){
    var path=Art+key+"_"+kind+".png";var ti=AssetImporter.GetAtPath(path) as TextureImporter;if(ti==null)continue;
    ti.textureType=kind=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;
    ti.sRGBTexture=kind=="Albedo";ti.mipmapEnabled=true;ti.wrapMode=TextureWrapMode.Repeat;ti.anisoLevel=8;ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.CompressedHQ;ti.SaveAndReimport();
   }
   var mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=key+" purchased ground",enableInstancing=true};
   mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+key+"_Albedo.png"));mat.SetColor("_BaseColor",Color.white);
   mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+key+"_Normal.png"));mat.SetFloat("_BumpScale",.6f);mat.EnableKeyword("_NORMALMAP");
   mat.SetTexture("_OcclusionMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+key+"_AO.png"));mat.SetFloat("_OcclusionStrength",.5f);mat.EnableKeyword("_OCCLUSIONMAP");
   mat.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+key+"_MetalSmooth.png"));mat.SetFloat("_Smoothness",1);mat.EnableKeyword("_METALLICSPECGLOSSMAP");
   var pathMat=Art+key+".mat";var old=AssetDatabase.LoadAssetAtPath<Material>(pathMat);if(old){EditorUtility.CopySerialized(mat,old);UnityEngine.Object.DestroyImmediate(mat);EditorUtility.SetDirty(old);}else AssetDatabase.CreateAsset(mat,pathMat);
  }
  AssetDatabase.SaveAssets();SessionState.SetBool("GroundStudyRun",true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;
 }
 static void State(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("GroundStudyRun",false)){SessionState.SetBool("GroundStudyRun",false);new GameObject("Purchased ground Zoom 1 comparison").AddComponent<GroundZoom1Capture>();}}
}
public class GroundZoom1Capture:MonoBehaviour {
 Camera cam;MeshRenderer land;Material baseline;Material[] candidates;PrototypeRegionView view;float next;int stage;int selected=-1;float repeat=4; bool finished;
 void Start(){Directory.CreateDirectory(GroundZoom1Study.Output);PrototypeRegionView.Open("Purchased ground Zoom 1",null,730421,RegionSize.Medium);view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();view.EnterDistrict(3);var rig=view.GetComponentInChildren<CityForgeCameraController>();cam=rig.GetComponent<Camera>();var terrain=view.GetComponentInChildren<Terrain>();
  var trees=view.transform.Find("RandomTrees");if(trees)trees.gameObject.SetActive(false);var distant=view.GetComponent<DistantTreeRenderer>();if(distant)distant.enabled=false;
  foreach(var canvas in view.GetComponentsInChildren<Canvas>())canvas.gameObject.SetActive(false);view.enabled=false;
  var p=new Vector3(632,0,160);p.y=terrain.SampleHeight(p);typeof(CityForgeCameraController).GetField("focus",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(rig,p);rig.SetZoomLevel(1);rig.ApplyPose();
  land=view.transform.Find("RegionLandCover").GetComponent<MeshRenderer>();baseline=land.sharedMaterial;candidates=new Material[5];for(int i=0;i<5;i++)candidates[i]=new Material(AssetDatabase.LoadAssetAtPath<Material>(GroundZoom1Study.Art+GroundZoom1Study.Keys[i]+".mat"));
  EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();next=Time.realtimeSinceStartup+5;
 }
 void Show(int index,float meters){selected=index;repeat=meters;land.sharedMaterial=index<0?baseline:candidates[index];if(index>=0){var scale=Vector2.one*(view.MapSide/meters);candidates[index].SetTextureScale("_BaseMap",scale);}}
 void Update(){if(finished||Time.realtimeSinceStartup<next||RenderPipelineManager.currentPipeline==null)return;
  if(stage==0){Capture("00_CurrentGround");stage++;Show(0,4);}
  else {var i=(stage-1)%5;var meters=stage<=5?4:8;Capture((stage).ToString("D2")+"_"+GroundZoom1Study.Keys[i]+"_"+meters+"m");stage++;if(stage>10){finished=true;Show(0,4);Debug.Log("GROUND ZOOM1 STUDY COMPLETE");}else Show((stage-1)%5,stage<=5?4:8);}
  next=Time.realtimeSinceStartup+1;
 }
 void Capture(string name){int w=1920,h=1080;var rt=new RenderTexture(w,h,24);rt.Create();var old=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var im=new Texture2D(w,h,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,w,h),0,0);im.Apply();File.WriteAllBytes(GroundZoom1Study.Output+name+".png",im.EncodeToPNG());DestroyImmediate(im);}finally{RenderTexture.active=old;rt.Release();DestroyImmediate(rt);}}
 void OnGUI(){if(!finished)return;GUILayout.BeginArea(new Rect(15,15,310,280),GUI.skin.box);GUILayout.Label("Purchased ground — Zoom 1");if(GUILayout.Button("Original ground"))Show(-1,repeat);for(int i=0;i<5;i++)if(GUILayout.Button(GroundZoom1Study.Keys[i]))Show(i,repeat);GUILayout.BeginHorizontal();if(GUILayout.Button("4 m repeat"))Show(selected,4);if(GUILayout.Button("8 m repeat"))Show(selected,8);GUILayout.EndHorizontal();GUILayout.EndArea();}
}
}
