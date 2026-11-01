using System;using System.Collections.Generic;using UnityEngine;using UnityEngine.Rendering;
namespace CityForge.World {
/// <summary>Seeded individual stones in the actual wet bed, outside scanned cluster footprints.</summary>
public sealed class IndividualStreamRockScatter:MonoBehaviour {
 RiverBedRockAssets assets;Matrix4x4[][] batches;Bounds bounds;
 public int RockCount {get;private set;}
 public int EmergingCount {get;private set;}
 public int VariantCount=>assets?assets.meshes.Length:0;
 public void Initialize(MeadowDistrictLandscape land,StreamRockStudy clusters){
  assets=Resources.Load<RiverBedRockAssets>("IndividualStreamRocks/Stones");if(!assets||assets.meshes.Length!=10)throw new InvalidOperationException("Prepare the ten private individual stream rocks first.");
  var lists=new List<Matrix4x4>[assets.meshes.Length];for(int i=0;i<lists.Length;i++)lists[i]=new List<Matrix4x4>();
  var rng=new System.Random(land.View.Data.Seed^762941);float R()=>(float)rng.NextDouble();
  for(float z=-506;z<506;z+=1.15f)for(int j=0;j<3;j++){
   float zz=z+(R()-.5f)*1.15f,x=StreamRockTestRegion.Center(zz)+(R()*2-1)*StreamRockTestRegion.HalfWidth(zz)*.9f;
   float band=R(),diameter=band<.62f?Mathf.Lerp(.16f,.40f,R()):band<.92f?Mathf.Lerp(.40f,.75f,R()):Mathf.Lerp(.85f,1.5f,R());
   int type=rng.Next(lists.Length);float heading=R()*360;var p=new Vector2(x,zz);
   if(!land.Contains(p)||clusters.OverlapsCluster(p,diameter*.71f+.12f))continue;
   float ground=land.StreamRockHeight(p);if(ground>StreamRockTestRegion.WaterLevel-.04f)continue;
   float height=ground-diameter*.12f;
   var scale=Vector3.one*diameter;var rotation=Quaternion.Euler(0,heading,0);
   lists[type].Add(Matrix4x4.TRS(new Vector3(x,height,zz),rotation,scale));RockCount++;
   if(height+assets.meshes[type].bounds.max.y*diameter>StreamRockTestRegion.WaterLevel)EmergingCount++;
  }
  batches=new Matrix4x4[lists.Length][];for(int i=0;i<lists.Length;i++)batches[i]=lists[i].ToArray();
  bounds=new Bounds(new Vector3(land.Origin.x+land.Side/2,15,land.Origin.y+land.Side/2),new Vector3(land.Side+4,30,land.Side+4));
 }
 public float BedShelf(Vector2 point){
  float shelf=0;foreach(var batch in batches)foreach(var matrix in batch){var position=matrix.GetColumn(3);float radius=matrix.GetColumn(0).magnitude*.72f+.10f;float distance=Vector2.Distance(point,new Vector2(position.x,position.z));shelf=Mathf.Max(shelf,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(radius,radius+.35f,distance)));if(shelf>=1)return 1;}return shelf;
 }
 void LateUpdate(){
  if(!assets||batches==null)return;
  for(int i=0;i<batches.Length;i++)if(batches[i].Length>0){var parameters=new RenderParams(assets.materials[i]){worldBounds=bounds,shadowCastingMode=ShadowCastingMode.On,receiveShadows=true};Graphics.RenderMeshInstanced(parameters,assets.meshes[i],0,batches[i]);}
 }
}
}
