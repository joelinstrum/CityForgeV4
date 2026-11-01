using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CityForge.World {
/// <summary>Private scanned patches and approved bank shrubs for the opt-in stream fixture.</summary>
public sealed class StreamRockStudy:MonoBehaviour {
 RiverBedRockAssets assets;Matrix4x4[] patches,inversePatches;Bounds bounds;
 public int PatchCount=>patches?.Length??0;
 public int EmergingPatchCount {get;private set;}
 public int ShrubCount {get;private set;}
 public float MinimumCenterClearance {get;private set;}=float.PositiveInfinity;
 public void Initialize(MeadowDistrictLandscape land){
  assets=Resources.Load<RiverBedRockAssets>("StreamRocks/Rocks");if(!assets||assets.meshes.Length==0)throw new System.InvalidOperationException("Prepare the private stream rock assets first.");
  bool revised=StreamRockTestRegion.Revised(land.View),refined=StreamRockTestRegion.Refined(land.View);
  var random=new System.Random(land.View.Data.Seed);float R()=>(float)random.NextDouble();var instances=new List<Matrix4x4>();
  for(float z=-505;z<505;z+=6.3f){
   float zz=z+(R()-.5f)*2,x=StreamRockTestRegion.Center(zz)+(R()-.5f)*2;
   var p=new Vector2(x,zz);if(!land.Contains(p))continue;
   float scale=Mathf.Lerp(revised?.60f:.72f,revised?.98f:1.05f,R()),height=land.StreamRockHeight(p)-.10f;
   float heading=R()*360;
   // Long irregular gaps between clusters expose continuous stretches of the bed.
   if(revised&&Mathf.Sin(z*.055f)+.55f*Mathf.Sin(z*.137f)<-.2f)continue;
   var size=Vector3.one*scale;
   if(refined&&!StreamRockTestRegion.IndividualMix(land.View)){
    float sign=Mathf.Sin(z*.067f)>0?1:-1;
    float dx=(StreamRockTestRegion.Center(zz+1)-StreamRockTestRegion.Center(zz-1))*.5f;
    heading=Mathf.Atan2(dx,1)*Mathf.Rad2Deg+(heading/360-.5f)*36;
    size=new Vector3(Mathf.Lerp(.23f,.36f,scale),Mathf.Lerp(.65f,.85f,scale),Mathf.Lerp(.40f,.60f,scale));
    float angle=heading*Mathf.Deg2Rad;var extent=assets.meshes[0].bounds.extents;
    float ex=Mathf.Abs(Mathf.Cos(angle))*extent.x*size.x+Mathf.Abs(Mathf.Sin(angle))*extent.z*size.z;
    float ez=Mathf.Abs(Mathf.Sin(angle))*extent.x*size.x+Mathf.Abs(Mathf.Cos(angle))*extent.z*size.z;
    x=StreamRockTestRegion.Center(zz)+sign*(StreamRockTestRegion.HalfWidth(zz)*.85f);
    // Keep even the conservative projected bounds away from a connected centre route.
    float clearance=float.PositiveInfinity;
    for(int i=0;i<=8;i++)clearance=Mathf.Min(clearance,sign*(x-StreamRockTestRegion.Center(zz+Mathf.Lerp(-ez,ez,i/8f)))-ex);
    if(clearance<1){x+=sign*(1-clearance);clearance=1;}
    MinimumCenterClearance=Mathf.Min(MinimumCenterClearance,clearance);
    p=new Vector2(x,zz);if(!land.Contains(p))continue;
    height=Mathf.Min(land.StreamRockHeight(p),StreamRockTestRegion.WaterLevel+.05f)-.20f;
   }
   instances.Add(Matrix4x4.TRS(new Vector3(x,height,zz),Quaternion.Euler(0,heading,0),size));
   if(height+assets.meshes[0].bounds.max.y*size.y>StreamRockTestRegion.WaterLevel)EmergingPatchCount++;
  }
  patches=instances.ToArray();inversePatches=new Matrix4x4[patches.Length];for(int i=0;i<patches.Length;i++)inversePatches[i]=patches[i].inverse;
  if(StreamRockTestRegion.IndividualMix(land.View))gameObject.AddComponent<IndividualStreamRockScatter>().Initialize(land,this);
  bounds=new Bounds(new Vector3(land.Origin.x+land.Side/2,15,land.Origin.y+land.Side/2),new Vector3(land.Side+16,30,land.Side+16));
  // Use the native Terrain tree path so seasonal materials, LOD and culling match the biome.
  var trees=new List<TreeInstance>(land.DisplayTerrain.terrainData.treeInstances);
  var library=Resources.Load<SeasonalTreeLibrary>("Seasons/SeasonalTrees");var prefabs=library?library.Prefabs(land.View.VisualSeason):null;
  var shrubs=new List<int>();if(prefabs!=null)for(int i=0;i<prefabs.Length&&i<land.DisplayTerrain.terrainData.treePrototypes.Length;i++){var prefab=prefabs[i];if(prefab&&(prefab.name.ToLowerInvariant().Contains("bush")||prefab.name.ToLowerInvariant().Contains("willow")))shrubs.Add(i);}
  for(float z=-498;z<498;z+=11){
   float sign=R()<.5f?-1:1;var p=new Vector2(StreamRockTestRegion.Center(z)+sign*(StreamRockTestRegion.HalfWidth(z,revised)+Mathf.Lerp(-.4f,1.7f,R())),z);
   if(!land.Contains(p)||shrubs.Count==0)continue;
   float h=land.Height(p);if(h<StreamRockTestRegion.WaterLevel-.12f)continue;
   int prototype=shrubs[random.Next(shrubs.Count)];float rotation=R()*Mathf.PI*2,scale=Mathf.Lerp(.5f,.85f,R());
   var position=new Vector3((p.x-land.Origin.x)/land.Side,(h-.06f)/land.DisplayTerrain.terrainData.size.y,(p.y-land.Origin.y)/land.Side);
   // Cached study districts already contain these deterministic placements.
   bool exists=trees.Exists(t=>t.prototypeIndex==prototype&&Mathf.Abs(t.position.x-position.x)<.000001f&&Mathf.Abs(t.position.z-position.z)<.000001f);
   if(!exists)trees.Add(new TreeInstance{position=position,prototypeIndex=prototype,widthScale=scale,heightScale=scale,rotation=rotation,color=Color.white,lightmapColor=Color.white});ShrubCount++;
  }
  land.DisplayTerrain.terrainData.SetTreeInstances(trees.ToArray(),false);
 }
 public float BedShelf(Vector2 point){
  float shelf=0;var extent=assets.meshes[0].bounds.extents;
  for(int i=0;i<patches.Length;i++){var local=inversePatches[i].MultiplyPoint3x4(new Vector3(point.x,0,point.y));float distance=Mathf.Max(Mathf.Abs(local.x)-extent.x,Mathf.Abs(local.z)-extent.z)*patches[i].GetColumn(0).magnitude;shelf=Mathf.Max(shelf,1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.1f,.6f,distance)));if(shelf>=1)return 1;}
  return shelf;
 }
 public bool OverlapsCluster(Vector2 point,float radius){
  var extent=assets.meshes[0].bounds.extents;
  for(int i=0;i<patches.Length;i++){var local=inversePatches[i].MultiplyPoint3x4(new Vector3(point.x,0,point.y));float margin=radius/patches[i].GetColumn(0).magnitude;if(Mathf.Abs(local.x)<extent.x+margin&&Mathf.Abs(local.z)<extent.z+margin)return true;}return false;
 }
 void LateUpdate(){if(!assets||patches==null||patches.Length==0)return;var parameters=new RenderParams(assets.materials[0]){worldBounds=bounds,shadowCastingMode=ShadowCastingMode.On,receiveShadows=true};Graphics.RenderMeshInstanced(parameters,assets.meshes[0],0,patches);}
}
}
