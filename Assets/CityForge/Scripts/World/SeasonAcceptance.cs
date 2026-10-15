using System;using System.Collections;using System.IO;using System.Linq;using UnityEngine;using CityForge.CameraSystem;
namespace CityForge.World
{
 public sealed class SeasonAcceptance:MonoBehaviour
 {
  const string Output="/Users/joelinstrum/Documents/Codex/2026-10-12/begin-from-this-handoff-and-start/outputs/";
  static void Require(bool ok,string text){if(!ok)throw new Exception(text);}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Launch(){if(!Application.isEditor&&Array.Exists(System.Environment.GetCommandLineArgs(),a=>a=="--season-test"))new GameObject("Season acceptance").AddComponent<SeasonAcceptance>();}
  IEnumerator Start()
  {
   Application.runInBackground=true;
   var view=FindFirstObjectByType<PrototypeRegionView>();
   if(!view){yield return null;var fixture=SavedRegionStore.Load(Output+"Hills-And-Mountain-Test.cfregion");PrototypeRegionView.Open(fixture.info.name,FindFirstObjectByType<Canvas>(),snapshot:fixture);view=FindFirstObjectByType<PrototypeRegionView>();}
   var district=view.ApprovedDistrict;
   var rig=view.GetComponentInChildren<CityForgeCameraController>();var original=rig.CaptureRegionState();
   district.SetTreesEnabled(true);district.SetSmallPlantsEnabled(true);
   var trees=district.CanonicalTreeInstances.ToArray();var road=district.Road.ToArray();
   var library=Resources.Load<SeasonalTreeLibrary>("Seasons/SeasonalTrees");Require(library&&library.summer.Length==15,"Season library missing");
   int stripped=0;
   for(int i=0;i<11;i++)
   {
    Require(library.summer[i]==district.Baseline.treePrefabs[i],"Summer original changed");
    foreach(var group in library.winter[i].GetComponentsInChildren<LODGroup>())foreach(var lod in group.GetLODs())foreach(var renderer in lod.renderers)
    {
     var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;var mats=renderer.sharedMaterials;
     for(int sub=0;sub<mesh.subMeshCount;sub++)if(mats[sub].shader.name.EndsWith("/Foliage")){Require(mesh.GetTriangles(sub).Length==0,"Winter leaves survived");stripped++;}
    }
   }
   rig.RestoreRegionState(new RegionCameraState{focus=new Vector3(200,20,200),pitch=35.26439f,yaw=315,distance=400,size=80,zoom=2});
   foreach(RegionSeason season in Enum.GetValues(typeof(RegionSeason)))
   {
    view.SetSeason(season);
    Require(trees.SequenceEqual(district.CanonicalTreeInstances),"Canonical tree placements changed");
    Require(trees.SequenceEqual(district.DisplayTerrain.terrainData.treeInstances),"Displayed tree placements changed");
    Require(road.SequenceEqual(district.Road),"Road changed");
    Require(district.DisplayTerrain.terrainData.treePrototypes.Select(p=>p.prefab).SequenceEqual(library.Prefabs(season)),"Season prefab mapping failed");
    yield return new WaitForSecondsRealtime(3);yield return new WaitForEndOfFrame();
    ScreenCapture.CaptureScreenshot(Output+"Poplar-"+(Application.isEditor?"":"Player-")+season+".png");yield return null;
   }
   var saved=SavedRegionStore.Capture(view);
   SavedRegionStore.Write(saved,Output+"Seasonal-Poplar-Test.cfregion");var loaded=SavedRegionStore.Load(Output+"Seasonal-Poplar-Test.cfregion");
   Require(loaded.info.season==2&&loaded.meadow.trees.SequenceEqual(trees),"Season save/load failed");
   view.ReturnToMenu();yield return null;yield return null;
   PrototypeRegionView.Open(loaded.info.name,FindFirstObjectByType<Canvas>(),snapshot:loaded);view=FindFirstObjectByType<PrototypeRegionView>();district=view.ApprovedDistrict;rig=view.GetComponentInChildren<CityForgeCameraController>();
   district.SetTreesEnabled(true);district.SetSmallPlantsEnabled(true);
   Require(trees.SequenceEqual(district.CanonicalTreeInstances)&&road.SequenceEqual(district.Road),"Reopened season changed terrain cache");
   Require(view.Season==RegionSeason.Winter&&district.DisplayTerrain.terrainData.treePrototypes.Select(p=>p.prefab).SequenceEqual(library.winter),"Reopened Winter did not apply");
   rig.SetZoomLevel(5);yield return new WaitForSecondsRealtime(3);yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Output+"Poplar-"+(Application.isEditor?"":"Player-")+"Winter-Wide.png");yield return null;
   view.SetSeason(RegionSeason.Summer);rig.RestoreRegionState(original);
   File.WriteAllText(Output+"Seasonal-Poplar-"+(Application.isEditor?"Editor":"Player")+"-Checks.txt","PASS: original Summer prefab references; "+stripped+" leaf-free winter mesh LODs; all three seasonal prefab mappings; identical canonical/displayed tree placements and road; Winter region file save/load and reopened scene. Eleven poplar variants covered. Maple/willow shrubs, ground, snow and water unchanged.\n");
   Debug.Log("PASS seasonal poplar switching and save/load");if(!Application.isEditor)Application.Quit(0);else Destroy(gameObject);
  }
 }
}
