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
    /// <summary>Disposable copy only; never saves edits to a player's region.</summary>
    public static class ForestRemovalChecks
    {
        const string Report="Library/ForestRemovalChecks.txt";
        static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
        static IEnumerator Frames(){yield return null;yield return null;yield return null;}
        public static void Begin(string checkpoint){File.WriteAllText(Report,"RUNNING\n");UnityEngine.Object.FindFirstObjectByType<MainMenuController>().StartCoroutine(Check(checkpoint));}
        static IEnumerator Open(SavedRegionSnapshot saved)
        {
            yield return PrototypeRegionView.OpenStages(saved.info.name,UnityEngine.Object.FindFirstObjectByType<Canvas>(),snapshot:saved);
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();yield return view.EnterDistrictStages(saved.info.district,null);
        }
        static IEnumerator Check(string checkpoint)
        {
            var original=SavedRegionStore.Load(checkpoint);original.info.id="forest-removal-verification";original.info.name="Forest Removal Verification";
            var open=Open(original);while(open.MoveNext())yield return open.Current;
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();var land=view.ApprovedDistrict;var tools=view.GetComponent<NatureToolbar>();
            var holder=land.transform.Find("Approved mixed woodland");Require(holder&&holder.childCount>0,"Mixed forest required");
            var center=new Vector2(holder.GetChild(0).position.x,holder.GetChild(0).position.z);
            // Pick an interior tree to exercise both brush sizes away from cell boundaries.
            foreach(Transform tree in holder){var p=new Vector2(tree.position.x,tree.position.z);var local=p-land.Origin;if(local.x>100&&local.y>100&&local.x<land.Side-100&&local.y<land.Side-100){center=p;break;}}
            var rig=view.GetComponentInChildren<CameraSystem.CityForgeCameraController>();rig.SetZoomLevel(5);var distant=rig.CaptureRegionState();distant.size=300;rig.RestoreRegionState(distant);yield return Frames();
            tools.Select(NatureTool.Trees);Require(!tools.ForestBrush.Armed,"Opening forests immediately armed removal");
            Require(tools.Popup.transform.localScale==Vector3.one*2,"Popup did not double in size");
            tools.Popup.transform.Find("Option1").GetComponent<Button>().onClick.Invoke();Require(tools.ForestBrush.Armed&&!tools.Popup.gameObject.activeSelf,"Remove did not arm/collapse popup");
            var brush=tools.ForestBrush;Require(Mathf.Abs(brush.Radius-200/(2*Mathf.PI))<.001f,"200m circumference interpreted incorrectly");
            brush.ToggleSize();Require(brush.Circumference==400,"Double brush size failed");brush.ToggleSize();Require(brush.Circumference==200,"Brush toggle did not return");
            var before=land.CanonicalTreeInstances;int expected=before.Count(t=>(land.Origin+new Vector2(t.position.x,t.position.z)*land.Side-center).sqrMagnitude<=brush.Radius*brush.Radius);
            var spatial=holder.GetComponent<SpatialForestRenderer>();int submissions=spatial.SubmittedInstances,rebuilds=spatial.SelectionRebuilds;Require(submissions>0,"Check did not enter distant instancing");
            int removed=brush.RemoveAt(center);Require(removed==expected&&removed>0,"Wrong canonical trees removed");yield return Frames();
            Require(land.CanonicalTreeInstances.Length==before.Length-removed&&land.MixedForest.TreeCount==before.Length-removed&&spatial.TreeCount==land.MixedForest.TreeCount,"Visible and canonical populations diverged");
            Require(spatial.SelectionRebuilds>rebuilds&&spatial.SubmittedInstances<submissions,"Distant instance batches retained removed trees");
            foreach(var tree in land.CanonicalTreeInstances){var p=land.Origin+new Vector2(tree.position.x,tree.position.z)*land.Side;Require((p-center).sqrMagnitude>brush.Radius*brush.Radius,"Tree survived inside circle");Require(before.Any(t=>t.position.x==tree.position.x&&t.position.z==tree.position.z&&t.prototypeIndex==tree.prototypeIndex),"Outside tree changed");}
            brush.ToggleSize();int second=brush.RemoveAt(center);Require(second>0,"400m brush did not remove additional trees");yield return Frames();
            brush.ToggleSize();var middle=land.Origin+Vector2.one*land.Side*.5f;
            var desired=center+new Vector2(center.x>middle.x?-180:180,center.y>middle.y?-90:90);Vector2 end=center;float best=float.PositiveInfinity;
            foreach(var tree in land.CanonicalTreeInstances){var p=land.Origin+new Vector2(tree.position.x,tree.position.z)*land.Side;if(Vector2.Distance(p,center)<brush.Radius*4)continue;float distance=(p-desired).sqrMagnitude;if(distance<best){best=distance;end=p;}}
            Require(float.IsFinite(best),"No drag endpoint available");brush.BeginDrag(center);int stationary=view.ForestRemovals.Length;
            for(int i=0;i<20;i++)brush.ContinueDrag(center);Require(view.ForestRemovals.Length==stationary,"Stationary holding accumulated edits");
            int revision=view.WoodlandRevision;int dragRemoved=brush.ContinueDrag(end,true);brush.EndDrag();Require(dragRemoved>0&&view.WoodlandRevision==revision+1,"Fast drag did not batch removal");
            Require(view.ForestRemovals.Length>stationary+1,"Fast drag skipped interpolated points");
            var segment=end-center;
            foreach(var tree in land.CanonicalTreeInstances){var p=land.Origin+new Vector2(tree.position.x,tree.position.z)*land.Side;float t=Mathf.Clamp01(Vector2.Dot(p-center,segment)/segment.sqrMagnitude);Require(Vector2.Distance(p,center+segment*t)>brush.Radius*.95f,"Fast drag left a gap in its corridor");}
            int afterRelease=view.ForestRemovals.Length;brush.ContinueDrag(end+Vector2.one*100);Require(view.ForestRemovals.Length==afterRelease,"Unheld brush continued painting");
            yield return Frames();int remaining=land.CanonicalTreeInstances.Length;
            File.AppendAllText(Report,"PASS continuous drag removed "+dragRemoved+" additional trees: interpolated fast motion has no corridor gaps, stationary hold is a no-op, one batched data revision, release stops painting.\n");
            string signature=land.MixedForest.PlacementSignature;tools.Dismiss();Require(!brush.Armed&&Cursor.visible,"Dismiss did not restore cursor");
            foreach(var season in new[]{RegionSeason.Fall,RegionSeason.Winter,RegionSeason.Summer}){view.SetSeason(season);yield return Frames();Require(land.CanonicalTreeInstances.Length==remaining,"Season regrew trees");}
            File.AppendAllText(Report,"PASS 200/400m circumference toggle, explicit arming, doubled popup, "+removed+" + "+second+" trees removed, unchanged outside trees, immediate distant batch refresh and seasonal stability.\n");
            var saved=SavedRegionStore.Capture(view);SavedRegionStore.Write(saved,"Library/ForestRemovalRoundtrip.cfregion");
            // A stale cache must also respect the region-wide persisted edit mask.
            var stale=SavedRegionStore.Load("Library/ForestRemovalRoundtrip.cfregion");stale.meadow.trees=before;SavedRegionStore.Write(stale,"Library/ForestRemovalStaleCache.cfregion");
            view.ReturnToMenu();yield return null;open=Open(SavedRegionStore.Load("Library/ForestRemovalStaleCache.cfregion"));while(open.MoveNext())yield return open.Current;
            view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(view.ApprovedDistrict.CanonicalTreeInstances.Length==remaining&&view.ApprovedDistrict.MixedForest.PlacementSignature==signature,"Disk/stale cache reopened removed trees");
            File.AppendAllText(Report,"PASS persisted metadata and full disk reopen reject removed trees in stale district cache.\n");
            var regenerate=SavedRegionStore.Load("Library/ForestRemovalRoundtrip.cfregion");regenerate.meadow=null;regenerate.info.meadowCacheDistrict=-1;
            view.ReturnToMenu();yield return null;open=Open(regenerate);while(open.MoveNext())yield return open.Current;
            view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();Require(view.ApprovedDistrict.CanonicalTreeInstances.Length==remaining&&view.ApprovedDistrict.MixedForest.PlacementSignature==signature,"Regenerated district regrew trees");
            File.AppendAllText(Report,"PASS regeneration without a cache preserves removal; normal player saves untouched.\n");
            view.ReturnToMenu();yield return null;open=Open(SavedRegionStore.Load(checkpoint));while(open.MoveNext())yield return open.Current;
            view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();view.GetComponentInChildren<CameraSystem.CityForgeCameraController>().RestoreRegionState(original.info.camera);view.GetComponent<NatureToolbar>().Select(NatureTool.Trees);
            File.AppendAllText(Report,"PASS original user checkpoint restored without test deletions; forest popup ready.\n");
        }
    }
}
#endif
