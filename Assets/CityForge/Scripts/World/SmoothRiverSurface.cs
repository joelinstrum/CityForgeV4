using UnityEngine;
namespace CityForge.World
{
    /// <summary>Monotone cubic height interpolation: continuous bank slopes without overshooting saved height samples.</summary>
    public static class SmoothRiverSurface
    {
        static float Slope(float a,float b)=>a*b<=0?0:2*a*b/(a+b);
        static float Curve(float a,float b,float c,float d,float t){float t2=t*t,t3=t2*t;return (2*t3-3*t2+1)*b+(t3-2*t2+t)*Slope(b-a,c-b)+(-2*t3+3*t2)*c+(t3-t2)*Slope(c-b,d-c);}
        public static float Sample(float[,] heights,float u,float v)
        {
            float px=Mathf.Clamp01(u)*512,pz=Mathf.Clamp01(v)*512;int x=Mathf.Min(511,(int)px),z=Mathf.Min(511,(int)pz);float tx=px-x,tz=pz-z;
            int xm=Mathf.Max(0,x-1),xp=x+1,xpp=Mathf.Min(512,x+2);
            float Row(int row)=>Curve(heights[row,xm],heights[row,x],heights[row,xp],heights[row,xpp],tx);
            return Curve(Row(Mathf.Max(0,z-1)),Row(z),Row(z+1),Row(Mathf.Min(512,z+2)),tz);
        }
    }
}
