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
        // Version-one channels are evaluated continuously, not baked at regional resolution.
        public int channelVersion;
        public bool isStream;
        // New mountain-border routes advance along x, parallel to the rear range.
        public bool channelAlongX;
        public float bankWidthNormalized;
        public bool hasSurfaceLevel;public float surfaceLevelNormalized;
        public string appearanceProfile;
        public bool playerChannel;public float[] playerChannelLevels;public Vector2[] playerChannelGuides;public float playerChannelDepth;
        public bool playerCreated;public float playerDepth,playerBankWidth,playerOuterBankWidth;
        public float DistanceToCenterline(Vector2 point,out Vector2 downstream)
        {
            float best=float.PositiveInfinity;downstream=Vector2.zero;
            if(centerline==null)return best;
            for(int i=1;i<centerline.Length;i++){var a=centerline[i-1];var delta=centerline[i]-a;float t=delta.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude):0;float distance=Vector2.Distance(point,a+delta*t);
                if(distance<best){best=distance;downstream=-delta.normalized;}}
            return best;
        }
        // An exact segment hierarchy for immutable generated routes. This changes
        // query cost, not the stored curve or the distance field's resolution.
        [NonSerialized] volatile CenterlineIndex distanceIndex;
        [NonSerialized] internal PlayerChannelSampler playerSampler;
        public float IndexedCenterlineDistance(Vector2 point)
        {
            if(centerline==null||centerline.Length<2)return float.PositiveInfinity;
            if(distanceIndex==null||distanceIndex.Points!=centerline)distanceIndex=new CenterlineIndex(centerline);
            return distanceIndex.Distance(point);
        }
        public bool IndexedCurrentCoordinates(Vector2 point,float side,ref float best,out Vector4 result)
        {
            if(centerline==null||centerline.Length<2){result=default;return false;}
            if(distanceIndex==null||distanceIndex.Points!=centerline)distanceIndex=new CenterlineIndex(centerline);
            return distanceIndex.CurrentCoordinates(point,side,ref best,out result);
        }
        sealed class CenterlineIndex
        {
            public readonly Vector2[] Points;
            struct Node {public Vector2 min,max;public int first,count,left,right;}
            readonly Node[] nodes;readonly float[] lengths,stations;int used;
            public CenterlineIndex(Vector2[] points){
                Points=points;nodes=new Node[points.Length*2];lengths=new float[points.Length-1];stations=new float[points.Length-1];float station=0;
                for(int i=0;i<lengths.Length;i++){stations[i]=station;lengths[i]=(points[i+1]-points[i]).magnitude;if(lengths[i]>=.000001f)station+=lengths[i];}
                Build(0,points.Length-1);
            }
            int Build(int first,int count){
                int index=used++;var min=Points[first];var max=min;
                for(int i=first+1;i<=first+count;i++){min=Vector2.Min(min,Points[i]);max=Vector2.Max(max,Points[i]);}
                var node=new Node{min=min,max=max,first=first,count=count,left=-1,right=-1};
                if(count>8){int half=count/2;node.left=Build(first,half);node.right=Build(first+half,count-half);}
                nodes[index]=node;return index;
            }
            static float LowerBound(Node node,Vector2 p){float x=Mathf.Max(0,Mathf.Max(node.min.x-p.x,p.x-node.max.x)),y=Mathf.Max(0,Mathf.Max(node.min.y-p.y,p.y-node.max.y));return x*x+y*y;}
            public float Distance(Vector2 point){float best=float.PositiveInfinity;Search(0,point,ref best);return Mathf.Sqrt(best);}
            public bool CurrentCoordinates(Vector2 point,float side,ref float best,out Vector4 result){
                int segment=-1;float t=0;SearchCurrent(0,point,ref best,ref segment,ref t);result=default;if(segment<0)return false;
                var delta=Points[segment+1]-Points[segment];var offset=point-Points[segment]-delta*t;var downstream=-delta/lengths[segment];var cross=new Vector2(-downstream.y,downstream.x);
                result=new Vector4(-(stations[segment]+t*lengths[segment])*side,Vector2.Dot(offset,cross)*side,downstream.x,downstream.y);return true;
            }
            void SearchCurrent(int index,Vector2 point,ref float best,ref int segment,ref float fraction){
                var node=nodes[index];if(LowerBound(node,point)>best)return;
                if(node.left<0){for(int i=node.first;i<node.first+node.count;i++){
                    float length=lengths[i];if(length<.000001f)continue;var a=Points[i];var delta=Points[i+1]-a;
                    float t=Mathf.Clamp01(Vector2.Dot(point-a,delta)/(length*length));float distance=(point-a-delta*t).sqrMagnitude;
                    if(distance<best||(distance==best&&segment>=0&&i<segment)){best=distance;segment=i;fraction=t;}
                }return;}
                float left=LowerBound(nodes[node.left],point),right=LowerBound(nodes[node.right],point);
                if(left<=right){SearchCurrent(node.left,point,ref best,ref segment,ref fraction);SearchCurrent(node.right,point,ref best,ref segment,ref fraction);}
                else{SearchCurrent(node.right,point,ref best,ref segment,ref fraction);SearchCurrent(node.left,point,ref best,ref segment,ref fraction);}
            }
            void Search(int index,Vector2 point,ref float best){
                var node=nodes[index];if(LowerBound(node,point)>best)return;
                if(node.left<0){for(int i=node.first;i<node.first+node.count;i++){
                    var a=Points[i];var delta=Points[i+1]-a;float t=delta.sqrMagnitude>0?Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude):0;
                    // Match the original projection expression (including rounding).
                    best=Mathf.Min(best,(point-(a+delta*t)).sqrMagnitude);
                }return;}
                float left=LowerBound(nodes[node.left],point),right=LowerBound(nodes[node.right],point);
                if(left<=right){Search(node.left,point,ref best);Search(node.right,point,ref best);}
                else{Search(node.right,point,ref best);Search(node.left,point,ref best);}
            }
        }
        [NonSerialized] volatile OceanContourIndex oceanIndex;

        // Ocean descriptors are immutable after generation. Bucket edges by latitude so
        // repeated terrain and foam samples only test edges that can cross their ray.
        sealed class OceanContourIndex
        {
            const int Bands = 128;
            public readonly Vector2[] Outline;
            public readonly RegionWaterContour[] Holes;
            readonly (Vector2 a, Vector2 b, int contour)[][] edges;

            public OceanContourIndex(Vector2[] outline, RegionWaterContour[] holes)
            {
                Outline=outline;Holes=holes;
                var rows=new List<(Vector2,Vector2,int)>[Bands];
                Add(outline,1);
                if(holes!=null)for(int i=0;i<holes.Length;i++)Add(holes[i]?.points,1<<(i+1));
                edges=Array.ConvertAll(rows,row=>row?.ToArray()??Array.Empty<(Vector2,Vector2,int)>());

                void Add(Vector2[] contour,int flag)
                {
                    if(contour==null||contour.Length<3)return;
                    for(int i=0,j=contour.Length-1;i<contour.Length;j=i++){
                        var a=contour[i];var b=contour[j];
                        if(a.y==b.y)continue;
                        for(int band=Band(Mathf.Min(a.y,b.y));band<=Band(Mathf.Max(a.y,b.y));band++)
                            (rows[band]??=new List<(Vector2,Vector2,int)>()).Add((a,b,flag));
                    }
                }
            }

            static int Band(float y)=>Mathf.Clamp((int)(y*Bands),0,Bands-1);
            public bool Contains(Vector2 point)
            {
                int parity=0;
                foreach(var (a,b,contour) in edges[Band(point.y)])
                    if((a.y>point.y)!=(b.y>point.y)&&point.x<(b.x-a.x)*(point.y-a.y)/(b.y-a.y)+a.x)
                        parity^=contour;
                return parity==1;
            }
        }

        public bool Contains(Vector2 point)
        {
            if(kind==WaterBodyKind.Ocean&&(holes?.Length??0)<=30&&outline?.Length>=3&&!(centerline?.Length>1)){
                var index=oceanIndex;
                if(index==null||index.Outline!=outline||index.Holes!=holes)
                    oceanIndex=index=new OceanContourIndex(outline,holes);
                return index.Contains(point);
            }
            if(centerline!=null&&(playerChannel?IndexedCenterlineDistance(point):DistanceToCenterline(point,out _))<=halfWidthNormalized)return true;
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
        // Generated channel geometry is immutable after creation/load. Cache only
        // that opt-in geometry; legacy editable contours retain their old behavior.
        [NonSerialized] Vector2[] boundsPoints;
        [NonSerialized] float boundsWidth;
        [NonSerialized] Rect channelBounds;
        public Rect Bounds
        {
            get {if((channelVersion>=1||playerChannel)&&boundsPoints==centerline&&boundsWidth==halfWidthNormalized&&boundsPoints!=null)return channelBounds;bool isRiver=centerline!=null&&centerline.Length>1;var points=isRiver?centerline:outline;if(points==null||points.Length==0)return Rect.zero;var min=points[0];var max=min;foreach(var p in points){min=Vector2.Min(min,p);max=Vector2.Max(max,p);}float width=isRiver?halfWidthNormalized:0;var result=Rect.MinMaxRect(min.x-width,min.y-width,max.x+width,max.y+width);if(channelVersion>=1||playerChannel){channelBounds=result;boundsWidth=halfWidthNormalized;boundsPoints=centerline;}return result;}
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
