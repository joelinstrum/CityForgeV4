#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using CityForge.World;
using CityForge.UI;
namespace CityForge.Editor {
public static class DistrictWeatherChecks {
static void Require(bool value,string message){if(!value)throw new Exception(message);}
public static void Run(string source,string output)=>EditorCoroutineRunner.Begin(Guard(Check(source,output),output));
static IEnumerator Guard(IEnumerator check,string output){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"Status.txt"),"RUNNING");while(true){bool next;try{next=check.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(output,"Status.txt"),"FAIL "+e);(check as IDisposable)?.Dispose();yield break;}if(!next)break;yield return null;}File.WriteAllText(Path.Combine(output,"Status.txt"),"PASS weather availability, timing, water mask, particles, cleanup and canonical stability");}
static IEnumerator Check(string source,string output){
string temp=Path.Combine(Application.temporaryCachePath,"weather-"+Guid.NewGuid().ToString("N")+".cfregion");
try {
var old=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(old)old.ReturnToMenu();yield return null;yield return null;
var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();var snapshot=SavedRegionStore.Load(source);snapshot.info.creation.climate=RegionClimate.Temperate;snapshot.info.climate=RegionClimate.Temperate;snapshot.info.season=0;SavedRegionStore.Write(snapshot,temp);menu.OpenSavedRegion(SavedRegionStore.Load(temp).info);
double deadline=UnityEditor.EditorApplication.timeSinceStartup+180;while(menu.IsOpeningRegion&&UnityEditor.EditorApplication.timeSinceStartup<deadline)yield return null;
var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(view,"Normal fixture load");view.RequestDistrict(snapshot.info.meadowCacheDistrict);while(view.DistrictLoading&&UnityEditor.EditorApplication.timeSinceStartup<deadline)yield return null;
var land=view.ApprovedDistrict;Require(land,"District built");var weather=land.GetComponent<DistrictWeather>();Require(weather,"Weather initialized");var canonical=land.CanonicalTreeInstances.ToArray();var heights=land.DisplayTerrain.terrainData.GetHeights(0,0,land.DisplayTerrain.terrainData.heightmapResolution,land.DisplayTerrain.terrainData.heightmapResolution);
Button B(string name)=>view.transform.Find("RegionHud/TemporaryWeather/"+name).GetComponent<Button>();
Require(B("Rain").interactable&&!B("Snow").interactable,"Summer availability");weather.SetWeather(DistrictWeather.WeatherKind.Snow);Require(weather.Current==DistrictWeather.WeatherKind.Clear,"Summer rejects snow");B("Rain").onClick.Invoke();
float until=Time.time+2;while(Time.time<until)yield return null;Require(weather.Current==DistrictWeather.WeatherKind.Rain&&land.GetComponentInChildren<ParticleSystem>().particleCount>0,"Rain particles falling");ScreenCapture.CaptureScreenshot(Path.Combine(output,"Rain.png"));yield return null;
view.SetSeason(RegionSeason.Winter);yield return null;Require(weather.Current==DistrictWeather.WeatherKind.Clear&&!B("Rain").interactable&&B("Snow").interactable,"Winter availability and rain cleared");weather.SetWeather(DistrictWeather.WeatherKind.Rain);Require(weather.Current==DistrictWeather.WeatherKind.Clear,"Winter rejects rain");B("Snow").onClick.Invoke();
until=Time.time+2;while(Time.time<until)yield return null;Require(weather.SnowCoverage>.15f&&weather.SnowCoverage<.4f&&weather.FogAmount>.25f&&weather.FogAmount<.85f,"Partial snow and gradual fog at two seconds");ScreenCapture.CaptureScreenshot(Path.Combine(output,"Snow-2s.png"));yield return null;
until=Time.time+3;while(Time.time<until)yield return null;Require(weather.FogAmount==1&&weather.SnowCoverage<.8f,"Fog complete before snow");
until=Time.time+6;while(Time.time<until)yield return null;Require(weather.SnowCoverage==1&&RenderSettings.fog,"Full snow after ten seconds");Require(land.GetComponentInChildren<ParticleSystem>().particleCount>0,"Snow falling");
var snow=land.transform.Find("Accumulated snow");Require(snow,"Snow overlay");var material=snow.GetComponent<MeshRenderer>().sharedMaterial;Require(material.shader.isSupported,"Snow shader supported");var mask=(Texture2D)material.GetTexture("_Land");int dry=0,wet=0;
for(int z=0;z<mask.height;z+=4)for(int x=0;x<mask.width;x+=4){var p=land.Origin+new Vector2(x/(float)(mask.width-1),z/(float)(mask.height-1))*land.Side;float m=mask.GetPixel(x,z).r;if(land.WaterDistance(p)<=1.2f||land.ShoreDistance(p)<=.6f){Require(m<.01f,"Water excludes snow");wet++;}else{Require(m>.99f,"Land mask");dry++;}}
Require(dry>0&&wet>0,"Coastal wet/dry fixture");Require(canonical.SequenceEqual(land.CanonicalTreeInstances),"Canonical trees stable");var after=land.DisplayTerrain.terrainData.GetHeights(0,0,heights.GetLength(1),heights.GetLength(0));Require(heights.Cast<float>().SequenceEqual(after.Cast<float>()),"Terrain heights unchanged");ScreenCapture.CaptureScreenshot(Path.Combine(output,"Snow-11s.png"));yield return null;yield return null;
view.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>().SetZoomLevel(2);until=Time.time+1;while(Time.time<until)yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,"Snow-Close.png"));yield return null;yield return null;
var sun=land.GetComponentsInChildren<Light>().First(l=>l.type==LightType.Directional);var shadowMode=sun.shadows;sun.shadows=LightShadows.None;yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,"Snow-NoShadows.png"));yield return null;yield return null;sun.shadows=shadowMode;yield return null;
B("Clear").onClick.Invoke();Require(weather.IsClearing&&RenderSettings.fog&&snow.gameObject.activeSelf&&weather.SnowCoverage>.99f,"Clear preserves cover initially");
until=Time.time+2;while(Time.time<until)yield return null;Require(weather.SnowCoverage>.65f&&weather.SnowCoverage<.9f&&weather.FogAmount>0&&weather.PrecipitationStrength>0&&weather.PrecipitationStrength<.8f,"Clear tapers particles and melts gradually");ScreenCapture.CaptureScreenshot(Path.Combine(output,"Melt-2s.png"));yield return null;
until=Time.time+3;while(Time.time<until)yield return null;Require(weather.PrecipitationStrength==0&&weather.FogAmount==0&&weather.SnowCoverage>0,"Precipitation and fog finish before melt");ScreenCapture.CaptureScreenshot(Path.Combine(output,"Melt-5s.png"));yield return null;
until=Time.time+6;while(Time.time<until)yield return null;Require(!weather.IsClearing&&!RenderSettings.fog&&!snow.gameObject.activeSelf&&weather.SnowCoverage==0,"Clear completes melt");weather.SetWeather(DistrictWeather.WeatherKind.Snow);view.ShowOverview();yield return null;yield return null;Require(!UnityEngine.Object.FindFirstObjectByType<DistrictWeather>()&&!view.transform.Find("RegionHud/TemporaryWeather")&&!RenderSettings.fog,"Overview cleanup");view.ReturnToMenu();yield return null;yield return null;
menu.OpenSavedRegion(SavedRegionStore.Load(source).info);while(menu.IsOpeningRegion)yield return null;view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();view.RequestDistrict(view.LoadedMeadowDistrict);while(view.DistrictLoading)yield return null;weather=view.ApprovedDistrict.GetComponent<DistrictWeather>();view.SetSeason(RegionSeason.Winter);yield return null;Require(view.VisualSeason==RegionSeason.Summer&&B("Rain").interactable&&!B("Snow").interactable,"Tropical winter remains rain-only");view.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>().SetZoomLevel(1);
File.WriteAllText(Path.Combine(output,"Checks.txt"),"PASS Summer rain / Winter snow gating, blocked invalid calls, rain and snow particles, gradual 4s fog, partial coverage at 2s, complete coverage after 10s, dry land / water mask, unchanged canonical trees / terrain heights, gradual Clear taper / fog fade / ten-second melt, overview cleanup and Tropical winter rain-only. Original region restored. User save files never written.");
} finally {if(File.Exists(temp))File.Delete(temp);}
}
}
}
#endif
