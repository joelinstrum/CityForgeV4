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
            return RegionBuildSteps.ComputeRows(Resolution,z=>{
                for(int x=0;x<Resolution;x++)heights[z,x]=data.SmoothHeight(x/1024f,z/1024f);
            });
        }
        internal IEnumerator BuildOverviewStages()
        {
            // Filter before shading so abrupt slope changes cannot create a crease
            // in the mesh or a dark line in the atlas.
            var raw=new float[Resolution,Resolution];
            var horizontal=new float[Resolution,Resolution];
            yield return RegionBuildSteps.ComputeRows(Resolution,z=>{
                for(int x=0;x<Resolution;x++)raw[z,x]=data.SmoothHeight(x/1024f,z/1024f,false);
            });
            const int last=Resolution-1;
            yield return RegionBuildSteps.ComputeRows(Resolution,z=>{
                for(int x=0;x<Resolution;x++)
                    horizontal[z,x]=Filter(raw[z,Mathf.Max(0,x-4)],raw[z,Mathf.Max(0,x-2)],raw[z,x],
                        raw[z,Mathf.Min(last,x+2)],raw[z,Mathf.Min(last,x+4)]);
            });
            yield return RegionBuildSteps.ComputeRows(Resolution,z=>{
                int a=Mathf.Max(0,z-4),b=Mathf.Max(0,z-2),d=Mathf.Min(last,z+2),e=Mathf.Min(last,z+4);
                for(int x=0;x<Resolution;x++)
                    heights[z,x]=Filter(horizontal[a,x],horizontal[b,x],horizontal[z,x],horizontal[d,x],horizontal[e,x]);
            });

            static float Filter(float a,float b,float c,float d,float e)=>(a+4*b+6*c+4*d+e)/16;
        }
        public float Sample(float u,float v)
        {
            float px=Mathf.Clamp01(u)*1024,pz=Mathf.Clamp01(v)*1024;
            int x=Mathf.Min(1023,(int)px),z=Mathf.Min(1023,(int)pz);
            return Mathf.Lerp(Mathf.Lerp(heights[z,x],heights[z,x+1],px-x),Mathf.Lerp(heights[z+1,x],heights[z+1,x+1],px-x),pz-z);
        }
    }
}
