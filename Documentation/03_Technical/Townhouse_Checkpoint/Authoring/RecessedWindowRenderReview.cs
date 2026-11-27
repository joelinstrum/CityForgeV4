#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CityForge.UI;
using UnityEngine.UI;
using Object=UnityEngine.Object;
namespace CityForge.Lots
{
internal static class RecessedWindowRenderReview
{
 const string R="Assets/CityForge/Development/RecessedWindowsReview/",S="Assets/CityForge/Development/WindowFrameReview/";
 const string O="/Users/joelinstrum/Documents/Codex/2026-11-27/files-pasted-by-the-user-cityforge/outputs/Townhouse-Recessed-Windows/";
 const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Instance;
 static readonly List<string> checks=new();static readonly Queue<Action> jobs=new();static BuildingEditorPreview preview;static int wait;static int captures;
 static void Check(bool ok,string msg){if(!ok)throw new Exception(msg);checks.Add("PASS "+msg);}
 public static void Run()
 {
  Directory.CreateDirectory(O);preview=new BuildingEditorPreview(1400,1400);
  foreach(string tier in new[]{"Near","Medium","Far"})foreach(bool night in new[]{false,true})foreach(bool after in new[]{false,true})
  {string t=tier;bool n=night,a=after;jobs.Enqueue(()=>{var asset=Asset(t);if(!a){asset.prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CityForge/Development/FacadeWeatheringReview/"+t+"-Weathering.prefab");asset.distantPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CityForge/Development/FacadeWeatheringReview/Medium-Weathering.prefab");}var state=State(3);state.windowFrameColor=a?"weathered-cream":"warm-ivory";preview.Set(asset,state);preview.SetTime(n);});
   foreach(int view in tier=="Near"?new[]{0,2,3,4}:new[]{0,2}){int v=view;jobs.Enqueue(()=>Capture(t+"-"+(n?"Night":"Day")+"-"+(a?"After":"Before")+"-"+new[]{"Gameplay","Distant","Side-Detail","Front-Detail","Oblique-Recess"}[v],v));}
  }
  EditorApplication.update+=Tick;
 }
 public static void MoreViews()
 {
  preview=new BuildingEditorPreview(1400,1400);foreach(bool night in new[]{false,true})foreach(bool after in new[]{false,true}){bool n=night,a=after;jobs.Enqueue(()=>{var asset=Asset("Near");if(!a){asset.prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CityForge/Development/FacadeWeatheringReview/Near-Weathering.prefab");asset.distantPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CityForge/Development/FacadeWeatheringReview/Medium-Weathering.prefab");}var state=State(3);state.windowFrameColor=a?"weathered-cream":"warm-ivory";preview.Set(asset,state);preview.SetTime(n);});foreach(int view in new[]{5,6,7}){int v=view;jobs.Enqueue(()=>Capture("Near-"+(n?"Night":"Day")+"-"+(a?"After":"Before")+"-"+(v==5?"Rear-Left":v==6?"Rear-Right":"Front-Other"),v));}}EditorApplication.update+=Tick;
 }
 static void Tick()
 {
  if(EditorApplication.isCompiling||EditorApplication.isUpdating||ShaderUtil.anythingCompiling||++wait<4)return;wait=0;
  try{if(jobs.Count>0){jobs.Dequeue()();EditorApplication.QueuePlayerLoopUpdate();return;}preview?.Dispose();ShaderChecks();File.WriteAllLines(O+"Validation.txt",checks);File.WriteAllText(O+"Complete.txt",captures+" native URP captures\n");EditorApplication.Exit(0);}
  catch(Exception e){File.WriteAllText(O+"ERROR.txt",e.ToString());EditorApplication.Exit(1);}
 }
 static LotAssetCatalogEntry Asset(string tier)=>new(){id="cf-review-bt2-facade",label="Brick Townhouse 2 · facade weathering review",category="Buildings",group="residential",subcategory="townhome",eraId="modern",fromYear=1921,toYear=1945,prefab=AssetDatabase.LoadAssetAtPath<GameObject>(R+tier+"-Authenticity.prefab"),distantPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(R+"Medium-Authenticity.prefab"),defaultBuildingScale=1,footprint=new Vector2(8,20),buildingDimensionsMeters=new Vector3(7.64f,13.91f,18.935f),preserveEmissionColors=true,playerPlaceable=true};
 static void Prepare()
 {
  AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
  var importer=(TextureImporter)AssetImporter.GetAtPath(R+"Townhouse-Architecture-RGBA.png");importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;importer.SaveAndReimport();
  var packed=AssetDatabase.LoadAssetAtPath<Texture2D>(R+"Townhouse-Architecture-RGBA.png");var remap=new Dictionary<Material,Material>();
  foreach(string tier in new[]{"Near","Medium","Far"})
  {
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(S+tier+"-Authenticity.prefab");var root=Object.Instantiate(source);root.name="Brick Townhouse 2 · facade weathering · "+tier;
   var profile=root.GetComponentInChildren<BuildingArchitecturalWeathering>(true);Check(profile&&profile.materials.Length>0,tier+" existing explicit weathering bindings");
   foreach(var m in profile.materials)
   {
    if(remap.ContainsKey(m))continue;var copy=new Material(m){name=m.name+"-FacadeReview"};copy.shader=Shader.Find("CityForge/Architecture/Facade Weathered Lit");copy.SetTexture("_CFFacadeMask",packed);
    string name=m.name.Contains("b6efb51a6e76a4")||m.name.Contains("55e9b708f82137")?"brick":m.name.Contains("dfa54c72771242")||m.name.Contains("8f766330669779")||m.name.Contains("ff3b406d80831d")||m.name.Contains("bb326fdc75f526")||m.name.Contains("5cd2096fee2643")?"stone":m.name.Contains("b7e683fb858d9f")?"roof":m.name.Contains("ddf679c99c3b42")?"wood":"metal";Vector4 response=name.Contains("brick")?new(1,1,1,.008f):name.Contains("stone")?new(.8f,1.15f,.75f,.006f):name.Contains("roof")?new(.35f,.05f,.4f,.003f):name.Contains("wood")?new(.6f,0,1.2f,.012f):new(.4f,.15f,.65f,.003f);
    copy.SetVector("_CFMaterialResponse",response);string path=R+copy.name+".mat";var old=AssetDatabase.LoadAssetAtPath<Material>(path);if(old){EditorUtility.CopySerialized(copy,old);Object.DestroyImmediate(copy);copy=old;EditorUtility.SetDirty(old);}else AssetDatabase.CreateAsset(copy,path);remap[m]=copy;
   }
   foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>remap.TryGetValue(m,out var replacement)?replacement:m).ToArray();
   foreach(var tint in root.GetComponentsInChildren<BuildingTintProfile>(true))foreach(var b in tint.bindings)if(b!=null&&b.material&&remap.TryGetValue(b.material,out var replacement))b.material=replacement;
   profile.materials=profile.materials.Select(m=>remap[m]).ToArray();profile.customizationEnabled=true;profile.Apply();
   Check(root.GetComponentsInChildren<MeshFilter>(true).Sum(x=>x.sharedMesh.triangles.Length)==source.GetComponentsInChildren<MeshFilter>(true).Sum(x=>x.sharedMesh.triangles.Length),tier+" geometry exactly retained");
   PrefabUtility.SaveAsPrefabAsset(root,R+tier+"-Authenticity.prefab");Object.DestroyImmediate(root);
  }
  string[] types={"Brick","Sandstone","Limestone","PaintedWood","Concrete","PaintedMetal"};
  Vector4[] responses={new(1,1,1,.008f),new(.8f,1.15f,.75f,.006f),new(.72f,1.1f,.6f,.005f),new(.6f,0,1.2f,.012f),new(1.1f,1.25f,.8f,.006f),new(.4f,.15f,.65f,.003f)};
  for(int i=0;i<types.Length;i++){string path=R+"Response-"+types[i]+".asset";var response=AssetDatabase.LoadAssetAtPath<FacadeWeatheringMaterialResponse>(path);if(!response){response=ScriptableObject.CreateInstance<FacadeWeatheringMaterialResponse>();AssetDatabase.CreateAsset(response,path);}response.absorption=responses[i].x;response.mineralSusceptibility=responses[i].y;response.fadingResponse=responses[i].z;response.microNormalStrength=responses[i].w;EditorUtility.SetDirty(response);}
  AssetDatabase.SaveAssets();
 }
 static BuildingAppearanceSettings State(int preset)=>new(){windowFrameColor="warm-ivory",windowFrameMaterial="painted-wood",facadeWeathering=preset<0?default:Seeded(preset)};
 static FacadeWeatheringSettings Seeded(int preset){var s=FacadeWeatheringSettings.Preset(preset);s.seed=137;return s;}
 static void Tests()
 {
  var asset=Asset("Near");Check(BuildingArchitecturalWeathering.Supports(asset),"Near/Medium explicit capability");Check(BuildingWindowFrames.Supports(asset),"window customization retained");
  var catalog=LotAssetCatalog.Load();catalog.entries=catalog.entries.Where(e=>e.id!=asset.id).Concat(new[]{asset}).ToArray();
  var lot=new LotDefinition{name="Facade QA",width=2,length=5};const string id="11111111111111111111111111111111";lot.objects.Add(new LotObjectDefinition{id=id,assetId=asset.id});
  var session=new LotEditorSession();session.Open(lot);var state=State(2);state.facadeWeathering.seed=137;
  session.Edit(l=>BuildingAppearance.Apply(l,new Dictionary<string,BuildingAppearanceSettings>{{id,state}},catalog));
  var current=(LotDefinition)typeof(LotEditorSession).GetProperty("Current",F)?.GetValue(session);
  // Session public property is inspected below by normal serialization, not a new persistence route.
  var parsed=LotDefinition.Parse(JsonUtility.ToJson(lot));
  // Session.Open may clone; use a direct apply to verify the canonical lot JSON path independently.
  BuildingAppearance.Apply(lot,new Dictionary<string,BuildingAppearanceSettings>{{id,state}},catalog);parsed=LotDefinition.Parse(JsonUtility.ToJson(lot));
  Check(BuildingAppearance.Same(parsed.objects[0].buildingAppearance,state),"lot JSON roundtrip keeps all six channels, seed and independent window choices");
  Check(session.Dirty&&session.CanUndo,"weathering participates in native dirty/undo state");session.Undo();Check(!session.Lot.objects[0].buildingAppearance.facadeWeathering.enabled,"undo restores original weathering");session.Redo();Check(BuildingAppearance.Same(session.Lot.objects[0].buildingAppearance,state),"redo restores weathering and windows");
  Check(FacadeWeatheringSettings.StableSeed(id)==FacadeWeatheringSettings.StableSeed(id)&&FacadeWeatheringSettings.StableSeed(id)!=FacadeWeatheringSettings.StableSeed("other"),"stable instance ID seed");
  for(int i=0;i<6;i++){var invalid=state;invalid.facadeWeathering.Set(i,float.NaN);bool rejected=false;try{BuildingAppearance.Validate(invalid);}catch(InvalidDataException){rejected=true;}Check(rejected,"reject nonfinite channel "+i);}
  var old=new LotAssetCatalogEntry{id="unsupported",category="Buildings",prefab=AssetDatabase.LoadAssetAtPath<GameObject>(S+"Near-Finish-Review.prefab"),distantPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(S+"Medium-Finish-Review.prefab")};catalog.entries=catalog.entries.Concat(new[]{old}).ToArray();var item=new LotObjectDefinition{id="22222222222222222222222222222222",assetId=old.id};lot.objects.Add(item);var before=JsonUtility.ToJson(lot);bool failed=false;try{BuildingAppearance.Apply(lot,new Dictionary<string,BuildingAppearanceSettings>{{id,State(1)},{item.id,state}},catalog);}catch(InvalidDataException){failed=true;}Check(failed&&JsonUtility.ToJson(lot)==before,"unsupported batch change is atomic");
  foreach(string tier in new[]{"Near","Medium","Far"})
  {
   var instance=Object.Instantiate(Asset(tier).prefab);var profile=instance.GetComponentInChildren<BuildingArchitecturalWeathering>();var refs=instance.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).ToArray();
   BuildingArchitecturalWeathering.Set(instance,state,id);var r=profile.GetComponentsInChildren<Renderer>(true).First(r=>r.sharedMaterials.Any(profile.materials.Contains));int slot=Array.FindIndex(r.sharedMaterials,m=>profile.materials.Contains(m));var b=new MaterialPropertyBlock();r.GetPropertyBlock(b,slot);Check(b.GetVector("_CFFacadeA")==new Vector4(.6f,.55f,.4f,.6f),tier+" identical saved control values");
   instance.transform.SetPositionAndRotation(new Vector3(100,3,-70),Quaternion.Euler(0,73,0));profile.Apply();r.GetPropertyBlock(b,slot);var p=new Vector3(.3f,5,2);Check(Vector3.Distance(b.GetMatrix("_CFWorldToBuilding").MultiplyPoint3x4(profile.transform.TransformPoint(p)),p)<.0001f,tier+" weathering follows moved/rotated assembly");
   Check(instance.GetComponentsInChildren<Renderer>(true).SelectMany(x=>x.sharedMaterials).SequenceEqual(refs),tier+" no material instances created");BuildingArchitecturalWeathering.Set(instance,default,id);r.GetPropertyBlock(b,slot);Check(b.GetFloat("_CFFacadeEnabled")==0,tier+" exact authored override reset");Object.DestroyImmediate(instance);
  }
 }
 static void BuildJobs()
 {
  preview=new BuildingEditorPreview(1400,1400);
  foreach(string tier in new[]{"Near","Medium","Far"})foreach(bool night in new[]{false,true})foreach(int preset in new[]{-1,1,2,3})
  {
   string t=tier;bool n=night;int p=preset;jobs.Enqueue(()=>{preview.Set(Asset(t),State(p));preview.SetTime(n);});
   foreach(int view in t=="Near"?new[]{0,1,2,3}:new[]{0,1}){int v=view;jobs.Enqueue(()=>Capture(t+"-"+(n?"Night":"Day")+"-"+(p<0?"Original":p==1?"Subtle":p==2?"Moderate":"Heavy")+"-"+new[]{"Gameplay","Distant","Side-Detail","Front-Detail"}[v],v));}
  }
  jobs.Enqueue(()=>{var state=State(0);preview.Set(Asset("Near"),state);preview.SetTime(false);});jobs.Enqueue(()=>Capture("Pristine-Gameplay",0));
  jobs.Enqueue(()=>{preview.Set(Asset("Near"),State(-1));preview.SetTime(false);});jobs.Enqueue(()=>Capture("Reset-Gameplay",0));
  // Independent channels and seed stability at the same side-wall close-up.
  for(int i=-1;i<6;i++){int c=i;jobs.Enqueue(()=>{var state=State(0);if(c>=0)state.facadeWeathering.Set(c,1);preview.Set(Asset("Near"),state);preview.SetTime(false);});jobs.Enqueue(()=>Capture("Channel-"+(c<0?"Zero":c.ToString()),2));}
  jobs.Enqueue(()=>{var state=State(2);state.facadeWeathering.seed=777;preview.Set(Asset("Near"),state);preview.SetTime(false);});jobs.Enqueue(()=>Capture("Alternate-Seed",0));
  jobs.Enqueue(PerformanceProbe);jobs.Enqueue(BuildUI);jobs.Enqueue(CaptureUI);jobs.Enqueue(TestUI);

 }
 static void Capture(string name,int view)
 {
  preview.Render();var camera=(Camera)typeof(BuildingEditorPreview).GetField("camera",F).GetValue(preview);var key=(Light)typeof(BuildingEditorPreview).GetField("key",F).GetValue(preview);var model=(GameObject)typeof(BuildingEditorPreview).GetField("model",F).GetValue(preview);var bounds=(Bounds)typeof(BuildingEditorPreview).GetField("bounds",F).GetValue(preview);
  var rotation=Quaternion.Euler(view==2?10:view==4?12:30,view==2?115:view==4?150:view==5?45:view==6?315:view==7?225:135,0);var center=view==2||view==4?model.transform.position+new Vector3(-3.6f,6.2f,.5f):view==3?model.transform.position+new Vector3(-1.3f,7.5f,7):bounds.center;
  if(view==3)rotation=Quaternion.Euler(12,180,0);camera.orthographic=true;camera.orthographicSize=view==4?2.3f:view==2?4.6f:view==3?3.3f:view==1?28:bounds.extents.magnitude*.92f;camera.transform.SetPositionAndRotation(center-rotation*Vector3.forward*50,rotation);camera.nearClipPlane=.1f;camera.farClipPlane=150;key.transform.rotation=Quaternion.Euler(40,165,0);
  var sun=RenderSettings.sun;try{key.enabled=true;RenderSettings.sun=key;for(int warm=0;warm<2;warm++)RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=(RenderTexture)preview.Texture});}finally{key.enabled=false;RenderSettings.sun=sun;}
  Save((RenderTexture)preview.Texture,O+name+".png");File.WriteAllText(O+name+".txt",$"Native URP / Heavy weathering; controlled camera and lighting. Geometry and frame finish vary by comparison state. Camera {camera.transform.position:R}; rotation {camera.transform.rotation:R}; ortho {camera.orthographicSize:R}; key rotation {key.transform.rotation:R}; key intensity {key.intensity:R}\n");captures++;
 }
 static void Save(RenderTexture rt,string path)
 {
  var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGBAFloat,false,true);tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();var pixels=tex.GetPixels();if(QualitySettings.activeColorSpace==ColorSpace.Linear)for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;var png=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);png.SetPixels(pixels);png.Apply();File.WriteAllBytes(path,png.EncodeToPNG());Object.DestroyImmediate(png);Object.DestroyImmediate(tex);RenderTexture.active=old;
 }
 static GameObject uiRoot;static BuildingEditorModal modal;static Camera uiCamera;static RenderTexture uiTarget;
 static void BuildUI()
 {
  var asset=Asset("Near");var catalog=ScriptableObject.CreateInstance<LotAssetCatalog>();catalog.entries=new[]{asset};
  uiRoot=new GameObject("Facade weathering UI review",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var canvas=uiRoot.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;
  uiCamera=new GameObject("UI review camera").AddComponent<Camera>();uiCamera.enabled=false;uiCamera.orthographic=true;uiCamera.backgroundColor=new Color(.035f,.05f,.06f);uiCamera.clearFlags=CameraClearFlags.SolidColor;uiCamera.cullingMask=1<<29;uiCamera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
  uiTarget=new RenderTexture(1600,1100,24,RenderTextureFormat.ARGBHalf);uiTarget.Create();uiCamera.targetTexture=uiTarget;canvas.worldCamera=uiCamera;canvas.planeDistance=1;
  var scaler=uiRoot.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1100);
  var go=BuildingEditorModal.Show(uiRoot.transform,Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),new[]{new LotObjectDefinition{id="11111111111111111111111111111111",assetId=asset.id,buildingAppearance=State(1)}},catalog,"Lot Editor · facade weathering review",()=>{},apply:changes=>{});modal=go.GetComponent<BuildingEditorModal>();
  foreach(var t in uiRoot.GetComponentsInChildren<Transform>(true))t.gameObject.layer=29;
  Canvas.ForceUpdateCanvases();var scroll=go.GetComponentInChildren<ScrollRect>();scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();
  var preview=(BuildingEditorPreview)typeof(BuildingEditorModal).GetField("preview",F).GetValue(modal);preview.Render();typeof(BuildingEditorModal).GetMethod("Fit",F).Invoke(modal,null);Canvas.ForceUpdateCanvases();
 }
 static void CaptureUI()
 {
  Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(uiCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=uiTarget});Save(uiTarget,O+"Appearance-Weathering-UI.png");
 }
 static void TestUI()
 {
  var presets=modal.GetComponentsInChildren<Button>(false).Where(b=>b.name=="Weathered").ToArray();Check(presets.Length==1,"weathering section in existing Appearance tab");presets[0].onClick.Invoke();Canvas.ForceUpdateCanvases();
  var drafts=(Dictionary<string,BuildingAppearanceSettings>)typeof(BuildingEditorModal).GetField("drafts",F).GetValue(modal);var draft=drafts.Values.First();Check(draft.facadeWeathering.surfaceVariation==.6f&&draft.windowFrameColor=="warm-ivory","preset changes weathering without changing independent windows");
  var sliders=modal.GetComponentsInChildren<Slider>(false).Where(x=>x.name.StartsWith("Weathering ·")).ToArray();Check(sliders.Length==6,"six independent live weathering sliders");sliders.First(x=>x.name.EndsWith("Sun fading")).value=72;draft=drafts.Values.First();Check(draft.facadeWeathering.sunFading==.72f&&draft.facadeWeathering.foundationGrime==.6f,"slider changes only its own saved channel");
  modal.GetComponentsInChildren<Button>(false).First(b=>b.name=="Reset to original appearance").onClick.Invoke();draft=drafts.Values.First();Check(!draft.facadeWeathering.enabled&&draft.windowFrameColor=="warm-ivory","weathering reset preserves window customization");
  typeof(BuildingEditorModal).GetField("tab",F).SetValue(modal,1);typeof(BuildingEditorModal).GetMethod("ShowTab",F).Invoke(modal,null);Check(modal.GetComponentsInChildren<Button>(false).Any(b=>b.name=="Original black"),"Windows tab remains available");
  Object.DestroyImmediate(uiRoot);Object.DestroyImmediate(uiCamera.gameObject);uiTarget.Release();Object.DestroyImmediate(uiTarget);
 }
 static void PerformanceProbe()
 {
  var material=Asset("Near").prefab.GetComponentInChildren<BuildingArchitecturalWeathering>().materials[0];var roots=new List<GameObject>();var watch=System.Diagnostics.Stopwatch.StartNew();
  for(int i=0;i<1000;i++){var root=GameObject.CreatePrimitive(PrimitiveType.Cube);root.name="Weathering instance probe";root.GetComponent<Renderer>().sharedMaterial=material;var profile=root.AddComponent<BuildingArchitecturalWeathering>();profile.materials=new[]{material};profile.customizationEnabled=true;BuildingArchitecturalWeathering.Set(root,State(2),i.ToString());roots.Add(root);}
  watch.Stop();double setup=watch.Elapsed.TotalMilliseconds;watch.Restart();foreach(var root in roots)BuildingArchitecturalWeathering.Set(root,State(1));watch.Stop();double update=watch.Elapsed.TotalMilliseconds;
  Check(roots.All(x=>x.GetComponent<Renderer>().sharedMaterial==material),"1000 parameterized instances retain one shared material");foreach(var root in roots)Object.DestroyImmediate(root);
  var shader=material.shader;var method=typeof(ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);var code=method?.Invoke(null,new object[]{shader,0});
  File.WriteAllText(O+"Performance.txt",$"1000 simple cube instances, isolated Editor CPU probe. Creation/profile binding {setup:F2}ms; one explicit control update across all 1000 {update:F2}ms. No runtime material clones; cached renderer slots; idle profiles only check root transform. Shader SRP compatibility code: {code}. MaterialPropertyBlocks bypass the SRP Batcher for these overridden renderers. Per-instance weather fields are declared in a Unity instancing buffer; actual renderer batching and full-city GPU cost remain to be profiled. No additional geometry, per-window decals, passes or per-instance textures. One shared 2048x1024 packed architecture mask per assembly family. This CPU probe is not a city-frame/GPU benchmark.\n");
 }
 static void ShaderChecks(){var shader=Shader.Find("CityForge/Architecture/Facade Weathered Lit");Check(shader&&shader.isSupported,"URP weathered shader supported");var variants=new ShaderVariantCollection();variants.Add(new ShaderVariantCollection.ShaderVariant(shader,PassType.ScriptableRenderPipeline,new[]{"INSTANCING_ON"}));variants.WarmUp();Check(variants.isWarmedUp,"GPU instancing variant warmed");Object.DestroyImmediate(variants);Check(!ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity.ToString()=="Error"),"no compiled weathered shader errors");}
}
}
#endif
