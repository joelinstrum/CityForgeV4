using System;
using System.Collections.Generic;
using System.Linq;
using CityForge.Lots;
using UnityEngine;

namespace CityForge.World
{
    // Sparse, reversible edits to the runtime district terrain, never the regional heightfield.
    // The lot stores the grade; rebuilding/deleting a lot restores the source samples first.
    internal sealed class DistrictLotGrading : MonoBehaviour
    {
        readonly Dictionary<Vector2Int,float> original = new();
        readonly List<RectInt> patches = new();
        bool dirty;
        Terrain terrain;
        TerrainData data;
        Vector2 spacing,origin;
        float vertical,originY;
        internal readonly HashSet<ZoneGroundPlane> AppliedPlanes=new();
        void Awake(){terrain=GetComponent<Terrain>();data=terrain.terrainData;spacing=new Vector2(data.size.x,data.size.z)/(data.heightmapResolution-1);origin=new Vector2(transform.position.x,transform.position.z);vertical=data.size.y;originY=transform.position.y;}
        TerrainData Data => data;
        Vector2 Spacing => spacing;
        Vector2 World(int x,int z) => new(origin.x+x*spacing.x,origin.y+z*spacing.y);
        float Source(int x,int z) => original.TryGetValue(new Vector2Int(x,z),out float h)?h:Data.GetHeight(x,z)/Data.size.y;
        internal float NaturalHeight(Vector2 p)
        {
            if(original.Count==0)return terrain.SampleHeight(new Vector3(p.x,0,p.y))+originY;
            var d=Data;var step=Spacing;float x=Mathf.Clamp((p.x-transform.position.x)/step.x,0,d.heightmapResolution-1),z=Mathf.Clamp((p.y-transform.position.z)/step.y,0,d.heightmapResolution-1);
            int ix=Mathf.Min(Mathf.FloorToInt(x),d.heightmapResolution-2),iz=Mathf.Min(Mathf.FloorToInt(z),d.heightmapResolution-2);
            if(!original.ContainsKey(new Vector2Int(ix,iz))&&!original.ContainsKey(new Vector2Int(ix+1,iz))&&!original.ContainsKey(new Vector2Int(ix,iz+1))&&!original.ContainsKey(new Vector2Int(ix+1,iz+1)))
                return terrain.SampleHeight(new Vector3(p.x,0,p.y))+transform.position.y;
            return Mathf.Lerp(Mathf.Lerp(Source(ix,iz),Source(ix+1,iz),x-ix),Mathf.Lerp(Source(ix,iz+1),Source(ix+1,iz+1),x-ix),z-iz)*d.size.y+transform.position.y;
        }
        RectInt Samples(Rect bounds,float margin=0)
        {
            var d=Data;var step=Spacing;var origin=new Vector2(transform.position.x,transform.position.z);
            var lo=Vector2Int.FloorToInt((bounds.min-origin-Vector2.one*margin)/step);
            var hi=Vector2Int.CeilToInt((bounds.max-origin+Vector2.one*margin)/step);
            lo=Vector2Int.Max(Vector2Int.zero,lo);hi=Vector2Int.Min(Vector2Int.one*(d.heightmapResolution-1),hi);
            return new RectInt(lo,hi-lo+Vector2Int.one);
        }
        internal float Minimum(Rect bounds,Func<Vector2,bool> contains,float sampledLow,bool current=false)
        {
            var rect=Samples(bounds);float low=sampledLow;var heights=Data.GetHeights(rect.x,rect.y,rect.width,rect.height);
            for(int z=rect.yMin;z<rect.yMax;z++)for(int x=rect.xMin;x<rect.xMax;x++)
                if(contains(World(x,z)))low=Mathf.Min(low,(!current&&original.TryGetValue(new Vector2Int(x,z),out float h)?h:heights[z-rect.y,x-rect.x])*vertical+originY);
            return low;
        }
        internal void Cut(Rect bounds,Func<Vector2,bool> contains,float level,Func<Vector2,bool> protectedGround,bool exact=false,Func<Vector2,float> targetHeight=null)
        {
            float margin=Spacing.magnitude;var rect=Samples(bounds,margin);var d=Data;
            var heights=d.GetHeights(rect.x,rect.y,rect.width,rect.height);bool changed=false;
            for(int z=rect.yMin;z<rect.yMax;z++)for(int x=rect.xMin;x<rect.xMax;x++)
            {
                var p=World(x,z);if(!contains(p)||protectedGround(p))continue;
                float h=Mathf.Clamp01(((targetHeight==null?level:targetHeight(p))-transform.position.y)/d.size.y);
                int lx=x-rect.x,lz=z-rect.y;if(exact?Mathf.Abs(heights[lz,lx]-h)<.000001f:heights[lz,lx]<=h)continue;
                var key=new Vector2Int(x,z);if(!original.ContainsKey(key))original[key]=heights[lz,lx];
                heights[lz,lx]=h;changed=true;
            }
            if(changed){d.SetHeightsDelayLOD(rect.x,rect.y,heights);patches.Add(rect);dirty=true;}
        }
        internal (float Min,float Mean) Statistics(Rect bounds,Func<Vector2,bool> contains,float fallback)
        {
            var rect=Samples(bounds);var heights=Data.GetHeights(rect.x,rect.y,rect.width,rect.height);float low=float.PositiveInfinity;double sum=0;int count=0;
            for(int z=rect.yMin;z<rect.yMax;z++)for(int x=rect.xMin;x<rect.xMax;x++)if(contains(World(x,z))){float h=heights[z-rect.y,x-rect.x]*vertical+originY;low=Mathf.Min(low,h);sum+=h;count++;}
            return count>0?(low,(float)(sum/count)):(fallback,fallback);
        }
        struct PlaneSample { internal float Distance,Height,Start;internal int Stroke;internal bool Core,Nature; }
        readonly Dictionary<Vector2Int,PlaneSample> planeSamples=new();bool hasNatureSamples;
        internal bool NatureAt(Vector2 p){if(!hasNatureSamples)return false;var c=Vector2Int.FloorToInt((p-origin)/spacing);for(int z=0;z<=1;z++)for(int x=0;x<=1;x++)if(planeSamples.TryGetValue(c+new Vector2Int(x,z),out var sample)&&sample.Nature)return true;return false;}
        // Accumulate the union once per native sample. A new brush segment never
        // scans historical planes, even after a long or repeatedly crossed drag.
        internal void CutPlanes(Rect bounds,ZoneGroundPlane stamp,Func<Vector2,bool> inside)
        {
            var rect=Samples(bounds,Spacing.magnitude);var heights=Data.GetHeights(rect.x,rect.y,rect.width,rect.height);bool changed=false;
            float radius=PrototypeRegionView.GroundPlaneBlend(stamp);hasNatureSamples|=stamp.nature;
            for(int z=rect.yMin;z<rect.yMax;z++)for(int x=rect.xMin;x<rect.xMax;x++)
            {
                var p=World(x,z);float distance=stamp.Distance(p);if(distance>radius||!inside(p))continue;
                var key=new Vector2Int(x,z);bool known=planeSamples.TryGetValue(key,out var sample);
                bool core=distance<=0;bool sameStroke=known&&sample.Stroke==stamp.stroke;
                bool reuse=!core&&(stamp.nature?sameStroke:known)&&(sample.Core||distance>=sample.Distance);
                int lx=x-rect.x,lz=z-rect.y;float source=original.TryGetValue(key,out float old)?old:heights[lz,lx];
                float start=stamp.nature?(sameStroke?sample.Start:heights[lz,lx]*vertical+originY):source*vertical+originY;
                float weight=(core?1:1-PrototypeRegionView.LevelerEdgeBlend(stamp,p,distance))*(stamp.strength>0?stamp.strength:1);
                float target=reuse?sample.Height:stamp.nature?Mathf.Lerp(start,stamp.height,weight):core?stamp.height:Mathf.Lerp(stamp.height,start,PrototypeRegionView.LevelerEdgeBlend(stamp,p,distance));
                if(!reuse)planeSamples[key]=new PlaneSample{Core=core,Distance=distance,Height=target,Start=start,Stroke=stamp.stroke,Nature=stamp.nature||known&&sample.Nature};
                float h=Mathf.Clamp01((target-originY)/vertical);
                if(Mathf.Abs(heights[lz,lx]-h)<.000001f)continue;
                original.TryAdd(key,source);heights[lz,lx]=h;changed=true;
            }
            if(changed){Data.SetHeightsDelayLOD(rect.x,rect.y,heights);patches.Add(rect);dirty=true;}
        }
        internal bool Finish(){if(!dirty)return false;Data.SyncHeightmap();terrain.Flush();dirty=false;return true;}
        internal void Restore()
        {
            AppliedPlanes.Clear();planeSamples.Clear();hasNatureSamples=false;
            if(original.Count==0)return;
            foreach(var rect in patches)
            {
                var heights=Data.GetHeights(rect.x,rect.y,rect.width,rect.height);
                for(int z=rect.yMin;z<rect.yMax;z++)for(int x=rect.xMin;x<rect.xMax;x++)
                    if(original.TryGetValue(new Vector2Int(x,z),out float h))heights[z-rect.y,x-rect.x]=h;
                Data.SetHeightsDelayLOD(rect.x,rect.y,heights);dirty=true;
            }
            original.Clear();patches.Clear();Finish();
        }
    }

