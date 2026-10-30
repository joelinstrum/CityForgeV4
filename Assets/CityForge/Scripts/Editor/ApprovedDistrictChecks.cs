#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using UnityEditor;
using UnityEngine;
using CityForge.World;
using CityForge.UI;
using CityForge.CameraSystem;
namespace CityForge.EditorTools
{
    [InitializeOnLoad] public static class ApprovedDistrictChecks
    {
        const string Folder="Library/ApprovedDistrictChecks";
        static ApprovedDistrictChecks(){EditorApplication.update+=Poll;}
        static void Poll(){const string request="Library/ApprovedDistrictChecks.request";if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
            string output=File.ReadAllText(request).Trim();File.Delete(request);Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/Status.txt","RUNNING: loading original save through menu");CityForge.Editor.EditorCoroutineRunner.Begin(Guard(Check(output)));}
        static IEnumerator Guard(IEnumerator routine){while(true){object current;try{EditorApplication.QueuePlayerLoopUpdate();if(!routine.MoveNext())yield break;current=routine.Current;}catch(Exception e){File.WriteAllText(Folder+"/Status.txt","FAIL: "+e);Debug.LogException(e);yield break;}yield return current;}}
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        static string Hash(string path){using var sha=System.Security.Cryptography.SHA256.Create();return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path)));}
        static IEnumerator Check(string output)
        {
            var saved=SavedRegionStore.ListRegions();var original=saved.Find(x=>x.id==SavedRegionStore.TestId);Require(original!=null,"Expected the original saved valley");var path=original.path;string hash=Hash(path);
            var existing=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();if(existing){existing.ReturnToMenu();yield return null;}
            var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();menu.OpenSavedRegion(saved[0]);
            double deadline=EditorApplication.timeSinceStartup+120;
            while((!UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>()||menu.IsOpeningRegion)&&EditorApplication.timeSinceStartup<deadline)yield return null;
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(view&&view.SelectedDistrict==-1,"Menu opens relief overview");Require(view.GridSide==2&&view.CreationSettings==null,"Legacy grid and missing recipe preserved");
            var report=new System.Text.StringBuilder();Directory.CreateDirectory(output);
            foreach(int district in new[]{0,1,2,3}){
                view.RequestDistrict(district);deadline=EditorApplication.timeSinceStartup+120;
                while(view.DistrictLoading&&EditorApplication.timeSinceStartup<deadline)yield return null;
                Require(!view.DistrictLoading&&view.SelectedDistrict==district&&view.NaturalLandscape&&view.ApprovedDistrict,"Normal staged district uses approved meadow");
                var land=view.ApprovedDistrict;var forest=land.GetComponent<ApprovedDistrictForest>();Require(forest&&forest.TreeCount>0&&land.TreesEnabled,"Approved authored and native trees visible");
                if(district==3){Require(land.UsedSavedTerrain&&land.FullTreeCount==5361,"Existing canonical cache reused intact");Require(forest.CoreCount==400&&forest.EdgeCount==150&&forest.OutlierCount==50,"Approved 400/150/50 grove distribution");}
                var rig=view.GetComponentInChildren<CityForgeCameraController>();Require(rig.ZoomLevel==5,"Normal entry camera stays zoom5");
                for(int frame=0;frame<8;frame++)yield return null;
                ScreenCapture.CaptureScreenshot(Path.Combine(output,"District"+district+"-Overview.png"));yield return null;
                var river=land.GetComponentInChildren<CrestDistrictRiverPreview>();
                if(district<2)Require(land.Water&&land.Water.AnimatedWavesLod.Enabled&&land.Water.FoamLod.Enabled,"Saved coastline uses Crest waves and foam");
                if(district==3)Require(river&&river.Settings.GetFloat("_CityForgeRippleEvolutionSpeed")==2&&river.BoundaryClipEnabled,"River uses approved evolving Crest glints and clipping");
                int native=land.FullTreeCount;view.SetSeason(RegionSeason.Fall);yield return null;view.SetSeason(RegionSeason.Winter);yield return null;view.SetSeason(RegionSeason.Summer);yield return null;Require(land.FullTreeCount==native,"Season switches preserve canonical trees");
                rig.SetZoomLevel(1);for(int frame=0;frame<8;frame++)yield return null;ScreenCapture.CaptureScreenshot(Path.Combine(output,"District"+district+"-Close.png"));yield return null;
                report.AppendLine($"District {district}: meadow; native={land.FullTreeCount}; authored={forest.TreeCount}; ocean={(bool)land.Water}; approvedRiver={(bool)river}; entry zoom5; seasonal canonical count preserved.");
                if(district<3){view.ShowOverview();yield return null;Require(!view.ApprovedDistrict&&view.SelectedDistrict==-1,"Return to relief removes detailed district");}
                File.WriteAllText(Folder+"/Status.txt",report.ToString());
            }
            Require(Hash(path)==hash,"Player save remains byte identical");report.AppendLine("PASS: single saved region, real menu/preloader, all four districts, relief transitions, seasonal counts, original save hash unchanged.");File.WriteAllText(Folder+"/Status.txt",report.ToString());
        }
    }
}
#endif
