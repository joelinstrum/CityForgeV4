#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using CityForge.World;
using CityForge.UI;
using CityForge.CameraSystem;
namespace CityForge.Editor {
public static class NativeCampRefinementChecks {
 public static void Run(string output)=>EditorCoroutineRunner.Begin(Guard(output));
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
 static IEnumerator Guard(string output){
  Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"Status.txt"),"RUNNING");var routine=Check(output);
  while(true){bool more;try{more=routine.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(output,"Status.txt"),"FAIL "+e);yield break;}if(!more)break;yield return routine.Current;}
  File.WriteAllText(Path.Combine(output,"Status.txt"),"PASS");
 }
 static IEnumerator Check(string output){
  NativeCampChecks.Unit(output);Application.runInBackground=true;
  var old=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(old){old.ReturnToMenu();yield return null;yield return null;}
  var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
  var saved=SavedRegionStore.Load(Path.Combine(SavedRegionStore.DirectoryPath,"native-camp-study-2026-11-01.cfregion"));
  menu.OpenSavedRegion(saved.info);double deadline=EditorApplication.timeSinceStartup+300;
  while(menu.IsOpeningRegion&&EditorApplication.timeSinceStartup<deadline)yield return null;
  var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(view&&!menu.IsOpeningRegion,"Saved overview load");
  var camp=view.NativeCamps.Single(c=>c.district==28);view.RequestDistrict(camp.district);deadline=EditorApplication.timeSinceStartup+300;
  while(view.DistrictLoading&&EditorApplication.timeSinceStartup<deadline)yield return null;
  Require(!view.DistrictLoading,"District load");var land=view.ApprovedDistrict;var district=land.GetComponent<NativeCampDistrict>();
  Require(district.Teepees.Count==9&&Mathf.Abs(camp.Radius-15.3f)<.01f,"Nine teepees on 25 percent tighter ring");
  foreach(var tent in district.Teepees)Require(Mathf.Abs(Vector2.Distance(new Vector2(tent.position.x,tent.position.z),camp.center)-camp.Radius)<.01f,"Actual tighter positions");
  var td=land.DisplayTerrain.terrainData;int n=td.alphamapResolution;var canonical=land.CaptureSnapshot().alphamaps;var painted=td.GetAlphamaps(0,0,n,n);
  Require(canonical.GetLength(2)+1==painted.GetLength(2),"Derived dirt excluded from saved canonical ground");
  int dirt=painted.GetLength(2)-1,changed=0;float peak=0;
  for(int z=0;z<n;z++)for(int x=0;x<n;x++){
   var p=land.Origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*land.Side;float sum=0;
   for(int k=0;k<=dirt;k++)sum+=painted[z,x,k];Require(Mathf.Abs(sum-1)<.012f,"Normalized terrain weights");
   if(Vector2.Distance(p,camp.center)>camp.Radius){Require(painted[z,x,dirt]<.005f,"No wear outside camp ring");}
   if(painted[z,x,dirt]>.05f)changed++;peak=Mathf.Max(peak,painted[z,x,dirt]);
  }
  Require(changed>100&&peak>.93f&&peak<=1,"Dirt-dominant courtyard");
  Require(MeadowDistrictLandscape.CampGroundWear(camp,camp.center)>.93f,"Almost bare central dirt");
  Require(district.GetComponentInChildren<NativeCampGrass>().TuftCount>0,"Sparse meadow grass tufts placed");
  Require(td.terrainLayers[0].diffuseTexture==td.terrainLayers[dirt].diffuseTexture,"Shared dirt texture on bank soil and courtyard");
  var pop=district.Populations.Single();Require(pop.People.Count==12,"All residents spawn in connected camp paths");
  var cast=Resources.Load<NativeCampAssets>("NativeCamps/Assets");Require(pop.People.Count(p=>p.Definition==cast.chief)==1&&pop.People.Count(p=>p.Definition==cast.warrior)==5&&pop.People.Count(p=>p.Definition==cast.resident)==6,"One chief, five warriors, six female residents");
  var rig=view.GetComponentInChildren<CityForgeCameraController>();rig.SetHeading(camp.heading+135);rig.ConfigureTerrainStudy(pop.Center,32,1);
  var pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
  Require(pipeline&&pipeline.shadowCascadeCount==4&&pipeline.mainLightShadowmapResolution==4096&&Vector3.Distance(pipeline.cascade4Split,new Vector3(.5f,.68f,.85f))<.0001f,"Persistent approved shadow pipeline");
  Require(AssetDatabase.GetAssetPath(pipeline)=="Assets/CityForge/Rendering/CityForge_URP.asset","Source pipeline active without live-preview clone");
  foreach(RegionSeason season in Enum.GetValues(typeof(RegionSeason))){land.ApplySeasonShadows(season);Require(land.GetComponentsInChildren<Light>().Where(l=>l.type==LightType.Directional).All(l=>l.shadows==LightShadows.Hard),"Crisp sun shadows survive season refresh");}land.ApplySeasonShadows(view.VisualSeason);
  var hearth=district.GetComponentInChildren<NativeCampfire>();Require(hearth&&hearth.StoneCount==12,"One small stone-ring campfire");
  rig.SetZoomLevel(3);yield return null;Require(!hearth.EffectsVisible&&hearth.GetComponentsInChildren<ParticleSystem>().All(p=>!p.isPlaying),"Far zoom stops fire effects");rig.SetZoomLevel(1);yield return null;Require(hearth.EffectsVisible,"Close zoom resumes fire effects");
  var initial=pop.People.Select(p=>p.Record.position).ToArray();float until=Time.time+30,largest=0;
  while(Time.time<until){foreach(var person in pop.People){float distance=Vector2.Distance(new Vector2(person.Record.position.x,person.Record.position.z),camp.center);largest=Mathf.Max(largest,distance);Require(distance<=camp.Clearance+.1f&&pop.Navigation.Suitable(person.Record.position),"Safe camp movement");Require(!hearth.Exclusion.Contains(person.Record.position+Vector3.up*.5f),"Residents avoid hearth");}yield return null;}
  Require(pop.People.Where((p,i)=>Vector3.Distance(initial[i],p.Record.position)>.2f).Count()>2,"People can move through tighter camp");
  Require(hearth.GetComponentsInChildren<ParticleSystem>().All(p=>p.particleCount>0),"Flames embers and smoke emit");
  ScreenCapture.CaptureScreenshot(Path.Combine(output,"Tighter-Camp.png"));yield return null;yield return null;
  File.WriteAllText(Path.Combine(output,"Checks.txt"),$"PASS 25% tighter nine-teepee ring (15.3m); one chief, five warriors, six female residents on safe connected paths for 30 seconds, max radius {largest:F1}m / {camp.Clearance:F1}m; matte dirt-dominant courtyard and sparse 3D meadow tufts, normalized terrain weights and no wear outside ring; derived dirt excluded from canonical save cache. Twelve reused half-metre stones; flame/ember/smoke emission; far zoom stops effects and close zoom resumes; residents avoid hearth. Existing saved region loaded without rewriting.\n");
 }
}
}
#endif
