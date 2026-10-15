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
    public sealed class MeadowTreePanningBenchmark:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartRequested(){if(!Application.isEditor&&System.Environment.GetEnvironmentVariable("CITYFORGE_TREE_PANNING_BENCHMARK")=="1"){Application.runInBackground=true;new GameObject("Tree panning comparison").AddComponent<MeadowTreePanningBenchmark>();}}
        IEnumerator Start()
        {
            const string output="/Users/joelinstrum/Documents/Codex/2026-10-11/be/outputs/";
            QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;yield return null;
            var saved=SavedRegionStore.Load(SavedRegionStore.ListRegions().First(x=>x.id==SavedRegionStore.TestId).path);
            PrototypeRegionView.Open(saved.info.name,FindFirstObjectByType<Canvas>(),snapshot:saved);
            var view=FindFirstObjectByType<PrototypeRegionView>();var pilot=view.ApprovedDistrict;var rig=view.GetComponentInChildren<CityForgeCameraController>();
            for(int z=0;z<=5;z++){
                view.transform.Find("RegionHud/ZoomLevel"+z).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                if(rig.ZoomLevel!=z||rig.PanSpeedMultiplier!=(z<=1?1.69f:1f))throw new Exception("Zoom/pan validation failed");
            }
            var original=pilot.CanonicalTreeInstances;
            if(!pilot.FramePacing60||Application.targetFrameRate!=60||QualitySettings.vSyncCount!=0)throw new Exception("Default pacing failed");
            view.transform.Find("RegionHud/FramePacing").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            if(Application.targetFrameRate!=-1)throw new Exception("Uncapped HUD failed");
            view.transform.Find("RegionHud/FramePacing").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            if(Application.targetFrameRate!=60)throw new Exception("60 FPS HUD failed");
            pilot.ApplyPresentation(false);
            if(Application.targetFrameRate!=-1||QualitySettings.vSyncCount!=0)throw new Exception("Pacing restore failed");
            pilot.ApplyPresentation(true);
            pilot.SetTreesEnabled(false);view.transform.Find("RegionHud/Trees").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            if(!pilot.DisplayTerrain.drawTreesAndFoliage)throw new Exception("Tree HUD ON failed");
            view.transform.Find("RegionHud/Trees").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            if(pilot.DisplayTerrain.drawTreesAndFoliage||!ReferenceEquals(original,pilot.CanonicalTreeInstances))throw new Exception("Tree HUD OFF/canonical validation failed");
            var text=new StringBuilder("Tree/panning A/B experiment, "+SystemInfo.deviceModel+", "+SystemInfo.graphicsDeviceName+", "+Screen.width+"x"+Screen.height+"; uncapped development player. Same saved region, camera, terrain and Crest.\n");
            bool smoke=System.Environment.GetEnvironmentVariable("CITYFORGE_TREE_PANNING_SMOKE")=="1";
            using(var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count"))
            for(int phase=smoke?4:0;phase<(smoke?6:8);phase++){
                int level=1;bool trees=phase%2==0;bool panning=phase>=2;
                Application.targetFrameRate=phase>=4&&phase<6?60:-1;
                pilot.Water.gameObject.SetActive(phase<6);
                pilot.SetSmallPlantsEnabled(false);pilot.SetTreesEnabled(trees);rig.ResetView();rig.SetZoomLevel(level);yield return null;
                var cover=pilot.GetComponentInChildren<MeadowRuntimeVegetation>(true);
                yield return new WaitForSecondsRealtime(3);
                var frames=new System.Collections.Generic.List<float>();double drawSum=0;int drawFrames=0;float elapsed=0;var start=Time.realtimeSinceStartup;var position=rig.transform.position;
                var timings=new FrameTiming[1];double gpu=0;int gpuFrames=0;
                while(Time.realtimeSinceStartup-start<6){
                    if(panning){rig.enabled=false;float t=Time.realtimeSinceStartup-start;rig.transform.position=position+new Vector3(Mathf.Sin(t)*80,0,Mathf.Sin(t*.7f)*64);}
                    FrameTimingManager.CaptureFrameTimings();yield return null;frames.Add(Time.unscaledDeltaTime*1000);elapsed+=Time.unscaledDeltaTime;
                    if(draws.Valid){drawSum+=draws.LastValue;drawFrames++;}
                    if(FrameTimingManager.GetLatestTimings(1,timings)>0&&timings[0].gpuFrameTime>0){gpu+=timings[0].gpuFrameTime;gpuFrames++;}
                }
                if(panning){rig.enabled=true;rig.ResetView();}
                frames.Sort();text.AppendLine("Zoom "+level+", trees "+(trees?"ON":"OFF")+", cap="+Application.targetFrameRate+", Crest="+(phase<6?"ON":"OFF")+(panning?", panning":" stationary")+": mean="+(elapsed*1000/frames.Count).ToString("F2")+" ms; p95="+frames[Mathf.Min(frames.Count-1,(int)(frames.Count*.95))].ToString("F2")+" ms; p99="+frames[Mathf.Min(frames.Count-1,(int)(frames.Count*.99))].ToString("F2")+" ms; frames >25ms="+(100f*frames.Count(f=>f>25)/frames.Count).ToString("F1")+"%; draws="+(drawFrames>0?(drawSum/drawFrames).ToString("F0"):"unavailable")+"; GPU="+(gpuFrames>0?(gpu/gpuFrames).ToString("F2"):"unavailable")+" ms; stored display tree instances="+pilot.DisplayTerrain.terrainData.treeInstanceCount+"; canonical trees="+pilot.FullTreeCount+"; ground cover active="+(cover&&cover.gameObject.activeInHierarchy));
            }
            text.AppendLine("PASS actual zoom HUD levels 0-5 and pan multipliers 1.69 at 0/1; tree HUD ON/OFF; canonical planting retained; 60 FPS/uncapped HUD and fallback settings restoration.");
            text.AppendLine("Small plants OFF throughout; identical saved region and camera path. Tree visibility does not change canonical planting, terrain paint or save data. Water OFF only in final diagnostic phases. Cap only in benchmark phases 4/5. Normal meadow defaults to reversible 60 FPS pacing; prior settings restored on leaving meadow.");
            File.WriteAllText(output+(smoke?"Meadow-Tree-Pacing-Validation.txt":"Meadow-Tree-Panning-Player.txt"),text.ToString());Application.Quit();
        }
    }
}
