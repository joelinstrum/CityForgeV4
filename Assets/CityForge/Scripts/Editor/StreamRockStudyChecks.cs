#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using CityForge.World;
using CityForge.UI;
using CityForge.CameraSystem;
namespace CityForge.Editor {
public static class StreamRockStudyChecks {
 public static void Run(string output)=>EditorCoroutineRunner.Begin(Guard(output));
 static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
 static IEnumerator Guard(string output){
  Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"Status.txt"),"RUNNING");var work=Check(output);
  while(true){bool more;try{more=work.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(output,"Status.txt"),"FAIL "+e);(work as IDisposable)?.Dispose();yield break;}if(!more)break;yield return work.Current;}
  File.WriteAllText(Path.Combine(output,"Status.txt"),"PASS");
 }
 static IEnumerator Check(string output){
  var fixture=StreamRockTestRegion.Create();var again=StreamRockTestRegion.Create();Require(fixture.data.Heights.Cast<float>().SequenceEqual(again.data.Heights.Cast<float>()),"Deterministic study field");
  string path=Path.Combine(SavedRegionStore.DirectoryPath,StreamRockTestRegion.Id+".cfregion");
  // Only create our uniquely named test save. Never replace existing player data on reruns.
  if(!File.Exists(path))SavedRegionStore.Save(fixture);
  var loaded=SavedRegionStore.Load(path);Require(loaded.data.Heights.Cast<float>().SequenceEqual(fixture.data.Heights.Cast<float>()),"Authoritative saved study field");
  var old=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(old)old.ReturnToMenu();yield return null;yield return null;
  var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();menu.OpenSavedRegion(loaded.info);
  double deadline=UnityEditor.EditorApplication.timeSinceStartup+240;while(menu.IsOpeningRegion&&UnityEditor.EditorApplication.timeSinceStartup<deadline)yield return null;
  var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(view&&!menu.IsOpeningRegion&&view.SelectedDistrict==-1,"Normal saved study overview open");
  view.RequestDistrict(3);deadline=UnityEditor.EditorApplication.timeSinceStartup+240;while(view.DistrictLoading&&UnityEditor.EditorApplication.timeSinceStartup<deadline)yield return null;
  Require(!view.DistrictLoading&&view.SelectedDistrict==3,"Normal study district entry");
  var land=view.ApprovedDistrict;Require(land,"Approved native district");var study=land.GetComponent<StreamRockStudy>();Require(study&&study.PatchCount>25&&study.PatchCount<60&&study.EmergingPatchCount>5&&study.ShrubCount>10,"Rocks, emerging tops and bank shrubs");
  Require(land.GetComponentInChildren<CrestDistrictRiverPreview>(),"Approved running river water");
  float minDepth=10,maxDepth=0,minWidth=100,maxWidth=0,oldWidthTotal=0,newWidthTotal=0,oldBankTotal=0,newBankTotal=0;
  var reference=StreamRockTestRegion.Create(false);
  for(float z=80;z<450;z+=15){
   float center=StreamRockTestRegion.Center(z),depth=12-land.Height(new Vector2(center,z));minDepth=Mathf.Min(minDepth,depth);maxDepth=Mathf.Max(maxDepth,depth);float width=0;
   for(float x=center-15;x<center+15;x+=.05f){
    float h=land.Height(new Vector2(x,z)),oldH=reference.data.SmoothHeight(x/1024+.5f,z/1024+.5f)*PrototypeRegionData.Height;
    if(h<12){width+=.05f;newWidthTotal+=.05f;}else if(h<13.4f)newBankTotal+=.05f;
    if(oldH<12)oldWidthTotal+=.05f;else if(oldH<13.4f)oldBankTotal+=.05f;
   }
   minWidth=Mathf.Min(minWidth,width);maxWidth=Mathf.Max(maxWidth,width);
  }
  float widthRatio=newWidthTotal/oldWidthTotal,bankRatio=newBankTotal/oldBankTotal;
  Require(widthRatio>1.22f&&widthRatio<1.29f,"Retained wider wet channel");
  if(!StreamRockTestRegion.SoftBanks(view))Require(bankRatio>.65f&&bankRatio<.84f,"Narrow exposed slopes");
  var settings=land.GetComponentInChildren<CrestDistrictRiverPreview>().Settings;
  Require(Mathf.Abs(settings.GetVector("_Crest_Absorption").x-.72f)<.001f&&Mathf.Abs(settings.GetColor("_Crest_Scattering").b-.11f)<.001f,"Muted shallow stream material");
  var singles=land.GetComponent<IndividualStreamRockScatter>();Require(singles&&singles.VariantCount==10&&singles.RockCount>150&&singles.EmergingCount>0,"Ten individual rock variants scattered through gaps");
  Require(study.PatchCount==49&&singles.RockCount==564&&singles.EmergingCount==98,"Approved cluster and individual-rock mix retained");
  Require(Mathf.Abs(settings.GetFloat("_Crest_PlanarReflectionsIntensity")-.35f)<.001f,"Stream reflection contrast");
  Require(minDepth>.15f&&maxDepth<(StreamRockTestRegion.DeeperBed(view)?.9f:.8f)&&minWidth>3&&maxWidth<10,"Shallow narrow stream in actual displayed terrain");
  Require(land.DisplayTerrain.terrainData.terrainLayers.Last().name=="Damp earth stream bank","Dedicated owned bank soil layer");
  if(StreamRockTestRegion.DeeperBed(view)){var bed=land.GetComponent<StreamBedDepthStudy>();Require(bed&&bed.LoweredVertices>500&&bed.MaximumLowering>.29f&&bed.MaximumDryChange<.0001f,"Deeper open bed between preserved rock shelves");}
  var rig=view.GetComponentInChildren<CityForgeCameraController>();rig.SetHeading(45);rig.ConfigureTerrainStudy(new Vector3(StreamRockTestRegion.Center(170),12,170),24,1);
  float until=Time.time+3;while(Time.time<until)yield return null;
  ScreenCapture.CaptureScreenshot(Path.Combine(output,"Stream-Close.png"));yield return null;yield return null;
  rig.SetZoomLevel(2);until=Time.time+1;while(Time.time<until)yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,"Stream-Reach.png"));yield return null;yield return null;
  rig.SetZoomLevel(1);
  File.WriteAllText(Path.Combine(output,"Checks.txt"),$"PASS deterministic explicit stream fixture; authoritative save round trip; normal menu open into district3; approved native biome and Crest current; {study.PatchCount} scanned patches, {study.EmergingPatchCount} patches with tops above water, {study.ShrubCount} bank shrubs. Displayed center depth {minDepth:F2}–{maxDepth:F2}m; wet width {minWidth:F1}–{maxWidth:F1}m; width ratio {widthRatio:F3}, exposed-slope ratio {bankRatio:F3}; muted teal stream material and reflection contrast; {singles.RockCount} individual stones using {singles.VariantCount} variants in cluster-free bed; {singles.EmergingCount} bounds above water. New study save only; no ordinary generator/old save changes. Screenshots require review; not a MapMagic creation or performance qualification.");
 }
}
}
#endif
