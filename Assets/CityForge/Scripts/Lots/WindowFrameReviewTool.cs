#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using CityForge.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;
namespace CityForge.Lots
{
// Opt-in editor review. Never changes a source mesh or an original material/default.
[InitializeOnLoad] internal static class WindowFrameReviewTool
{
 const string Source="Assets/CityForge/Resources/Lots/RebuiltBuildings/BrickTownhouse02/";
 const string Shared="Assets/CityForge/SharedArchitecture/WindowFrames/";
 const string Review="Assets/CityForge/Development/WindowFrameReview/";
 const string Output="/Users/joelinstrum/Documents/Codex/2026-11-27/files-pasted-by-the-user-cityforge/outputs/Window-Frame-Variants/";
 static readonly List<Action> jobs=new();static readonly List<string> checks=new();static int index,ticks;static BuildingEditorPreview preview;
 static WindowFrameReviewTool(){EditorApplication.update+=Tick;AssemblyReloadEvents.beforeAssemblyReload+=Dispose;EditorApplication.quitting+=Dispose;}
 static void Dispose(){preview?.Dispose();preview=null;}
 static void Check(bool value,string message){if(!value)throw new Exception(message);checks.Add("PASS "+message);}
 static void Tick()
 {
  if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode)return;
  const string request="Library/WindowFrameReview.request";
  try
  {
   if(File.Exists(request)){string command=File.ReadAllText(request).Trim();File.Delete(request);Directory.CreateDirectory(Output);if(command=="prepare")Prepare();else if(command=="capture")CaptureAll();else if(command=="ui")ShowUI();}
   if(jobs.Count>0&&++ticks>=4){ticks=0;if(index<jobs.Count){jobs[index++]();EditorApplication.QueuePlayerLoopUpdate();}else{jobs.Clear();Dispose();File.WriteAllText(Output+"Validation.txt",string.Join("\n",checks));File.WriteAllText(Output+"Capture-Complete.txt",DateTime.UtcNow.ToString("O"));}}
  }
  catch(Exception e){jobs.Clear();Dispose();File.WriteAllText(Output+"ERROR.txt",e.ToString());Debug.LogException(e);}
 }
 static Material Load(string name)=>AssetDatabase.LoadAssetAtPath<Material>(Source+name+".mat");
 static void Prepare()
 {
  checks.Clear();Directory.CreateDirectory(Shared);Directory.CreateDirectory(Review);AssetDatabase.Refresh();
  foreach(string file in Directory.GetFiles(Shared,"*.png"))
  {var importer=(TextureImporter)AssetImporter.GetAtPath(file);importer.sRGBTexture=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=file.Contains("Mask")?TextureWrapMode.Clamp:TextureWrapMode.Repeat;importer.mipmapEnabled=true;importer.SaveAndReimport();}
  var shader=Shader.Find("CityForge/Architecture/Window Frame Finish");Check(shader,"Masked frame shader exists");
  var detail=AssetDatabase.LoadAssetAtPath<Texture2D>(Shared+"Frame-Fine-Grain.png");
  var names=new[]{"BT2_3ae3efafff4ef0","BT2_ede19d1b680aaa","BT2_5f57048b018205","BT2_f397d4e89a889c","BT2_261f6f57b5153b","BT2_4994b11da7bcf4","BT2_52d4d1708f5981"};
  var templates=new Dictionary<Material,Material>();
  for(int i=0;i<names.Length;i++)
  {
   var original=Load(names[i]);Check(original,"Source frame material "+names[i]);
   string path=Shared+names[i]+"-Frame-Template.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(!material){material=new Material(original);material.shader=shader;material.name=names[i]+"-Frame-Template";AssetDatabase.CreateAsset(material,path);}
   material.SetTexture("_FrameDetail",detail);
   if(i>0){string sourceMap=AssetDatabase.GetAssetPath(original.GetTexture("_BaseMap"));string mask=Path.GetFileName(sourceMap).Split(new[]{"Adjusted-"},StringSplitOptions.None).Last().Replace("-Albedo","-FrameMask");var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Shared+mask);Check(texture,"Far frame-only mask "+mask);material.SetTexture("_FrameMask",texture);}
   templates[original]=material;EditorUtility.SetDirty(material);
  }
  foreach(string tier in new[]{"Near","Medium","Far"})
  {
   string path=Review+tier+"-Finish-Review.prefab";var root=PrefabUtility.LoadPrefabContents(path);
   try
   {
    var profile=root.GetComponent<BuildingWindowFrameProfile>()??root.AddComponent<BuildingWindowFrameProfile>();
    var used=root.GetComponentsInChildren<Renderer>(true).SelectMany(r=>r.sharedMaterials).Distinct().Where(templates.ContainsKey);
    profile.bindings=used.Select(m=>new BuildingWindowFrameBinding{original=m,customizable=templates[m]}).ToArray();
    Check(profile.bindings.Length==(tier=="Far"?6:1),tier+" explicit frame bindings only");PrefabUtility.SaveAsPrefabAsset(root,path);
   }
   finally{PrefabUtility.UnloadPrefabContents(root);}
   Check(BuildingWindowFrames.HasBindings(AssetDatabase.LoadAssetAtPath<GameObject>(path)),tier+" profile has valid material slots");
  }
  // Portable reusable presets. Runtime color/type choices remain independent.
  foreach(var c in BuildingWindowFrames.Colors)
  {
   var type=c.Id=="weathered-bronze"?BuildingWindowFrames.Surfaces[2]:c.Id=="dark-walnut"?BuildingWindowFrames.Surfaces[3]:BuildingWindowFrames.Surfaces[0];
   string path=Shared+c.Id+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}
   mat.SetColor("_FrameTint",c.Color);mat.SetFloat("_FrameColorSet",1);mat.SetFloat("_FrameSurfaceSet",1);mat.SetFloat("_FrameMetallic",type.Metallic);mat.SetFloat("_FrameSmoothness",type.Smoothness);mat.SetFloat("_FrameGrain",type.Grain);mat.SetTexture("_FrameDetail",detail);EditorUtility.SetDirty(mat);
  }
  foreach(string tier in new[]{"Near","Medium"})
  {
   string fbx=Review+tier+"-Thin-Frames.fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(fbx);Check(importer,"Thin frame export exists "+tier);importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.isReadable=true;importer.SaveAndReimport();
   var frames=AssetDatabase.LoadAssetAtPath<GameObject>(fbx);var root=PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Review+tier+"-Finish-Review.prefab")) as GameObject;
   try
   {
    var original=root.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.sharedMaterials.Contains(Load(names[0])));
    var candidate=frames.GetComponentInChildren<MeshFilter>();var target=original.GetComponent<MeshFilter>();
    Check(Vector3.Distance(target.sharedMesh.bounds.center,candidate.sharedMesh.bounds.center)<.002f,tier+" thinner test native alignment");
    target.sharedMesh=candidate.sharedMesh;PrefabUtility.SaveAsPrefabAsset(root,Review+tier+"-Thin-Review.prefab");
   }
   finally{Object.DestroyImmediate(root);}
  }
  AssetDatabase.SaveAssets();File.WriteAllText(Output+"Setup-Validation.txt",string.Join("\n",checks));
 }
 static LotAssetCatalogEntry Asset(string tier,bool thin=false)=>new LotAssetCatalogEntry{id="cf-building-art-deco-brick-townhouse-2-001",label="Art Deco Brick Townhouse 2",category="Buildings",prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Review+tier+(thin?"-Thin-Review":"-Finish-Review")+".prefab"),distantPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Review+"Medium-Finish-Review.prefab"),footprint=new Vector2(8,20),defaultBuildingScale=1,preserveEmissionColors=true};
 static void ValidateSelections()
 {
  foreach(string tier in new[]{"Near","Medium","Far"})
  {
   var asset=Asset(tier);Check(BuildingWindowFrames.Supports(asset),tier+" supports independent frame controls");
   var instance=Object.Instantiate(asset.prefab);var before=instance.GetComponentsInChildren<Renderer>(true).ToDictionary(r=>r,r=>r.sharedMaterials);
   try
   {
    BuildingWindowFrames.Attach(instance,asset,default);var feature=instance.GetComponent<BuildingWindowFrames>();
    Check(before.All(p=>p.Value.SequenceEqual(p.Key.sharedMaterials)),tier+" empty settings are an exact material no-op");
    foreach(var color in BuildingWindowFrames.Colors)foreach(var surface in BuildingWindowFrames.Surfaces)
    {
     var state=new BuildingAppearanceSettings{windowFrameColor=color.Id,windowFrameMaterial=surface.Id};
     var saved=JsonUtility.ToJson(state);var loaded=JsonUtility.FromJson<BuildingAppearanceSettings>(saved);BuildingAppearance.Validate(loaded);Check(BuildingAppearance.Same(state,loaded),tier+" save/reload "+color.Id+" / "+surface.Id);feature.Set(loaded);
     foreach(var pair in before)
     {
      var profile=pair.Key.GetComponentInParent<BuildingWindowFrameProfile>();var materials=pair.Key.sharedMaterials;
      for(int i=0;i<materials.Length;i++)
      {
       var binding=profile?.bindings.FirstOrDefault(b=>b.original==pair.Value[i]);
       Check(materials[i]==(binding==null?pair.Value[i]:binding.customizable),tier+" only explicit frame slot changes "+pair.Key.name+" #"+i);
       if(binding!=null){var block=new MaterialPropertyBlock();pair.Key.GetPropertyBlock(block,i);Check(block.GetFloat("_FrameMetallic")==surface.Metallic&&block.GetFloat("_FrameSmoothness")==surface.Smoothness,tier+" surface response "+surface.Id);Check(block.GetColor("_FrameTint")==color.Color,tier+" independent color "+color.Id);}
      }
     }
    }
    feature.Set(new BuildingAppearanceSettings{windowFrameColor="warm-ivory"});feature.Set(new BuildingAppearanceSettings{windowFrameMaterial="painted-metal"});feature.Set(default);
    Check(before.All(p=>p.Value.SequenceEqual(p.Key.sharedMaterials)),tier+" Original restores every source material");
   }
   finally{Object.DestroyImmediate(instance);}
  }
  bool rejected=false;try{BuildingAppearance.Validate(new BuildingAppearanceSettings{windowFrameColor="unknown"});}catch(InvalidDataException){rejected=true;}Check(rejected,"Unknown frame IDs are rejected");
  Check(BuildingAppearance.Same(default,JsonUtility.FromJson<BuildingAppearanceSettings>("{}")),"Legacy saves retain original defaults");
 }
 static void CaptureAll()
 {
  checks.Clear();ValidateSelections();jobs.Clear();index=ticks=0;preview=new BuildingEditorPreview(1600,1800);
  foreach(string tier in new[]{"Near","Medium","Far"})foreach(bool night in new[]{false,true})
  {
   string t=tier;bool n=night;
   jobs.Add(()=>{preview.Set(Asset(t));preview.SetTime(n);});
   foreach(string id in new[]{"original","warm-ivory","weathered-cream","dark-walnut","forest-green","soft-charcoal","weathered-bronze"})
   {
    string color=id;var state=id=="original"?default:new BuildingAppearanceSettings{windowFrameColor=id,windowFrameMaterial=id=="weathered-bronze"?"exposed-metal":id=="dark-walnut"?"stained-wood":"painted-wood"};
    jobs.Add(()=>preview.Paint(state));jobs.Add(()=>Capture(t+"-"+(n?"Night":"Day")+"-"+color));
   }
  }
  foreach(string tier in new[]{"Near","Medium"})foreach(bool thin in new[]{false,true})
  {string t=tier;bool v=thin;jobs.Add(()=>{preview.Set(Asset(t,v),new BuildingAppearanceSettings{windowFrameColor="warm-ivory",windowFrameMaterial="painted-wood"});preview.SetTime(false);});jobs.Add(()=>Capture(t+"-Thickness-"+(v?"75":"100")));}
 }
 static void Capture(string name)
 {
  preview.Render();var rt=(RenderTexture)preview.Texture;var previous=RenderTexture.active;RenderTexture.active=rt;
  var image=new Texture2D(rt.width,rt.height,TextureFormat.RGBAFloat,false,true);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();var pixels=image.GetPixels();if(QualitySettings.activeColorSpace==ColorSpace.Linear)for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;
  var png=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);png.SetPixels(pixels);png.Apply();File.WriteAllBytes(Output+name+".png",png.EncodeToPNG());Object.DestroyImmediate(image);Object.DestroyImmediate(png);RenderTexture.active=previous;
  var camera=(Camera)typeof(BuildingEditorPreview).GetField("camera",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(preview);
  File.WriteAllText(Output+name+".txt","Actual BuildingEditorPreview + native URP. Linear to sRGB once. Camera "+camera.transform.position+" rotation "+camera.transform.eulerAngles+" ortho "+camera.orthographicSize+"\nSource prefab "+(name.Contains("Thickness-75")?Review:Source)+"\n");
 }
 static void ShowUI()
 {
  var existing=GameObject.Find("Window frame customization review");if(existing)Object.DestroyImmediate(existing);
  var canvas=new GameObject("Window frame customization review",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;var scaler=canvas.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1000);
  if(!Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>())new GameObject("Review EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.EventSystems.StandaloneInputModule));
  var asset=Asset("Near");var catalog=ScriptableObject.CreateInstance<LotAssetCatalog>();catalog.entries=new[]{asset};
  var modal=BuildingEditorModal.Show(canvas.transform,Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"),new[]{new LotObjectDefinition{id="frame-review",assetId=asset.id}},catalog,"Window frame comparison · no lot modified",()=>Object.DestroyImmediate(canvas),"frame-review",changes=>{});
  var component=modal.GetComponent<BuildingEditorModal>();typeof(BuildingEditorModal).GetField("tab",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(component,1);typeof(BuildingEditorModal).GetMethod("ShowTab",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(component,null);
  // Native UI inspection is handled through the regular catalog; no lot or source asset is edited.
 }
}
}
#endif
