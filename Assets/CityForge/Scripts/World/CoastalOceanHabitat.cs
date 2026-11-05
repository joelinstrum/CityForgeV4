using System;
using System.Collections.Generic;
using UnityEngine;
namespace CityForge.World
{
    /// <summary>Ocean-only, union-aware navigation. Streams and lakes never qualify.</summary>
    public sealed class CoastalOceanHabitat
    {
        readonly MeadowDistrictLandscape land;
        readonly RegionWaterBody[] oceans;
        public readonly List<Vector2> PatrolPoints=new();
        public float Sea=>land.View.SeaLevel;
        public CoastalOceanHabitat(MeadowDistrictLandscape source)
        {
            land=source;oceans=Array.FindAll(land.View.Data.WaterBodies,b=>b.kind==WaterBodyKind.Ocean);
            const float step=24;
            for(float z=land.Origin.y+12;z<land.Origin.y+land.Side-12;z+=step)for(float x=land.Origin.x+12;x<land.Origin.x+land.Side-12;x+=step){
                var p=new Vector2(x,z);if(!SharkPoint(p))continue;
                float shore=float.PositiveInfinity;foreach(var ocean in oceans)shore=Mathf.Min(shore,Mathf.Abs(land.LakeShoreDistance(ocean,Sea,p)));
                if(shore>=14&&shore<=180)PatrolPoints.Add(p);
            }
        }
        public bool OceanPoint(Vector2 p,float depth)
        {
            if(!land.Contains(p)||Sea-land.Height(p)<depth)return false;
            var uv=p/land.View.MapSide+Vector2.one*.5f;
            foreach(var ocean in oceans)if(ocean.Bounds.Contains(uv)&&ocean.Contains(uv))return true;
            return false;
        }
        public bool SharkPoint(Vector2 p)=>OceanPoint(p,2.2f)&&OceanPoint(p+Vector2.right*2.8f,2.2f)&&OceanPoint(p-Vector2.right*2.8f,2.2f)&&OceanPoint(p+Vector2.up*2.8f,2.2f)&&OceanPoint(p-Vector2.up*2.8f,2.2f);
        public bool SharkPath(Vector2 a,Vector2 b)
        {
            int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)));
            for(int i=0;i<=steps;i++)if(!SharkPoint(Vector2.Lerp(a,b,i/(float)steps)))return false;return true;
        }
    }
}
