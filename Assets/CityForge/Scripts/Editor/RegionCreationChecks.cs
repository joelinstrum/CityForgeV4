#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using CityForge.World;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class RegionCreationChecks
    {
        const string Output="/Users/joelinstrum/Documents/Codex/2026-10-14/continue-work-on-cityforge-v4-project/outputs/Region-Creation-Checks.txt";
        static RegionCreationChecks(){EditorApplication.update+=Poll;}
        static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Library/RegionCreation.request"))return;var action=File.ReadAllText("Library/RegionCreation.request").Trim();File.Delete("Library/RegionCreation.request");if(action=="capture"){ScreenCapture.CaptureScreenshot(Path.GetDirectoryName(Output)+"/Region-District.png");return;}if(action=="integration"){if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;File.WriteAllText("Library/RegionCreation.request","integration");return;}CityForge.Editor.EditorCoroutineRunner.Begin(Integration());}else Run();}
        [MenuItem("CityForge/Checks/Region Creation")] public static void Run()
        {
            try{
                var settings=new RegionCreationSettings{seed=42,topography=0};
                var flat=RegionMapGenerator.Generate("Flat",RegionSize.Medium,settings);
                Check(flat.info.district==-1&&flat.info.waterless&&flat.data.WaterBodies.Length==0,"Dry map and overview");
                foreach(float h in flat.data.Heights)Check(h==35f/1200,"Flat terrain");
                for(int t=1;t<=4;t++){settings.topography=t;var relief=RegionMapGenerator.Generate("Relief",RegionSize.Small,settings);float highest=0;foreach(float h in relief.data.Heights)highest=Mathf.Max(highest,h);Check(highest>35f/1200,"Topography option changes height");}
                settings.topography=4;settings.rivers=3;settings.streams=2;settings.era="Founder";settings.period="Frontier";
                var wet=RegionMapGenerator.Generate("Wet",RegionSize.Large,settings);var repeat=RegionMapGenerator.Generate("Wet",RegionSize.Large,settings);
                Check(wet.data.WaterBodies.Length==8&&!wet.info.waterless,"River/stream counts");
                float min=1,max=0;int submerged=0;
                for(int z=0;z<513;z++)for(int x=0;x<513;x++){
                    float h=wet.data.Heights[z,x];Check(h==repeat.data.Heights[z,x],"Repeatable seed");min=Mathf.Min(min,h);max=Mathf.Max(max,h);if(h<.01f)submerged++;
                }
                Check(max-min>.1f&&submerged>0,"Relief and channel carving");
                settings.seed=43;var other=RegionMapGenerator.Generate("Other",RegionSize.Large,settings);Check(other.data.Heights[300,400]!=wet.data.Heights[300,400],"Seed changes map");
                string path=Path.Combine(Application.temporaryCachePath,"cityforge-creation-check.cfregion");
                try{SavedRegionStore.Write(wet,path);var loaded=SavedRegionStore.Load(path);Check(loaded.info.creation.seed==42&&loaded.info.creation.rivers==3&&loaded.info.creation.period=="Frontier"&&loaded.info.district==-1,"Recipe round trip");for(int z=0;z<513;z++)for(int x=0;x<513;x++)Check(loaded.data.Heights[z,x]==wet.data.Heights[z,x]&&loaded.data.WaterDistance[z,x]==wet.data.WaterDistance[z,x],"Sample round trip");Check(loaded.data.WaterBodies.Length==8,"Water descriptors round trip");}
                finally{if(File.Exists(path))File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");}
                var coast=CoastlineTestRegion.Generate();Check(coast.info.creation==null&&coast.info.coastline,"Existing coast remains separate");
                Directory.CreateDirectory(Path.GetDirectoryName(Output));File.WriteAllText(Output,"PASS: flat dry terrain; 3 rivers + 5 streams; mountain relief; carved water channels; deterministic seed; seed variation; exact terrain/water save round trip; recipe persistence; overview default; existing coast recipe unchanged.\n");
                Debug.Log("Region creation checks passed");
            }catch(Exception e){File.WriteAllText(Output,"FAIL: "+e);Debug.LogException(e);}
        }
        static System.Collections.IEnumerator Integration()
        {
            var old=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(old)old.ReturnToMenu();yield return null;yield return null;
            var menu=UnityEngine.Object.FindFirstObjectByType<CityForge.UI.MainMenuController>();
            menu.Handle(CityForge.UI.MenuAction.CreateRegion);
            var modal=UnityEngine.Object.FindFirstObjectByType<CityForge.UI.CityForgeModal>();modal.SetRegionName("Generation Integration");modal.SetRegionSize(RegionSize.Small);modal.SelectCreateRegionSection(1);
            var terrain=modal.transform.Find("Panel75/RoundedContentClip/ScrollViewport/ScrollContent/CreateRegionTerrainIntro");
            terrain.Find("TopographyDropdown").GetComponent<UnityEngine.UI.Dropdown>().value=1;
            terrain.Find("RiversDropdown").GetComponent<UnityEngine.UI.Dropdown>().value=1;
            terrain.Find("StreamsDropdown").GetComponent<UnityEngine.UI.Dropdown>().value=1;
            var seedInput=terrain.Find("MapSeedInput").GetComponent<UnityEngine.UI.InputField>();seedInput.text="";
            Check(terrain.gameObject.activeSelf&&!terrain.Find("NextButton").GetComponent<UnityEngine.UI.Button>().interactable,"Seed editing stays on terrain and disables creation");
            seedInput.text="8472";
            ScreenCapture.CaptureScreenshot(Path.GetDirectoryName(Output)+"/Region-Wizard.png");yield return null;
            modal.CreatePrototypeRegion();yield return null;yield return null;
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            Check(view&&view.SelectedDistrict==-1&&view.Data.Seed==8472&&view.Data.WaterBodies.Length==3,"Wizard to overview");
            ScreenCapture.CaptureScreenshot(Path.GetDirectoryName(Output)+"/Region-Overview.png");yield return null;
            view.EnterDistrict(0);yield return null;yield return null;
            Check(view.ApprovedDistrict&&view.ApprovedDistrict.DistrictIndex==0,"Approved southwest district renderer");
            Check(view.ApprovedDistrict.Water,"Typed river renderer");
            var capture=SavedRegionStore.Capture(view);Check(capture.info.creation.seed==8472&&capture.info.district==0,"Capture retains recipe");
            var path=Path.Combine(Application.temporaryCachePath,"cityforge-creation-integration.cfregion");SavedRegionStore.Write(capture,path);
            var loaded=SavedRegionStore.Load(path);File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");
            view.ReturnToMenu();yield return null;yield return null;
            PrototypeRegionView.Open(loaded.info.name,menu.GetComponentInParent<Canvas>(),snapshot:loaded);yield return null;
            view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Check(view.SelectedDistrict==0&&view.CreationSettings.seed==8472&&view.ApprovedDistrict.UsedSavedTerrain,"Reopen district with cached approved terrain");
            float ready=Time.realtimeSinceStartup+3;while(Time.realtimeSinceStartup<ready||ShaderUtil.anythingCompiling)yield return null;
            ScreenCapture.CaptureScreenshot(Path.GetDirectoryName(Output)+"/Region-District.png");yield return null;
            File.AppendAllText(Output,"PASS: live wizard selections -> overview -> southwest approved district + typed rivers -> capture -> disk -> reload with recipe, district, and approved terrain cache. Screenshots captured.\n");
        }
        static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    }
}
#endif
