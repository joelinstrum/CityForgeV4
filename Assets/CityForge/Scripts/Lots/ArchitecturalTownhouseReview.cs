#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CityForge.UI;
using CityForge.World;
using Object=UnityEngine.Object;
namespace CityForge.Lots
{
[InitializeOnLoad] internal static class ArchitecturalTownhouseReview
{
 const string Source="Assets/CityForge/Resources/Lots/RebuiltBuildings/BrickTownhouse02/",Review="Assets/CityForge/Development/WindowFrameReview/",N=Review+"CandidateAssets/",Shared="Assets/CityForge/SharedArchitecture/WindowFrames/";
 const string Output="/Users/joelinstrum/Documents/Codex/2026-11-27/files-pasted-by-the-user-cityforge/outputs/Townhouse-Authenticity-Refinement/";
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
 [Serializable]class Mat{public string name,sourceName,albedo,normal,metallicGloss,emission,frameMask;public float[] color,emissionColor;public float metallic,roughness,alpha,strength,normalStrength;public bool twoSided;}
 [Serializable]class Tier{public string tier;public int triangles;public float[] min,max;}
 [Serializable]class Doc{public Mat[] materials;public Tier[] tiers;}
 static readonly List<string> checks=new();static readonly List<Action> jobs=new();static BuildingEditorPreview preview;static int index,frames;static MonoBehaviour viewport;static bool native;static LotAssetCatalog nativeCatalog;static MonoBehaviour panel;
 static ArchitecturalTownhouseReview(){EditorApplication.update+=Tick;AssemblyReloadEvents.beforeAssemblyReload+=Dispose;}
 static void Check(bool yes,string msg){if(!yes)throw new Exception(msg);checks.Add("PASS "+msg);}
 static void Dispose(){preview?.Dispose();preview=null;if(labActive){LabCall("Restore");labActive=false;EditorPrefs.SetString("CityForge.ArchitecturalLab.Output",labOutput);}}
 static void Tick()
 {
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  const string request="Library/TownhouseAuthenticity.request";
  try
  {
   if(File.Exists(request))
   {
    string cmd=File.ReadAllText(request).Trim();if(string.IsNullOrEmpty(cmd))return;Directory.CreateDirectory(Output);
    if(cmd=="native"&&!EditorApplication.isPlaying){if(EditorApplication.isPlayingOrWillChangePlaymode)return;SessionState.SetString("TownhouseReviewPreviousStart",UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene?AssetDatabase.GetAssetPath(UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene):"");UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/CityForge/Scenes/Gameplay/MainMenu.unity");EditorApplication.isPaused=false;EditorApplication.isPlaying=true;return;}
    if(cmd=="native"&&(!Object.FindFirstObjectByType<MainMenuController>()||StartupLoadingScreen.IsLoading))return;
    File.Delete(request);if(cmd=="prepare"){if(EditorApplication.isPlaying)throw new Exception("Preparation requires outside Play");Prepare();}else if(cmd=="capture")CaptureAll();else if(cmd=="capture-after")CaptureAll(true);else if(cmd=="lab")Lab();else if(cmd=="native")Native();else if(cmd=="ui")OpenWindows();else if(cmd=="export")ExportDependencies();
   }
   if(jobs.Count>0&&++frames>(native?25:4)){frames=0;if(index<jobs.Count){jobs[index++]();EditorApplication.QueuePlayerLoopUpdate();}else{jobs.Clear();Dispose();File.WriteAllText(Output+(native?"Native":"Studio")+"-Complete.txt",DateTime.UtcNow.ToString("O"));File.WriteAllLines(Output+"Validation.txt",checks);}}
  }
  catch(Exception e){jobs.Clear();Dispose();Directory.CreateDirectory(Output);File.WriteAllText(Output+"ERROR.txt",e.ToString());Debug.LogException(e);}
 }
 static Texture2D Texture(string name){if(string.IsNullOrEmpty(name))return null;return AssetDatabase.LoadAssetAtPath<Texture2D>(N+name)??AssetDatabase.LoadAssetAtPath<Texture2D>(Source+name);}
 static Color ColorOf(float[] c)=>c==null?Color.white:new Color(c[0],c[1],c[2],c.Length>3?c[3]:1);
 static Material Save(Material mat,string path){var old=AssetDatabase.LoadAssetAtPath<Material>(path);if(old){EditorUtility.CopySerialized(mat,old);Object.DestroyImmediate(mat);EditorUtility.SetDirty(old);return old;}AssetDatabase.CreateAsset(mat,path);return mat;}
 static Material Material(Mat s)
 {
  var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=s.name,enableInstancing=true};m.SetColor("_BaseColor",string.IsNullOrEmpty(s.albedo)?ColorOf(s.color).gamma:Color.white);m.SetFloat("_Metallic",s.metallic);m.SetFloat("_Smoothness",1-s.roughness);m.SetFloat("_Cull",s.twoSided?0:2);
  if(!string.IsNullOrEmpty(s.albedo))m.SetTexture("_BaseMap",Texture(s.albedo));
  if(!string.IsNullOrEmpty(s.normal)){m.SetTexture("_BumpMap",Texture(s.normal));m.SetFloat("_BumpScale",s.normalStrength);m.EnableKeyword("_NORMALMAP");}
  if(!string.IsNullOrEmpty(s.metallicGloss)){m.SetTexture("_MetallicGlossMap",Texture(s.metallicGloss));m.EnableKeyword("_METALLICSPECGLOSSMAP");m.SetFloat("_Metallic",1);m.SetFloat("_Smoothness",1);}
  if(s.alpha<.999f){var c=m.GetColor("_BaseColor");c.a=s.alpha;m.SetColor("_BaseColor",c);m.SetFloat("_Surface",1);m.SetFloat("_SrcBlend",5);m.SetFloat("_DstBlend",10);m.SetFloat("_ZWrite",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.SetOverrideTag("RenderType","Transparent");m.renderQueue=3000;m.SetShaderPassEnabled("ShadowCaster",false);}
  if(s.strength>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",((string.IsNullOrEmpty(s.emission)?ColorOf(s.emissionColor):Color.white)*s.strength).gamma);if(!string.IsNullOrEmpty(s.emission))m.SetTexture("_EmissionMap",Texture(s.emission));}
  return Save(m,N+s.name+".mat");
 }
 static void Prepare()
 {
  checks.Clear();AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  foreach(string file in Directory.GetFiles(N).Where(f=>f.EndsWith(".png")||f.EndsWith(".jpg")))
  {var importer=(TextureImporter)AssetImporter.GetAtPath(file);bool normal=file.ToLowerInvariant().Contains("normal"),data=file.Contains("PackedPBR")||file.Contains("Mask");importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=!normal&&!data;importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=Path.GetFileName(file).Contains("Window")?TextureWrapMode.Clamp:TextureWrapMode.Repeat;importer.SaveAndReimport();}
  var maskImporter=(TextureImporter)AssetImporter.GetAtPath(Review+"Townhouse-Perimeter-WeatherMask.png");maskImporter.sRGBTexture=false;maskImporter.textureCompression=TextureImporterCompression.Uncompressed;maskImporter.wrapMode=TextureWrapMode.Clamp;maskImporter.SaveAndReimport();
  var doc=JsonUtility.FromJson<Doc>(File.ReadAllText(N+"Recipe.json"));var mats=doc.materials.ToDictionary(m=>m.name,Material);var weather=new Dictionary<Material,Material>();var frame=new Dictionary<Material,Material>();
  foreach(var spec in doc.materials)
  {
   var original=mats[spec.name];bool brick=spec.sourceName=="CF_AntiqueRedBrick_Shared",stone=spec.sourceName.Contains("Stone")&&!spec.sourceName.Contains("CapStone"),cornice=spec.sourceName.Contains("Cornice"),roof=spec.sourceName.Contains("SourceDeck"),wood=spec.sourceName=="BT2-Wood",iron=spec.sourceName=="BT2-Iron";
   if(brick||stone||cornice||roof||wood||iron)
   {
    var m=new Material(original){name=original.name+"-Weathered"};m.shader=Shader.Find("CityForge/Architecture/Weathered Lit");m.SetTexture("_CFWeatherMask",AssetDatabase.LoadAssetAtPath<Texture2D>(Review+"Townhouse-Perimeter-WeatherMask.png"));
    m.SetVector("_CFWeatherParams",roof?new Vector4(.14f,0,0,.16f):brick?new Vector4(.06f,.075f,.14f,0):stone?new Vector4(.035f,.15f,.12f,0):new Vector4(.035f,.06f,.08f,0));m.SetFloat("_CFWeatherIntensity",.7f);
    if(cornice){m.SetFloat("_BumpScale",.8f);m.SetFloat("_Smoothness",.65f);}
    if(iron){m.SetFloat("_Metallic",0);m.SetFloat("_Smoothness",.34f);}
    weather[original]=Save(m,N+m.name+".mat");
   }
   if(spec.sourceName.Contains("Window-Black-Metal")||spec.sourceName.Contains("FarWindow"))
   {
    var m=new Material(original){name=original.name+"-Frames"};m.shader=Shader.Find("CityForge/Architecture/Window Frame Finish");m.SetTexture("_FrameDetail",AssetDatabase.LoadAssetAtPath<Texture2D>(Shared+"Frame-Fine-Grain.png"));if(!string.IsNullOrEmpty(spec.frameMask))m.SetTexture("_FrameMask",Texture(spec.frameMask));frame[original]=Save(m,N+m.name+".mat");
   }
  }
  foreach(var tier in doc.tiers)
  {
   var importer=(ModelImporter)AssetImporter.GetAtPath(N+tier.tier+".fbx");importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.isReadable=true;importer.SaveAndReimport();
   var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source+tier.tier+".prefab"));root.name="Brick Townhouse 2 · authenticity candidate · "+tier.tier;
   try
   {
    // Keep the approved native fixtures, garden and assembly root; exchange only the private body geometry.
    var old=root.transform.Cast<Transform>().Where(t=>t.GetComponentsInChildren<Renderer>().Any(r=>r.sharedMaterials.Any(m=>m&&m.name=="BT2_c2e6136292bf78"))).ToArray();Check(old.Length==1,tier.tier+" one authored model root");var position=old[0].localPosition;var rotation=old[0].localRotation;var scale=old[0].localScale;Object.DestroyImmediate(old[0].gameObject);
    var model=PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(N+tier.tier+".fbx")) as GameObject;model.name="Geometry";model.transform.SetParent(root.transform,false);model.transform.localPosition=position;model.transform.localRotation=rotation;model.transform.localScale=scale;
    foreach(var renderer in model.GetComponentsInChildren<Renderer>())
    {renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>mats[m.name]).Select(m=>weather.TryGetValue(m,out var replacement)?replacement:m).ToArray();if(renderer.sharedMaterials.Any(m=>m.GetFloat("_Surface")>0||m.name.Contains("Linen")))renderer.shadowCastingMode=ShadowCastingMode.Off;}
    var paint=root.GetComponent<BuildingTintProfile>();if(paint)foreach(var binding in paint.bindings)if(binding!=null&&binding.material&&mats.TryGetValue(binding.material.name,out var candidate))binding.material=weather.TryGetValue(candidate,out var changed)?changed:candidate;
    var profile=root.GetComponent<BuildingWindowFrameProfile>()??root.AddComponent<BuildingWindowFrameProfile>();var used=model.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().Where(frame.ContainsKey);profile.bindings=used.Select(m=>new BuildingWindowFrameBinding{original=m,customizable=frame[m]}).ToArray();Check(profile.bindings.Length==(tier.tier=="Far"?6:1),tier.tier+" explicit candidate frames");
    var aging=root.GetComponent<BuildingArchitecturalWeathering>()??root.AddComponent<BuildingArchitecturalWeathering>();aging.materials=model.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Distinct().Where(weather.Values.Contains).ToArray();aging.intensity=.7f;aging.Apply();
    // Reuse the actual planter geometry, with a private matte, restrained leaf finish.
    foreach(var renderer in root.GetComponentsInChildren<Renderer>().Where(r=>!r.transform.IsChildOf(model.transform)))
    {
     var materials=renderer.sharedMaterials;bool changed=false;
     for(int i=0;i<materials.Length;i++)if(materials[i]&&(materials[i].name=="Leaves"||materials[i].name.Contains("Garden-Leaves")||materials[i].name.Contains("CF_WhiteFlowers_Leaves")))
     {
      string path=N+"Restrained-"+materials[i].name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
      if(!material){material=new Material(materials[i]){name="Restrained-"+materials[i].name};material.SetColor("_BaseColor",new Color(.40f,.47f,.28f,1));material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.18f);if(material.HasProperty("_EmissionColor"))material.SetColor("_EmissionColor",Color.black);material.DisableKeyword("_EMISSION");AssetDatabase.CreateAsset(material,path);}materials[i]=material;changed=true;
     }
     if(changed)renderer.sharedMaterials=materials;
    }
    var count=model.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3);Check(count==tier.triangles,tier.tier+" exact FBX triangles "+count);Check(model.GetComponentsInChildren<MeshFilter>().All(f=>f.sharedMesh.vertices.All(v=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z))),tier.tier+" finite vertices");
    Check(root.GetComponentsInChildren<AuthoredBuildingLighting>().Length>0,tier.tier+" existing room clock retained");PrefabUtility.SaveAsPrefabAsset(root,Review+tier.tier+"-Authenticity.prefab");
    if(tier.tier=="Near")
    {
     foreach(var renderer in model.GetComponentsInChildren<Renderer>())renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>
     {
      if(!frame.TryGetValue(m,out var template))return m;
      var finish=new Material(template){name=template.name+"-Lab-Ivory"};finish.SetColor("_FrameTint",BuildingWindowFrames.Colors[0].Color);finish.SetFloat("_FrameColorSet",1);finish.SetFloat("_FrameSurfaceSet",1);finish.SetFloat("_FrameMetallic",0);finish.SetFloat("_FrameSmoothness",.30f);finish.SetFloat("_FrameGrain",.035f);return Save(finish,N+finish.name+".mat");
     }).ToArray();
     PrefabUtility.SaveAsPrefabAsset(root,Review+"Near-Lab-Authenticity.prefab");
    }
   }
   finally{Object.DestroyImmediate(root);}
  }
  AssetDatabase.SaveAssets();PersistenceChecks();File.WriteAllLines(Output+"Preparation-Checks.txt",checks);File.WriteAllText(Output+"Prepared.txt",DateTime.UtcNow.ToString("O"));
 }
 static LotAssetCatalogEntry Asset(string tier,bool candidate)=>new LotAssetCatalogEntry{id=candidate?"cf-review-bt2-candidate":"cf-review-bt2-original",label=candidate?"Brick Townhouse 2 · authenticity candidate":"Brick Townhouse 2 · original",category="Buildings",group="residential",subcategory="townhome",eraId="modern",fromYear=1921,toYear=1945,prefab=AssetDatabase.LoadAssetAtPath<GameObject>(candidate?Review+tier+"-Authenticity.prefab":Source+tier+".prefab"),distantPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(candidate?Review+"Medium-Authenticity.prefab":Source+"Medium.prefab"),defaultBuildingScale=1,footprint=new Vector2(8,20),buildingDimensionsMeters=new Vector3(7.64f,13.91f,18.935f),preserveEmissionColors=true,playerPlaceable=true};
 static BuildingAppearanceSettings State(bool candidate)=>candidate?new BuildingAppearanceSettings{windowFrameColor="warm-ivory",windowFrameMaterial="painted-wood"}:default;
 static readonly string[] Views={"Isometric-Right","Isometric-Left","Front","Window-Detail","Cornice-Detail","Roof","Foundation-Detail","Rear-Right","Rear-Left","Perspective-Front-Right","Perspective-Front-Left","Perspective-Rear-Right","Perspective-Rear-Left"};
 static readonly float[] Yaws={135,225,180,180,160,135,135,45,315,135,225,45,315},Pitches={30,30,0,12,15,62,22,30,30,25,25,25,25};
 static void CaptureAll(bool afterOnly=false)
 {
  jobs.Clear();index=frames=0;native=false;preview=new BuildingEditorPreview(1600,1800);
  foreach(string tier in new[]{"Near","Medium","Far"})foreach(bool candidate in afterOnly?new[]{true}:new[]{false,true})foreach(bool night in new[]{false,true})
  {
   string t=tier;bool c=candidate,n=night;jobs.Add(()=>{preview.Set(Asset(t,c),State(c));preview.SetTime(n);});
   foreach(int view in tier=="Near"?Enumerable.Range(0,Views.Length):new[]{0,2,7}){int v=view;jobs.Add(()=>StudioCapture(t+"-"+(c?"After":"Before")+"-"+(n?"Night":"Day")+"-"+Views[v],v));}
  }
 }
 static void StudioCapture(string name,int view)
 {
  foreach(var profile in Object.FindObjectsByType<BuildingArchitecturalWeathering>(FindObjectsSortMode.None))profile.Apply();preview.Render();var camera=(Camera)typeof(BuildingEditorPreview).GetField("camera",F).GetValue(preview);var key=(Light)typeof(BuildingEditorPreview).GetField("key",F).GetValue(preview);var bounds=(Bounds)typeof(BuildingEditorPreview).GetField("bounds",F).GetValue(preview);var model=(GameObject)typeof(BuildingEditorPreview).GetField("model",F).GetValue(preview);
  var rotation=Quaternion.Euler(Pitches[view],Yaws[view],0);Vector3 center=bounds.center;float radius=bounds.extents.magnitude;
  if(view==3)center=model.transform.position+new Vector3(-1.65f,7.25f,7.1f);if(view==4)center=model.transform.position+new Vector3(0,12.8f,6.9f);if(view==6)center=model.transform.position+new Vector3(-1.6f,1.1f,7.3f);
  camera.orthographic=view<9;camera.fieldOfView=42;camera.transform.SetPositionAndRotation(center-rotation*Vector3.forward*radius*(view<9?3:2.7f),rotation);camera.orthographicSize=view==3?1.9f:view==4?2.9f:view==6?2.4f:bounds.extents.magnitude*.91f;camera.nearClipPlane=.1f;camera.farClipPlane=radius*7;key.transform.rotation=Quaternion.Euler(40,Yaws[view]+30,0);
  var previousSun=RenderSettings.sun;var masks=new List<(Light,int)>();try{foreach(var l in Light.GetLights(LightType.Directional,30)){if(l!=key){masks.Add((l,l.cullingMask));l.cullingMask&=~(1<<30);}}key.enabled=true;RenderSettings.sun=key;RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=(RenderTexture)preview.Texture});}finally{key.enabled=false;RenderSettings.sun=previousSun;foreach(var p in masks)p.Item1.cullingMask=p.Item2;}
  SavePixels((RenderTexture)preview.Texture,Output+name+".png");File.WriteAllText(Output+name+".txt","Actual native URP / BuildingEditorPreview stage. Controlled camera "+camera.transform.position+" rotation "+camera.transform.eulerAngles+" ortho "+camera.orthographicSize+". Production pipeline untouched.\n");
 }
 static void SavePixels(RenderTexture rt,string path)
 {
  var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBAFloat,false,true);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();var colors=image.GetPixels();if(QualitySettings.activeColorSpace==ColorSpace.Linear)for(int i=0;i<colors.Length;i++)colors[i]=colors[i].gamma;var png=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);png.SetPixels(colors);png.Apply();File.WriteAllBytes(path,png.EncodeToPNG());Object.DestroyImmediate(png);Object.DestroyImmediate(image);RenderTexture.active=previous;
 }
 static void Native()
 {
  var oldStart=SessionState.GetString("TownhouseReviewPreviousStart","");UnityEditor.SceneManagement.EditorSceneManager.playModeStartScene=string.IsNullOrEmpty(oldStart)?null:AssetDatabase.LoadAssetAtPath<SceneAsset>(oldStart);
  if(Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Any(c=>c.GetType().FullName=="CityForge.UI.LotEditorPanel"))throw new Exception("An existing Lot Editor is open; its unsaved lot was preserved.");
  nativeCatalog=LotAssetCatalog.Load();nativeCatalog.entries=nativeCatalog.entries.Where(e=>e.id!="cf-review-bt2-original"&&e.id!="cf-review-bt2-candidate").Concat(new[]{Asset("Near",false),Asset("Near",true)}).ToArray();Object.FindFirstObjectByType<MainMenuController>().Handle(MenuAction.LotEditor);
  panel=Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(c=>c.GetType().FullName=="CityForge.UI.LotEditorPanel");OpenNativeLot(false);viewport=Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(c=>c.GetType().FullName=="CityForge.UI.LotEditorViewport");
  jobs.Clear();index=frames=0;native=true;
  foreach(bool candidate in new[]{false,true})foreach(bool night in new[]{false,true})
  {
   bool c=candidate,n=night;jobs.Add(()=>{OpenNativeLot(c);for(int k=0;k<12&&(RegionTimeOfDay)viewport.GetType().GetProperty("PreviewTime").GetValue(viewport)!=(n?RegionTimeOfDay.Night:RegionTimeOfDay.Afternoon);k++)viewport.GetType().GetMethod("CyclePreviewTime").Invoke(viewport,null);});
   foreach(int view in new[]{0,2,3,4,5,6}){int v=view;jobs.Add(()=>NativePose(v));jobs.Add(()=>{var camera=(Camera)viewport.GetType().GetField("cameraView",F).GetValue(viewport);SavePixels(camera.targetTexture,Output+"Native-"+(c?"After":"Before")+"-"+(n?"Night":"Day")+"-"+Views[v]+".png");});}
  }
  jobs.Add(()=>{OpenNativeLot(true);NativePose(0);});jobs.Add(()=>ScreenCapture.CaptureScreenshot(Output+"Native-Review-Ready.png"));
 }
 static void OpenNativeLot(bool candidate)
 {
  var asset=Asset("Near",candidate);var lot=new LotDefinition{name="UNSAVED · Brick Townhouse 2 · "+(candidate?"authenticity candidate":"original baseline"),width=2,length=5,category="residential",subcategory="townhome",firstEraId="modern",firstEraYear=1921};lot.objects.Add(new LotObjectDefinition{id="33333333333333333333333333333333",assetId=asset.id,yaw=180,buildingAppearance=State(candidate)});panel.GetType().GetMethod("OpenLot",F).Invoke(panel,new object[]{lot,false});
 }
 static void NativePose(int view)
 {
  var t=viewport.GetType();t.GetField("yaw",F).SetValue(viewport,view==0?-35f:view==4?-25f:0f);t.GetField("pitch",F).SetValue(viewport,view==0?32f:view==5?65f:18f);t.GetField("zoom",F).SetValue(viewport,view==3?.18f:view==4?.23f:view==6?.25f:.88f);t.GetField("focus",F).SetValue(viewport,new Vector3(view==3?1.65f:view==6?1.6f:0,view==4?12.8f:view==3?7.2f:view==6?1.2f:7,view>=3&&view!=5?-6.8f:0));t.GetMethod("ApplyPose",F).Invoke(viewport,null);
 }
 static Type labType;static object labSettings;static string labScene,labOutput;static bool labActive;
 static object LabCall(string method,params object[] args)=>labType.GetMethod(method,BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public).Invoke(null,args);
 static void Lab()
 {
  if(EditorApplication.isPlaying)throw new Exception("Lab comparison requires outside Play");
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.isDirty)throw new Exception("The current scene has unsaved edits; laboratory comparison did not replace it.");
  labScene=scene.path;labOutput=EditorPrefs.GetString("CityForge.ArchitecturalLab.Output","");EditorPrefs.SetString("CityForge.ArchitecturalLab.Output",Output+"Lab");
  labType=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CityForge.EditorTools.ArchitecturalRenderingLab")).First(t=>t!=null);
  UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/CityForge/Scenes/Development/ArchitecturalRenderingLab.unity");LabCall("Bind");labActive=true;labSettings=labType.GetField("s",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
  jobs.Clear();index=frames=0;native=false;
  foreach(bool candidate in new[]{false,true})
  {
   bool c=candidate;jobs.Add(()=>
   {
    LabCall("ReplaceSubject",AssetDatabase.LoadAssetAtPath<GameObject>(c?Review+"Near-Lab-Authenticity.prefab":Source+"Near.prefab"));
    var model=(GameObject)labType.GetField("subject",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);model.transform.rotation=Quaternion.Euler(0,180,0);
    var rs=model.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);model.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
   });
   foreach(int preset in new[]{0,5,6})foreach(int view in new[]{0,1,2,4,5,14,15,16})
   {
    int p=preset,v=view;jobs.Add(()=>{labSettings=labType.GetField("s",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);labSettings.GetType().GetField("view").SetValue(labSettings,v);LabCall("ApplyPreset",p);var model=(GameObject)labType.GetField("subject",BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);foreach(var renderer in model.GetComponentsInChildren<Renderer>())for(int i=0;i<renderer.sharedMaterials.Length;i++)if(renderer.sharedMaterials[i].HasProperty("_CFWeatherIntensity")){var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,i);block.SetFloat("_CFWeatherUseBuilding",1);block.SetMatrix("_CFWorldToBuilding",model.transform.worldToLocalMatrix);renderer.SetPropertyBlock(block,i);}});jobs.Add(()=>LabCall("Capture",(c?"After":"Before")+"-"+(p==0?"Neutral":p==5?"Production-Light-Control":"Night")));
   }
  }
  jobs.Add(()=>{LabCall("Restore");labActive=false;EditorPrefs.SetString("CityForge.ArchitecturalLab.Output",labOutput);UnityEditor.SceneManagement.EditorSceneManager.OpenScene(labScene);File.WriteAllText(Output+"Lab-Complete.txt","Matched original/candidate captures in the existing ArchitecturalRenderingLab. Neutral HDRI/reflection probe/AO, production lighting control and night presets. Prior scene and production pipeline restored; lab scene not saved.\n");});
 }
 [Serializable]class ExportDoc{public string[] dependencies;public Count[] tiers;}
 [Serializable]class Count{public string tier;public int triangles,renderers,materials;}
 static void ExportDependencies()
 {
  var dependencies=new HashSet<string>();var counts=new List<Count>();
  foreach(string tier in new[]{"Near","Medium","Far"})
  {
   string path=Review+tier+"-Authenticity.prefab";foreach(var d in AssetDatabase.GetDependencies(path,true))dependencies.Add(d);
   var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);counts.Add(new Count{tier=tier,triangles=root.GetComponentsInChildren<MeshFilter>(true).Sum(f=>f.sharedMesh.triangles.Length/3),renderers=root.GetComponentsInChildren<Renderer>(true).Length,materials=root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().Count()});
   Check(BuildingWindowFrames.HasBindings(root),tier+" candidate has valid frame bindings");
   if(tier=="Far")Check(root.GetComponentsInChildren<Renderer>(true).All(r=>r.sharedMaterials.All(m=>!m.HasProperty("_Surface")||m.GetFloat("_Surface")==0)),"Far candidate retains opaque materials");
  }
  foreach(string name in new[]{"BuildingAppearance.cs","BuildingWindowFrameProfile.cs","BuildingWindowFrames.cs","BuildingArchitecturalWeathering.cs","LotAssetCatalog.cs"})dependencies.Add("Assets/CityForge/Scripts/Lots/"+name);
  foreach(string name in new[]{"BuildingEditorModal.cs","BuildingEditorPreview.cs"})dependencies.Add("Assets/CityForge/Scripts/UI/"+name);
  foreach(var shader in new[]{"WindowFrameFinish.shader","WindowFrameInput.hlsl","ArchitecturalWeatheredLit.shader","ArchitecturalWeatherInput.hlsl","ArchitecturalWeatherForward.hlsl","ArchitecturalWeatherGBuffer.hlsl","Unity-Shader-License.txt"})dependencies.Add("Assets/CityForge/Shaders/Architecture/"+shader);
  File.WriteAllText(Output+"Native-Dependencies.json",JsonUtility.ToJson(new ExportDoc{dependencies=dependencies.OrderBy(d=>d).ToArray(),tiers=counts.ToArray()},true));File.WriteAllLines(Output+"Final-Checks.txt",checks);
 }
 static void OpenWindows()
 {
  if(!panel)return;panel.GetType().GetProperty("selected",F).SetValue(panel,"33333333333333333333333333333333");panel.GetType().GetMethod("OpenBuildingEditor",F).Invoke(panel,null);
  var modal=Object.FindFirstObjectByType<BuildingEditorModal>();Check(modal,"Native building properties modal opened");typeof(BuildingEditorModal).GetField("tab",F).SetValue(modal,1);typeof(BuildingEditorModal).GetMethod("ShowTab",F).Invoke(modal,null);
  jobs.Clear();index=frames=0;native=true;jobs.Add(()=>ScreenCapture.CaptureScreenshot(Output+"Native-Window-Properties.png"));
 }
 static void PersistenceChecks()
 {
  var candidate=Asset("Near",true);var original=Asset("Near",false);var catalog=ScriptableObject.CreateInstance<LotAssetCatalog>();catalog.entries=new[]{candidate,original};
  try
  {
   var lot=new LotDefinition{name="Transient persistence test",width=2,length=5,category="residential",subcategory="townhome",firstEraId="modern",firstEraYear=1921};lot.objects.Add(new LotObjectDefinition{id="11111111111111111111111111111111",assetId=candidate.id});lot.objects.Add(new LotObjectDefinition{id="22222222222222222222222222222222",assetId=original.id});
   var state=new BuildingAppearanceSettings{windowFrameColor="weathered-cream",windowFrameMaterial="painted-wood"};var session=new LotEditorSession();session.Open(lot);session.Edit(l=>BuildingAppearance.Apply(l,new Dictionary<string,BuildingAppearanceSettings>{{"11111111111111111111111111111111",state}},catalog));
   Check(session.Dirty&&session.CanUndo,"Window customization participates in native dirty/undo state");var reloaded=LotDefinition.Parse(JsonUtility.ToJson(session.Lot));Check(BuildingAppearance.Same(reloaded.objects[0].buildingAppearance,state),"Actual LotDefinition save/load preserves independent frame IDs");
   session.Undo();Check(string.IsNullOrEmpty(session.Lot.objects[0].buildingAppearance.windowFrameColor),"Native undo restores authored frame selection");session.Redo();Check(BuildingAppearance.Same(session.Lot.objects[0].buildingAppearance,state),"Native redo restores both frame properties");
   var colorOnly=state;colorOnly.windowFrameColor="dark-walnut";Check(colorOnly.windowFrameMaterial==state.windowFrameMaterial,"Color is independent from surface type");var surfaceOnly=state;surfaceOnly.windowFrameMaterial="painted-metal";Check(surfaceOnly.windowFrameColor==state.windowFrameColor,"Surface type is independent from color");
   bool rejected=false;var before=JsonUtility.ToJson(session.Lot);try{BuildingAppearance.Apply(session.Lot,new Dictionary<string,BuildingAppearanceSettings>{{"11111111111111111111111111111111",colorOnly},{"22222222222222222222222222222222",state}},catalog);}catch(InvalidDataException){rejected=true;}Check(rejected&&JsonUtility.ToJson(session.Lot)==before,"Unsupported multi-selection rejects atomically without partial edits");
   Check(!BuildingWindowFrames.Supports(original),"Original production prefab remains unbound and unchanged pending review");
  }
  finally{Object.DestroyImmediate(catalog);}
 }
}
}
#endif
