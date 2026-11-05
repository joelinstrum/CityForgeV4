#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using CityForge.World;
using CityForge.UI;
namespace CityForge.Editor
{
    public static class DistrictTreeCoverageChecks
    {
        const string Report="Library/DistrictTreeCoverageChecks.txt";
        static void Require(bool value,string message){if(!value)throw new Exception(message);}
        [MenuItem("CityForge/Forest/Check district coverage planner")]
        public static void Planner()
        {
            var report=new StringBuilder();
            for(int seed=10;seed<18;seed++){
                var recipe=new RegionCreationSettings{seed=seed,treeCoverageVersion=1};
                List<DistrictTreeCoverage.Plant> previous=null;
                foreach(int percent in new[]{25,50,75}){
                    recipe.treeCoveragePercent=percent;recipe.Validate();
                    var plants=DistrictTreeCoverage.Plan(recipe,0,8192,p=>p.x> -4000&&p.y> -4030,out int eligible);
                    Require(plants.Count==Mathf.RoundToInt(eligible*percent/100f),"Coverage target mismatch");
                    Require(plants.All(p=>p.point.x> -4000&&p.point.y> -4030),"Excluded ground was planted");
                    var repeat=DistrictTreeCoverage.Plan(recipe,0,8192,p=>p.x> -4000&&p.y> -4030,out _);
                    Require(plants.Select(p=>p.point).SequenceEqual(repeat.Select(p=>p.point)),"Seeded planting drifted");
                    if(previous!=null)Require(previous.Select(p=>p.point).SequenceEqual(plants.Take(previous.Count).Select(p=>p.point)),"Increasing coverage thinned or moved existing trees");
                    previous=plants;
                }
            }
            var defaults=JsonUtility.FromJson<RegionCreationSettings>(JsonUtility.ToJson(new RegionCreationSettings{treeCoverageVersion=1}));defaults.Validate();Require(defaults.TreeCoverage(0)==50,"Default coverage lost during JSON copy");
            var r=new RegionCreationSettings{seed=70331,treeCoverageVersion=1,treeCoveragePercent=50,districtTreeCoverage=Enumerable.Repeat(50,64).ToArray()};
            var neighbor=DistrictTreeCoverage.Plan(r,1,8192,p=>true,out _);r.districtTreeCoverage[0]=75;
            Require(neighbor.Select(p=>p.point).SequenceEqual(DistrictTreeCoverage.Plan(r,1,8192,p=>true,out _).Select(p=>p.point)),"One district changed its neighbor");
            var decoded=JsonUtility.FromJson<RegionCreationSettings>(JsonUtility.ToJson(r));decoded.Validate();Require(decoded.districtTreeCoverage.SequenceEqual(r.districtTreeCoverage),"Recipe coverage round trip failed");
            Require(!DistrictTreeCoverage.Applies(JsonUtility.FromJson<RegionCreationSettings>("{}")),"Legacy recipe opted in");
            r.districtTreeCoverage[0]=30;bool rejected=false;try{r.Validate();}catch(ArgumentException){rejected=true;}Require(rejected,"Invalid coverage accepted");
            report.AppendLine("PASS 8 seeds × 3 coverages: exact eligible-land targets, exclusions, determinism, nested footprints.").AppendLine("PASS independent district choices, recipe JSON round trip, legacy opt-out and invalid-value rejection.");
            foreach(int percent in new[]{25,50,75}){r.districtTreeCoverage[0]=percent;var points=DistrictTreeCoverage.Plan(r,0,8192,p=>true,out int eligible);report.AppendLine(percent+"%: "+points.Count+" / "+eligible+" planting cells per 1024m flat district.");}
            File.WriteAllText(Report,report.ToString());Debug.Log(report.ToString());
        }
        [MenuItem("CityForge/Forest/Check district coverage live (disposable region)")]
        public static void BeginLive(){Planner();EditorCoroutineRunner.Begin(Run());}
        static IEnumerator Enter(PrototypeRegionView view,int index){view.RequestDistrict(index);yield return null;float deadline=Time.realtimeSinceStartup+90;while(view.DistrictLoading){Require(Time.realtimeSinceStartup<deadline,"District load timeout");yield return null;}Require(view.ApprovedDistrict&&view.SelectedDistrict==index,"District did not open");}
        static IEnumerator Run()
        {
            var host=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();Require(host,"Main menu required");
            var recipe=new RegionCreationSettings{seed=70331,forestAppearanceVersion=1,treeCoverageVersion=1,treeCoveragePercent=50,waterwayVersion=5,districtTreeCoverage=Enumerable.Repeat(50,64).ToArray()};
            recipe.districtTreeCoverage[0]=25;recipe.districtTreeCoverage[1]=50;recipe.districtTreeCoverage[8]=75;
            host.CreatePrototypeRegion("Tree Coverage Verification",RegionSize.Small,recipe);yield return null;float deadline=Time.realtimeSinceStartup+120;
            while(host.IsOpeningRegion){Require(Time.realtimeSinceStartup<deadline,"Region creation timeout");yield return null;}
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(view,"Region not created");int[] counts=new int[3];int k=0;
            foreach(int index in new[]{0,1,8}){
                var enter=Enter(view,index);while(enter.MoveNext())yield return enter.Current;
                var land=view.ApprovedDistrict;var plan=DistrictTreeCoverage.Plan(land);Require(land.MixedForest&&land.MixedForest.Ready,"Mixed library missing");
                Require(land.CanonicalTreeInstances.Length==plan.Count&&land.MixedForest.TreeCount==plan.Count,"Planner/native/mixed populations diverged");
                foreach(Transform tree in land.transform.Find("Approved mixed woodland")){var p=new Vector2(tree.position.x,tree.position.z);Require(view.InActiveDistrict(p)&&land.WaterDistance(p)>=8.99f&&land.MountainSlope(p)<=25.01f,"Invalid displayed placement");Require(Mathf.Abs(tree.position.y-land.Height(p))<.002f,"Ungrounded mixed tree");}
                counts[k++]=plan.Count;File.AppendAllText(Report,"PASS live district "+index+" coverage "+recipe.TreeCoverage(index)+"%: "+plan.Count+" mixed trees; exact canonical/plan match.\n");
                view.GetComponentInChildren<CameraSystem.CityForgeCameraController>().SetZoomLevel(5);yield return null;yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot("Library/TreeCoverage"+recipe.TreeCoverage(index)+".png");yield return null;
                view.ShowOverview();yield return null;
            }
            Require(counts[0]<counts[1]&&counts[1]<counts[2],"Flat districts did not increase coverage");
            view.CombineDistrictCells(new[]{0,1,8});var combined=Enter(view,0);while(combined.MoveNext())yield return combined.Current;
            Require(view.ActiveDistrictMembers.Length==3,"L union missing");Require(view.ApprovedDistrict.MixedForest.TreeCount==counts.Sum(),"Combined district changed per-cell planting");
            string signature=view.ApprovedDistrict.MixedForest.PlacementSignature;var snapshot=SavedRegionStore.Capture(view);SavedRegionStore.Write(snapshot,"Library/TreeCoverageRoundtrip.cfregion");
            var saved=SavedRegionStore.Load("Library/TreeCoverageRoundtrip.cfregion");Require(saved.info.creation.districtTreeCoverage.SequenceEqual(recipe.districtTreeCoverage),"Coverage choices lost on disk");
            view.ShowOverview();combined=Enter(view,0);while(combined.MoveNext())yield return combined.Current;Require(view.ApprovedDistrict.MixedForest.PlacementSignature==signature,"Cached forest moved");
            view.SetSeason(RegionSeason.Fall);view.SetSeason(RegionSeason.Winter);view.SetSeason(RegionSeason.Summer);yield return null;
            Require(view.ApprovedDistrict.MixedForest.PlacementSignature==signature,"Season changed planting");
            File.AppendAllText(Report,"PASS L-shaped union retains all three district coverages, private disk round trip, cached reopen and seasonal stability.\nNo normal player saves modified.\n");Debug.Log("District coverage live checks passed");
        }
    }
}
#endif
