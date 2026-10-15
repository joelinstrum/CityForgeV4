using System;using UnityEngine;
namespace CityForge.World {
public static class HillsMountainTestRegion {
public const string Id="hills-and-mountain-test";
public static SavedRegionSnapshot Generate(int seed=730424){
const float side=2048,vertical=360;var heights=new float[513,513];var water=new float[513,513];
float offset=new System.Random(seed).Next(100,10000);
float Hill(float x,float z,float cx,float cz,float rx,float rz,float h)=>h*Mathf.Exp(-((x-cx)*(x-cx)/(rx*rx)+(z-cz)*(z-cz)/(rz*rz))*1.7f);
for(int z=0;z<513;z++)for(int x=0;x<513;x++){
float px=x/512f*side-side/2,pz=z/512f*side-side/2;
float valleyX=360+55*Mathf.Sin(pz*.005f),valley=Mathf.SmoothStep(0,1,Mathf.InverseLerp(65,230,Mathf.Abs(px-valleyX)));
float hills=Hill(px,pz,170,240,230,250,75)+Hill(px,pz,690,230,250,310,90);
float mountains=Hill(px,pz,160,770,250,350,235)+Hill(px,pz,750,790,290,320,285)+Hill(px,pz,590,590,220,210,155);
// Unequal secondary summits and a saddle, blended out below the mountain belt.
float upper=Mathf.SmoothStep(0,1,Mathf.InverseLerp(420,650,pz));
mountains+=upper*(Hill(px,pz,235,900,130,150,24)+Hill(px,pz,850,720,125,170,31)-Hill(px,pz,640,780,110,130,26));
float detail=(Mathf.PerlinNoise(offset+px*.012f,offset+pz*.012f)-.5f)*18+(Mathf.PerlinNoise(offset+px*.031f,offset+pz*.031f)-.5f)*5;
float north=Mathf.SmoothStep(0,1,Mathf.InverseLerp(350,650,pz));
float h=18+hills*valley+mountains*Mathf.Lerp(valley,1,north*.65f)+detail*Mathf.Clamp01((hills+mountains)/120);
heights[z,x]=Mathf.Clamp01(h/vertical);water[z,x]=10000;
}
RefineMountains(heights,seed);
return new SavedRegionSnapshot{data=new PrototypeRegionData(seed,heights,water){WaterBodies=Array.Empty<RegionWaterBody>()},info=new SavedRegionInfo{id=Id,name="Hills and Mountain Test",seed=seed,size=1,district=3,meadow=true,waterless=true,terrainHeightRange=vertical,savedUtc=DateTime.UtcNow.ToString("O"),camera=new RegionCameraState{focus=new Vector3(500,75,510),pitch=35.26439f,yaw=315,distance=700,size=310,zoom=4}}};
}
// MapMagic's installed hydraulic erosion implementation, baked once into the saved authoritative heightfield.
static void RefineMountains(float[,] heights,int seed){
const int n=513;var rect=new Den.Tools.CoordRect(0,0,n,n);var matrix=new Den.Tools.Matrices.Matrix(rect);
var original=(float[,])heights.Clone();float phase=(seed%997)*.013f;
for(int z=0;z<n;z++)for(int x=0;x<n;x++){
float px=x*4-1024,pz=z*4-1024,h=heights[z,x]*360;
float weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(420,620,pz))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(100,190,h));
float ridge=1-Mathf.Abs(Mathf.PerlinNoise(px*.008f+phase+500,pz*.008f+700)*2-1);
float warp=Mathf.PerlinNoise(px*.007f+100,pz*.007f+200)*3;
float gullies=Mathf.Pow(Mathf.Clamp01((Mathf.PerlinNoise(px*.022f+warp+300,pz*.022f+400)-.35f)*2.4f),2);
matrix.arr[z*n+x]=Mathf.Clamp01((h+weight*((ridge-.65f)*42-gullies*15))/360);
}
var order=new Den.Tools.Matrix2D<int>(rect);var torrents=new Den.Tools.Matrices.Matrix(rect);var mud=new Den.Tools.Matrices.Matrix(rect);var sediment=new Den.Tools.Matrices.Matrix(rect);
for(int i=0;i<6;i++){Den.Tools.Erosion.SetOrder(matrix,order);Den.Tools.Erosion.MaskBorders(order);Den.Tools.Erosion.CreateTorrents(matrix,order,torrents);Den.Tools.Erosion.Erode(matrix,torrents,mud,order,.83f,1,.65f);Den.Tools.Erosion.TransferSettleMudflow(matrix,mud,sediment,order,3);}
for(int z=0;z<n;z++)for(int x=0;x<n;x++){
float h=original[z,x]*360,pz=z*4-1024;float weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(420,620,pz))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(100,190,h));
heights[z,x]=Mathf.Lerp(original[z,x],Mathf.Clamp01(matrix.arr[z*n+x]),weight);
}
}

}}
