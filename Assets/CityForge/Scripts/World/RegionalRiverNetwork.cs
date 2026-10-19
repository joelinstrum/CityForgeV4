using System.Collections.Generic;
using UnityEngine;
namespace CityForge.World
{
    /// <summary>Seeded connected valley routes; the same curves constrain landforms and water masks.</summary>
    public static class RegionalRiverNetwork
    {
        public static float MainCenter(float v,int seed)
        {float phase=(seed%997)*.0063f;return .5f+.11f*Mathf.Sin(v*7.1f+phase)+.045f*Mathf.Sin(v*14.2f+phase*.4f);}
        public static List<RegionWaterBody> Create(RegionCreationSettings recipe)
        {
            int total=recipe.rivers+(recipe.streams==0?0:recipe.streams==1?2:5);var result=new List<RegionWaterBody>();if(total==0)return result;
            var main=new Vector2[129];for(int j=0;j<main.Length;j++){float v=j/128f;main[j]=new Vector2(MainCenter(v,recipe.seed),v);}
            result.Add(Body("regional-trunk",main,recipe.rivers>0?.014f:.003f));
            for(int i=1;i<total;i++){
                float join=.12f+.64f*i/total,head=Mathf.Min(.98f,join+.18f+.04f*Mathf.Sin(i*3.7f));int sign=i%2==0?1:-1;
                var points=new Vector2[65];for(int j=0;j<points.Length;j++){float t=j/64f,v=Mathf.Lerp(join,head,t);float u=Mathf.Lerp(MainCenter(join,recipe.seed),Mathf.Clamp01(MainCenter(head,recipe.seed)+sign*(.22f+.04f*Mathf.Sin(i))),t)+.018f*Mathf.Sin(t*9+i)*Mathf.Sin(t*Mathf.PI);points[j]=new Vector2(u,v);}
                result.Add(Body("regional-tributary-"+i,points,i<recipe.rivers?.006f:.0025f));
            }
            return result;
        }
        static RegionWaterBody Body(string id,Vector2[] points,float width)=>new RegionWaterBody{id=id,kind=WaterBodyKind.River,centerline=points,halfWidthNormalized=width,flowSpeed=.12f,appearanceProfile="calm-river",hasSurfaceLevel=true,surfaceLevelNormalized=12f/PrototypeRegionData.Height};
        public static float BankDistance(RegionWaterBody body,float u,float v)
        {
            var points=body.centerline;float distance=float.PositiveInfinity;var point=new Vector2(u,v);for(int k=0;k<points.Length-1;k++){var a=points[k];var delta=points[k+1]-a;float t=Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude);distance=Mathf.Min(distance,Vector2.Distance(point,a+delta*t));}return distance-body.halfWidthNormalized;
        }
    }
}
