#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using CityForge.World;
using CityForge.UI;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class RegionDeleteChecks
    {
        const string Output="/Users/joelinstrum/Documents/Codex/2026-10-14/continue-work-on-cityforge-v4-project/outputs/Region-Delete-Checks.txt";
        static RegionDeleteChecks(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Library/RegionDelete.request";
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            File.Delete(request);EditorApplication.isPaused=false;Run();
        }
        static void Check(bool valid,string message){if(!valid)throw new Exception(message);}
        static void Run()
        {
            string path=null,archive=null;
            try{
                var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(view)view.ReturnToMenu();
                var sample=RegionMapGenerator.Generate("Delete option check",RegionSize.Small,new RegionCreationSettings());
                sample.info.id="delete-check-"+Guid.NewGuid().ToString("N");path=SavedRegionStore.Save(sample);File.Copy(path,path+".bak");
                var before=SavedRegionStore.ListRegions();var info=before.Find(x=>x.path==path);Check(info!=null,"Test save listed");
                var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();menu.Handle(MenuAction.OpenRegion);
                var modal=UnityEngine.Object.FindFirstObjectByType<CityForgeModal>();var row=modal.GetComponentsInChildren<Button>();
                Button open=null,delete=null;
                foreach(var button in row)if(button.name=="OpenRegion_"+info.id){open=button;delete=button.transform.Find("DeleteRegion").GetComponent<Button>();}
                Check(open&&delete,"Each saved row has delete action");
                delete.onClick.Invoke();Check(File.Exists(path),"First click only confirms");
                open.onClick.Invoke();Check(File.Exists(path)&&delete.GetComponentInChildren<Text>().text=="DELETE","Cancel retains region and resets button");
                delete.onClick.Invoke();delete.onClick.Invoke();
                Check(!File.Exists(path)&&!File.Exists(path+".bak"),"Confirmed delete removes save and backup from active folder");
                Check(!SavedRegionStore.ListRegions().Exists(x=>x.path==path),"Deleted region absent after relisting");
                foreach(var file in Directory.GetFiles(Path.Combine(SavedRegionStore.DirectoryPath,"Deleted"),Path.GetFileName(path),SearchOption.AllDirectories))archive=file;
                Check(archive!=null&&File.Exists(archive+".bak")&&SavedRegionStore.Load(archive).info.id==info.id,"Recoverable archive and backup remain readable");
                foreach(var other in before)if(other.path!=path)Check(File.Exists(other.path),"Other regions untouched");
                bool rejected=false;try{SavedRegionStore.Delete(new SavedRegionInfo{path=Path.Combine(Application.temporaryCachePath,"outside.cfregion")});}catch(ArgumentException){rejected=true;}
                Check(rejected,"Out-of-folder deletion rejected");
                ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(Output),"Open-Region-Delete.png"));
                File.WriteAllText(Output,"PASS: per-row Delete; confirmation; cancellation; confirmed removal and refreshed list; readable archived save + backup; other regions untouched; out-of-folder rejection.\n");
            }catch(Exception e){File.WriteAllText(Output,"FAIL: "+e);Debug.LogException(e);}
            finally{
                if(path!=null){if(File.Exists(path))File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");}
                if(archive!=null)Directory.Delete(Path.GetDirectoryName(archive),true);
            }
        }
    }
}
#endif
