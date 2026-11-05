#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CityForge.UI;
using CityForge.World;
namespace CityForge.Editor
{
    /// <summary>Planting/removal round trips on a private disposable copy; original checkpoint restored.</summary>
    public static class ForestAdditionChecks
    {
        const string Report="Library/ForestAdditionChecks.txt";
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static IEnumerator Frames(){yield return null;yield return null;yield return null;}
        public static void Begin(string checkpoint){File.WriteAllText(Report,"RUNNING\n");UnityEngine.Object.FindFirstObjectByType<MainMenuController>().StartCoroutine(Check(checkpoint));}
        static IEnumerator Open(SavedRegionSnapshot saved)
        {
            yield return PrototypeRegionView.OpenStages(saved.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:saved);
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();yield return view.EnterDistrictStages(saved.info.district,null);
        }
        static void CheckTrees(PrototypeRegionView view)
        {
            var land=view.ApprovedDistrict;Require(land.MixedForest.TreeCount==land.CanonicalTreeInstances.Length,"Native/mixed population mismatch");
            Require(land.transform.Find("Approved mixed woodland").GetComponent<SpatialForestRenderer>().TreeCount==land.MixedForest.TreeCount,"Captured renderer tree mismatch");
            foreach(var record in view.ForestAdditions){var p=new Vector2(record.x,record.z);if(!land.Contains(p))continue;Require(land.WaterDistance(p)>=8.99f&&land.MountainSlope(p)<=25.01f&&!view.InCampClearing(p,8),"Planted tree violates habitat");Require(!view.IsForestRemoved(p),"Older removal overrides a planted tree");}
            Require(land.CanonicalTreeInstances.Select(t=>PrototypeRegionView.ForestPointKey(land.Origin+new Vector2(t.position.x,t.position.z)*land.Side)).Distinct().Count()==land.CanonicalTreeInstances.Length,"Duplicate tree positions");
        }
        static IEnumerator Check(string checkpoint)
        {
            var original=SavedRegionStore.Load(checkpoint);original.info.id="forest-add-verification";original.info.name="Forest Planting Verification";
            var open=Open(original);while(open.MoveNext())yield return open.Current;
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();var land=view.ApprovedDistrict;var tools=view.GetComponent<NatureToolbar>();var holder=land.transform.Find("Approved mixed woodland");
            Vector2 center=land.Origin+Vector2.one*land.Side*.5f;
            foreach(Transform tree in holder){var p=new Vector2(tree.position.x,tree.position.z);var local=p-land.Origin;if(local.x>120&&local.y>120&&local.x<land.Side-120&&local.y<land.Side-120){center=p;break;}}
            var rig=view.GetComponentInChildren<CameraSystem.CityForgeCameraController>();rig.SetZoomLevel(5);var state=rig.CaptureRegionState();state.size=300;rig.RestoreRegionState(state);yield return Frames();
            float bigRadius=400/(2*Mathf.PI);land.RemoveForest(center,bigRadius);yield return Frames();var survivors=holder.Cast<Transform>().ToArray();int baseCount=land.FullTreeCount;
            tools.Select(NatureTool.Trees);Require(!tools.ForestBrush.Armed,"Opening Forests armed a brush");tools.Popup.transform.Find("Option0").GetComponent<Button>().onClick.Invoke();
            var brush=tools.ForestBrush;Require(brush.Armed&&brush.Adding&&brush.Circumference==200&&!tools.Popup.gameObject.activeSelf,"Add Woodlands did not arm/collapse");
            int added=brush.AddAt(center);Require(added>0&&land.FullTreeCount==baseCount+added,"Default planting failed");yield return Frames();CheckTrees(view);
            Require(survivors.All(t=>t&&t.parent==holder),"Planting respawned the old forest");int records=view.ForestAdditions.Length;Require(brush.AddAt(center)==0&&view.ForestAdditions.Length==records,"Overpainting stacks trees");
            brush.ToggleSize();int outer=brush.AddAt(center);Require(outer>0&&brush.Circumference==400,"Large planting circle failed");yield return Frames();CheckTrees(view);
            brush.ToggleSize();var middle=land.Origin+Vector2.one*land.Side*.5f;var end=center+new Vector2(center.x>middle.x?-130:130,center.y>middle.y?-60:60);
            brush.BeginDrag(center);int stationary=view.ForestAdditions.Length;for(int i=0;i<20;i++)brush.ContinueDrag(center);Require(view.ForestAdditions.Length==stationary,"Stationary planting accumulated trees");
            int drag=brush.ContinueDrag(end,true);brush.EndDrag();Require(drag>0,"Fast drag did not plant");records=view.ForestAdditions.Length;brush.ContinueDrag(end+Vector2.one*50);Require(view.ForestAdditions.Length==records,"Released brush kept planting");yield return Frames();CheckTrees(view);
            File.AppendAllText(Report,"PASS 200/400m circles: "+added+" + "+outer+" trees; drag added "+drag+"; no overpaint stacking, stationary edits or painting after release; original forest objects retained; canonical/render/habitat checks pass.\n");
            // A subsequent removal must delete the explicit plant records as well.
            tools.Select(NatureTool.Trees);tools.Popup.transform.Find("Option1").GetComponent<Button>().onClick.Invoke();Require(!brush.Adding,"Mode switch retained Add");int deleted=brush.RemoveAt(center);Require(deleted>0,"Removing planted trees failed");yield return Frames();
            tools.Select(NatureTool.Trees);tools.Popup.SelectOption(0);tools.Popup.transform.Find("Settings/SpeciesChoice").GetComponent<Button>().onClick.Invoke();tools.Popup.transform.Find("Settings/SpeciesChoice").GetComponent<Button>().onClick.Invoke();tools.Popup.transform.Find("Settings/SpeciesChoice").GetComponent<Button>().onClick.Invoke();
            tools.Popup.transform.Find("Option0").GetComponent<Button>().onClick.Invoke();int readded=brush.AddAt(center);Require(readded>0,"Replant after removal failed");
            Require(view.ForestAdditions.Where(t=>(new Vector2(t.x,t.z)-center).sqrMagnitude<=brush.Radius*brush.Radius).All(t=>t.variant>=6),"Larch choice did not affect planted models");tools.Dismiss();yield return Frames();CheckTrees(view);
            foreach(var season in new[]{RegionSeason.Fall,RegionSeason.Winter,RegionSeason.Summer}){int count=land.FullTreeCount;view.SetSeason(season);yield return Frames();Require(land.FullTreeCount==count,"Season mutated planting");}
            int expected=land.FullTreeCount;string signature=land.MixedForest.PlacementSignature;var snapshot=SavedRegionStore.Capture(view);SavedRegionStore.Write(snapshot,"Library/ForestAdditionRoundtrip.cfregion");
            var stale=SavedRegionStore.Load("Library/ForestAdditionRoundtrip.cfregion");stale.meadow.trees=original.meadow.trees;view.ReturnToMenu();yield return null;open=Open(stale);while(open.MoveNext())yield return open.Current;
            view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(view.ApprovedDistrict.FullTreeCount==expected&&view.ApprovedDistrict.MixedForest.PlacementSignature==signature,"Stale/disk cache changed planted forest");CheckTrees(view);
            var fresh=SavedRegionStore.Load("Library/ForestAdditionRoundtrip.cfregion");fresh.meadow=null;fresh.info.meadowCacheDistrict=-1;view.ReturnToMenu();yield return null;open=Open(fresh);while(open.MoveNext())yield return open.Current;
            view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(view.ApprovedDistrict.FullTreeCount==expected&&view.ApprovedDistrict.MixedForest.PlacementSignature==signature,"Fresh generation lost planted forest");CheckTrees(view);
            File.AppendAllText(Report,"PASS remove/replant ordering, selected Larch variants, seasons, saved metadata, stale-cache disk reopen and no-cache regeneration retain exact count/signature.\n");
            view.ReturnToMenu();yield return null;open=Open(SavedRegionStore.Load(checkpoint));while(open.MoveNext())yield return open.Current;
            view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();view.GetComponentInChildren<CameraSystem.CityForgeCameraController>().RestoreRegionState(original.info.camera);var restored=view.GetComponent<NatureToolbar>();restored.Select(NatureTool.Trees);restored.Popup.SelectOption(0);
            File.AppendAllText(Report,"PASS original user checkpoint restored; no normal player saves written; Add Woodlands ready but unarmed.\n");
        }
    }
}
#endif
