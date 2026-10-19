#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using CityForge.UI;
using CityForge.World;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class RegionLoadingChecks
    {
        const string Out="/Users/joelinstrum/Documents/Codex/2026-10-14/continue-work-on-cityforge-v4-project/outputs/";
        static RegionLoadingChecks(){EditorApplication.update+=Poll;}
        static void Poll(){const string request="Library/RegionLoading.request";if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}File.Delete(request);EditorApplication.isPaused=false;CityForge.Editor.EditorCoroutineRunner.Begin(Guard(Run()));}
        static IEnumerator Guard(IEnumerator routine){while(true){object current;try{if(!routine.MoveNext())yield break;current=routine.Current;}catch(Exception e){File.AppendAllText(Out+"Region-Loading-Checks.txt","FAIL: "+e+"\n");Debug.LogException(e);yield break;}yield return current;}}
        static void Require(bool valid,string message){if(!valid)throw new Exception(message);}
        static IEnumerator Run(){
            File.WriteAllText(Out+"Region-Loading-Checks.txt","");
            var old=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(old)old.ReturnToMenu();yield return null;yield return null;
            var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            // A missing file must recover without leaving a partial region or blocking another Open.
            menu.OpenSavedRegion(new SavedRegionInfo{name="Missing region check",path=Path.Combine(Application.temporaryCachePath,Guid.NewGuid()+".cfregion")});
            Require(menu.IsOpeningRegion&&UnityEngine.Object.FindFirstObjectByType<RegionLoadingOverlay>(),"Overlay appears immediately");
            double deadline=EditorApplication.timeSinceStartup+90;
            while(menu.IsOpeningRegion){EditorApplication.isPaused=false;Require(EditorApplication.timeSinceStartup<deadline,"Missing-file load timed out");yield return null;}
            EditorApplication.isPaused=false;yield return null;
            Require(!UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>(),"Failed read leaves no partial region");
            File.AppendAllText(Out+"Region-Loading-Checks.txt","PASS missing file: overlay appears immediately, error returns to menu, opening guard resets, no partial region.\n");
            var path=Path.Combine(SavedRegionStore.DirectoryPath,HillsMountainTestRegion.Id+".cfregion");var source=SavedRegionStore.Load(path);bool hadCache=source.meadow!=null&&source.info.meadowCacheDistrict==source.info.district;var original=File.GetLastWriteTimeUtc(path);var length=new FileInfo(path).Length;
            for(int pass=0;pass<2;pass++){
                var info=source.info;string temporary=null;
                if(pass==1){source.meadow=null;temporary=Path.Combine(Application.temporaryCachePath,"region-loading-"+Guid.NewGuid().ToString("N")+".cfregion");SavedRegionStore.Write(source,temporary);info=SavedRegionStore.Load(temporary).info;}
                menu.OpenSavedRegion(info);var loading=UnityEngine.Object.FindFirstObjectByType<RegionLoadingOverlay>();Require(loading,"Loading canvas exists");
                menu.OpenSavedRegion(info);Require(UnityEngine.Object.FindObjectsByType<RegionLoadingOverlay>(FindObjectsSortMode.None).Length==1,"Duplicate Open guarded");
                var stages=new List<string>();float last=0;int ticks=0,frames=0,lastFrame=Time.frameCount;double previous=EditorApplication.timeSinceStartup,maxGap=0;deadline=previous+180;bool captured=false;long longestStep=0;string longestStage="";
                while(menu.IsOpeningRegion){
                    EditorApplication.isPaused=false;Require(EditorApplication.timeSinceStartup<deadline,"Region load timed out");
                    if(loading){longestStep=loading.LongestStepMilliseconds;longestStage=loading.LongestStepStage;Require(loading.Progress>=last&&loading.Progress<=1,"Monotonic loading progress");last=loading.Progress;if(stages.Count==0||stages[stages.Count-1]!=loading.Stage)stages.Add(loading.Stage);
                        if(pass==1&&!captured&&loading.Stage.Contains("Painting")){ScreenCapture.CaptureScreenshot(Out+"Region-Loading-Progress.png");captured=true;}}
                    ticks++;if(lastFrame!=Time.frameCount){frames++;lastFrame=Time.frameCount;}double now=EditorApplication.timeSinceStartup;maxGap=Math.Max(maxGap,now-previous);previous=now;
                    yield return null;
                }
                yield return null;var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(view&&view.enabled&&view.NaturalLandscape&&view.SelectedDistrict==source.info.district,"Saved approved district opens");
                Require(last==1&&frames>=3,"Progress completes after rendered frames");Require(view.ApprovedDistrict.EvergreenTreeCount>0,"Fir mix preserved");
                Require(view.ApprovedDistrict.UsedSavedTerrain==(pass==0&&hadCache),"Cached and rebuilt terrain paths exercised");
                Require(File.GetLastWriteTimeUtc(path)==original&&new FileInfo(path).Length==length,"Original save not rewritten");
                File.AppendAllText(Out+"Region-Loading-Checks.txt",$"PASS {(pass==0?"cached":"uncached")}: {ticks} UI checks, {frames} rendered-frame advances, maximum observed editor-update gap {maxGap*1000:F0} ms. longest main-thread build step {longestStep} ms ({longestStage}). Duplicate Open blocked; progress completed; approved terrain and fir mix restored; original save untouched. Stages: {string.Join(" -> ",stages)}\n");
                if(temporary!=null)File.Delete(temporary);
                if(pass==0){view.ReturnToMenu();yield return null;yield return null;}
            }
        }
    }
}
#endif