    public sealed partial class PrototypeRegionView
    {
        void PrepareLotGrading(DistrictLotRecord record)
        {
            if(record.definition.IsWaterfront)return;
            if(TryZoneGroundHeight(record.center,out float readyLevel)){record.cutToLowest=true;record.cutGroundHeight=readyLevel;return;}
            var footprint=new DistrictLotFootprint(record);
            DistrictLotFoundation.SampleLevel(record.definition.Size,footprint.WorldPoint,NaturalLotHeight,out float low);
            var ground=ApprovedDistrict?ApprovedDistrict.DisplayTerrain:null;
            if(ground)
            {
                var grading=ground.GetComponent<DistrictLotGrading>()??ground.gameObject.AddComponent<DistrictLotGrading>();
                low=grading.Minimum(footprint.Bounds,p=>footprint.Contains(p),low);
            }
            if(TryZoneGroundHeight(record.center,out float sharedLevel))low=sharedLevel;
            record.cutToLowest=true;record.cutGroundHeight=Mathf.Max(SeaLevel+.2f,low);
        }
        void ApplyLotGrading(IEnumerable<DistrictLotRecord> records)
        {
            ApplyZoneGroundPlanes();
            var ground=ApprovedDistrict?ApprovedDistrict.DisplayTerrain:null;
            if(!ground)return;
            var cuts=records.Where(r=>r.cutToLowest&&!r.definition.IsWaterfront&&ActiveDistrictMembers.Contains(r.district)).ToArray();
            if(cuts.Length==0){GetComponentInChildren<DistrictWeather>()?.RefreshGround();return;}
            var grading=ground.GetComponent<DistrictLotGrading>()??ground.gameObject.AddComponent<DistrictLotGrading>();
            float margin=new Vector2(ground.terrainData.size.x,ground.terrainData.size.z).magnitude/(ground.terrainData.heightmapResolution-1);
            foreach(var record in cuts)
            {
                var footprint=new DistrictLotFootprint(record);
                grading.Cut(footprint.Bounds,p=>footprint.Contains(p,-margin),record.cutGroundHeight,p=>LotGradeRoadPoint(p)||TryZoneGroundHeight(p,out _));
            }
            grading.Finish();
            GetComponentInChildren<DistrictWeather>()?.RefreshGround();
        }
        bool LotGradeRoadPoint(Vector2 p)
        {
            // Keep the carriageway at its original grade, including clipped empty lot corners.
            var rect=new Rect(p-Vector2.one*.01f,Vector2.one*.02f);
            return PolygonOverlapsDistrictRoad(new[]{rect.min,new Vector2(rect.xMax,rect.yMin),rect.max,new Vector2(rect.xMin,rect.yMax)},rect);
        }
    }
}

