#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using CityForge.Lots;
using CityForge.UI;
using CityForge.World;
namespace CityForge.EditorTools
{
 [InitializeOnLoad] public static class BowfrontRefinementPrepare
 {
  const string Root="Assets/CityForge/Development/ArchitecturalRenderingLab/SubjectAssets/V006/";
  const string Request="Library/BowfrontRefinementPrepare.request";
  const string Id="cf-building-victorian-bowfront-brownstone-001";
  const string Output="/Users/joelinstrum/Documents/Codex/2026-11-27/files-pasted-by-the-user-cityforge/outputs/Architectural-Rendering-Lab/Preparation/";
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  [Serializable] class Recipe {public MaterialRecipe[] materials;public int nearTriangles,mediumTriangles;}
  [Serializable] class MaterialRecipe {public string name,sourceName,albedo,normal,metallicGloss,emission;public float[] color,emissionValue;public float roughnessValue,metallicValue,normalStrength,emissionGain,alpha;public bool transparent,repeat;}
  static int stage,frames,viewIndex;static bool nightPhase;static MonoBehaviour viewport;
  static readonly string[] Views={"Front","Front-Right","Front-Left","Rear-Right","Rear-Left","Roof","Facade-Detail","Cornice-Detail","Window-Detail"};
  static readonly float[] Yaws={0,-35,35,-145,145,145,0,-25,0};
  static readonly float[] Pitches={18,32,32,32,32,65,18,25,18};
  static readonly float[] Zooms={.73f,.88f,.88f,.88f,.88f,.80f,.49f,.23f,.16f};
  static void SetView(int i){var t=viewport.GetType();t.GetField("yaw",Private).SetValue(viewport,Yaws[i]);t.GetField("pitch",Private).SetValue(viewport,Pitches[i]);t.GetField("zoom",Private).SetValue(viewport,Zooms[i]);t.GetField("focus",Private).SetValue(viewport,new Vector3(i==8?-3.1f:0,i==7?12.5f:i==8?6.8f:7,i>=6?-6.6f:0));t.GetMethod("ApplyPose",Private).Invoke(viewport,null);}
  static bool CaptureView(string name){var camera=(Camera)viewport.GetType().GetField("cameraView",Private).GetValue(viewport);if(!camera||!camera.targetTexture)return false;var rt=camera.targetTexture;var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBAFloat,false,true);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();var pixels=image.GetPixels();for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;var display=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);display.SetPixels(pixels);display.Apply();File.WriteAllBytes(Output+name+".png",display.EncodeToPNG());UnityEngine.Object.DestroyImmediate(display);UnityEngine.Object.DestroyImmediate(image);RenderTexture.active=previous;return true;}
  static BowfrontRefinementPrepare(){EditorApplication.update+=Tick;}
  [MenuItem("CityForge/Development/Prepare refined brownstone")]
  public static void Open(){File.WriteAllText(Request,"open");}
  static void Tick()
  {
   if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
   try
   {
    if(File.Exists(Request))
    {
     string command=File.ReadAllText(Request).Trim();
     if(command=="capture")
     {
      File.Delete(Request);viewport=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(o=>o.GetType().FullName=="CityForge.UI.LotEditorViewport");EditorApplication.isPaused=false;SetTime(RegionTimeOfDay.Noon);viewIndex=0;nightPhase=false;SetView(0);stage=1;frames=0;return;
     }
     if(command=="prepare")
     {
      if(EditorApplication.isPlaying)throw new Exception("Preparation requires outside Play; review session was not interrupted.");
      File.Delete(Request);Prepare();return;
     }
     if(command=="open")
     {
      if(!EditorApplication.isPlaying){UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/CityForge/Scenes/Gameplay/MainMenu.unity");EditorApplication.isPaused=false;EditorApplication.isPlaying=true;return;}
      if(UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Any(o=>o.GetType().FullName=="CityForge.UI.LotEditorPanel"))throw new Exception("An editor is already open; its unsaved lot was preserved.");
      var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();if(!menu)return;
      File.Delete(Request);UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/CityForge/Scenes/Gameplay/Startup.unity");InjectSessionEntry();menu.Handle(MenuAction.LotEditor);
      var panel=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(o=>o.GetType().FullName=="CityForge.UI.LotEditorPanel");
      var lot=new LotDefinition{name="Victorian Bow-front — window and cornice review",width=2,length=5,category="residential",subcategory="townhome",firstEraId="modern",firstEraYear=1921};
      lot.objects.Add(new LotObjectDefinition{assetId=Id,yaw=180});
      panel.GetType().GetMethod("OpenLot",Private).Invoke(panel,new object[]{lot,false});
      viewport=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(o=>o.GetType().FullName=="CityForge.UI.LotEditorViewport");
      SetTime(RegionTimeOfDay.Noon);viewIndex=0;nightPhase=false;SetView(0);stage=1;frames=0;return;
     }
    }
    if(stage>0&&++frames>50)
    {
     frames=0;
     if(!CaptureView(Views[viewIndex]+(nightPhase?"-Night":"-Day")))return;
     viewIndex++;
     if(viewIndex<Views.Length){SetView(viewIndex);}
     else if(!nightPhase){nightPhase=true;viewIndex=0;SetTime(RegionTimeOfDay.Night);SetView(0);}
     else{SetTime(RegionTimeOfDay.Noon);SetView(1);stage=0;ScreenCapture.CaptureScreenshot(Output+"Live-Ready.png");File.WriteAllText(Output+"Live-Complete.txt","Actual unsaved refined 2x5 Lot Editor review open. Matched native preview camera day/night captures completed; session-only catalog entry, no saved lots or approved roster changed.");}
    }
   }
   catch(Exception e){stage=0;if(File.Exists(Request))File.Delete(Request);Directory.CreateDirectory(Output);File.WriteAllText(Output+"Error.txt",e.ToString());Debug.LogException(e);}
  }
  static void SetTime(RegionTimeOfDay time)
  {
   var t=viewport.GetType();for(int n=0;n<12&&(RegionTimeOfDay)t.GetProperty("PreviewTime").GetValue(viewport)!=time;n++)t.GetMethod("CyclePreviewTime").Invoke(viewport,null);
  }
  static void InjectSessionEntry()
  {
   var catalog=LotAssetCatalog.Load();
   var entry=new LotAssetCatalogEntry{id=Id,label="Victorian Bow-front · Window and Cornice Review",description="Private 2x5 window and cornice review: supplied brownstone, closed bay joins, varied windows, side openings and aged roof.",category="Buildings",group="residential",subcategory="townhome",eraId="modern",fromYear=1921,toYear=1945,prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Near.prefab"),distantPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Medium.prefab"),defaultBuildingScale=1,fitModel=false,footprint=new Vector2(8,20),buildingDimensionsMeters=new Vector3(8,14.26f,20),preserveEmissionColors=true,playerPlaceable=true,growableZone=TownZone.None};
   if(!entry.prefab||!entry.distantPrefab)throw new Exception("Prepare the native review first.");
   catalog.entries=catalog.entries.Where(e=>e.id!=Id).Append(entry).ToArray();
  }
  static Texture2D Texture(string file){return string.IsNullOrEmpty(file)?null:AssetDatabase.LoadAssetAtPath<Texture2D>(Root+file);}
  static Color ColorOf(float[] v)=>v==null||v.Length<3?Color.white:new Color(v[0],v[1],v[2],v.Length>3?v[3]:1);
  static Material Save(Material m,string path)
  {
   var old=AssetDatabase.LoadAssetAtPath<Material>(path);if(old){EditorUtility.CopySerialized(m,old);UnityEngine.Object.DestroyImmediate(m);EditorUtility.SetDirty(old);return old;}AssetDatabase.CreateAsset(m,path);return m;
  }
  static void Prepare()
  {
   Directory.CreateDirectory(Output);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   var recipe=JsonUtility.FromJson<Recipe>(File.ReadAllText(Root+"Materials.json"));var roles=new Dictionary<string,bool>();
   foreach(var r in recipe.materials)
   {
    if(!string.IsNullOrEmpty(r.albedo))roles[r.albedo]=true;
    if(!string.IsNullOrEmpty(r.emission))roles[r.emission]=true;
    foreach(var file in new[]{r.normal,r.metallicGloss})if(!string.IsNullOrEmpty(file))roles[file]=false;
   }
   foreach(var pair in roles)
   {
    var importer=(TextureImporter)AssetImporter.GetAtPath(Root+pair.Key);bool normal=recipe.materials.Any(r=>r.normal==pair.Key);
    bool changed=importer.npotScale!=TextureImporterNPOTScale.None||importer.sRGBTexture!=pair.Value||importer.textureType!=(normal?TextureImporterType.NormalMap:TextureImporterType.Default);
    importer.npotScale=TextureImporterNPOTScale.None;importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=pair.Value;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.CompressedHQ;if(changed)importer.SaveAndReimport();
   }
   var mats=new Dictionary<string,Material>();
   foreach(var r in recipe.materials)
   {
    var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=r.name,enableInstancing=true};
    var c=ColorOf(r.color).gamma;c.a=r.transparent?r.alpha:1;m.SetColor("_BaseColor",c);
    if(Texture(r.albedo))m.SetTexture("_BaseMap",Texture(r.albedo));
    m.SetFloat("_Metallic",r.metallicValue);m.SetFloat("_Smoothness",1-r.roughnessValue);
    if(Texture(r.metallicGloss)){m.SetTexture("_MetallicGlossMap",Texture(r.metallicGloss));m.SetFloat("_Smoothness",1);m.EnableKeyword("_METALLICSPECGLOSSMAP");}
    if(Texture(r.normal)){m.SetTexture("_BumpMap",Texture(r.normal));m.SetFloat("_BumpScale",r.normalStrength);m.EnableKeyword("_NORMALMAP");}
    if(r.emissionGain>0)
    {
     var emission=ColorOf(r.emissionValue)*r.emissionGain;m.SetColor("_EmissionColor",emission);
     if(Texture(r.emission))m.SetTexture("_EmissionMap",Texture(r.emission));
     m.EnableKeyword("_EMISSION");m.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
    }
    if(r.transparent)
    {
     m.SetFloat("_Surface",1);m.SetFloat("_Blend",0);m.SetFloat("_BlendModePreserveSpecular",0);m.SetFloat("_Smoothness",.18f);m.SetFloat("_SpecularHighlights",0);m.SetFloat("_EnvironmentReflections",0);m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");m.DisableKeyword("_ALPHAPREMULTIPLY_ON");m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;m.SetShaderPassEnabled("ShadowCaster",false);m.SetFloat("_Cull",0);
    }
    mats[r.name]=Save(m,Root+r.name+".mat");
   }
   var log=new List<string>();
   foreach(string tier in new[]{"Near","Medium"})
   {
    string file=Root+(tier=="Near"?"Model.fbx":"Distant.fbx");var mi=(ModelImporter)AssetImporter.GetAtPath(file);mi.globalScale=1;mi.useFileScale=true;mi.importLights=false;mi.importCameras=false;mi.importAnimation=false;mi.importNormals=ModelImporterNormals.Import;mi.importTangents=ModelImporterTangents.CalculateMikk;mi.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;mi.SaveAndReimport();
    var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(file));
    try
    {
     foreach(var renderer in instance.GetComponentsInChildren<Renderer>())
     {
      renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>m&&mats.TryGetValue(m.name,out var material)?material:throw new Exception("Unknown material "+(m?m.name:"null"))).ToArray();
     }
     var clock=instance.AddComponent<AuthoredBuildingLighting>();clock.preserveAuthoredEmissionStrength=true;var pools=new List<Light>();
     foreach(float x in new[]{-1.188f,1.188f})
     {
      var o=new GameObject("Entrance lantern pool");o.transform.SetParent(instance.transform,false);o.transform.localPosition=new Vector3(2.58f+x*.89f,3.65f*.89f,6.85f+.35f*.89f);var light=o.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.66f,.32f);light.intensity=2.4f;light.range=4;light.shadows=LightShadows.None;light.enabled=false;pools.Add(light);
     }
     clock.fixtureLights=pools.ToArray();clock.SetTime(RegionTimeOfDay.Noon);
     var renderers=instance.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
     int tris=instance.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3);
     if(bounds.min.x < -4.01f||bounds.max.x>4.01f||bounds.min.z < -10.01f||bounds.max.z>10.01f||Mathf.Abs(bounds.size.y-14.26f)>.1f)throw new Exception("Invalid native bounds "+bounds);
     PrefabUtility.SaveAsPrefabAsset(instance,Root+tier+".prefab");
     foreach(var time in new[]{RegionTimeOfDay.Night,RegionTimeOfDay.Evening,RegionTimeOfDay.Noon})
     {
      clock.SetTime(time);if(pools.Any(l=>l.enabled)!=(time!=RegionTimeOfDay.Noon))throw new Exception("Fixture clock failed");
      foreach(var renderer in renderers)for(int slot=0;slot<renderer.sharedMaterials.Length;slot++)
      {if(!renderer.sharedMaterials[slot].IsKeywordEnabled("_EMISSION"))continue;var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,slot);if((block.GetColor("_EmissionColor").maxColorComponent>0)!=(time!=RegionTimeOfDay.Noon))throw new Exception("Emission clock failed");}
     }
     log.Add(tier+": "+tris+" triangles, "+renderers.Length+" renderers; bounds "+bounds+"; clear glazing with restrained reflection; all native textures/normal/PBR bindings; emission and both lantern pools switch Night/Evening/Noon.");
    }
    finally{UnityEngine.Object.DestroyImmediate(instance);}
   }
   foreach(var mat in mats.Values)AssetDatabase.SaveAssetIfDirty(mat);File.WriteAllLines(Output+"Preparation-Checks.txt",log);Debug.Log("BOWFRONT NATIVE REVIEW PREPARED");
  }
 }
}
#endif
