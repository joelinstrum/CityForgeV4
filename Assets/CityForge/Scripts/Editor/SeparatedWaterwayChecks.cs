#if UNITY_EDITOR
using System;
using CityForge.World;
using UnityEngine;
namespace CityForge.Editor {
public static class SeparatedWaterwayChecks {
 static void Assert(bool ok,string m){if(!ok)throw new Exception(m);}
 [UnityEditor.MenuItem("CityForge/Checks/Separated waterways")]
 public static void Run(){
 int cases=0;
 foreach(bool current in new[]{false,true})foreach(bool mountain in new[]{false,true})foreach(int grid in new[]{2,8,16})for(int size=0;size<3;size++)for(int seed=0;seed<30;seed++)for(int rivers=0;rivers<=3;rivers++)for(int streams=0;streams<=2;streams++){
 var recipe=new RegionCreationSettings{seed=seed,rivers=rivers,streams=streams,districtGrid=grid,waterwayVersion=current?5:(mountain?2:1),terrainShape=mountain?RegionTerrainShape.MountainBorder:RegionTerrainShape.Land};
 float side=NativeCampPlanner.MapSide(size,grid);var bodies=RegionalRiverNetwork.CreateSeparated(recipe,side);
 Assert(bodies.Count==rivers+(streams==0?0:streams==1?2:5),"count");
 foreach(var b in bodies){if(mountain)Assert(b.channelAlongX&&b.Bounds.yMax+b.bankWidthNormalized<.70f,"mountain range and foothill clearance");if(b.isStream)Assert(b.halfWidthNormalized*side*2>=(current?1.8288f:3.048f)-.0001f&&b.halfWidthNormalized*side*2<=(current?6.0961f:3.6576f),"width");}
 // Every complete bank envelope is contained in a separate x interval. This is
 // a stronger check than testing intersection only at the route vertices.
 for(int i=0;i<bodies.Count;i++)for(int j=i+1;j<bodies.Count;j++){
 var a=bodies[i];var b=bodies[j];
 float gap=(mountain?Math.Max(b.Bounds.yMin-a.Bounds.yMax,a.Bounds.yMin-b.Bounds.yMax):Math.Max(b.Bounds.xMin-a.Bounds.xMax,a.Bounds.xMin-b.Bounds.xMax))-a.bankWidthNormalized-b.bankWidthNormalized;
 Assert(gap>0,"overlapping graded banks");}
 cases++;
 }
 var oldRecipe=new RegionCreationSettings{rivers=1,streams=2};Assert(oldRecipe.waterwayVersion==0,"legacy default");
 var old=RegionalRiverNetwork.Create(oldRecipe);Assert(old[0].halfWidthNormalized==.014f&&old[0].channelVersion==0,"legacy trunk");
 var heights=new float[513,513];var distances=new float[513,513];var data=new PrototypeRegionData(1,heights,distances);
 var route=RegionalRiverNetwork.CreateSeparated(new RegionCreationSettings{streams=1},4096)[0];data.WaterBodies=new[]{route};
 foreach(int j in new[]{0,32,64,96,128}){
 var p=route.centerline[j];float bed=RegionalRiverNetwork.ContinuousHeight(data,p.x,p.y,.04f);
 Assert(bed<route.surfaceLevelNormalized,"wet center");
 float dry=RegionalRiverNetwork.ContinuousHeight(data,p.x+route.halfWidthNormalized*1.1f,p.y,.04f);
 Assert(dry>route.surfaceLevelNormalized,"dry outside stream");
 }
 var combined=RegionalRiverNetwork.CreateSeparated(new RegionCreationSettings{rivers=3,streams=2},4096);data.WaterBodies=combined.ToArray();
 var random=new System.Random(714);
 for(int i=0;i<10000;i++){
 var p=new Vector2((float)random.NextDouble(),(float)random.NextDouble());float expected=.02f;
 foreach(var body in combined)expected=Mathf.Min(expected,body.DistanceToCenterline(p,out _)-body.halfWidthNormalized);
 Assert(Mathf.Abs(RegionalRiverNetwork.SignedDistance(data,p)-expected)<.000001f,"optimized distance matches full segment search");
 }
 // Exercise the formerly expensive five-stream overview path. Cache samples
 // match analytic heights at atlas pixels; district sampling remains analytic.
 var mountainRoutes=RegionalRiverNetwork.CreateSeparated(new RegionCreationSettings{rivers=3,streams=2,waterwayVersion=2,terrainShape=RegionTerrainShape.MountainBorder},4096);
 data.WaterBodies=mountainRoutes.ToArray();
 for(int i=0;i<10000;i++){
 var point=new Vector2((float)random.NextDouble(),(float)random.NextDouble());float expected=.02f;
 foreach(var body in mountainRoutes)expected=Mathf.Min(expected,body.DistanceToCenterline(point,out _)-body.halfWidthNormalized);
 Assert(Mathf.Abs(RegionalRiverNetwork.SignedDistance(data,point)-expected)<.000001f,"horizontal route optimized distances");
 if(point.y>=.75f)Assert(RegionalRiverNetwork.ContinuousHeight(data,point.x,point.y,.4f)==.4f,"mountain heights remain unchanged");
 }
 var cache=new RegionalReliefHeightCache(data);var build=cache.BuildStages();int frames=0;
 while(build.MoveNext())frames++;
 Assert(frames>0,"overview height prepass yields to loading UI");
 for(int i=0;i<1000;i++){
 int x=random.Next(1025),z=random.Next(1025);float u=x/1024f,v=z/1024f;
 Assert(Mathf.Abs(cache.Sample(u,v)-data.SmoothHeight(u,v))<.0000001f,"overview cache matches channel profile at pixels");
 }
 var first=combined[0];var oldBounds=first.Bounds;first.halfWidthNormalized*=.5f;
 Assert(first.Bounds.width<oldBounds.width,"bounds cache invalidates on width change");
 var copied=(Vector2[])first.centerline.Clone();copied[0].x-=.1f;first.centerline=copied;
 Assert(first.Bounds.xMin<oldBounds.xMin,"bounds cache invalidates on route replacement");
 var flush=RegionalRiverNetwork.CreateSeparated(new RegionCreationSettings{rivers=1,streams=2,waterwayVersion=3,terrainShape=RegionTerrainShape.MountainBorder},4096);
 for(int z=0;z<513;z++)for(int x=0;x<513;x++)heights[z,x]=(35+x*.01f+z*.005f)/PrototypeRegionData.Height;
 data.WaterBodies=flush.ToArray();
 foreach(var body in flush)for(int j=1;j<128;j+=7){
 var p=body.centerline[j];float original=SmoothRiverSurface.Sample(heights,p.x,p.y);
 float water=RegionalRiverNetwork.GroundWaterLevel(data,p.x,p.y);
 Assert(Mathf.Abs(original*PrototypeRegionData.Height-water-.20f)<.0001f,"water is20cm below original meadow");
 Assert(RegionalRiverNetwork.ContinuousHeight(data,p.x,p.y,original)*PrototypeRegionData.Height<water,"bed below local water");
 var across=body.channelAlongX?Vector2.up:Vector2.right;var dry=p+across*(body.halfWidthNormalized+2f/4096);
 float ground=SmoothRiverSurface.Sample(heights,dry.x,dry.y);
 Assert(RegionalRiverNetwork.ContinuousHeight(data,dry.x,dry.y,ground)==ground,"no broad bank incision outside ribbon");
 }
 data.WaterBodies=old.ToArray();Assert(RegionalRiverNetwork.ContinuousHeight(data,.5f,.5f,.123f)==.123f,"legacy heights unchanged");
 Debug.Log("PASS: "+cases+" recipes; counts, legacy and6–20ft stream widths, disjoint bank envelopes, wet/dry profiles, legacy defaults.");
 }
}

}
#endif
