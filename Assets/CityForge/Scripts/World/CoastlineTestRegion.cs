using System;using UnityEngine;
namespace CityForge.World {
public static class CoastlineTestRegion {
 public const string Id="coastline-test";public const float Sea=24,HeightRange=160;
 public static float ShoreX(float z)=>335.25755f+75*Mathf.Sin(z*.0045f)-185*Mathf.Exp(-Mathf.Pow((z-780)/145,2));
 public static SavedRegionSnapshot Generate(int seed=730425){
 var heights=new float[513,513];var water=new float[513,513];const float side=2048;
 for(int z=0;z<513;z++)for(int x=0;x<513;x++){
 float px=x*4-1024,pz=z*4-1024,d=px-ShoreX(pz);float head=Mathf.Exp(-Mathf.Pow((pz-780)/155,2));
 float h=d<0?Sea+Mathf.Max(-23,d*.10f):Sea+d*.065f+head*52*Mathf.SmoothStep(0,1,Mathf.Clamp01(d/32));
 h+=Mathf.SmoothStep(0,1,Mathf.Clamp01(d/100))*(Mathf.PerlinNoise(px*.007f+125,pz*.007f+seed%1000)-.5f)*12;
 heights[z,x]=Mathf.Clamp01(h/HeightRange);water[z,x]=Mathf.Max(0,d)*PrototypeRegionData.Side/side;
 }
 var outline=new System.Collections.Generic.List<Vector2>{Vector2.zero,Vector2.up};
 for(int i=64;i>=0;i--){float z=-1024+i*32;outline.Add(new Vector2(ShoreX(z)/side+.5f,z/side+.5f));}
 var ocean=new RegionWaterBody{id="coastal-ocean",kind=WaterBodyKind.Ocean,outline=outline.ToArray(),hasSurfaceLevel=true,surfaceLevelNormalized=Sea/HeightRange,appearanceProfile="coastal-ocean"};
 return new SavedRegionSnapshot{data=new PrototypeRegionData(seed,heights,water){WaterBodies=new[]{ocean}},info=new SavedRegionInfo{id=Id,name="Coastline Test",seed=seed,size=1,district=3,meadow=true,coastline=true,coastVersion=2,waterBodies=new[]{ocean},terrainHeightRange=HeightRange,savedUtc=DateTime.UtcNow.ToString("O"),camera=new RegionCameraState{focus=new Vector3(512,26,512),pitch=35.26439f,yaw=315,distance=700,size=512,zoom=5}}};
 }
}}
