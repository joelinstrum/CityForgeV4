#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using CityForge.World;
using CityForge.UI;
namespace CityForge.Editor
{
    /// <summary>Explicit disposable-region acceptance checks; never writes the player's Regions directory.</summary>
    public static class MixedForestIntegrationChecks
    {
        const string Report="Library/MixedForestIntegrationChecks.txt";
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        public static void Begin(){File.WriteAllText(Report,"RUNNING\n");EditorCoroutineRunner.Begin(Guard(Run()));}
        static IEnumerator Guard(IEnumerator run)
        {
            while(true){bool more;try{more=run.MoveNext();}catch(Exception error){File.AppendAllText(Report,"FAIL: "+error+"\n");Debug.LogException(error);yield break;}if(!more)yield break;yield return run.Current;}
        }
        static void Note(StringBuilder report,string text){report.AppendLine(text);File.WriteAllText(Report,report.ToString());}
        // Unity re-seats native Y coordinates against quantized terrain when restoring a cache.
        // Planar positions, species, rotations and scales are the canonical ecology contract.
        static string Canonical(MeadowDistrictLandscape land)
        {
            unchecked {uint h=2166136261;foreach(var tree in land.CanonicalTreeInstances){h=(h^(uint)tree.prototypeIndex)*16777619;h=(h^(uint)new Vector2(tree.position.x,tree.position.z).GetHashCode())*16777619;h=(h^(uint)tree.rotation.GetHashCode())*16777619;h=(h^(uint)tree.widthScale.GetHashCode())*16777619;h=(h^(uint)tree.heightScale.GetHashCode())*16777619;}return h.ToString("x8");}
        }
        static void CheckPlacement(MeadowDistrictLandscape land)
        {
            var forest=land.MixedForest;Require(forest&&forest.Ready&&forest.TreeCount>0,"New forest is missing");
            var holder=forest.transform.Find("Approved mixed woodland");Require(holder&&holder.childCount==forest.TreeCount,"Forest count mismatch");
            var renderer=holder.GetComponent<SpatialForestRenderer>();Require(renderer&&renderer.TreeCount==forest.TreeCount&&renderer.simplifiedDistantShadows,"Approved renderer or shadow configuration missing");
            foreach(Transform tree in holder){var point=new Vector2(tree.position.x,tree.position.z);Require(land.Contains(point)&&land.View.InActiveDistrict(point),"Tree outside active district union");Require(!land.View.InCampClearing(point,8),"Tree in camp clearing");Require(land.WaterDistance(point)>=8.99f&&land.RoadDistance(point)>=4.99f,"Tree violates water/road clearance");Require(Mathf.Abs(tree.position.y-land.Height(point))<.002f,"Tree is not grounded");}
        }
        static IEnumerator Enter(PrototypeRegionView view,int cell)
        {
            view.RequestDistrict(cell);float deadline=Time.realtimeSinceStartup+90;yield return null;
            while(view.DistrictLoading){Require(Time.realtimeSinceStartup<deadline,"District loading timeout");yield return null;}
            Require(view.ApprovedDistrict&&view.SelectedDistrict==view.DistrictAnchor(cell),"Requested district did not open");
        }
        static IEnumerator Run()
        {
            var report=new StringBuilder();var old=JsonUtility.FromJson<RegionCreationSettings>("{\"version\":12,\"seed\":70331}");Require(old.forestAppearanceVersion==0,"Missing forest version must remain legacy");
            var library=MixedForestLibrary.Load();library.Validate();
            var recipe=new RegionCreationSettings{forestAppearanceVersion=1,seed=70331,streams=1,waterwayVersion=5,topography=1,vegetation=RegionVegetation.Woodland};recipe.Validate();
            bool rejected=false;try{var invalid=JsonUtility.FromJson<RegionCreationSettings>(JsonUtility.ToJson(recipe));invalid.forestAppearanceVersion=2;invalid.Validate();}catch(ArgumentException){rejected=true;}Require(rejected,"Unknown forest version accepted");
            var controller=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();Require(controller,"Main menu unavailable");float started=Time.realtimeSinceStartup;
            controller.CreatePrototypeRegion("Approved mixed woodland verification",RegionSize.Small,recipe);float deadline=Time.realtimeSinceStartup+120;yield return null;
            while(controller.IsOpeningRegion){Require(Time.realtimeSinceStartup<deadline,"Region creation timeout");yield return null;}
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(view&&view.CreationSettings.forestAppearanceVersion==1,"Creation flow dropped forest version");
            Note(report,"PASS new main-menu creation; forest version1; overview "+(Time.realtimeSinceStartup-started).ToString("F2")+"s");
            int cell=view.Districts.OrderByDescending(d=>d.woodland*(1-d.waterFraction)).First().index;started=Time.realtimeSinceStartup;
            var enter=Enter(view,cell);while(enter.MoveNext())yield return enter.Current;
            var land=view.ApprovedDistrict;CheckPlacement(land);string signature=land.MixedForest.PlacementSignature,canonical=Canonical(land);int count=land.MixedForest.TreeCount;
            Note(report,"PASS district "+cell+"; "+count+" mixed trees; "+(Time.realtimeSinceStartup-started).ToString("F2")+"s; signature "+signature);
            var holder=land.MixedForest.transform.Find("Approved mixed woodland");
            land.SetTreesEnabled(false);Require(!holder.gameObject.activeSelf,"Trees OFF left mixed trees visible");land.SetTreesEnabled(true);Require(holder.gameObject.activeSelf,"Trees ON did not restore forest");
            view.SetSeason(RegionSeason.Winter);Require(!holder.gameObject.activeSelf,"Summer forest remained in winter");Require(land.DisplayTerrain.terrainData.treeInstanceCount>0,"Winter native presentation missing");
            view.SetSeason(RegionSeason.Fall);Require(!holder.gameObject.activeSelf,"Summer forest remained in autumn");
            view.SetSeason(RegionSeason.Summer);Require(holder.gameObject.activeSelf,"Summer forest did not return");
            land.SetSmallPlantsEnabled(true);yield return null;land.SetSmallPlantsEnabled(false);
            Require(Canonical(land)==canonical&&land.MixedForest.PlacementSignature==signature,"Visibility/season controls mutated canonical ecology");Note(report,"PASS tree/understory toggles; native fall/winter; summer restore; canonical planar ecology unchanged");
            var snapshot=SavedRegionStore.Capture(view);string path="Library/MixedForestRoundtrip.cfregion";SavedRegionStore.Write(snapshot,path);var saved=SavedRegionStore.Load(path);Require(saved.info.creation.forestAppearanceVersion==1,"Saved forest version missing");
            view.ShowOverview();int neighbor=cell%view.GridSide<view.GridSide-1?cell+1:cell-1;enter=Enter(view,neighbor);while(enter.MoveNext())yield return enter.Current;CheckPlacement(view.ApprovedDistrict);view.ShowOverview();enter=Enter(view,cell);while(enter.MoveNext())yield return enter.Current;
            Require(view.ApprovedDistrict.MixedForest.PlacementSignature==signature&&Canonical(view.ApprovedDistrict)==canonical,"District cache changed forest");Note(report,"PASS neighboring district and cached reopening");
            view.ReturnToMenu();yield return null;PrototypeRegionView.Open(saved.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:SavedRegionStore.Load(path));view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();CheckPlacement(view.ApprovedDistrict);
            Require(view.ApprovedDistrict.MixedForest.PlacementSignature==signature&&Canonical(view.ApprovedDistrict)==canonical,"Disk round trip changed forest");Note(report,"PASS disk save/load; canonical trees and mixed placement identical");
            var legacy=SavedRegionStore.Load(path);legacy.info.creation.forestAppearanceVersion=0;view.ReturnToMenu();yield return null;PrototypeRegionView.Open(legacy.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:legacy);view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(!view.ApprovedDistrict.MixedForest,"Legacy recipe received new forest");Note(report,"PASS legacy recipe retains previous forest");
            view.ReturnToMenu();yield return null;PrototypeRegionView.Open(saved.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:SavedRegionStore.Load(path));view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();view.ShowOverview();int grid=view.GridSide;int anchor=grid+1;view.CombineDistrictCells(new[]{anchor,anchor+1,anchor+grid});enter=Enter(view,anchor);while(enter.MoveNext())yield return enter.Current;CheckPlacement(view.ApprovedDistrict);Require(view.ActiveDistrictMembers.Length==3,"Combined district metadata missing");
            var excluded=view.DistrictOrigin(anchor+grid+1)+Vector2.one*view.DistrictSide*.5f;Require(!view.InActiveDistrict(excluded),"L-shaped excluded corner filled");Note(report,"PASS L-shaped union: all mixed trees within selected cells, excluded corner empty");
            view.ReturnToMenu();yield return null;PrototypeRegionView.Open(saved.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:SavedRegionStore.Load(path));view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();CheckPlacement(view.ApprovedDistrict);
            view.GetComponentInChildren<CityForge.CameraSystem.CityForgeCameraController>().SetZoomLevel(3);
            Note(report,"PASS all integration checks; temporary files only; no player saves modified");File.WriteAllText(Report,report.ToString());Debug.Log("Mixed forest integration checks passed");
        }
    }
}
#endif
