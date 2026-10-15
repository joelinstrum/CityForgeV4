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
 [InitializeOnLoad] public static class FreshAmericanElmPreview
 {
  public const string Art="Assets/CityForge/Art/Trees/AmericanElmFresh/";
  public const string Output="/Users/joelinstrum/Documents/Codex/2026-10-06/continue-the-cityforge-forest-asset-work/outputs/AmericanElmFresh/";
  static FreshAmericanElmPreview(){EditorApplication.playModeStateChanged+=OnState;}
  static Material SaveMaterial(Material m,string path){var prior=AssetDatabase.LoadAssetAtPath<Material>(path);if(prior){EditorUtility.CopySerialized(m,prior);UnityEngine.Object.DestroyImmediate(m);EditorUtility.SetDirty(prior);return prior;}AssetDatabase.CreateAsset(m,path);return m;}
  public static GameObject Import(){
   AssetDatabase.Refresh();
   foreach(var name in new[]{"ElmLeaf_AlbedoOpacity.png","ElmLeaf_Normal.jpg","ElmLeaf_MetalSmooth.png","ElmBark_Albedo.jpg","ElmBark_Normal.jpg","ElmBark_MetalSmooth.png"}){
    var ti=(TextureImporter)AssetImporter.GetAtPath(Art+name);ti.textureType=name.Contains("Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=name.Contains("Albedo");ti.alphaIsTransparency=name.Contains("Opacity");ti.mipmapEnabled=true;ti.anisoLevel=8;ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.wrapMode=name.Contains("Leaf")?TextureWrapMode.Clamp:TextureWrapMode.Repeat;ti.SaveAndReimport();
   }
   var mi=(ModelImporter)AssetImporter.GetAtPath(Art+"AmericanElm.fbx");mi.materialImportMode=ModelImporterMaterialImportMode.None;mi.importCameras=false;mi.importLights=false;mi.importAnimation=false;mi.SaveAndReimport();
   Material Make(bool leaf){var label=leaf?"ElmLeaf":"ElmBark";var m=new Material(Shader.Find(leaf?"Universal Render Pipeline/Nature/SpeedTree8":"Universal Render Pipeline/Lit"));m.name=leaf?"Elm Summer Leaves":"Elm Gray Brown Bark";
    var color=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+label+(leaf?"_AlbedoOpacity.png":"_Albedo.jpg"));m.SetTexture(leaf?"_MainTex":"_BaseMap",color);m.SetColor(leaf?"_Color":"_BaseColor",leaf?new Color(.48f,.62f,.38f,1):new Color(.92f,.88f,.82f,1));m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+label+"_Normal.jpg"));
    if(leaf){m.SetFloat("_NormalMapKwToggle",1);m.EnableKeyword("EFFECT_BUMP");m.SetFloat("_SubsurfaceKwToggle",1);m.EnableKeyword("EFFECT_SUBSURFACE");m.SetTexture("_SubsurfaceTex",color);m.SetColor("_SubsurfaceColor",new Color(.10f,.16f,.06f));m.SetFloat("_SubsurfaceIndirect",.25f);m.SetFloat("_TwoSided",0);m.SetFloat("_WindQuality",0);m.EnableKeyword("_WINDQUALITY_NONE");m.SetFloat("_Glossiness",.08f);m.renderQueue=(int)RenderQueue.AlphaTest;}
    else{m.SetFloat("_BumpScale",.65f);m.EnableKeyword("_NORMALMAP");m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Art+label+"_MetalSmooth.png"));m.EnableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Smoothness",.5f);m.SetFloat("_Cull",2);}m.enableInstancing=true;return SaveMaterial(m,Art+label+".mat");}
   var bark=Make(false);var foliage=Make(true);var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Art+"AmericanElm.fbx"));root.name="American Elm - Easy Tree - Zoom 1";
   try{foreach(var r in root.GetComponentsInChildren<MeshRenderer>()){r.sharedMaterial=r.name.Contains("Foliage")?foliage:bark;r.shadowCastingMode=ShadowCastingMode.TwoSided;r.receiveShadows=true;}var p=PrefabUtility.SaveAsPrefabAsset(root,Art+"AmericanElm.prefab");AssetDatabase.SaveAssets();return p;}finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  [MenuItem("CityForge/American Elm/Preview Fresh Easy Tree Zoom 1")]
  public static void Run(){if(EditorApplication.isPlaying){SessionState.SetBool("FreshElmRestart",true);EditorApplication.isPlaying=false;return;}Import();SessionState.SetBool("FreshElmPreview",true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;}
  static void OnState(PlayModeStateChange s){if(s==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool("FreshElmRestart",false)){SessionState.SetBool("FreshElmRestart",false);EditorApplication.delayCall+=Run;}if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("FreshElmPreview",false)){SessionState.SetBool("FreshElmPreview",false);new GameObject("Fresh American Elm Zoom 1 Test").AddComponent<FreshAmericanElmCameraCapture>();}}
 }
 public sealed class FreshAmericanElmCameraCapture:MonoBehaviour
 {
  Camera camera;float captureAt;bool captured;
  void Start(){Directory.CreateDirectory(FreshAmericanElmPreview.Output);PrototypeRegionView.Open("Fresh American Elm",null,730421,RegionSize.Medium);var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();view.EnterDistrict(3);var rig=view.GetComponentInChildren<CityForgeCameraController>();camera=rig.GetComponent<Camera>();var terrain=view.GetComponentInChildren<Terrain>();view.transform.Find("RandomTrees").gameObject.SetActive(false);view.GetComponent<DistantTreeRenderer>().enabled=false;foreach(var c in view.GetComponentsInChildren<Canvas>())c.gameObject.SetActive(false);view.enabled=false;
   var tree=Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(FreshAmericanElmPreview.Art+"AmericanElm.prefab"));var p=new Vector3(632,0,160);p.y=terrain.SampleHeight(p);tree.transform.position=p;foreach(var r in tree.GetComponentsInChildren<MeshRenderer>()){r.lightProbeUsage=LightProbeUsage.CustomProvided;var ambient=new MaterialPropertyBlock();var skyFill=new SphericalHarmonicsL2();skyFill.AddAmbientLight(new Color(.32f,.35f,.39f));skyFill.AddDirectionalLight(Vector3.up,new Color(.30f,.38f,.48f),.65f);skyFill.AddDirectionalLight(Vector3.down,new Color(.22f,.19f,.14f),.35f);ambient.CopySHCoefficientArraysFrom(new[]{skyFill});r.SetPropertyBlock(ambient);Debug.Log("Fresh ambient SH0="+RenderSettings.ambientProbe[0,0]);Debug.Log("FreshElm renderer "+r.name+" bounds="+r.bounds+" scale="+r.transform.lossyScale+" material="+r.sharedMaterial.name);}typeof(CityForgeCameraController).GetField("focus",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(rig,p+Vector3.up*7);rig.SetZoomLevel(1);rig.ApplyPose();EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();captureAt=Time.realtimeSinceStartup+4;
  }
  void Update(){if(captured||Time.realtimeSinceStartup<captureAt||RenderPipelineManager.currentPipeline==null)return;Capture("Unity_Zoom1",1920,1080);Capture("Unity_Zoom1_4K",3840,2160);captured=true;File.WriteAllText(FreshAmericanElmPreview.Output+"camera.json",JsonUtility.ToJson(new CameraRecord{position=camera.transform.position,euler=camera.transform.eulerAngles,orthographicSize=camera.orthographicSize},true));Debug.Log("FRESH AMERICAN ELM ZOOM1 CAPTURE COMPLETE");}
  [Serializable] class CameraRecord{public Vector3 position,euler;public float orthographicSize;}
  void Capture(string name,int width,int height){var rt=new RenderTexture(width,height,24);rt.Create();var old=RenderTexture.active;try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(FreshAmericanElmPreview.Output+name+".png",image.EncodeToPNG());DestroyImmediate(image);}finally{RenderTexture.active=old;rt.Release();DestroyImmediate(rt);}}
 }
}
