using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
namespace CityForge.World
{
    public static class LakeWaterComparison
    {
        public static IEnumerator Run()
        {
            string path=System.IO.Path.Combine(SavedRegionStore.DirectoryPath,HillLakeTestRegion.Id+".cfregion");
            int cap=Application.targetFrameRate,vsync=QualitySettings.vSyncCount;
            var report=new StringBuilder("Hill and Lake; identical saved terrain/vegetation, same panning path. Unity Editor "+Screen.width+"x"+Screen.height+". Uncapped ABBA: Crest / Still / Still / Crest. 3-second warmup, 5-second measurement per phase.\n");
            try{
                for(int phase=0;phase<4;phase++){
                    var old=Object.FindFirstObjectByType<PrototypeRegionView>();if(old)old.ReturnToMenu();yield return null;yield return null;
                    var saved=SavedRegionStore.Load(path);saved.info.stillWater=phase==1||phase==2;
                    PrototypeRegionView.Open(saved.info.name,Object.FindFirstObjectByType<Canvas>(),snapshot:saved);yield return null;yield return null;
                    var view=Object.FindFirstObjectByType<PrototypeRegionView>();var rig=view.GetComponentInChildren<CameraSystem.CityForgeCameraController>();var origin=rig.transform.position;rig.enabled=false;
                    QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
                    float start=Time.realtimeSinceStartup;
                    while(Time.realtimeSinceStartup-start<3){float t=Time.realtimeSinceStartup-start;rig.transform.position=origin+new Vector3(Mathf.Sin(t)*25,0,Mathf.Sin(t*.7f)*18);yield return null;}
                    var frames=new List<float>();start=Time.realtimeSinceStartup;
                    while(Time.realtimeSinceStartup-start<5){float t=Time.realtimeSinceStartup-start;rig.transform.position=origin+new Vector3(Mathf.Sin(t)*25,0,Mathf.Sin(t*.7f)*18);yield return null;frames.Add(Time.unscaledDeltaTime*1000);}
                    frames.Sort();report.AppendLine($"phase={phase}; still={saved.info.stillWater}; mean={frames.Average():F3}ms; p95={frames[(int)(frames.Count*.95)]:F3}ms; >25ms={100f*frames.Count(f=>f>25)/frames.Count:F2}%");
                    System.IO.File.WriteAllText("/Users/joelinstrum/Documents/Codex/2026-10-11/be/outputs/Lake-Water-Comparison.txt",report.ToString());rig.transform.position=origin;rig.enabled=true;
                }
                var current=Object.FindFirstObjectByType<PrototypeRegionView>();if(current)current.ReturnToMenu();yield return null;yield return null;
                var final=SavedRegionStore.Load(path);PrototypeRegionView.Open(final.info.name,Object.FindFirstObjectByType<Canvas>(),snapshot:final);yield return null;
                var restored=Object.FindFirstObjectByType<PrototypeRegionView>();
                if(!restored.StillWater||restored.ApprovedDistrict.Water||!restored.ApprovedDistrict.GetComponentInChildren<StillWaterRenderer>())throw new System.Exception("Still-water restore failed.");
                Debug.Log("PASS lake water ABBA comparison; still-water saved preference restored.");
            }finally{Application.targetFrameRate=cap;QualitySettings.vSyncCount=vsync;}
        }
    }
}
