using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using UnityEngine;
using UnityEngine.Rendering;
namespace CityForge.World {
public sealed partial class PrototypeRegionView {
 internal static Vector2[] DenseBoulevardLine(IReadOnlyList<Vector2> line,bool closed,float step=2f){
  var points=new List<Vector2>();if(line==null||line.Count<2)return points.ToArray();
  for(int i=0;i<line.Count-(closed?0:1);i++){var a=line[i];var b=line[(i+1)%line.Count];int count=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/step));for(int j=0;j<count;j++)points.Add(Vector2.Lerp(a,b,j/(float)count));}
  if(!closed)points.Add(line[^1]);return points.ToArray();
 }
 private IEnumerable<(Vector2[] Points,bool Closed)> VehicleRoadRoutes()=>districtRoadMeshes.Keys.Select(r=>(Points:MotorBoulevardCenterline(r),Closed:r.closed)).Concat(BridgeRoadRoutes());
 private Vector2[] MotorBoulevardCenterline(DistrictRoadRecord road){
  var original=RoadCenterline(road);if(!IsDividedBoulevard(road.surfaceId))return original.ToArray();
  var line=DenseBoulevardLine(original,road.closed);var result=new Vector2[line.Length];float total=0;for(int i=1;i<line.Length;i++)total+=Vector2.Distance(line[i-1],line[i]);float along=0;
  for(int i=0;i<line.Length;i++){
   if(i>0)along+=Vector2.Distance(line[i-1],line[i]);var before=line[i==0?(road.closed?line.Length-1:0):i-1];var after=line[i==line.Length-1?(road.closed?0:i):i+1];var tangent=(after-before).normalized;
   float blend=road.closed?1:Mathf.SmoothStep(0,1,Mathf.Min(along,total-along)/10f);
   foreach(var other in districtRoadMeshes.Keys)if(other!=road){float gap=LineDistance(line[i],RoadCenterline(other),other.closed)-other.width*.5f-2;blend=Mathf.Min(blend,Mathf.SmoothStep(0,1,gap/8f));}
   // One-way motor traffic uses a paved carriageway, never the planted center.
   result[i]=line[i]+new Vector2(tangent.y,-tangent.x)*((road.width+4)*.25f)*blend;
  }
  return result;
 }
 private void PresentBoulevardMedians(DistrictRoadRecord[] roads,Action<string,Mesh,Material> add){
  var divided=roads.Where(r=>IsDividedBoulevard(r.surfaceId)).ToArray();if(divided.Length==0)return;
  var curb=LotPathMaterial.Find("weathered-concrete-pavement");var turf=LotPathMaterial.Find("tileable-grass");var curbMat=curb.CreateMaterial();var turfMat=turf.CreateMaterial();curbMat.renderQueue=turfMat.renderQueue=DistrictLotSurfaces.SidewalkRenderQueue+1;sidewalkAssets.Add(curbMat);sidewalkAssets.Add(turfMat);
  foreach(var road in divided){var line=DenseBoulevardLine(RoadCenterline(road),road.closed,1);if(line.Length<2)continue;var part=new List<Vector2>();float total=0;for(int i=1;i<line.Length;i++)total+=Vector2.Distance(line[i-1],line[i]);float at=0;bool firstClear=false;
   for(int i=0;i<line.Length;i++){if(i>0)at+=Vector2.Distance(line[i-1],line[i]);var point=line[i];bool clear=road.closed||at>=8&&at<=total-8;
    if(clear)foreach(var other in roads)if(other!=road&&LineDistance(point,RoadCenterline(other),other.closed)<other.width*.5f+12){clear=false;break;}
    if(i==0)firstClear=clear;if(clear)part.Add(point);else Flush();
   }if(road.closed&&firstClear&&part.Count>0)part.Add(line[0]);Flush();
   void Flush(){if(part.Count>=2){var path=new LotPathDefinition{points=part.ToArray(),width=4};add("Boulevard median curb",LotPathSurface.BuildMesh(path,curb.TileSize,.15f,LotHeight),curbMat);path.width=3;add("Boulevard planted median",LotPathSurface.BuildMesh(path,turf.TileSize,.17f,LotHeight),turfMat);}part.Clear();}
  }
 }
}
}
