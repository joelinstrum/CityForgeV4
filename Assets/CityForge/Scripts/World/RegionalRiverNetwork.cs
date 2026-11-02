using System.Collections.Generic;
using UnityEngine;
namespace CityForge.World
{
    /// <summary>Versioned regional routes; continuous channel curves constrain terrain and water masks.</summary>
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
        // Disjoint corridors guarantee separation over the entire continuous curves,
        // including bank grading. Shuffle types so majors are not always on one side.
        public static List<RegionWaterBody> CreateSeparated(RegionCreationSettings recipe,float side)
        {
            int streams=recipe.streams==0?0:recipe.streams==1?2:5,total=recipe.rivers+streams;
            var result=new List<RegionWaterBody>();if(total==0)return result;
            var random=new System.Random(recipe.seed^0x61731);var lanes=new int[total];
            for(int i=0;i<total;i++)lanes[i]=i;
            for(int i=total-1;i>0;i--){int j=random.Next(i+1);(lanes[i],lanes[j])=(lanes[j],lanes[i]);}
            // Current natural Mountain Border occupies the rear quarter (v>=.75).
            // Keep the entire graded channel envelope in the front70%, flowing
            // across the lowlands parallel to the range, never through its crest.
            bool mountainLowlands=recipe.waterwayVersion>=2&&recipe.version>=9&&recipe.terrainShape==RegionTerrainShape.MountainBorder;
            float span=mountainLowlands?.70f:1f;
            float spacing=span/(total+1),amplitude=Mathf.Min(.025f,spacing*.12f);
            for(int i=0;i<total;i++){
                bool stream=i>=recipe.rivers;float phase=(float)random.NextDouble()*Mathf.PI*2;
                bool meander=stream&&recipe.waterwayVersion>=4;
                // Keep the along-axis monotonic for fast distance lookup. Broad
                // drift plus irregular shorter bends stays inside a disjoint lane.
                float cycles=side/(280f+60f*Mathf.Sin(phase));
                int segments=meander?Mathf.Clamp(Mathf.CeilToInt(cycles*24),128,2048):128;
                var points=new Vector2[segments+1];
                for(int j=0;j<points.Length;j++){
                    float v=j/(float)segments;
                    float bend=amplitude*Mathf.Sin(v*9+phase);
                    if(meander){
                        float local=Mathf.Min(28f/side,spacing*.20f);
                        bend+=local*(.76f*Mathf.Sin(v*cycles*Mathf.PI*2+phase+.65f*Mathf.Sin(v*13+phase))+.24f*Mathf.Sin(v*cycles*Mathf.PI*3.14f+phase*1.7f));
                    }
                    float across=(lanes[i]+1)*spacing+bend;
                    points[j]=mountainLowlands?new Vector2(v,across):new Vector2(across,v);
                }
                // Version5 broadens the seeded per-stream range to6–20 feet.
                // Consume the same one random draw so routes remain reproducible.
                float widthSample=stream?(float)random.NextDouble():0;
                float half=stream?(recipe.waterwayVersion>=5?Mathf.Lerp(1.8288f,6.096f,widthSample):3.048f+widthSample*.4572f)/side/2:.014f;
                var body=Body((stream?"stream-":"river-")+i,points,half);
                body.channelVersion=recipe.waterwayVersion>=3?2:1;body.isStream=stream;body.channelAlongX=mountainLowlands;
                body.bankWidthNormalized=body.channelVersion>=2?2f/side:Mathf.Min(80f/side,spacing*.18f);
                result.Add(body);
            }
            return result;
        }
        public static bool HasContinuousChannels(PrototypeRegionData data)
        {if(data.WaterBodies!=null)foreach(var body in data.WaterBodies)if(body.channelVersion>=1)return true;return false;}
        public static float ContinuousHeight(PrototypeRegionData data,float u,float v,float height)
        {
            if(data.WaterBodies==null)return height;
            foreach(var body in data.WaterBodies)if(body.kind==WaterBodyKind.Ocean&&body.Contains(new Vector2(u,v)))return height;
            foreach(var body in data.WaterBodies){
                if(body.channelVersion<1)continue;
                float reach=body.halfWidthNormalized+body.bankWidthNormalized;
                var bounds=body.Bounds;
                if(u<bounds.xMin-body.bankWidthNormalized||u>bounds.xMax+body.bankWidthNormalized||v<bounds.yMin-body.bankWidthNormalized||v>bounds.yMax+body.bankWidthNormalized)continue;
                float distance=ChannelDistance(body,new Vector2(u,v),reach);
                if(distance>=reach)continue;
                if(body.channelVersion>=2){
                    // Preserve adjacent ground. Only a shallow wetted ribbon is cut;
                    // the surface follows the original ground rather than sea level.
                    if(distance>body.halfWidthNormalized)continue;
                    float localLevel=height-.20f/PrototypeRegionData.Height;
                    foreach(var ocean in data.WaterBodies)if(ocean.kind==WaterBodyKind.Ocean&&ocean.hasSurfaceLevel)localLevel=Mathf.Max(localLevel,ocean.surfaceLevelNormalized);
                    float localBed=localLevel-(body.isStream?.45f:2f)/PrototypeRegionData.Height;
                    height=Mathf.Lerp(localBed,height,Mathf.Pow(Mathf.Clamp01(distance/body.halfWidthNormalized),4));
                    continue;
                }
                float level=body.surfaceLevelNormalized,edge=level+.08f/PrototypeRegionData.Height;
                float bed=level-(body.isStream?.65f:7f)/PrototypeRegionData.Height;
                float channel=Mathf.Lerp(bed,edge,Mathf.Pow(Mathf.Clamp01(distance/body.halfWidthNormalized),4));
                float bank=Mathf.Lerp(edge,Mathf.Max(height,edge),Mathf.SmoothStep(0,1,Mathf.InverseLerp(body.halfWidthNormalized,reach,distance)));
                height=distance<=body.halfWidthNormalized?channel:bank;
            }
            return height;
        }
        public static bool HasGroundFollowingWater(PrototypeRegionData data)
        {if(data.WaterBodies!=null)foreach(var body in data.WaterBodies)if(body.channelVersion>=2)return true;return false;}
        public static float GroundWaterLevel(PrototypeRegionData data,float u,float v)
        {
            float level=SmoothRiverSurface.Sample(data.Heights,u,v)*PrototypeRegionData.Height-.20f;
            if(data.WaterBodies!=null)foreach(var body in data.WaterBodies)if(body.kind==WaterBodyKind.Ocean&&body.hasSurfaceLevel)return Mathf.Max(level,body.surfaceLevelNormalized*PrototypeRegionData.Height);
            return level;
        }
        public static float SignedDistance(PrototypeRegionData data,Vector2 point)
        {
            float distance=.02f;
            if(data.WaterBodies!=null)foreach(var body in data.WaterBodies){
                if(distance<=0)return distance;
                if(body.centerline!=null){var bounds=body.Bounds;if(point.x<bounds.xMin-distance||point.x>bounds.xMax+distance||point.y<bounds.yMin-distance||point.y>bounds.yMax+distance)continue;distance=Mathf.Min(distance,body.channelVersion>=1?ChannelDistance(body,point,distance+body.halfWidthNormalized)-body.halfWidthNormalized:body.DistanceToCenterline(point,out _)-body.halfWidthNormalized);}
                else if(body.kind==WaterBodyKind.Ocean&&body.Contains(point))return -1;
            }
            return distance;
        }
        // New routes have uniformly spaced, monotone coordinates along their axis. A segment
        // outside this axis window cannot be within limit, so terrain/mask sampling
        // need not project onto all route segments per waterway.
        static float ChannelDistance(RegionWaterBody body,Vector2 point,float limit)
        {
            float squared=limit*limit;var points=body.centerline;
            float along=body.channelAlongX?point.x:point.y;int segments=points.Length-1;
            int first=Mathf.Clamp(Mathf.FloorToInt((along-limit)*segments),0,segments-1);
            int last=Mathf.Clamp(Mathf.FloorToInt((along+limit)*segments),0,segments-1);
            for(int i=first;i<=last;i++){
                var a=points[i];var delta=points[i+1]-a;
                float t=Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude);
                squared=Mathf.Min(squared,(point-a-delta*t).sqrMagnitude);
            }
            return Mathf.Sqrt(squared);
        }
        public static float BankDistance(RegionWaterBody body,float u,float v)
        {
            var points=body.centerline;float distance=float.PositiveInfinity;var point=new Vector2(u,v);for(int k=0;k<points.Length-1;k++){var a=points[k];var delta=points[k+1]-a;float t=Mathf.Clamp01(Vector2.Dot(point-a,delta)/delta.sqrMagnitude);distance=Mathf.Min(distance,Vector2.Distance(point,a+delta*t));}return distance-body.halfWidthNormalized;
        }
    }
}
