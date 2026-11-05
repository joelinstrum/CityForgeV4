using System.Collections.Generic;
using UnityEngine;
namespace CityForge.Geometry
{
    /// <summary>Consumer-independent polygon operations in world XZ or normalized coordinates.</summary>
    public static class PointWireGeometry
    {
        public static float Area(IReadOnlyList<Vector2> p){float a=0;for(int i=0;i<p.Count;i++){var b=p[(i+1)%p.Count];a+=p[i].x*b.y-b.x*p[i].y;}return Mathf.Abs(a)*.5f;}
        public static Vector2[] RoundClosed(IReadOnlyList<Vector2> guide,int passes=3)
        {
            var p=new List<Vector2>(guide);for(int k=0;k<Mathf.Clamp(passes,0,3);k++){var next=new List<Vector2>(p.Count*2);for(int i=0;i<p.Count;i++){var a=p[i];var b=p[(i+1)%p.Count];next.Add(Vector2.Lerp(a,b,.25f));next.Add(Vector2.Lerp(a,b,.75f));}p=next;}return p.ToArray();
        }
        public static Vector2[] RoundOpen(IReadOnlyList<Vector2> guide,int passes=2)
        {
            var p=new List<Vector2>(guide);for(int k=0;k<Mathf.Clamp(passes,0,3);k++){var next=new List<Vector2>{p[0]};for(int i=1;i<p.Count;i++){next.Add(Vector2.Lerp(p[i-1],p[i],.25f));next.Add(Vector2.Lerp(p[i-1],p[i],.75f));}next.Add(p[p.Count-1]);p=next;}return p.ToArray();
        }
        public static bool Contains(IReadOnlyList<Vector2> p,Vector2 q)
        {
            bool inside=false;for(int i=0,j=p.Count-1;i<p.Count;j=i++)if((p[i].y>q.y)!=(p[j].y>q.y)&&q.x<(p[j].x-p[i].x)*(q.y-p[i].y)/(p[j].y-p[i].y)+p[i].x)inside=!inside;return inside;
        }
        public static float SignedDistance(IReadOnlyList<Vector2> p,Vector2 q)
        {
            float best=float.PositiveInfinity;for(int i=0;i<p.Count;i++){var a=p[i];var d=p[(i+1)%p.Count]-a;float t=d.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(q-a,d)/d.sqrMagnitude):0;best=Mathf.Min(best,(q-a-d*t).sqrMagnitude);}return Mathf.Sqrt(best)*(Contains(p,q)?-1:1);
        }
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        public static bool IsSimple(IReadOnlyList<Vector2> p)
        {
            if(p.Count<3)return false;
            for(int i=0;i<p.Count;i++){
                if(!float.IsFinite(p[i].x)||!float.IsFinite(p[i].y)||(p[i]-p[(i+1)%p.Count]).sqrMagnitude<.00000001f)return false;
                for(int j=i+1;j<p.Count;j++){
                    if(j==i+1||(i==0&&j==p.Count-1))continue;
                    var a=p[i];var d=p[(i+1)%p.Count]-a;var b=p[j];var e=p[(j+1)%p.Count]-b;float cross=Cross(d,e);
                    if(Mathf.Abs(cross)<.000001f){if(Mathf.Abs(Cross(b-a,d))<.000001f&&Mathf.Max(Mathf.Min(a.x,a.x+d.x),Mathf.Min(b.x,b.x+e.x))<=Mathf.Min(Mathf.Max(a.x,a.x+d.x),Mathf.Max(b.x,b.x+e.x))&&Mathf.Max(Mathf.Min(a.y,a.y+d.y),Mathf.Min(b.y,b.y+e.y))<=Mathf.Min(Mathf.Max(a.y,a.y+d.y),Mathf.Max(b.y,b.y+e.y)))return false;continue;}
                    float t=Cross(b-a,e)/cross,u=Cross(b-a,d)/cross;if(t>=0&&t<=1&&u>=0&&u<=1)return false;
                }
            }return Area(p)>.000001f;
        }
    }
}
