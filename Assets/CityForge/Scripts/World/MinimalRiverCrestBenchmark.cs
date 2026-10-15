using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Unity.Profiling;
namespace CityForge.World
{
    public sealed class MinimalRiverCrestBenchmark:MonoBehaviour
    {
        IEnumerator Start()
        {
            var view=GetComponent<PrototypeRegionView>();var pilot=view.ApprovedDistrict;var water=pilot.Water;
            var rig=view.GetComponentInChildren<CameraSystem.CityForgeCameraController>();var original=rig.transform.position;
            bool rigEnabled=rig.enabled;int cap=Application.targetFrameRate;int vsync=QualitySettings.vSyncCount;
            var report=new StringBuilder("Minimal Crest river A/B; Unity Editor, "+SystemInfo.graphicsDeviceName+", "+Screen.width+"x"+Screen.height+". Same loaded region, trees/plants/rocks/camera path. Uncapped ABBA; generation excluded.\n");
            rig.enabled=false;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
            try {
                using var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count");
                for(int phase=0;phase<4;phase++){
                    bool wet=phase==1||phase==2;water.gameObject.SetActive(wet);
                    float start=Time.realtimeSinceStartup;while(Time.realtimeSinceStartup-start<4){float t=Time.realtimeSinceStartup-start;rig.transform.position=original+new Vector3(Mathf.Sin(t)*25,0,Mathf.Sin(t*.7f)*18);yield return null;}
                    var frames=new List<float>();double drawSum=0;start=Time.realtimeSinceStartup;
                    while(Time.realtimeSinceStartup-start<5){float t=Time.realtimeSinceStartup-start;rig.transform.position=original+new Vector3(Mathf.Sin(t)*25,0,Mathf.Sin(t*.7f)*18);yield return null;frames.Add(Time.unscaledDeltaTime*1000);if(draws.Valid)drawSum+=draws.LastValue;}
                    frames.Sort();report.AppendLine("phase="+phase+"; Crest="+wet+"; mean="+frames.Average().ToString("F3")+"ms; p95="+frames[(int)(frames.Count*.95)].ToString("F3")+"ms; >25ms="+(100f*frames.Count(f=>f>25)/frames.Count).ToString("F2")+"%; draws="+(drawSum/frames.Count).ToString("F0"));
                    System.IO.File.WriteAllText("/Users/joelinstrum/Documents/Codex/2026-10-11/be/outputs/Minimal-River-Crest-Performance.txt",report.ToString());
                }
                report.AppendLine("Reflections="+water.Reflections.Enabled+"; underwater="+water.Underwater.Enabled+"; waves="+water.AnimatedWavesLod.Enabled+"; dynamicWaves="+water.DynamicWavesLod.Enabled+"; foam="+water.FoamLod.Enabled+"; flow="+water.FlowLod.Enabled+"; depth="+water.DepthLod.Enabled+"; shadow="+water.ShadowLod.Enabled+"; collisionProvider="+water.CollisionProvider.GetType().Name);
                System.IO.File.WriteAllText("/Users/joelinstrum/Documents/Codex/2026-10-11/be/outputs/Minimal-River-Crest-Performance.txt",report.ToString());
                Debug.Log("PASS minimal river Crest ABBA comparison completed.");
            } finally {water.gameObject.SetActive(true);rig.transform.position=original;rig.enabled=rigEnabled;Application.targetFrameRate=cap;QualitySettings.vSyncCount=vsync;Destroy(this);}
        }
    }
}
