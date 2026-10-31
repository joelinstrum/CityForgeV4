using System;
using System.Collections.Generic;
using UnityEngine;
namespace CityForge.World
{
    public enum WaterBodyKind { Unknown=0, River=1, Ocean=2, Lake=3, Pond=4 }
    [Serializable] public sealed class RegionWaterContour {public Vector2[] points;}
    [Serializable]
    public sealed class RegionWaterBody
    {
        public string id;
        public WaterBodyKind kind;
        // Coordinates are normalized over the whole region, independent of district/camera.
        public Vector2[] outline,centerline;
        // Land holes inside an ocean polygon; absent in legacy saves.
        public RegionWaterContour[] holes;
        public IEnumerable<Vector2[]> Contours {get {if(outline!=null)yield return outline;if(holes!=null)foreach(var hole in holes)if(hole?.points!=null)yield return hole.points;}}
        public float halfWidthNormalized,flowSpeed;
        public bool hasSurfaceLevel;public float surfaceLevelNormalized;
        public string appearanceProfile;
        public float DistanceToCenterline(Vector2 point,out Vector2 downstream)
        {
            float best=float.PositiveInfinity;downstream=Vector2.zero;
            if(centerline==null)return best;
            for(int i=1;i<centerline.Length;i++){var a=centerline[i-1];var delta=centerline[i]-a;float t=delta.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude):0;float distance=Vector2.Distance(point,a+delta*t);
                if(distance<best){best=distance;downstream=-delta.normalized;}}
            return best;
        }
        public bool Contains(Vector2 point)
        {
            if(centerline!=null&&DistanceToCenterline(point,out _)<=halfWidthNormalized)return true;
            if(!PolygonContains(outline,point))return false;
            if(holes!=null)foreach(var hole in holes)if(PolygonContains(hole?.points,point))return false;
            return true;
        }
        static bool PolygonContains(Vector2[] points,Vector2 p)
        {
            if(points==null||points.Length<3)return false;bool inside=false;
            for(int i=0,j=points.Length-1;i<points.Length;j=i++)if((points[i].y>p.y)!=(points[j].y>p.y)&&p.x<(points[j].x-points[i].x)*(p.y-points[i].y)/(points[j].y-points[i].y)+points[i].x)inside=!inside;
            return inside;
        }
        public bool Intersects(Rect area)
        {
            if(!Bounds.Overlaps(area))return false;
            foreach(var p in new[]{area.min,area.max,new Vector2(area.xMin,area.yMax),new Vector2(area.xMax,area.yMin)})if(Contains(p))return true;
            foreach(var points in Contours)for(int i=0;i<points.Length;i++){
                var a=points[i];var b=points[(i+1)%points.Length];float lo=0,hi=1;var delta=b-a;
                bool Clip(float origin,float direction,float min,float max){if(Mathf.Abs(direction)<1e-8f)return origin>=min&&origin<=max;float t0=(min-origin)/direction,t1=(max-origin)/direction;if(t0>t1)(t0,t1)=(t1,t0);lo=Mathf.Max(lo,t0);hi=Mathf.Min(hi,t1);return lo<=hi;}
                if(Clip(a.x,delta.x,area.xMin,area.xMax)&&Clip(a.y,delta.y,area.yMin,area.yMax))return true;
            }return false;
        }
        public Rect Bounds
        {
            get {bool isRiver=centerline!=null&&centerline.Length>1;var points=isRiver?centerline:outline;if(points==null||points.Length==0)return Rect.zero;var min=points[0];var max=min;foreach(var p in points){min=Vector2.Min(min,p);max=Vector2.Max(max,p);}float width=isRiver?halfWidthNormalized:0;return Rect.MinMaxRect(min.x-width,min.y-width,max.x+width,max.y+width);}
        }
        public static RegionWaterBody[] Generate(PrototypeRegionData data,int riverVersion,bool waterless,float side)
        {
            if(waterless&&riverVersion==0)return Array.Empty<RegionWaterBody>();
            var bodies=new List<RegionWaterBody>();
            RegionWaterBody River(string id,bool tributary){
                var points=new Vector2[65];for(int i=0;i<points.Length;i++){float v=Mathf.Lerp(tributary?.52f:.24f,1,i/64f);points[i]=new Vector2(tributary?data.TributaryRiverCenter(v):data.MainRiverCenter(v),v);}
                return new RegionWaterBody{id=id,kind=WaterBodyKind.River,centerline=points,halfWidthNormalized=(riverVersion>=2?10:tributary?14:22)/side,flowSpeed=.12f,appearanceProfile="calm-river"};
            }
            if(riverVersion<2){
                if(!waterless){var coast=new List<Vector2>{Vector2.zero,Vector2.right};for(int i=64;i>=0;i--){float u=i/64f;coast.Add(new Vector2(u,PrototypeRegionData.Coast(u)));}bodies.Add(new RegionWaterBody{id="ocean",kind=WaterBodyKind.Ocean,outline=coast.ToArray(),appearanceProfile="coastal-ocean"});}
                bodies.Add(River("main-river",false));
            }
            bodies.Add(River("tributary-river",true));return bodies.ToArray();
        }
    }
    /// <summary>Defaults selected by semantic type; never infer an ocean from a blue material.</summary>
    public readonly struct WaterAppearanceProfile
    {
        public readonly bool flow,waves,foam,reflections;
        public readonly float scatteringMultiplier,absorptionMultiplier,lightingMultiplier,rippleStrength,rippleSpeed,rippleScale;
        WaterAppearanceProfile(bool current,bool swell,float scatter,float absorption,float light,float strength,float speed,float scale){flow=current;waves=swell;foam=false;reflections=true;scatteringMultiplier=scatter;absorptionMultiplier=absorption;lightingMultiplier=light;rippleStrength=strength;rippleSpeed=speed;rippleScale=scale;}
        public static WaterAppearanceProfile For(WaterBodyKind kind)=>kind switch {
            WaterBodyKind.River=>new(true,false,1.5f,.6f,1.5f,.10f,0f,.7f),
            WaterBodyKind.Ocean=>new(false,true,1f,1f,1f,.15f,1f,3f),
            WaterBodyKind.Lake=>new(false,false,1.2f,.7f,1.2f,.06f,0f,3f),
            WaterBodyKind.Pond=>new(false,false,1.2f,.5f,1.2f,.03f,0f,2f),
            _=>new(false,false,1f,1f,1f,0,0,3f)
        };
    }
    // Runtime objects point back to the canonical map body. All reaches share one ID.
    public sealed class RegionWaterBodyTag:MonoBehaviour
    {
        public string bodyId;public WaterBodyKind kind;
    }
}
