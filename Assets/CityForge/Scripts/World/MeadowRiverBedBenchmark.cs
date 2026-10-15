using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CityForge.CameraSystem;
using Unity.Profiling;
using UnityEngine;
namespace CityForge.World
{
    public sealed class MeadowRiverBedBenchmark:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Requested(){if(!Application.isEditor&&System.Environment.GetEnvironmentVariable("CITYFORGE_RIVERBED_BENCHMARK")=="1"){Application.runInBackground=true;DontDestroyOnLoad(new GameObject("Riverbed A/B benchmark").AddComponent<MeadowRiverBedBenchmark>().gameObject);}}
        IEnumerator Start()
        {
            const string output="/Users/joelinstrum/Documents/Codex/2026-10-11/be/outputs/Meadow-Riverbed-Performance-Raw.txt";
            var report=new StringBuilder("Dry riverbed A/B: "+SystemInfo.graphicsDeviceName+", "+Screen.width+"x"+Screen.height+" development player. Same seed, native terrain shader, source assets, camera translation; no Crest in either map.\nA=original saved valley with waterless flag; B=saved excavated/textured riverbeds. Camera vertical datum offset compensated by 8 m.\n");
            var regionDirectory=System.Environment.GetEnvironmentVariable("CITYFORGE_REGION_DIRECTORY")??SavedRegionStore.DirectoryPath;
            var deep=SavedRegionStore.Load(Path.Combine(regionDirectory,"meadow-river-beds-dry.cfregion"));
            var pose=deep.info.camera;
            using(var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count"))
            for(int phase=0;phase<12;phase++){
                bool modified=phase%4==1||phase%4==2;bool terrainOnly=phase>=8;bool capped=phase>=4&&phase<8;
                var saved=SavedRegionStore.Load(Path.Combine(regionDirectory,modified?"meadow-river-beds-dry.cfregion":SavedRegionStore.TestId+".cfregion"));saved.info.waterless=true;
                PrototypeRegionView.Open(saved.info.name,FindFirstObjectByType<Canvas>(),snapshot:saved);
                var view=FindFirstObjectByType<PrototypeRegionView>();var pilot=view.ApprovedDistrict;var rig=view.GetComponentInChildren<CityForgeCameraController>();
                if(pilot.Water)throw new Exception("Unexpected water in benchmark");
                pilot.SetSmallPlantsEnabled(!terrainOnly);pilot.SetTreesEnabled(!terrainOnly);
                rig.RestoreRegionState(new RegionCameraState{focus=pose.focus+Vector3.down*(modified?0:8),pitch=pose.pitch,yaw=pose.yaw,distance=pose.distance,size=pose.size,zoom=pose.zoom});
                QualitySettings.vSyncCount=0;Application.targetFrameRate=capped?60:-1;
                yield return null;var cover=pilot.GetComponentInChildren<MeadowRuntimeVegetation>(true);while(!terrainOnly&&cover&&cover.PendingTiles>0)yield return null;
                rig.enabled=false;var position=rig.transform.position;
                float warm=Time.realtimeSinceStartup;while(Time.realtimeSinceStartup-warm<4){float t=Time.realtimeSinceStartup-warm;rig.transform.position=position+new Vector3(Mathf.Sin(t)*45,0,Mathf.Sin(t*.7f)*36);yield return null;}
                // Let vegetation streaming settle before sampling the same repeated path.
                while(!terrainOnly&&cover&&cover.PendingTiles>0)yield return null;
                var frames=new List<float>();double drawSum=0,gpuSum=0;int gpuFrames=0;var timings=new FrameTiming[1];float start=Time.realtimeSinceStartup;
                while(Time.realtimeSinceStartup-start<6){float t=Time.realtimeSinceStartup-start;rig.transform.position=position+new Vector3(Mathf.Sin(t)*45,0,Mathf.Sin(t*.7f)*36);FrameTimingManager.CaptureFrameTimings();yield return null;
                    frames.Add(Time.unscaledDeltaTime*1000);if(draws.Valid)drawSum+=draws.LastValue;
                    if(FrameTimingManager.GetLatestTimings(1,timings)>0&&timings[0].gpuFrameTime>0){gpuSum+=timings[0].gpuFrameTime;gpuFrames++;}}
                frames.Sort();report.AppendLine("phase="+phase+"; map="+(modified?"B":"A")+"; content="+(terrainOnly?"terrain-only":"trees+plants")+"; cap="+(capped?60:-1)+"; mean="+frames.Average().ToString("F3")+"; p95="+frames[(int)(frames.Count*.95)].ToString("F3")+"; p99="+frames[(int)(frames.Count*.99)].ToString("F3")+"; over25ms="+(100f*frames.Count(f=>f>25)/frames.Count).ToString("F2")+"%; draws="+(drawSum/frames.Count).ToString("F0")+"; GPU="+(gpuFrames>0?(gpuSum/gpuFrames).ToString("F3"):"unavailable")+"; canonicalTrees="+pilot.FullTreeCount+"; layers="+pilot.DisplayTerrain.terrainData.alphamapLayers);
                File.WriteAllText(output,report.ToString());view.ReturnToMenu();yield return null;yield return null;
            }
            report.AppendLine("ABBA order repeated for uncapped full content, capped full content and uncapped terrain-only. Six seconds measured after each warm-up; load/generation excluded. Full content includes intentional bed vegetation exclusion, so compare terrain-only to isolate depth/material cost. GPU values are timing samples, not CPU cost. Normal game settings/saves untouched.");File.WriteAllText(output,report.ToString());Application.Quit();
        }
    }
}
