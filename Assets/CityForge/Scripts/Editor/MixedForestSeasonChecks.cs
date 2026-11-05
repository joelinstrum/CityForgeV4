#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using CityForge.World;
using CityForge.CameraSystem;
namespace CityForge.Editor
{
    public static class MixedForestSeasonChecks
    {
        [MenuItem("CityForge/Forest/Check summer visibility after seasons")]
        public static void Begin()
        {
            var view=UnityEngine.Object.FindFirstObjectByType<PrototypeRegionView>();
            if(!Application.isPlaying||!view||!view.ApprovedDistrict||!view.ApprovedDistrict.MixedForest||!view.ApprovedDistrict.MixedForest.Ready)throw new InvalidOperationException("Open an approved mixed-forest district in Play mode first.");
            view.StartCoroutine(Check(view));
        }
        static void Press(PrototypeRegionView view,RegionSeason season)=>view.transform.Find("RegionHud/Season"+season).GetComponent<Button>().onClick.Invoke();
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        static IEnumerator Frames(int count){for(int i=0;i<count;i++)yield return null;}
        static IEnumerator Check(PrototypeRegionView view)
        {
            var land=view.ApprovedDistrict;var forest=land.MixedForest;var holder=land.transform.Find("Approved mixed woodland");
            var spatial=holder.GetComponent<SpatialForestRenderer>();var rig=view.GetComponentInChildren<CityForgeCameraController>();
            var oldCamera=rig.CaptureRegionState();bool oldControl=rig.enabled,oldTrees=land.TreesEnabled;var oldSeason=view.Season;
            string signature=forest.PlacementSignature;int treeCount=forest.TreeCount,canonicalCount=land.CanonicalTreeInstances.Length;
            var report=new StringBuilder();bool complete=false;
            try{
                MixedForestLibrary.Load().Validate();land.SetTreesEnabled(true);rig.enabled=false;
                var focus=holder.GetChild(0);int best=0;
                for(int i=0;i<holder.childCount;i+=25){var tree=holder.GetChild(i);int nearby=0;foreach(Transform other in holder)if((other.position-tree.position).sqrMagnitude<3600)nearby++;if(nearby>best){best=nearby;focus=tree;}}
                var state=rig.CaptureRegionState();state.focus=focus.position;rig.RestoreRegionState(state);
                foreach(int zoom in new[]{2,4,2}){
                    rig.SetZoomLevel(zoom);yield return Frames(3);int rebuilds=spatial.SelectionRebuilds;
                    Press(view,RegionSeason.Fall);yield return Frames(3);Require(!holder.gameObject.activeSelf&&land.DisplayTerrain.terrainData.treeInstanceCount>0,"Fall presentation missing");
                    Press(view,RegionSeason.Winter);yield return Frames(3);Require(!holder.gameObject.activeSelf&&land.DisplayTerrain.terrainData.treeInstanceCount>0,"Winter presentation missing");
                    Press(view,RegionSeason.Summer);yield return Frames(8);Require(holder.gameObject.activeSelf,"Summer forest root remains hidden");
                    bool instanced=spatial.viewCamera.orthographicSize>=spatial.minimumInstancedViewSize;
                    if(instanced){Require(spatial.SubmittedInstances>0&&spatial.SubmittedBatches>0,"Summer instancing did not submit trees");Require(spatial.SelectionRebuilds>rebuilds,"Season activation reused stale LOD selection");}
                    else Require(holder.GetComponentsInChildren<LODGroup>().Any(l=>l.enabled)&&holder.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&r.isVisible),"Summer individual LODs did not render");
                    // Verify actual output, not just active flags or submission counters.
                    yield return new WaitForEndOfFrame();var with=ScreenCapture.CaptureScreenshotAsTexture();
                    holder.gameObject.SetActive(false);yield return Frames(12);yield return new WaitForEndOfFrame();var without=ScreenCapture.CaptureScreenshotAsTexture();
                    holder.gameObject.SetActive(true);yield return Frames(8);
                    Require(with.width==without.width&&with.height==without.height,"Game view resized during check");
                    var a=with.GetPixels32();var b=without.GetPixels32();int changed=0,sampled=0;
                    for(int y=with.height/5;y<with.height*4/5;y+=4)for(int x=with.width/10;x<with.width*7/10;x+=4){int index=y*with.width+x;var p=a[index];var q=b[index];if(Math.Abs(p.r-q.r)+Math.Abs(p.g-q.g)+Math.Abs(p.b-q.b)>38)changed++;sampled++;}
                    UnityEngine.Object.Destroy(with);UnityEngine.Object.Destroy(without);
                    Require(changed>sampled*.04f,"Summer forest contributes no substantial visible pixels");
                    Require(forest.TreeCount==treeCount&&forest.PlacementSignature==signature&&land.CanonicalTreeInstances.Length==canonicalCount,"Season switching changed stored forest");
                    report.AppendLine("PASS UI Fall/Winter/Summer at zoom "+zoom+"; backend="+(instanced?"spatial":"individual")+"; visible pixel changes="+changed+"/"+sampled);
                }
                land.SetTreesEnabled(false);Press(view,RegionSeason.Winter);Press(view,RegionSeason.Summer);yield return Frames(3);
                Require(!holder.gameObject.activeSelf&&!land.DisplayTerrain.drawTreesAndFoliage,"Season switch overrode Trees Off");
                land.SetTreesEnabled(true);yield return Frames(3);Require(holder.gameObject.activeSelf,"Trees On did not reactivate summer forest");
                report.AppendLine("PASS Trees Off/On across season changes").AppendLine("Approved mixed trees="+treeCount+"; signature="+signature+"; canonical trees="+canonicalCount).AppendLine("No saved regions or private art changed.");complete=true;
            }finally{
                Press(view,oldSeason);land.SetTreesEnabled(oldTrees);rig.RestoreRegionState(oldCamera);rig.enabled=oldControl;
                if(!complete)report.AppendLine("FAIL: seasonal render check did not complete; see Console.");
                File.WriteAllText("Library/MixedForestSeasonChecks.txt",report.ToString());Debug.Log(report.ToString());
            }
        }
    }
}
#endif
