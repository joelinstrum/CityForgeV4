using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using UnityEngine;
using CityForge.CameraSystem;
namespace CityForge.World
{
    public sealed class MeadowPlantExperimentBenchmark:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartRequested(){if(!Application.isEditor&&System.Environment.GetEnvironmentVariable("CITYFORGE_PLANT_EXPERIMENT_BENCHMARK")=="1"){Application.runInBackground=true;new GameObject("Small plant comparison").AddComponent<MeadowPlantExperimentBenchmark>();}}
        IEnumerator Start()
        {
            const string output="/Users/joelinstrum/Documents/Codex/2026-10-11/be/outputs/";
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;yield return null;
            var saved=SavedRegionStore.Load(SavedRegionStore.ListRegions().First(x=>x.id==SavedRegionStore.TestId).path);
            PrototypeRegionView.Open(saved.info.name,FindFirstObjectByType<Canvas>(),snapshot:saved);
            var view=FindFirstObjectByType<PrototypeRegionView>();var pilot=view.ApprovedDistrict;pilot.SetTreesEnabled(true);var rig=view.GetComponentInChildren<CityForgeCameraController>();
            var text=new StringBuilder("Small plant A/B experiment, "+SystemInfo.deviceModel+", "+SystemInfo.graphicsDeviceName+", "+Screen.width+"x"+Screen.height+"; uncapped development player. Same saved region, camera, terrain and Crest.\n");
            using(var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count"))
            for(int phase=0;phase<5;phase++){
                bool enabled=phase==0||phase==2;int level=phase<2?0:1;
                pilot.SetSmallPlantsEnabled(enabled);rig.SetZoomLevel(level);yield return null;
                var cover=pilot.GetComponentInChildren<MeadowRuntimeVegetation>(true);
                while(enabled&&cover.PendingTiles>0)yield return null;
                yield return new WaitForSecondsRealtime(3);
                if(phase<4)ScreenCapture.CaptureScreenshot(output+"Meadow-Plants-"+(enabled?"On":"Off")+"-Zoom"+level+".png");
                yield return null;
                var frames=new System.Collections.Generic.List<float>();double drawSum=0;int drawFrames=0;float elapsed=0;var start=Time.realtimeSinceStartup;var position=rig.transform.position;
                var timings=new FrameTiming[1];double gpu=0;int gpuFrames=0;
                while(Time.realtimeSinceStartup-start<6){
                    if(phase==4){rig.enabled=false;float t=Time.realtimeSinceStartup-start;rig.transform.position=position+new Vector3(Mathf.Sin(t)*80,0,Mathf.Sin(t*.7f)*64);}
                    FrameTimingManager.CaptureFrameTimings();yield return null;frames.Add(Time.unscaledDeltaTime*1000);elapsed+=Time.unscaledDeltaTime;
                    if(draws.Valid){drawSum+=draws.LastValue;drawFrames++;}
                    if(FrameTimingManager.GetLatestTimings(1,timings)>0&&timings[0].gpuFrameTime>0){gpu+=timings[0].gpuFrameTime;gpuFrames++;}
                }
                if(phase==4){rig.enabled=true;rig.ResetView();}
                frames.Sort();text.AppendLine("Zoom "+level+", plants "+(enabled?"ON":"OFF")+(phase==4?", panning":"")+": mean="+(elapsed*1000/frames.Count).ToString("F2")+" ms; p95="+frames[Mathf.Min(frames.Count-1,(int)(frames.Count*.95))].ToString("F2")+" ms; draws="+(drawFrames>0?(drawSum/drawFrames).ToString("F0"):"unavailable")+"; GPU="+(gpuFrames>0?(gpu/gpuFrames).ToString("F2"):"unavailable")+" ms; rendered tree instances="+pilot.DisplayTerrain.terrainData.treeInstanceCount+"; canonical trees="+pilot.FullTreeCount+"; ground cover active="+(cover&&cover.gameObject.activeInHierarchy));
            }
            text.AppendLine("Includes grass/flowers/clover, leaf piles, shoreline reeds/grass, juvenile poplars (0-2), maple bushes/small willows (11-14). Mature poplars (3-10), terrain textures/blending, road, lighting and Crest unchanged. World/save data retains full original planting. No normal-game benchmark work.");
            File.WriteAllText(output+"Meadow-Small-Plant-Experiment-Player.txt",text.ToString());Application.Quit();
        }
    }
}
