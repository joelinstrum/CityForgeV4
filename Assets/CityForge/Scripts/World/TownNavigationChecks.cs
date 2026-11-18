#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using CityForge.CameraSystem;
using CityForge.Lots;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace CityForge.Editor
{
 public static class TownNavigationChecks
 {
  static void Require(bool value,string message){if(!value)throw new Exception(message);}
  static void Call(object target,string method)=>target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,null);
  static void CheckWorkObstacles()
  {
   var root=new GameObject("Disposable cached obstacles");var view=root.AddComponent<World.PrototypeRegionView>();
   var catalog=LotAssetCatalog.Load();
   try
   {
    var building=catalog.entries.First(a=>a.category=="Buildings"&&a.prefab);
    var lot=new World.DistrictLotRecord{center=new Vector2(20,25),RotationSteps=1,definition=new LotDefinition{objects=new System.Collections.Generic.List<LotObjectDefinition>{new LotObjectDefinition{assetId=building.id,yaw=30},new LotObjectDefinition{assetId="weathered-picket-section",position=new Vector2(10,-4),fenceLength=3.5f}}}};
    view.PlacedLots.Add(lot);
    bool Reference(Vector2 point,float clearance)
    {
     foreach(var record in view.PlacedLots)
     {
      var d=point-record.center;var q=Quaternion.Euler(0,-record.FacingDegrees,0)*new Vector3(d.x,0,d.y);
      foreach(var item in record.definition.objects)
      {
       var asset=catalog.Find(item.assetId);if(asset==null||(asset.category!="Buildings"&&asset.category!="Fences"))continue;
       var bounds=LotRuntimeLoader.Footprint(item,asset);bounds.min-=Vector2.one*clearance;bounds.max+=Vector2.one*clearance;
       if(bounds.Contains(new Vector2(q.x,q.z)))return true;
      }
     }
     return false;
    }
    var random=new System.Random(81294);
    for(int i=0;i<2000;i++){var point=new Vector2((float)random.NextDouble()*80-20,(float)random.NextDouble()*80-20);float clearance=i%2==0?.4f:.9f;Require(view.LumberjackObstacle(point,clearance)==Reference(point,clearance),"Cached obstacle changed rotated clearance");}
    int builds=view.WorkObstacleBuilds;for(int i=0;i<1000;i++)view.LumberjackObstacle(Vector2.zero);
    Require(view.WorkObstacleBuilds==builds,"Obstacle cache rebuilt without edits");
    lot.center+=Vector2.right*100;
    typeof(World.PrototypeRegionView).GetField("<LotPresentationBuilds>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(view,view.LotPresentationBuilds+1);
    Require(view.LumberjackObstacle(lot.center)&&!view.LumberjackObstacle(new Vector2(20,25))&&view.WorkObstacleBuilds==builds+1,"Moved-lot cache invalidation failed");
    view.PlacedLots.Clear();Require(!view.LumberjackObstacle(lot.center),"Removed lot retained collision");
    catalog.Find(building.id);long start=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++)catalog.Find(building.id);
    Require(GC.GetAllocatedBytesForCurrentThread()-start<128,"Catalog lookups still allocate per call");
    Debug.Log("WORK COLLISION CHECKS PASS: cached footprints match 2000 rotated building/fence clearance probes; reuse, move/remove invalidation; 10000 catalog lookups without per-call allocation.");
   }
   finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  static void CheckNavigationBatches()
  {
   var root=new GameObject("Disposable navigation forest batches");
   try
   {
    var cameraObject=new GameObject("Moving camera");cameraObject.transform.SetParent(root.transform);
    var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=475;
    var quality=cameraObject.AddComponent<CameraMotionQuality>();Call(quality,"OnEnable");
    var holder=new GameObject("Captured forest");holder.transform.SetParent(root.transform);
    var library=World.MixedForestLibrary.Load();
    foreach(float x in new[]{-300f,-100f,100f,300f}){var tree=UnityEngine.Object.Instantiate(library.prefabs[0],holder.transform);tree.transform.position=new Vector3(x,0,0);}
    var renderer=holder.AddComponent<SpatialForestRenderer>();renderer.viewCamera=camera;renderer.Capture();Call(renderer,"LateUpdate");
    int batches=renderer.SubmittedBatches,instances=renderer.SubmittedInstances;
    Require(batches>0&&renderer.BatchCellMultiplier==1,"Stationary forest lost approved batch cells");
    var reduced=typeof(CameraMotionQuality).GetField("<ReducedQuality>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic);reduced.SetValue(quality,true);Call(renderer,"LateUpdate");
    Require(renderer.BatchCellMultiplier==4&&renderer.SubmittedBatches<batches&&renderer.SubmittedInstances==instances,"Coarser batches changed instances or failed to reduce draws");
    int rebuilds=renderer.SelectionRebuilds;camera.transform.position+=Vector3.right*10;Call(renderer,"LateUpdate");
    Require(renderer.SelectionRebuilds==rebuilds,"Orthographic pan rebuilt forest groups per frame");
    reduced.SetValue(quality,false);Call(renderer,"LateUpdate");
    Require(renderer.BatchCellMultiplier==1&&renderer.SubmittedInstances==instances,"Settling did not restore fine forest cells");
    Debug.Log("NAVIGATION FOREST BATCH CHECKS PASS: same tree instances and existing meshes/materials, fewer coarse-cell submissions, no per-frame orthographic regroup, fine cells restored.");
   }
   finally{UnityEngine.Object.DestroyImmediate(root);}
  }
  public static void Run()
  {
   LotPropStyleChecks.Run();
   CheckWorkObstacles();
   CheckNavigationBatches();
   var root=new GameObject("Disposable navigation detail check");
   var rig=root.AddComponent<CityForgeCameraController>();rig.SetZoomLevel(1);
   var quality=root.AddComponent<CameraMotionQuality>();Call(quality,"OnEnable");
   var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
   float scale=pipeline?pipeline.renderScale:1,lod=QualitySettings.lodBias;int cascades=pipeline?pipeline.shadowCascadeCount:1,shadowResolution=pipeline?pipeline.mainLightShadowmapResolution:4096;float split=pipeline?pipeline.cascade2Split:.5f;int events=0;quality.QualityChanged+=reduced=>events++;
   try
   {
    var catalog=LotAssetCatalog.Load();var asset=catalog.entries.First(a=>a.category=="Buildings"&&a.prefab&&a.distantPrefab);
    var instance=LotRuntimeLoader.InstantiateObject(new LotObjectDefinition{assetId=asset.id},asset,root.transform,World.RegionSeason.Summer,rig);
    var detail=instance.GetComponent<World.DistrictBuildingDetail>();Require(detail,"No building detail component");
    Renderer[] Renderers(string field)=>(Renderer[])typeof(World.DistrictBuildingDetail).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(detail);
    var close=Renderers("closeRenderers");var far=Renderers("distantRenderers");
    Require(close.All(r=>r.enabled)&&far.All(r=>!r.enabled),"Stationary close view lost authored detail");
    Call(quality,"LateUpdate");root.transform.position+=Vector3.right;Call(quality,"LateUpdate");
    Require(quality.ReducedQuality&&close.All(r=>!r.enabled)&&far.All(r=>r.enabled),"Panning did not switch building to reduced model");
    for(int i=0;i<20;i++){root.transform.position+=Vector3.right;Call(quality,"LateUpdate");}
    Require(events==1,"Motion resent quality changes each frame");
    typeof(CameraMotionQuality).GetField("lastMotion",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(quality,Time.unscaledTime-1f);Call(quality,"LateUpdate");
    Require(events==2,"Settling failed to publish one restoration event");
    Require(!quality.ReducedQuality&&close.All(r=>r.enabled)&&far.All(r=>!r.enabled),"Stopping quality did not restore close detail");
    Require(QualitySettings.lodBias==lod&&(!pipeline||Mathf.Approximately(pipeline.renderScale,scale)),"Original quality not restored exactly");
    rig.SetZoomLevel(4);Call(quality,"OnEnable");Call(quality,"LateUpdate");root.transform.position+=Vector3.right;Call(quality,"LateUpdate");
    Require(!pipeline||(pipeline.shadowCascadeCount==Mathf.Min(cascades,2)&&pipeline.mainLightShadowmapResolution==shadowResolution&&Mathf.Approximately(pipeline.renderScale,scale*.65f)),"Wide-panning reduction did not activate");
    Call(quality,"OnDisable");
    Require(far.All(r=>r.enabled)&&close.All(r=>!r.enabled),"Settling at distant zoom forced full detail");
    Require(!pipeline||(pipeline.shadowCascadeCount==cascades&&pipeline.mainLightShadowmapResolution==shadowResolution&&pipeline.cascade2Split==split&&pipeline.renderScale==scale),"Wide-panning shadow/resolution settings were not restored");
    rig.SetZoomLevel(1);quality.ReduceAuthoredDetailDuringNavigation=false;Call(quality,"OnEnable");Call(quality,"LateUpdate");root.transform.position+=Vector3.right;Call(quality,"LateUpdate");
    Require(quality.ReducedQuality&&close.All(r=>r.enabled),"Benchmark baseline isolation changed authored meshes");Call(quality,"OnDisable");
    Debug.Log("TOWN NAVIGATION CHECKS PASS: imported buildings switch to reduced models while moving, no repeated event per moving frame, exact restoration, distant zoom retained, baseline isolation, shared pebble and lot/path regressions.");
   }
   finally{Call(quality,"OnDisable");UnityEngine.Object.DestroyImmediate(root);}
  }
 }
}
#endif
