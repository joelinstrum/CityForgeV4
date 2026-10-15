using CityForge.World;
using CityForge.CameraSystem;
using UnityEditor;
using UnityEngine;
namespace CityForge.Editor
{
    public static class DistantTreeChecks
    {
        [MenuItem("CityForge/Validate Distant Trees")]
        public static void Validate()
        {
            var view=Object.FindFirstObjectByType<PrototypeRegionView>();
            if(!EditorApplication.isPlaying || view==null) throw new System.InvalidOperationException("Open a region first.");
            var far=view.GetComponent<DistantTreeRenderer>();var camera=view.GetComponentInChildren<CityForgeCameraController>();
            var settings=ForestPresentationSettings.Load();
            var variantCount=Resources.LoadAll<GameObject>(settings.resourceFolder).Length;
            var forest=view.transform.Find("RandomTrees");view.ShowOverview();
            foreach(var level in new[]{5,4,3,2,1,5})
            {
                camera.SetZoomLevel(level);far.Refresh();
                if(far.IsFar!=(level>=4)) throw new System.InvalidOperationException("Incorrect tree representation for zoom "+level);
                if(level>=4)
                {
                    if(far.FarTreeCount!=view.TreeCount || far.BatchCount!=variantCount || far.QuadTreeCount!=view.TreeCount)
                        throw new System.InvalidOperationException("Distant tree batch count/geometry mismatch.");
                    foreach(var renderer in forest.GetComponentsInChildren<Renderer>())
                        if(renderer.enabled) throw new System.InvalidOperationException("Detailed geometry still rendering at far zoom.");
                }
                else foreach(var lod in forest.GetComponentsInChildren<LODGroup>())
                {
                    var levels=lod.GetLODs();
                    for(var index=0;index<levels.Length;index++) foreach(var renderer in levels[index].renderers)
                        if(renderer.enabled!=(index==settings.MeshTier(level,levels.Length))) throw new System.InvalidOperationException("Incorrect active tree mesh tier at zoom "+level);
                }
            }
            for(var district=0;district<4;district++)
            {
                view.EnterDistrict(district);far.Refresh();var count=0;
                foreach(Transform tree in forest) if(tree.gameObject.activeSelf) count++;
                if(far.FarTreeCount!=count) throw new System.InvalidOperationException("Distant trees leaked across district boundaries.");
            }
            var diagnostic=view.GetComponent<RegionPreviewDiagnostics>();diagnostic.SetDiagnostic(true,1);far.Refresh();
            if(far.IsFar || far.FarTreeCount!=0) throw new System.InvalidOperationException("Diagnostic retained distant trees.");
            view.ShowOverview();far.Refresh();
            Debug.Log($"CityForge: distant tree validation passed: all five zoom levels, {variantCount} tree image batches, {view.TreeCount} represented trees / {far.FarTriangleCount} triangles including shadow proxies, all districts and grayscale hiding.");
        }
    }
}
