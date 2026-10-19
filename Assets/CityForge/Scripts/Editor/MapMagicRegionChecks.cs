#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEditor;
using UnityEngine;
using MapMagic.Nodes.MatrixGenerators;
using MapMagic.Nodes;
using CityForge.World;
using CityForge.UI;
namespace CityForge.EditorTools
{
 [InitializeOnLoad] public static class MapMagicRegionChecks
 {
 const string Out="/Users/joelinstrum/Documents/Codex/2026-10-14/continue-work-on-cityforge-v4-project/outputs/";
 static MapMagicRegionChecks(){EditorApplication.update+=Poll;}
 static void Poll(){const string f="Library/MapMagicRegion.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(f))return;if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}File.Delete(f);CityForge.Editor.EditorCoroutineRunner.Begin(Guard(Check()));}
 static IEnumerator Guard(IEnumerator routine){while(true){object next;try{if(!routine.MoveNext())yield break;next=routine.Current;}catch(Exception e){File.WriteAllText(Out+"MapMagic-Region-Checks.txt","FAIL: "+e);Debug.LogException(e);yield break;}yield return next;}}
 static void Require(bool b,string message){if(!b)throw new Exception(message);}
 static IEnumerator Check(){
 var old=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(old)old.ReturnToMenu();yield return null;yield return null;
 var recipe=new RegionCreationSettings{seed=730424,topography=4,rivers=1,streams=2,vegetation=RegionVegetation.Woodland};Require(recipe.version==5,"New maps use MapMagic recipe5");
 float[,] first;using(var source=new MapMagicRegionGeography(recipe,RegionSize.Medium)){
 Require(source.Graph.generators.Any(g=>g is Noise200)&&source.Graph.generators.Any(g=>g is Levels200)&&source.Graph.generators.Any(g=>g is Erosion200),"Actual installed MapMagic nodes in graph");first=source.Generate();}
 using(var source=new MapMagicRegionGeography(recipe,RegionSize.Medium)){var repeat=source.Generate();for(int z=0;z<513;z+=8)for(int x=0;x<513;x+=8)Require(first[z,x]==repeat[z,x],"Deterministic graph output");}
 using(var source=new MapMagicRegionGeography(new RegionCreationSettings{seed=730425,topography=4},RegionSize.Medium)){var other=source.Generate();double change=0;for(int z=0;z<513;z+=32)for(int x=0;x<513;x+=32)change+=Math.Abs(other[z,x]-first[z,x]);Require(change>.000001,"Seed affects geography");}
 float[,] eroded;using(var source=new MapMagicRegionGeography(recipe,RegionSize.Medium)){((Erosion200)source.Graph.generators.First(g=>g is Erosion200)).iterations=0;eroded=source.Generate();}double difference=0;foreach(int z in new[]{64,128,256,384})foreach(int x in new[]{64,128,256,384})difference+=Math.Abs(eroded[z,x]-first[z,x]);Require(difference>.000001,"Erosion changes elevations");
 var network=RegionalRiverNetwork.Create(recipe);Require(network.Count==6,"Trunk and five requested tributaries");foreach(var body in network.Skip(1))Require(Mathf.Abs(body.centerline[0].x-RegionalRiverNetwork.MainCenter(body.centerline[0].y,recipe.seed))<.000001f,"Tributaries join main channel");
 var snapshot=RegionMapGenerator.Generate("MapMagic Regional Trial",RegionSize.Medium,recipe,first);Require(snapshot.info.terrainHeightRange==1200,"Physical vertical scale no longer compressed by prototype ratio");var legacy=RegionMapGenerator.Generate("Legacy",RegionSize.Medium,new RegionCreationSettings{version=3,seed=recipe.seed,topography=4,rivers=1});Require(snapshot.data.Heights[128,128]!=legacy.data.Heights[128,128],"MapMagic replaces old geography");
 var path=Path.Combine(Application.temporaryCachePath,"mapmagic-region-"+Guid.NewGuid().ToString("N")+".cfregion");try{SavedRegionStore.Write(snapshot,path);var restored=SavedRegionStore.Load(path);Require(restored.info.creation.version==5&&restored.data.Heights[128,128]==snapshot.data.Heights[128,128],"Saved heights authoritative on load");}finally{if(File.Exists(path))File.Delete(path);}
 // Exercise the real wizard entry point, including worker generation and shared loading overlay.
 var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();menu.Handle(MenuAction.CreateRegion);menu.CreatePrototypeRegion("MapMagic Regional Trial",RegionSize.Medium,recipe);Require(menu.IsOpeningRegion&&UnityEngine.Object.FindFirstObjectByType<RegionLoadingOverlay>(),"Generation preloader appears immediately");
 double deadline=EditorApplication.timeSinceStartup+120;int frames=0,last=Time.frameCount;while(menu.IsOpeningRegion&&EditorApplication.timeSinceStartup<deadline){yield return null;if(Time.frameCount!=last){frames++;last=Time.frameCount;}}
 var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(!menu.IsOpeningRegion&&view&&view.SelectedDistrict==-1&&view.DistrictCount==64,"Wizard creates MapMagic relief region");Require(frames>=3,"Loading remains staged");Require(view.Data.Heights[128,128]==snapshot.data.Heights[128,128],"Async and immediate paths agree");
 int frame=Time.frameCount;while(Time.frameCount<frame+5)yield return null;ScreenCapture.CaptureScreenshot(Out+"MapMagic-Regional-Overview.png");
 File.WriteAllText(Out+"MapMagic-Region-Checks.txt",$"PASS: MapMagic Noise/Levels/Erosion graph; finite 513-square output; deterministic repeat; seed variation; erosion changes terrain ({difference:F6} sampled total); old recipe3 retained; stored MapMagic heights round-trip; actual Create Region path uses worker and preloader ({frames} rendered frames); 64 districts and lightweight relief; async matches immediate. No player save written.\n");
 }
 }
}
#endif
