using System.Collections;
using UnityEngine;
namespace CityForge.World
{
    /// <summary>Overview-only atlas samples: shade each channel height once,
    /// rather than repeating analytic route queries for every lighting tap.</summary>
    public sealed class RegionalReliefHeightCache
    {
        public const int Resolution=1025;
        readonly PrototypeRegionData data;
        readonly float[,] heights=new float[Resolution,Resolution];
        public RegionalReliefHeightCache(PrototypeRegionData source){data=source;}
        public IEnumerator BuildStages()
        {
            var timer=System.Diagnostics.Stopwatch.StartNew();
            for(int z=0;z<Resolution;z++){
                for(int x=0;x<Resolution;x++)heights[z,x]=data.SmoothHeight(x/1024f,z/1024f);
                if(timer.ElapsedMilliseconds>=8){yield return null;timer.Restart();}
            }
        }
        public float Sample(float u,float v)
        {
            float px=Mathf.Clamp01(u)*1024,pz=Mathf.Clamp01(v)*1024;
            int x=Mathf.Min(1023,(int)px),z=Mathf.Min(1023,(int)pz);
            return Mathf.Lerp(Mathf.Lerp(heights[z,x],heights[z,x+1],px-x),Mathf.Lerp(heights[z+1,x],heights[z+1,x+1],px-x),pz-z);
        }
    }
}
