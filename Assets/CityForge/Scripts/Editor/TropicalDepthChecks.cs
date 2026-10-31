#if UNITY_EDITOR
using System;using System.IO;using System.Linq;using System.Collections;
using UnityEngine;using CityForge.World;using CityForge.UI;
namespace CityForge.Editor {
public static class TropicalDepthChecks {
 static void Require(bool ok,string why){if(!ok)throw new Exception(why);}
 public static void Run(string output)=>EditorCoroutineRunner.Begin(Guard(output));
 static IEnumerator Guard(string output){Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"Status.txt"),"RUNNING");var check=Check(output);while(true){bool more;try{more=check.MoveNext();}catch(Exception e){File.WriteAllText(Path.Combine(output,"Status.txt"),"FAIL "+e);(check as IDisposable)?.Dispose();yield break;}if(!more)break;yield return null;}File.WriteAllText(Path.Combine(output,"Status.txt"),"PASS");}
 static IEnumerator Check(string output){
  var recipe=new RegionCreationSettings{seed=730421,climate=RegionClimate.Tropical,terrainShape=RegionTerrainShape.Coast};
  var flat=new float[513,513];var current=RegionMapGenerator.Generate("Tropical Shelf Preview",RegionSize.Small,recipe,flat);
  recipe.version=11;var old=RegionMapGenerator.Generate("Previous shelf",RegionSize.Small,recipe,flat);recipe.version=12;
  var layout=new RegionTerrainLayout(recipe);float maxDepth=0;int landSamples=0;
  for(int z=0;z<513;z+=4)for(int x=0;x<513;x+=4){float shore=layout.LandDistance(new Vector2(x/512f,z/512f));float h=current.data.Heights[z,x]*1200;
   Require(h>=0&&h<=1200,"Save-safe heights");if(shore>=0){Require(Mathf.Abs(h-old.data.Heights[z,x]*1200-(TropicalOceanDepth.Sea-12))<.001f,"Relative land relief retained");landSamples++;}else maxDepth=Mathf.Max(maxDepth,TropicalOceanDepth.Sea-h);
  }
  Require(landSamples>0&&maxDepth>TropicalOceanDepth.MaximumDepth-.1f,"Actual deep seabed");Require(current.info.waterBodies.All(b=>Mathf.Abs(b.surfaceLevelNormalized*1200-TropicalOceanDepth.Sea)<.001f),"Consistent water datum");
  recipe.terrainShape=RegionTerrainShape.Land;var land12=RegionMapGenerator.Generate("Land",RegionSize.Small,recipe,flat);recipe.version=11;var land11=RegionMapGenerator.Generate("Land legacy",RegionSize.Small,recipe,flat);Require(land12.data.Heights.Cast<float>().SequenceEqual(land11.data.Heights.Cast<float>()),"Inland unchanged");
  string temp=Path.Combine(Application.temporaryCachePath,"tropical-depth-"+Guid.NewGuid().ToString("N")+".cfregion");
  try{
   SavedRegionStore.Write(current,temp);var loaded=SavedRegionStore.Load(temp);Require(loaded.info.creation.version==12&&loaded.data.Heights.Cast<float>().SequenceEqual(current.data.Heights.Cast<float>()),"Authoritative field round trip");
   var oldView=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(oldView)oldView.ReturnToMenu();yield return null;yield return null;
   var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();menu.OpenSavedRegion(loaded.info);double deadline=UnityEditor.EditorApplication.timeSinceStartup+240;while(menu.IsOpeningRegion&&UnityEditor.EditorApplication.timeSinceStartup<deadline)yield return null;
   var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(view&&!menu.IsOpeningRegion,"Normal saved-region open");var rig=view.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>();rig.SetHeading(45);
   yield return null;yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,"Tropical-Region.png"));yield return null;yield return null;
   int[] chosen=null;float best=float.PositiveInfinity;
   for(int z=0;z<7;z++)for(int x=0;x<7;x++){int[] cells={z*8+x,z*8+x+1,(z+1)*8+x,(z+1)*8+x+1};float wet=cells.Average(i=>view.Districts[i].waterFraction);float score=Mathf.Abs(wet-.5f);if(score<best){best=score;chosen=cells;}}
   view.CombineDistrictCells(chosen);view.RequestDistrict(chosen[0]);deadline=UnityEditor.EditorApplication.timeSinceStartup+240;while(view.DistrictLoading&&UnityEditor.EditorApplication.timeSinceStartup<deadline)yield return null;
   var native=view.ApprovedDistrict;Require(native&&native.Water,"Native ocean");Require(native.GetComponentInChildren<WaveHarmonic.Crest.ScatteringLodInput>(),"Depth colors registered with Crest");
   var slab=native.transform.Find("District earth slab").GetComponent<MeshFilter>().sharedMesh;Require(slab.vertexCount==8*64*4,"No interior slab seams in four-cell group");
   float until=Time.time+2;while(Time.time<until)yield return null;
   foreach(float heading in new[]{45f,135f,225f,315f}){rig.SetHeading(heading);until=Time.time+.7f;while(Time.time<until)yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,"Tropical-District-"+heading+".png"));yield return null;yield return null;}
   rig.SetHeading(45);
   File.WriteAllText(Path.Combine(output,"Checks.txt"),"PASS 24m maximum physical depth; unchanged relative land relief; consistent water levels; unchanged inland generation; version12 authoritative heightfield round trip; normal saved-region open; real Crest scattering input; four-cell slab suppresses interior seams. Screenshots require visual review. Temporary fixture only; no user saves written.");
  }finally{if(File.Exists(temp))File.Delete(temp);}
 }
}
}
#endif
