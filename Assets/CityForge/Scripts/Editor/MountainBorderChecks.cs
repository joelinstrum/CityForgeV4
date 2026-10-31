#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using CityForge.World;
using CityForge.UI;
namespace CityForge.Editor {
public static class MountainBorderChecks {
static void Require(bool value,string message){if(!value)throw new Exception(message);}
public static void Run(string output)=>EditorCoroutineRunner.Begin(Guard(Check(output),output));
static IEnumerator Guard(IEnumerator check,string output){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"Status.txt"),"RUNNING");while(true){bool next;try{next=check.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(output,"Status.txt"),"FAIL "+e);(check as IDisposable)?.Dispose();yield break;}if(!next)break;yield return null;}File.WriteAllText(Path.Combine(output,"Status.txt"),"PASS natural mountain border generation, confinement, determinism, legacy compatibility, persistence and normal worker creation / district");}
static IEnumerator Check(string output){
var old=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(old)old.ReturnToMenu();yield return null;yield return null;
var recipe=new RegionCreationSettings{terrainShape=RegionTerrainShape.MountainBorder,topography=3,seed=730421,rivers=0,streams=0};
float[,] Field(){using var graph=new MapMagicRegionGeography(recipe,RegionSize.Small);return graph.Generate();}
recipe.version=6;var legacy=Field();recipe.version=9;var approved=Field();recipe.version=11;var rugged=Field();var repeat=Field();Require(rugged.Cast<float>().SequenceEqual(repeat.Cast<float>()),"Deterministic field");
float max=0,prior=legacy.Cast<float>().Max()*1200,interior=0,maxSlope=0;int peakX=0,peakZ=0;
for(int z=0;z<513;z++)for(int x=0;x<513;x++){float h=rugged[z,x]*1200;Require(float.IsFinite(h)&&h>=0&&h<=1200,"Finite physical heights");if(h>max){max=h;peakX=x;peakZ=z;}if(z/512f<.745f)interior=Mathf.Max(interior,h);if(x<512&&z<512){float dx=(rugged[z,x+1]-rugged[z,x])*1200/8,dz=(rugged[z+1,x]-rugged[z,x])*1200/8;maxSlope=Mathf.Max(maxSlope,Mathf.Atan(new Vector2(dx,dz).magnitude)*Mathf.Rad2Deg);}}
Require(max>200&&max<650&&maxSlope>15&&interior<15,"Tall steep mountains confined to edge");
recipe.topography=0;var flat=Field();Require(flat.Cast<float>().Max()*1200>200,"Mountain shape overrides Flat relief");recipe.topography=3;recipe.terrainShape=RegionTerrainShape.Land;recipe.version=6;var landLegacy=Field();recipe.version=11;Require(landLegacy.Cast<float>().SequenceEqual(Field().Cast<float>()),"Other landforms unchanged");recipe.terrainShape=RegionTerrainShape.MountainBorder;
var snapshot=RegionMapGenerator.Generate("Rocky mountain validation",RegionSize.Small,recipe,rugged);var caps=MountainSummitSnow.Select(snapshot.data,new MountainBorderVariation(recipe.seed).SnowCaps);Require(caps.Length>=1&&caps.Length<=3,"Seeded summit snow caps");foreach(var cap in caps){Require(MountainSummitSnow.Cover(caps,cap.uv,cap.height,recipe.seed)>.9f,"White summit");Require(MountainSummitSnow.Cover(caps,cap.uv,.10f,recipe.seed)==0,"Green foothills remain snow free");}string temp=Path.Combine(Application.temporaryCachePath,"mountain-"+Guid.NewGuid().ToString("N")+".cfregion");try{SavedRegionStore.Write(snapshot,temp);var loaded=SavedRegionStore.Load(temp);Require(loaded.info.creation.version==11&&loaded.data.Heights.Cast<float>().SequenceEqual(snapshot.data.Heights.Cast<float>()),"Stored field round trip");}finally{if(File.Exists(temp))File.Delete(temp);}
File.WriteAllText(Path.Combine(output,"Checks.txt"),$"PASS v6 peak {prior:F1}m -> v11 peak {max:F1}m; max sampled slope {maxSlope:F1} degrees; interior relief {interior:F1}m. Deterministic; Flat retains mountains; Land unchanged; authoritative save round trip.\n");
var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();menu.CreatePrototypeRegion("Natural Mountain Border Preview",RegionSize.Small,recipe);double deadline=UnityEditor.EditorApplication.timeSinceStartup+240;while(menu.IsOpeningRegion&&UnityEditor.EditorApplication.timeSinceStartup<deadline)yield return null;var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(view&&!menu.IsOpeningRegion&&view.CreationSettings.version==11,"Ordinary worker-backed creation");yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,"Mountain-Region.png"));yield return null;yield return null;
int district=Mathf.Min(7,peakZ/64)*8+Mathf.Min(7,peakX/64);view.RequestDistrict(district);deadline=UnityEditor.EditorApplication.timeSinceStartup+240;while(view.DistrictLoading&&UnityEditor.EditorApplication.timeSinceStartup<deadline)yield return null;Require(view.ApprovedDistrict&&!view.DistrictLoading,"Detailed mountain district");var terrain=view.ApprovedDistrict.DisplayTerrain;Require(terrain.terrainData.size.y>=1200,"Physical district elevation retained");yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,"Mountain-District.png"));yield return null;yield return null;
File.AppendAllText(Path.Combine(output,"Checks.txt"),"PASS ordinary preloader/worker creation, actual overview and mountain district "+district+"; unsaved preview left open.\n");
view.ReturnToMenu();yield return null;yield return null;
foreach(int nextSeed in new[]{730422,730423}){
 recipe.seed=nextSeed;var silhouette=new MountainBorderVariation(nextSeed);menu.CreatePrototypeRegion("Mountain Variation "+(silhouette.Pattern==0?"Single Summit":"Twin Summits"),RegionSize.Small,recipe);
 deadline=UnityEditor.EditorApplication.timeSinceStartup+240;while(menu.IsOpeningRegion&&UnityEditor.EditorApplication.timeSinceStartup<deadline)yield return null;
 view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(view&&!menu.IsOpeningRegion,"Normal variant creation");var peaks=MountainSummitSnow.Select(view.Data,silhouette.SnowCaps);Require(peaks.Length==silhouette.SnowCaps,"Variation cap count");
 using(var graph=new MapMagicRegionGeography(recipe,RegionSize.Small)){var again=graph.Generate();for(int z=0;z<513;z+=8)for(int x=0;x<513;x+=8)Require(Mathf.Abs(view.Data.Heights[z,x]-(35f/1200+again[z,x]))<.00001f,"Repeat seed reproduces actual created map");}
 for(int z=0;z<380;z+=8)for(int x=0;x<513;x+=8)Require(view.Data.Heights[z,x]*1200<55,"Variant rear-quarter confinement");
 yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,silhouette.Pattern==0?"Single-Summit.png":"Twin-Summits.png"));yield return null;yield return null;
 File.AppendAllText(Path.Combine(output,"Checks.txt"),"PASS variant "+silhouette.Pattern+" seed="+nextSeed+" snow caps="+peaks.Length+"; repeat-seed heights and rear-quarter confinement.\n");
 if(nextSeed!=730423){view.ReturnToMenu();yield return null;yield return null;}
}

}
}
}
#endif
