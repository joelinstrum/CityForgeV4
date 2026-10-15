using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    /// <summary>Versioned recipe; saved samples remain authoritative when opening a region.</summary>
    [Serializable] public sealed class RegionCreationSettings
    {
        public int version=1, seed=PrototypeRegionData.DefaultSeed, topography, rivers, streams;
        public string era, period;
        public void Validate()
        {
            if(version!=1||topography<0||topography>4||rivers<0||rivers>3||streams<0||streams>2)
                throw new ArgumentException("Unsupported region creation settings.");
        }
    }

    public static class RegionMapGenerator
    {
        public static SavedRegionSnapshot Generate(string name,RegionSize size,RegionCreationSettings settings)
        {
            settings.Validate();
            if((int)size<0||(int)size>2||string.IsNullOrWhiteSpace(name))throw new ArgumentException("Invalid region name or size.");
            // Copy the recipe so later wizard edits cannot change the region's provenance.
            var recipe=JsonUtility.FromJson<RegionCreationSettings>(JsonUtility.ToJson(settings));
            
            var heights=new float[513,513];var distances=new float[513,513];
            var bodies=new List<RegionWaterBody>();var rng=new System.Random(recipe.seed);
            float offset=rng.Next(100,10000);
            int count=recipe.rivers+(recipe.streams==0?0:recipe.streams==1?2:5);
            for(int i=0;i<count;i++){
                bool stream=i>=recipe.rivers;
                float center=(i+1f)/(count+1), phase=(float)rng.NextDouble()*6.28f;
                var points=new Vector2[129];
                for(int j=0;j<points.Length;j++){float v=j/128f;points[j]=new Vector2(center+.025f*Mathf.Sin(v*9+phase),v);}
                bodies.Add(new RegionWaterBody{id=(stream?"stream-":"river-")+i,kind=WaterBodyKind.River,centerline=points,
                    halfWidthNormalized=stream?.004f:.012f,flowSpeed=.12f,appearanceProfile="calm-river",hasSurfaceLevel=true,surfaceLevelNormalized=12f/PrototypeRegionData.Height});
            }
            for(int z=0;z<513;z++)for(int x=0;x<513;x++){
                float u=x/512f,v=z/512f;
                float broad=Mathf.PerlinNoise(offset+u*3,offset+v*3);
                float detail=Mathf.PerlinNoise(offset+u*8,offset+v*8);
                float relief=recipe.topography switch {
                    0=>0,
                    1=>broad*90,
                    2=>Mathf.Pow(Mathf.Max(0,(detail-.45f)/.55f),2)*150,
                    3=>broad*75+Mathf.Pow(Mathf.Max(0,(detail-.5f)*2),2)*300,
                    _=>broad*80+Mathf.Pow(Mathf.Clamp01((u-.45f)/.55f),2)*(220+detail*350)
                };
                float h=35+relief, bank=float.PositiveInfinity;
                foreach(var body in bodies){
                    float row=v*128;int j=Mathf.Min(127,(int)row);
                    float cx=Mathf.Lerp(body.centerline[j].x,body.centerline[j+1].x,row-j);
                    float d=Mathf.Abs(u-cx)-body.halfWidthNormalized;
                    bank=Mathf.Min(bank,d);
                }
                if(bodies.Count>0){float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.003f,.025f,bank));h=Mathf.Lerp(5,h,blend);}
                heights[z,x]=h/PrototypeRegionData.Height;
                distances[z,x]=bodies.Count==0?PrototypeRegionData.Side:bank*PrototypeRegionData.Side;
            }
            var data=new PrototypeRegionData(recipe.seed,heights,distances){WaterBodies=bodies.ToArray()};
            return new SavedRegionSnapshot{data=data,info=new SavedRegionInfo{id=Guid.NewGuid().ToString("N"),name=name.Trim(),seed=recipe.seed,size=(int)size,
                district=-1,meadow=true,waterless=bodies.Count==0,waterBodies=data.WaterBodies,creation=recipe,savedUtc=DateTime.UtcNow.ToString("O")}};
        }
    }
}
