using System;
using System.Collections;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using CityForge.CameraSystem;

namespace CityForge.World
{
    /// <summary>Opt-in player harness, active only with --landscape-pass1. No normal-game work.</summary>
    public sealed class LandscapePhaseOneBenchmark : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartWhenRequested()
        {
            if(Application.isEditor || !(Array.Exists(System.Environment.GetCommandLineArgs(),a=>a=="--landscape-pass1") || System.Environment.GetEnvironmentVariable("CITYFORGE_LANDSCAPE_BENCHMARK")=="1")) return;
            Application.runInBackground=true;
            Debug.Log("Landscape benchmark enabled.");
            new GameObject("Landscape benchmark harness").AddComponent<LandscapePhaseOneBenchmark>();
        }
        IEnumerator Start()
        {
            var output="/Users/joelinstrum/Documents/Codex/2026-10-11/be/outputs";
            Directory.CreateDirectory(output);
            QualitySettings.vSyncCount=0; Application.targetFrameRate=-1;
            yield return null;
            var build=System.Diagnostics.Stopwatch.StartNew();
            PrototypeRegionView.Open("Landscape Player Benchmark",FindFirstObjectByType<Canvas>(),size:RegionSize.Small);
            var view=FindFirstObjectByType<PrototypeRegionView>();
            if(view==null){File.WriteAllText(Path.Combine(output,"Meadow-Performance-Two-Player.txt"),"FAILED: region missing");Application.Quit(1);yield break;}
            view.EnterDistrict(3);build.Stop();
            long freshMilliseconds=build.ElapsedMilliseconds;
            var freshStages=view.ApprovedDistrict.GenerationTimings;
            var snapshot=SavedRegionStore.Capture(view,"benchmark-region");var cachePath=Path.Combine(output,"Meadow-Benchmark-Region.cfregion");SavedRegionStore.Write(snapshot,cachePath);
            view.ReturnToMenu();yield return null;
            var loadTimer=System.Diagnostics.Stopwatch.StartNew();var loaded=SavedRegionStore.Load(cachePath);
            PrototypeRegionView.Open(loaded.info.name,FindFirstObjectByType<Canvas>(),snapshot:loaded);view=FindFirstObjectByType<PrototypeRegionView>();loadTimer.Stop();
            if(!view.ApprovedDistrict.UsedSavedTerrain)throw new Exception("Benchmark did not use saved meadow terrain.");
            File.Delete(cachePath);
            var rig=view.GetComponentInChildren<CityForgeCameraController>();
            var result=new StringBuilder("Development macOS player, "+SystemInfo.deviceModel+", "+SystemInfo.graphicsDeviceName+"; resolution "+Screen.width+"x"+Screen.height+"\nGeneration + district entry: "+freshMilliseconds+" ms\nSaved-region load + entry: "+loadTimer.ElapsedMilliseconds+" ms\nFresh generation stages: "+freshStages+"\n");
            using(var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count"))
            using(var cpu=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread"))
            {
                for(var phase=0;phase<11;phase++)
                {
                    if(phase>=5)view.EnterDistrict(3);
                    view.SetNaturalLandscape(phase!=1&&phase!=3);
                    rig.SetZoomLevel(phase<2||phase>=5?1:4);
                    var pilot=view.ApprovedDistrict;pilot.SetSmallPlantsEnabled(true);var cover=pilot.GetComponentInChildren<MeadowRuntimeVegetation>(true);
                    var ground=pilot.GetComponentsInChildren<MeadowGroundCoverRenderer>();
                    foreach(var renderer in ground)renderer.enabled=true;
                    pilot.DisplayTerrain.drawTreesAndFoliage=true;pilot.Water.gameObject.SetActive(true);pilot.Water.Reflections.Enabled=true;
                    yield return null;
                    while(phase!=1&&phase!=3&&cover.PendingTiles>0)yield return null;
                    if(phase==6)foreach(var renderer in pilot.GetComponentsInChildren<MeadowGroundCoverRenderer>())renderer.enabled=false;
                    if(phase==7)pilot.DisplayTerrain.drawTreesAndFoliage=false;
                    if(phase==8)pilot.Water.gameObject.SetActive(false);
                    if(phase==9)pilot.Water.Reflections.Enabled=false;
                    if(phase==10)Application.targetFrameRate=60;
                    yield return new WaitForSecondsRealtime(3);
                    int frames=0,drawFrames=0,cpuFrames=0,gpuFrames=0;
                    double elapsed=0,drawTotal=0,cpuTotal=0,gpuTotal=0;
                    var timings=new FrameTiming[1];var start=Time.realtimeSinceStartup;
                    var frameSamples=new System.Collections.Generic.List<float>();
                    var startPosition=rig.transform.position;int tilesBefore=cover.BuiltTiles;
                    var allocatedBefore=Profiler.GetTotalAllocatedMemoryLong();
                    while(Time.realtimeSinceStartup-start<5)
                    {
                        if(phase==4&&frames%90==0) {view.ShowOverview();view.EnterDistrict((frames/90)%4);rig.SetZoomLevel(2);}
                        if(phase==5){float seconds=Time.realtimeSinceStartup-start;rig.transform.position=startPosition+new Vector3(Mathf.Sin(seconds*1.2f)*80,0,Mathf.Sin(seconds*.7f)*64);rig.enabled=false;}
                        FrameTimingManager.CaptureFrameTimings();
                        yield return null;
                        frames++;elapsed+=Time.unscaledDeltaTime;frameSamples.Add(Time.unscaledDeltaTime*1000);
                        if(draws.Valid){drawTotal+=draws.LastValue;drawFrames++;}
                        if(cpu.Valid){cpuTotal+=cpu.LastValue/1e6;cpuFrames++;}
                        if(FrameTimingManager.GetLatestTimings(1,timings)>0&&timings[0].gpuFrameTime>0){gpuTotal+=timings[0].gpuFrameTime;gpuFrames++;}
                    }
                    if(phase==5){rig.enabled=true;rig.ResetView();}
                    frameSamples.Sort();float p95=frameSamples[Mathf.Min(frameSamples.Count-1,(int)(frameSamples.Count*.95f))];
                    string Metric(double sum,int count)=>count>0?(sum/count).ToString("F2"):"unavailable";
                    result.AppendLine(new[]{"Native meadow close","Legacy close","Native meadow distant","Legacy distant","District changes","Native close panning","Diagnostic close without ground cover","Diagnostic close without terrain trees","Diagnostic close without Crest","Diagnostic close without planar reflections","Native close capped at 60"}[phase]+": wall-frame="+(elapsed*1000/frames).ToString("F2")+" ms; main-thread="+Metric(cpuTotal,cpuFrames)+" ms; GPU="+Metric(gpuTotal,gpuFrames)+" ms; draws="+Metric(drawTotal,drawFrames)+"; allocated="+Profiler.GetTotalAllocatedMemoryLong()+" bytes; allocation delta="+(Profiler.GetTotalAllocatedMemoryLong()-allocatedBefore)+" bytes; reserved="+Profiler.GetTotalReservedMemoryLong()+" bytes; p95="+p95.ToString("F2")+" ms; tiles built="+(cover.BuiltTiles-tilesBefore)+"; tile build max="+cover.MaximumTileBuildMilliseconds.ToString("F2")+" ms; streaming slice max="+cover.MaximumStreamingSliceMilliseconds.ToString("F2")+" ms");
                }
            }
            result.AppendLine("Native generation stages: "+view.ApprovedDistrict.GenerationTimings);
            result.AppendLine("Natural mode uses the approved Crest planar reflection configuration; reflection cost is included in the overall frame metrics and is not isolated. GPU timing may be unavailable on this Metal setup. Development-player measurements include existing vegetation; no production budget claim. Camera district changes exercise existing tile cleanup; bounded long-run streaming remains Pass 2/3 validation.");
            File.WriteAllText(Path.Combine(output,"Meadow-Performance-Two-Player.txt"),result.ToString());
            Debug.Log(result.ToString());Application.Quit();
        }
    }
}
