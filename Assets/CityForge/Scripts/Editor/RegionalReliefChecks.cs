#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEditor;
using UnityEngine;
using CityForge.World;
namespace CityForge.EditorTools
{
 [InitializeOnLoad] public static class RegionalReliefChecks
 {
 const string Out="/Users/joelinstrum/Documents/Codex/2026-10-14/continue-work-on-cityforge-v4-project/outputs/";
 static RegionalReliefChecks(){EditorApplication.update+=Poll;}
 static void Poll(){const string f="Library/RegionalRelief.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(f))return;if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}File.Delete(f);EditorApplication.isPaused=false;CityForge.Editor.EditorCoroutineRunner.Begin(Guard(Check()));}
 static IEnumerator Guard(IEnumerator routine){while(true){object next;try{if(!routine.MoveNext())yield break;next=routine.Current;}catch(Exception e){File.WriteAllText(Out+"Regional-Relief-Checks.txt","FAIL: "+e);Debug.LogException(e);yield break;}yield return next;}}
 static void Require(bool value,string name){if(!value)throw new Exception(name);}
 public static IEnumerator Check(){
  var old=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(old)old.ReturnToMenu();yield return null;yield return null;
  var recipe=new RegionCreationSettings{seed=730424,topography=4,rivers=1,streams=2,vegetation=RegionVegetation.Woodland};
  var saved=RegionMapGenerator.Generate("Regional Relief Trial",RegionSize.Medium,recipe);
  Require(saved.info.districts.Length==64,"64 districts generated");
  RegionBuildSteps.Complete(PrototypeRegionView.OpenStages(saved.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:saved));yield return null;
  var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();var relief=view.GetComponentInChildren<RegionReliefMap>();
  Require(view.SelectedDistrict==-1&&view.ApprovedDistrict==null,"Opens relief overview without native biome");Require(view.MapSide==8192&&view.DistrictSide==1024,"Regional scale and native district scale");
  Require(relief&&relief.GetComponent<MeshFilter>().sharedMesh.vertexCount==513*513,"Lightweight relief");Require(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("Region/RegionalRelief")),"Relief shader compiles");
  Require(relief.transform.Find("Regional slab")&&relief.transform.Find("Regional slab").GetComponent<MeshFilter>().sharedMesh.bounds.min.y<relief.GetComponent<MeshFilter>().sharedMesh.bounds.min.y,"Solid slab below map");Require(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("Region/RegionalSlab")),"Slab shader compiles");
  Require(!view.GetComponentsInChildren<Terrain>().Any(t=>t.terrainData.treeInstanceCount>0),"No individual trees in overview");
  for(int i=0;i<64;i++){var origin=view.DistrictOrigin(i);var p=new Vector3(origin.x+512,20000,origin.y+512);Require(relief.TryPick(new Ray(p,Vector3.down),out int selected)&&selected==i,"All 64 map cells pick correct district");}
  int frame=Time.frameCount;while(Time.frameCount<frame+4)yield return null;ScreenCapture.CaptureScreenshot(Out+"Regional-Relief-Overview.png");
  float[] firstEdge=null;
  foreach(int index in new[]{0,1,63}){
   view.EnterDistrict(index);Require(view.SelectedDistrict==index&&view.ApprovedDistrict&&view.ApprovedDistrict.Side==1024,"Selected detailed district");Require(view.GetComponentsInChildren<MeadowDistrictLandscape>().Length==1,"Only one detailed district loaded");Require(!relief.gameObject.activeSelf,"Neighboring map hidden");
   var terrain=view.ApprovedDistrict.DisplayTerrain;
   if(index==0){firstEdge=new float[17];for(int j=0;j<17;j++)firstEdge[j]=terrain.terrainData.GetInterpolatedHeight(1,j/16f);}
   if(index==1)for(int j=0;j<17;j++)Require(Mathf.Abs(firstEdge[j]-terrain.terrainData.GetInterpolatedHeight(0,j/16f))<.001f,"Rendered shared edge matches");
   foreach(RegionSeason season in Enum.GetValues(typeof(RegionSeason))){view.SetSeason(season);Require(terrain.terrainData.treePrototypes[0].prefab==Resources.Load<SeasonalTreeLibrary>("Seasons/SeasonalTrees").Prefabs(season)[0],"Approved seasonal biome retained");}view.SetSeason(RegionSeason.Summer);
var h=terrain.terrainData.GetHeights(0,0,1,1)[0,0];var bounds=RegionBiomeMap.DistrictBounds(index,8);float expected=SmoothRiverSurface.Sample(view.Data.Heights,bounds.x,bounds.y);Require(Mathf.Abs(h-expected)<.0001f,"District samples correct shared-region location");
   view.ShowOverview();yield return null;Require(view.ApprovedDistrict==null&&relief.gameObject.activeSelf,"Return to lightweight map");
  }
  var capture=SavedRegionStore.Capture(view);Require(capture.meadow!=null&&capture.info.meadowCacheDistrict==63,"Overview save retains district cache");var path=Path.Combine(Application.temporaryCachePath,"region-grid-check-"+Guid.NewGuid().ToString("N")+".cfregion");try{SavedRegionStore.Write(capture,path);var round=SavedRegionStore.Load(path);Require(round.info.creation.GridSide==8&&round.info.districts.Length==64&&round.info.meadowCacheDistrict==63,"Expanded save round trip");round.info.district=63;SavedRegionStore.Write(round,path);Require(SavedRegionStore.Load(path).info.district==63,"High district IDs accepted");}finally{if(File.Exists(path))File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");}
  var legacy=new RegionCreationSettings{version=2};Require(legacy.GridSide==2&&new RegionBiomeMap(legacy,saved.data).DescribeDistricts().Length==4,"Legacy four-district recipes retained");
  view.ReturnToMenu();yield return null;yield return null;
  var coastal=CoastlineTestRegion.Generate();RegionBuildSteps.Complete(PrototypeRegionView.OpenStages(coastal.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:coastal));
  frame=Time.frameCount;while(Time.frameCount<frame+4)yield return null;ScreenCapture.CaptureScreenshot(Out+"Regional-Coast-Slab-Preview.png");yield return null;
  File.WriteAllText(Out+"Regional-Relief-Checks.txt","PASS: 64 generated districts; 8192m region/1024m districts; relief/slab shaders; solid slab below map; coastal overview preview; no overview trees; all 64 cells ray-picked; districts 0, 1 and 63 independently load approved native biome and correct shared heights; rendered shared edge and seasonal biome; only selected district active; overview unloads detail; cached district retained on overview save; temporary expanded save/load and ID63; legacy version2 retains 4 districts. Player saves untouched.\n");
 }
 }
}
#endif
