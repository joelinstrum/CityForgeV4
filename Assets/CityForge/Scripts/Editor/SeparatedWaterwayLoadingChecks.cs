#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using CityForge.World;
using CityForge.UI;
namespace CityForge.Editor
{
    // Explicit request only. Does not run when importing or entering Play mode.
    [InitializeOnLoad] public static class SeparatedWaterwayLoadingChecks
    {
        const string Request="Library/SeparatedWaterwayLoading.request";
        static SeparatedWaterwayLoadingChecks(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Request)||!Application.isPlaying)return;
            string output=File.ReadAllText(Request).Trim();File.Delete(Request);
            EditorCoroutineRunner.Begin(Run(output));
        }
        static IEnumerator Run(string output)
        {
            Directory.CreateDirectory(output);string status=Path.Combine(output,"Loading-Check.txt");
            var previous=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            if(previous&&(previous.RegionName=="Mountain waterway verification"||previous.RegionName=="Stream loading verification")){
                EditorApplication.isPaused=false;previous.ReturnToMenu();yield return null;yield return null;
            }else if(previous){File.WriteAllText(status,"SKIPPED: an existing region is open.");yield break;}
            // Start from the disk-backed source baseline after iterative Play-mode
            // checks; owned district clones have already been destroyed above.
            var sourceBaseline=Resources.Load<MeadowRuntimeBaseline>("Region/MeadowRuntimeBaseline");
            if(sourceBaseline)Resources.UnloadAsset(sourceBaseline);
            var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            if(!menu||menu.IsOpeningRegion){File.WriteAllText(status,"SKIPPED: main menu unavailable.");yield break;}
            EditorApplication.isPaused=false;
            var timer=System.Diagnostics.Stopwatch.StartNew();
            File.WriteAllText(status,"RUNNING: no major rivers, many streams, mountain border.");
            menu.CreatePrototypeRegion("Mountain waterway verification",RegionSize.Small,new RegionCreationSettings{seed=8472,waterwayVersion=5,rivers=0,streams=2,terrainShape=RegionTerrainShape.MountainBorder});
            while(menu&&menu.IsOpeningRegion){if(timer.Elapsed.TotalSeconds>90){File.WriteAllText(status,"FAIL: loading exceeded90 seconds.");yield break;}yield return null;}
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            if(!view||view.SelectedDistrict!=-1||!view.GetComponentInChildren<RegionReliefMap>()){File.WriteAllText(status,"FAIL: overview did not finish.");yield break;}
            int streams=0;foreach(var body in view.Data.WaterBodies){if(body.isStream&&body.channelAlongX&&body.Bounds.yMax+body.bankWidthNormalized<.70f&&body.halfWidthNormalized*view.MapSide*2<=6.0961f)streams++;}
            bool mountainsUnchanged=true;
            for(int z=384;z<=512;z+=4)for(int x=0;x<=512;x+=4){float u=x/512f,v=z/512f;if(view.Data.SmoothHeight(u,v)!=SmoothRiverSurface.Sample(view.Data.Heights,u,v))mountainsUnchanged=false;}
            File.WriteAllText(status,(streams==5&&mountainsUnchanged?"PASS":"FAIL")+": full generation and overview completed in "+timer.Elapsed.TotalSeconds.ToString("F2")+" seconds; "+streams+" streams at most20 feet, parallel to range with clear banks; mountain heights unchanged; no major rivers. No save written.");
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"Stream-Overview.png"));
            if(output.EndsWith("flush-waterways",StringComparison.Ordinal)||output.EndsWith("pass17-generated-streams",StringComparison.Ordinal)||output.EndsWith("white-stream-streak",StringComparison.Ordinal)){
                yield return null;yield return null;
                var route=view.Data.WaterBodies[0].centerline;var p=route[route.Length/2];float best=-1;
                // Show a bend away from district slab edges for visual review.
                for(int i=route.Length/4;i<route.Length*3/4;i++){
                    var q=route[i];float x=Mathf.Repeat(q.x*view.GridSide,1),z=Mathf.Repeat(q.y*view.GridSide,1);
                    if(x<.18f||x>.82f||z<.18f||z>.82f)continue;
                    float turn=Vector2.Angle(route[i]-route[i-1],route[i+1]-route[i]);if(turn>best){best=turn;p=q;}
                }
                var world=new Vector3((p.x-.5f)*view.MapSide,0,(p.y-.5f)*view.MapSide);
                int district=view.DistrictAtWorld(world);bool done=false;Exception failure=null;
                var overlay=RegionLoadingOverlay.Show("Shallow channel verification",null,true);
                overlay.BeginDistrict(view.EnterDistrictStages(district,overlay.Report),()=>done=true,e=>{failure=e;done=true;},"Checking ground-following water");
                timer.Restart();while(!done){if(timer.Elapsed.TotalSeconds>150){File.AppendAllText(status,"\nFAIL: district timeout.");yield break;}yield return null;}
                var crest=view.GetComponentInChildren<CrestDistrictRiverPreview>();
                if(failure!=null||!crest||!crest.WaterEngine.LevelLod.Enabled){File.AppendAllText(status,"\nFAIL: local water level renderer not ready: "+failure);yield break;}
                var dressing=view.GetComponentInChildren<GeneratedStreamAppearance>();
                if(output.EndsWith("pass17-generated-streams",StringComparison.Ordinal)){
                    if(!dressing||dressing.PatchCount==0||dressing.StoneCount==0){File.AppendAllText(status,"\nFAIL: approved stream decoration missing.");yield break;}
                    File.AppendAllText(status,"\nPASS: Pass17 assets on generated route: "+dressing.PatchCount+" scanned patches, "+dressing.StoneCount+" individual rocks/pebbles, "+dressing.ShrubCount+" shrubs.");
                }
                float water=RegionalRiverNetwork.GroundWaterLevel(view.Data,p.x,p.y);
                float original=SmoothRiverSurface.Sample(view.Data.Heights,p.x,p.y)*PrototypeRegionData.Height;
                File.AppendAllText(status,"\nPASS: detailed district ready in "+timer.Elapsed.TotalSeconds.ToString("F2")+"s; Crest level input enabled; ground="+original+"m water="+water+"m.");
                var camera=view.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>();var state=camera.CaptureRegionState();
                state.focus=new Vector3(world.x,water,world.z);state.size=26;state.distance=200;state.zoom=1;camera.RestoreRegionState(state);
                for(int frame=0;frame<30;frame++)yield return null;
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"Stream-District.png"));
                if(output.EndsWith("white-stream-streak",StringComparison.Ordinal)){var diagnostics=StreamWhiteStreakChecks.Compare(view,output);while(diagnostics.MoveNext())yield return diagnostics.Current;}
                if(output.EndsWith("pass17-generated-streams",StringComparison.Ordinal)){
                    yield return null;
                    int patches=dressing.PatchCount,stones=dressing.StoneCount;var cached=view.ApprovedDistrict.CaptureSnapshot();int treeCount=cached.trees.Length;
                    // Simulate a cache from before the dedicated soil layer existed.
                    int n=cached.alphamaps.GetLength(0),layers=cached.alphamaps.GetLength(2);var old=new float[n,n,layers-1];
                    for(int z=0;z<n;z++)for(int x=0;x<n;x++){for(int l=0;l<layers-1;l++)old[z,x,l]=cached.alphamaps[z,x,l];old[z,x,0]+=cached.alphamaps[z,x,layers-1];}
                    view.ShowOverview();yield return null;yield return null;view.LoadedMeadow.alphamaps=old;
                    done=false;failure=null;overlay=RegionLoadingOverlay.Show("Stream cache verification",null,true);
                    overlay.BeginDistrict(view.EnterDistrictStages(district,overlay.Report),()=>done=true,e=>{failure=e;done=true;},"Restoring approved stream appearance");
                    timer.Restart();while(!done){if(timer.Elapsed.TotalSeconds>150){File.AppendAllText(status,"\nFAIL: cache reload timeout.");yield break;}yield return null;}
                    dressing=view.GetComponentInChildren<GeneratedStreamAppearance>();
                    bool valid=failure==null&&dressing&&dressing.PatchCount==patches&&dressing.StoneCount==stones&&dressing.ShrubCount==0&&view.ApprovedDistrict.UsedSavedTerrain&&view.ApprovedDistrict.CaptureSnapshot().trees.Length==treeCount;
                    var maps=view.ApprovedDistrict.DisplayTerrain.terrainData.GetAlphamaps(0,0,n,n);
                    // Terrain control textures quantize channels to8-bit; allow only
                    // the sum of each channel's half-LSB rounding error on readback.
                    float maximumError=0,tolerance=maps.GetLength(2)*.5f/255f+.00001f;
                    for(int z=0;z<n;z+=32)for(int x=0;x<n;x+=32){float sum=0;for(int l=0;l<maps.GetLength(2);l++){float w=maps[z,x,l];if(!float.IsFinite(w)||w<0||w>1)valid=false;sum+=w;}maximumError=Mathf.Max(maximumError,Mathf.Abs(sum-1));if(Mathf.Abs(sum-1)>tolerance)valid=false;}
                    File.AppendAllText(status,"\nStored weight sum max error="+maximumError+" (per-channel quantization bound="+tolerance+").");
                    File.AppendAllText(status,valid?"\nPASS: old-layer cache restored; deterministic rock counts; no duplicate shrubs; normalized bank weights.":"\nFAIL: cache/placement regression: used="+view.ApprovedDistrict.UsedSavedTerrain+" patches="+(dressing?dressing.PatchCount:-1)+"/"+patches+" stones="+(dressing?dressing.StoneCount:-1)+"/"+stones+" shrubsAdded="+(dressing?dressing.ShrubCount:-1)+" trees="+view.ApprovedDistrict.CaptureSnapshot().trees.Length+"/"+treeCount+" "+failure);
                    camera=view.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>();camera.RestoreRegionState(state);
                    for(int frame=0;frame<30;frame++)yield return null;
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"Stream-District-Reopened.png"));
                }

            }

        }
    }
}
#endif