namespace CityForge.World
{
    [Serializable]
    public sealed class ZoneGroundPlane
    {
        public Vector2 center,size;
        public float height,rotation,cornerRadius;
        public bool leveler,roughEdges,nature;
        public int stroke;public float strength;
        public Vector2 sweep; // World-space sweep from stroke start to end; zero is a single stamp.
        public int district;
        Vector2 right,forward;float cachedRotation;bool axesReady;
        void Axes(){if(axesReady&&cachedRotation==rotation)return;float r=rotation*Mathf.Deg2Rad;right=new Vector2(Mathf.Cos(r),-Mathf.Sin(r));forward=new Vector2(-right.y,right.x);cachedRotation=rotation;axesReady=true;}
        public Vector2 World(Vector2 p){Axes();return center+right*p.x+forward*p.y;}
        public bool Contains(Vector2 p,float margin=0){var b=Bounds;if(p.x<b.xMin-margin||p.x>b.xMax+margin||p.y<b.yMin-margin||p.y>b.yMax+margin)return false;return SignedDistance(p)<=margin;}
        float SignedDistance(Vector2 p){Axes();p-=center;
            if(sweep.sqrMagnitude>.0001f)
            {
                var local=new Vector2(Vector2.Dot(p,right),Vector2.Dot(p,forward));var delta=new Vector2(Vector2.Dot(sweep,right),Vector2.Dot(sweep,forward));
                float radius=Mathf.Clamp(cornerRadius,0,Mathf.Min(size.x,size.y)*.5f);var half=size*.5f-Vector2.one*radius;
                var box=local+delta*.5f;float lo=0,hi=1;
                bool Axis(float d,float min,float max){if(Mathf.Abs(d)<.00001f)return min<=0&&max>=0;float a=min/d,b=max/d;if(a>b){float temp=a;a=b;b=temp;}lo=Mathf.Max(lo,a);hi=Mathf.Min(hi,b);return lo<=hi;}
                if(Axis(delta.x,box.x-half.x,box.x+half.x)&&Axis(delta.y,box.y-half.y,box.y+half.y))return -radius;
                float BoxDistance(Vector2 v){var d=Vector2.Max(new Vector2(Mathf.Abs(v.x),Mathf.Abs(v.y))-half,Vector2.zero);return d.sqrMagnitude;}
                float distance=Mathf.Min(BoxDistance(box),BoxDistance(box-delta));
                foreach(var sign in SweepCorners){var corner=box+Vector2.Scale(half,sign);float t=Mathf.Clamp01(Vector2.Dot(corner,delta)/delta.sqrMagnitude);distance=Mathf.Min(distance,(corner-delta*t).sqrMagnitude);}
                return Mathf.Sqrt(distance)-radius;
            }
float r=Mathf.Clamp(cornerRadius,0,Mathf.Min(size.x,size.y)*.5f);var d=new Vector2(Mathf.Abs(Vector2.Dot(p,right)),Mathf.Abs(Vector2.Dot(p,forward)))-size*.5f+Vector2.one*r;return Vector2.Max(d,Vector2.zero).magnitude+Mathf.Min(Mathf.Max(d.x,d.y),0)-r;}
        static readonly Vector2[] SweepCorners={new(-1,-1),new(-1,1),new(1,1),new(1,-1)};
        public float Distance(Vector2 p)=>Mathf.Max(0,SignedDistance(p));
        Rect cachedBounds;Vector2 boundsCenter,boundsSize,boundsSweep;float boundsRotation;bool boundsReady;
        public Rect Bounds {get{if(boundsReady&&boundsCenter==center&&boundsSize==size&&boundsSweep==sweep&&boundsRotation==rotation)return cachedBounds;
            var half=size*.5f;var a=World(-half);var b=World(half);var c=World(new Vector2(half.x,-half.y));var d=World(new Vector2(-half.x,half.y));var extent=new Vector2(Mathf.Abs(sweep.x),Mathf.Abs(sweep.y))*.5f;
            cachedBounds=Rect.MinMaxRect(Mathf.Min(a.x,b.x,c.x,d.x)-extent.x,Mathf.Min(a.y,b.y,c.y,d.y)-extent.y,Mathf.Max(a.x,b.x,c.x,d.x)+extent.x,Mathf.Max(a.y,b.y,c.y,d.y)+extent.y);
            boundsReady=true;boundsCenter=center;boundsSize=size;boundsSweep=sweep;boundsRotation=rotation;return cachedBounds;}}
        internal static void Validate(ZoneGroundPlane[] planes,int districtCount){if(planes==null)return;if(planes.Length>10000)throw new System.IO.InvalidDataException("Too many zone ground planes.");foreach(var p in planes)if(p==null||p.district<0||p.district>=districtCount||!float.IsFinite(p.center.x)||!float.IsFinite(p.center.y)||!float.IsFinite(p.size.x)||!float.IsFinite(p.size.y)||p.size.x<=0||p.size.y<=0||p.size.x>2048||p.size.y>2048||!float.IsFinite(p.sweep.x)||!float.IsFinite(p.sweep.y)||Mathf.Abs(p.sweep.x)>65536||Mathf.Abs(p.sweep.y)>65536||p.stroke<0||!float.IsFinite(p.strength)||p.strength<0||p.strength>1||!float.IsFinite(p.height)||!float.IsFinite(p.rotation)||!float.IsFinite(p.cornerRadius)||p.cornerRadius<0||p.cornerRadius>Mathf.Min(p.size.x,p.size.y)*.5f)throw new System.IO.InvalidDataException("Invalid zone ground plane.");}
    }
    public sealed partial class PrototypeRegionView
    {
        internal readonly List<ZoneGroundPlane> ZoneGroundPlanes=new();
        const float ZoneGradeBlend=12f;
        internal static float GroundPlaneBlend(ZoneGroundPlane plane)=>plane.leveler&&plane.roughEdges?32f:ZoneGradeBlend;
        bool TryZoneGroundHeight(Vector2 p,out float height,bool blend=false)
        {
            if(SelectedDistrict>=0&&ApprovedDistrict&&ApprovedDistrict.DisplayTerrain){var ground=ApprovedDistrict.DisplayTerrain;
                if(gradedLotTerrain!=ground||!cachedLotGrading){gradedLotTerrain=ground;cachedLotGrading=ground.GetComponent<DistrictLotGrading>();}
                if(cachedLotGrading&&cachedLotGrading.NatureAt(p)){height=CurrentLotTerrainHeight(p);return true;}}
            for(int i=ZoneGroundPlanes.Count-1;i>=0;i--)if(ZoneGroundPlanes[i].Contains(p)){height=ZoneGroundPlanes[i].height;return true;}
            if(blend){ZoneGroundPlane nearest=null;float distance=float.PositiveInfinity;foreach(var plane in ZoneGroundPlanes){var b=plane.Bounds;float radius=GroundPlaneBlend(plane);if(p.x<b.xMin-radius||p.x>b.xMax+radius||p.y<b.yMin-radius||p.y>b.yMax+radius)continue;float d=plane.Distance(p);if(d<GroundPlaneBlend(plane)&&d<distance){distance=d;nearest=plane;}}if(nearest!=null){height=Mathf.Lerp(nearest.height,NaturalLotHeight(p),LevelerEdgeBlend(nearest,p,distance));return true;}}
            height=0;return false;
        }
        internal static float LevelerEdgeBlend(ZoneGroundPlane plane,Vector2 p,float distance)
        {
            if(!plane.roughEdges)return Mathf.SmoothStep(0,1,distance/ZoneGradeBlend);
            // World-anchored noise makes adjacent swept segments share the same bank.
            // The entire selected core stays level; only its outer feather varies.
            float broad=Mathf.PerlinNoise(p.x*.095f+31.7f,p.y*.095f-14.3f);
            float detail=Mathf.PerlinNoise(p.x*.31f-7.1f,p.y*.31f+53.9f);
            float fringe=.3f+1.7f*broad;
            float reach=GroundPlaneBlend(plane)-fringe-(1f-broad)*4f;
            float t=Mathf.Clamp01((distance-fringe)/reach);
            float blend=Mathf.SmoothStep(0,1,t);
            return Mathf.Clamp01(blend+(detail-.5f)*.025f*Mathf.Sin(t*Mathf.PI)*Mathf.Sin(t*Mathf.PI));
        }
        void CommitBlockGroundPlane(ZonedBlockPlan plan,List<DistrictRoadRecord> roads,bool diagonal)
        {
            // Include street width, sidewalks and a full extra4m zoning tile.
            var a=ZoneCorner(plan.Bounds.min);var b=ZoneCorner(plan.Bounds.max);
            float streetWidth=roads.Count==0?DistrictGridCellMeters:roads.Max(r=>(float)r.width);
            float apron=DistrictGridCellMeters*.5f+streetWidth*.5f+2f+DistrictGridCellMeters;
            var plane=new ZoneGroundPlane{center=(a+b)*.5f,size=b-a+Vector2.one*(apron*2),rotation=diagonal?45:0,district=SelectedDistrict};
            DistrictLotFoundation.SampleLevel(plane.size,plane.World,LotHeight,out float low);
            var terrain=ApprovedDistrict?ApprovedDistrict.DisplayTerrain:null;
            if(terrain){var g=terrain.GetComponent<DistrictLotGrading>()??terrain.gameObject.AddComponent<DistrictLotGrading>();low=g.Minimum(plane.Bounds,p=>plane.Contains(p),low);}
            // An adjoining drop extends the existing plane instead of introducing a new step.
            var adjoining=ZoneGroundPlanes.LastOrDefault(p=>(!p.nature||p.strength>=.999f)&&p.Bounds.Overlaps(plane.Bounds));
            plane.height=Mathf.Max(SeaLevel+.2f,adjoining?.height??low);ZoneGroundPlanes.Add(plane);
            ApplyZoneGroundPlanes();RebuildDistrictRoads();RebuildGroundPaint();RebuildParkPaths();
        }
        void ApplyZoneGroundPlanes(Rect? affected=null)
        {
            var terrain=ApprovedDistrict?ApprovedDistrict.DisplayTerrain:null;if(!terrain)return;
            var planes=ZoneGroundPlanes.Where(p=>ActiveDistrictMembers.Contains(p.district)).ToArray();if(planes.Length==0)return;
            var g=terrain.GetComponent<DistrictLotGrading>()??terrain.gameObject.AddComponent<DistrictLotGrading>();bool changed=false;
            float margin=new Vector2(terrain.terrainData.size.x,terrain.terrainData.size.z).magnitude/(terrain.terrainData.heightmapResolution-1);
            foreach(var plane in planes)if(g.AppliedPlanes.Add(plane)){var bounds=plane.Bounds;float feather=GroundPlaneBlend(plane);bounds.min-=Vector2.one*feather;bounds.max+=Vector2.one*feather;
                g.CutPlanes(bounds,plane,InActiveDistrict);if(plane.nature){ApprovedDistrict.ClearTerraformObjects(plane,bounds);ApprovedDistrict.QueueTerraformCoast(bounds);}changed=true;}
            if(changed&&g.Finish())GetComponentInChildren<DistrictWeather>()?.RefreshGround(affected);
        }
        readonly Dictionary<DistrictLotRecord,float> lotPopulationAfter=new();
        readonly List<(DistrictLotRecord lot,GameObject body)> delayedLotCharacters=new();
        void HoldLotPopulation(DistrictLotRecord lot)=>lotPopulationAfter[lot]=float.PositiveInfinity;
        void ReleaseLotPopulation(DistrictLotRecord lot)=>lotPopulationAfter[lot]=Time.unscaledTime+10f;
        bool LotPopulationReady(DistrictLotRecord lot)=>!lotPopulationAfter.TryGetValue(lot,out float at)||Time.unscaledTime>=at;
        void TickLotPopulation()
        {
            if(lotPopulationAfter.Count==0&&delayedLotCharacters.Count==0)return;
            foreach(var lot in lotPopulationAfter.Keys.Where(l=>!PlacedLots.Contains(l)||LotPopulationReady(l)).ToArray()){lotPopulationAfter.Remove(lot);pedestrianRoutesDirty=true;}
            for(int i=delayedLotCharacters.Count-1;i>=0;i--){var item=delayedLotCharacters[i];if(!item.body||!PlacedLots.Contains(item.lot)){delayedLotCharacters.RemoveAt(i);continue;}if(LotPopulationReady(item.lot)){item.body.SetActive(true);delayedLotCharacters.RemoveAt(i);}}
        }
    }
}
