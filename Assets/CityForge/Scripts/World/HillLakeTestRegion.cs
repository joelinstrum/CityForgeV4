using System;
using UnityEngine;
namespace CityForge.World
{
    /// <summary>A seeded terrain recipe for testing inland lake presentation.</summary>
    public static class HillLakeTestRegion
    {
        public const string Id="meadow-hill-lake-test";
        public static SavedRegionSnapshot Generate(int seed=730423)
        {
            const float side=1024,level=8;float vertical=PrototypeRegionData.Height*side/PrototypeRegionData.Side;
            var random=new System.Random(seed);float phase=(float)random.NextDouble()*6.28f;
            var lake=new Vector2(300+(float)random.NextDouble()*16,245+(float)random.NextDouble()*18);
            var hill=lake+new Vector2(-155,105);float rx=72+(float)random.NextDouble()*8,rz=55+(float)random.NextDouble()*8;
            float Radius(float angle)=>1+.075f*Mathf.Sin(angle*3+phase)+.035f*Mathf.Sin(angle*5-phase);
            var outline=new Vector2[96];for(int i=0;i<outline.Length;i++){float a=i*Mathf.PI*2/outline.Length;var p=lake+new Vector2(Mathf.Cos(a)*rx,Mathf.Sin(a)*rz)*Radius(a);outline[i]=p/side+Vector2.one*.5f;}
            var body=new RegionWaterBody{id="hill-lake",kind=WaterBodyKind.Lake,outline=outline,appearanceProfile="calm-lake",hasSurfaceLevel=true,surfaceLevelNormalized=level/vertical};
            var heights=new float[513,513];var distance=new float[513,513];
            for(int z=0;z<513;z++)for(int x=0;x<513;x++){
                var p=new Vector2(x/512f*side-side/2,z/512f*side-side/2);var q=p-lake;
                float angle=Mathf.Atan2(q.y/rz,q.x/rx);float r=new Vector2(q.x/rx,q.y/rz).magnitude/Radius(angle);
                float d=(r-1)*Mathf.Min(rx,rz);float h;
                if(d<0)h=level-6*Mathf.SmoothStep(0,1,Mathf.Clamp01(-d/35));
                else {float shelf=Mathf.SmoothStep(0,1,Mathf.Clamp01(d/14));h=level+2.6f*shelf;
                    var delta=p-hill;h+=22*Mathf.Exp(-(delta.x*delta.x/(95*95)+delta.y*delta.y/(110*110)))*Mathf.SmoothStep(0,1,Mathf.Clamp01(d/32));
                    h+=(Mathf.PerlinNoise(p.x/90+phase+400,p.y/90+800)-.5f)*1.1f*shelf;}
                heights[z,x]=Mathf.Clamp01(h/vertical);distance[z,x]=d/(side/PrototypeRegionData.Side);
            }
            var data=new PrototypeRegionData(seed,heights,distance){WaterBodies=new[]{body}};
            return new SavedRegionSnapshot{data=data,info=new SavedRegionInfo{id=Id,name="Meadow — Hill and Lake",seed=seed,size=0,district=3,meadow=true,waterless=false,minimalRiverCrest=false,riverBedVersion=0,waterBodies=data.WaterBodies,savedUtc=DateTime.UtcNow.ToString("O"),camera=new RegionCameraState{focus=new Vector3(lake.x-65,level,lake.y+45),pitch=35.26439f,yaw=315,distance=280,size=80,zoom=2}}};
        }
    }
}
