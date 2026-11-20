using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CityForge.Geometry;
namespace CityForge.World
{
    /// <summary>Immutable regional station/grade queries; shared exactly by both sides of district seams.</summary>
    public sealed class PlayerChannelSampler
    {
        readonly RegionWaterBody body;readonly float side;readonly float[] stations;
        public PlayerChannelSampler(RegionWaterBody source,float mapSide){body=source;side=mapSide;stations=new float[body.centerline.Length];for(int i=1;i<stations.Length;i++)stations[i]=stations[i-1]+Vector2.Distance(body.centerline[i-1],body.centerline[i])*side;}
        public Vector2 NearestPoint(Vector2 world)
        {
            Query(world,out _,out var current);int lo=0,hi=stations.Length-1;
            while(hi-lo>1){int mid=(lo+hi)/2;if(stations[mid]<=current.x)lo=mid;else hi=mid;}
            float t=Mathf.InverseLerp(stations[lo],stations[hi],current.x);
            return (Vector2.Lerp(body.centerline[lo],body.centerline[hi],t)-Vector2.one*.5f)*side;
        }
        public float Query(Vector2 world,out float level,out Vector4 current)
        {
            float best=float.PositiveInfinity;body.IndexedCurrentCoordinates(world/side+Vector2.one*.5f,side,ref best,out var coordinates);
            float station=-coordinates.x;int lo=0,hi=stations.Length-1;while(hi-lo>1){int mid=(lo+hi)/2;if(stations[mid]<=station)lo=mid;else hi=mid;}
            float t=Mathf.InverseLerp(stations[lo],stations[hi],station);level=Mathf.Lerp(body.playerChannelLevels[lo],body.playerChannelLevels[hi],t);
            current=-coordinates;return Mathf.Sqrt(best)*side;
        }
    }
    public static class PlayerChannelGeometry
    {
        public static PlayerChannelSampler Sampler(RegionWaterBody body,float side)=>body.playerSampler??=new PlayerChannelSampler(body,side);
        public static float Level(RegionWaterBody body,Vector2 p,float side){Sampler(body,side).Query(p,out float level,out _);// Keep excavation grades immutable; streams fill to 5 cm below the original bank top.
            return level+(body.playerChannel&&body.isStream?.15f:0);}
        public static Vector4 Current(RegionWaterBody body,Vector2 p,float side){Sampler(body,side).Query(p,out _,out var current);return current;}
        public static RegionWaterBody SnapStream(PrototypeRegionData data,Vector2 p,float side,float radius,out Vector2 snapped)
        {
            snapped=p;RegionWaterBody result=null;float best=radius;
            foreach(var body in data.WaterBodies)if(body.playerChannel&&body.isStream){
                var bounds=body.Bounds;bounds.min-=Vector2.one*(radius/side);bounds.max+=Vector2.one*(radius/side);
                if(!bounds.Contains(p/side+Vector2.one*.5f))continue;
                var sampler=Sampler(body,side);float distance=sampler.Query(p,out _,out _);
                if(distance<best){best=distance;snapped=sampler.NearestPoint(p);result=body;}
            }return result;
        }
        public static float Distance(PrototypeRegionData data,Vector2 p,float side,float limit=30)
        {
            float best=limit;foreach(var b in data.WaterBodies)if(b.playerChannel){var r=b.Bounds;r.min-=Vector2.one*(limit/side);r.max+=Vector2.one*(limit/side);if(r.Contains(p/side+Vector2.one*.5f)){float distance=Sampler(b,side).Query(p,out _,out var current);best=Mathf.Min(best,distance-PlayerStreamWidth.AtStation(b,current.x/side)*side);}}return best;
        }
        // Conservative per-row bands around immutable route segments. Excludes the
        // large dry corners of a winding channel's rectangle without changing wet samples.
        internal static void RasterRanges(RegionWaterBody body,float side,Vector2 origin,Vector2 step,int width,int height,float margin,out int[] first,out int[] last)
        {
            first=new int[height];last=new int[height];for(int z=0;z<height;z++){first[z]=width;last[z]=-1;}
            float reach=body.halfWidthNormalized*side+margin;
            for(int i=1;i<body.centerline.Length;i++){
                var a=(body.centerline[i-1]-Vector2.one*.5f)*side;var b=(body.centerline[i]-Vector2.one*.5f)*side;
                var min=Vector2.Min(a,b)-Vector2.one*reach;var max=Vector2.Max(a,b)+Vector2.one*reach;
                int z0=Mathf.Max(0,Mathf.FloorToInt((min.y-origin.y)/step.y)),z1=Mathf.Min(height-1,Mathf.CeilToInt((max.y-origin.y)/step.y));
                int x0=Mathf.Max(0,Mathf.FloorToInt((min.x-origin.x)/step.x)),x1=Mathf.Min(width-1,Mathf.CeilToInt((max.x-origin.x)/step.x));
                if(x0>x1)continue;for(int z=z0;z<=z1;z++){first[z]=Mathf.Min(first[z],x0);last[z]=Mathf.Max(last[z],x1);}
            }
        }
        public static void Apply(RegionWaterBody body,float[,] heights,Vector2 origin,float size,float vertical,float side)
        {
            float half=body.halfWidthNormalized*side,bank=body.bankWidthNormalized*side,reach=half+bank;var bounds=body.Bounds;var min=(bounds.min-Vector2.one*.5f)*side-Vector2.one*bank;var max=(bounds.max-Vector2.one*.5f)*side+Vector2.one*bank;
            int n=heights.GetLength(0),x0=Mathf.Clamp(Mathf.FloorToInt((min.x-origin.x)/size*(n-1)),0,n-1),x1=Mathf.Clamp(Mathf.CeilToInt((max.x-origin.x)/size*(n-1)),0,n-1),z0=Mathf.Clamp(Mathf.FloorToInt((min.y-origin.y)/size*(n-1)),0,n-1),z1=Mathf.Clamp(Mathf.CeilToInt((max.y-origin.y)/size*(n-1)),0,n-1);var sampler=Sampler(body,side);
            RasterRanges(body,side,origin,Vector2.one*(size/(n-1)),n,n,bank,out var first,out var last);
            for(int z=z0;z<=z1;z++)for(int x=Mathf.Max(x0,first[z]);x<=Mathf.Min(x1,last[z]);x++){
                var p=origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*size;float distance=sampler.Query(p,out float level,out var current);half=PlayerStreamWidth.AtStation(body,current.x/side)*side;if(distance>half+bank)continue;float old=heights[z,x]*vertical;
                float target=distance<=half?level-body.playerChannelDepth+(body.playerChannelDepth+.10f)*Mathf.Pow(distance/half,body.appearanceProfile=="player-canal-v1"?16:4):Mathf.Lerp(level+.10f,old,Mathf.SmoothStep(0,1,(distance-half)/bank));heights[z,x]=Mathf.Clamp01(Mathf.Min(old,target)/vertical);
            }
        }
        public static void Validate(RegionWaterBody[] bodies)
        {
            if(bodies==null)return;int count=0,stations=0;foreach(var b in bodies)if(b!=null&&b.playerChannel){
                if(b.playerStreamWidthVersion<0||b.playerStreamWidthVersion>1||b.playerStreamWidthVariation<0||b.playerStreamWidthVariation>2||b.playerStreamWidthVersion==1&&!b.isStream)throw new InvalidDataException("Invalid stream width settings.");
                if(++count>64||b.playerCreated||b.kind!=WaterBodyKind.River||b.channelVersion!=0||string.IsNullOrEmpty(b.id)||b.centerline==null||b.centerline.Length<2||b.centerline.Length>4096||b.playerChannelLevels==null||b.playerChannelLevels.Length!=b.centerline.Length||!float.IsFinite(b.halfWidthNormalized)||b.halfWidthNormalized<=0||b.halfWidthNormalized>.02f||!float.IsFinite(b.bankWidthNormalized)||b.bankWidthNormalized<=0||b.bankWidthNormalized>.02f||!float.IsFinite(b.playerChannelDepth)||b.playerChannelDepth<.2f||b.playerChannelDepth>5)throw new InvalidDataException("Invalid player waterway.");
                stations+=b.centerline.Length;if(stations>65536)throw new InvalidDataException("Too many waterway stations.");
                for(int i=0;i<b.centerline.Length;i++){var p=b.centerline[i];if(!float.IsFinite(p.x)||!float.IsFinite(p.y)||p.x<0||p.x>1||p.y<0||p.y>1||!float.IsFinite(b.playerChannelLevels[i])||b.playerChannelLevels[i]<0||b.playerChannelLevels[i]>10000||(i>0&&(p-b.centerline[i-1]).sqrMagnitude<1e-14f))throw new InvalidDataException("Invalid waterway station.");}
            }
        }
    }
    public sealed partial class PrototypeRegionView
    {
        public bool InPlayerChannelClearing(Vector2 p){foreach(var body in Data.WaterBodies)if(body.playerChannel&&InPlayerChannelClearing(body,p))return true;return false;}
        bool InPlayerChannelClearing(RegionWaterBody body,Vector2 p)
        {
            float margin=body.bankWidthNormalized*MapSide+2;
            var bounds=body.Bounds;
            bounds.min-=Vector2.one*(margin/MapSide);
            bounds.max+=Vector2.one*(margin/MapSide);
            return bounds.Contains(p/MapSide+Vector2.one*.5f)&&PlayerChannelGeometry.Sampler(body,MapSide).Query(p,out _,out _)<(body.halfWidthNormalized+body.bankWidthNormalized)*MapSide+2;
        }
        internal void RecordPlayerChannel(RegionWaterBody body,string replaceId){var list=new List<RegionWaterBody>();foreach(var b in Data.WaterBodies)if(replaceId==null||b.id!=replaceId)list.Add(b);list.Add(body);Data.WaterBodies=list.ToArray();Waterless=false;WoodlandRevision++;}
        // Snaps only exterior union edges with an actual neighboring region cell.
        public bool ChannelBoundary(Vector2 p,out Vector2 edge,out Vector2 outward)
        {
            edge=p;outward=Vector2.zero;float best=20;
            foreach(int member in ActiveDistrictMembers){var normalized=RegionBiomeMap.DistrictBounds(member,GridSide);var min=(normalized.min-Vector2.one*.5f)*MapSide;var max=(normalized.max-Vector2.one*.5f)*MapSide;
                foreach(var normal in new[]{Vector2.left,Vector2.right,Vector2.down,Vector2.up}){
                    var q=normal.x!=0?new Vector2(normal.x<0?min.x:max.x,Mathf.Clamp(p.y,min.y,max.y)):new Vector2(Mathf.Clamp(p.x,min.x,max.x),normal.y<0?min.y:max.y);
                    float d=Vector2.Distance(p,q);var probe=q+normal*2;if(d>best||Mathf.Abs(probe.x)>=MapSide*.5f||Mathf.Abs(probe.y)>=MapSide*.5f||InActiveDistrict(probe))continue;best=d;edge=q;outward=normal;
                }
            }return outward.sqrMagnitude>0;
        }
        public Vector2 SnapChannelPoint(Vector2 p,bool first,bool stream,bool canal=false)
        {
            if(stream){var hit=PlayerChannelGeometry.SnapStream(Data,p,MapSide,12,out var snapped);if(hit!=null&&InActiveDistrict(snapped))return snapped;}
            if(first){var original=p;float best=16;foreach(var b in Data.WaterBodies)if(b.playerChannel&&b.isStream==stream&&(b.appearanceProfile=="player-canal-v1")==canal){var end=(b.centerline[b.centerline.Length-1]-Vector2.one*.5f)*MapSide;float d=Vector2.Distance(end,original);if(d<best&&InActiveDistrict(end)){best=d;p=end;}}if(best<16)return p;}
            if(ChannelBoundary(p,out var edge,out var normal))return edge-normal*.15f;return p;
        }
    }
    public sealed partial class MeadowDistrictLandscape
    {
        public string CreatePlayerChannel(IReadOnlyList<Vector2> guides,bool stream,bool canal=false,StreamWidthVariation widthVariation=StreamWidthVariation.Gentle)
        {
            if(View.IsSaving||View.DistrictLoading||IsRegionOverview)return "Wait for the district to finish loading.";
            if(stream&&!Enum.IsDefined(typeof(StreamWidthVariation),widthVariation))return "Choose Uniform, Gentle, or Natural stream width.";
            if(guides.Count<2||guides.Count>64)return "Use 2–64 markers, then press Enter.";
            float side=View.MapSide;var raw=new List<Vector2>(guides);RegionWaterBody joined=null;
            foreach(var b in View.Data.WaterBodies)if(b.playerChannel&&b.isStream==stream&&(b.appearanceProfile=="player-canal-v1")==canal&&Vector2.Distance((b.centerline[b.centerline.Length-1]-Vector2.one*.5f)*side,raw[0])<1){joined=b;raw[0]=(b.centerline[b.centerline.Length-1]-Vector2.one*.5f)*side;break;}
            int total=0;foreach(var b in View.Data.WaterBodies)if(b.playerChannel)total++;if(joined==null&&total>=64)return "This region already has 64 added waterways.";
            for(int i=0;i<raw.Count;i++)if(!Contains(raw[i]))return "Place the route in this district; finish at its edge to cross into the next.";
            // Use original detailed ground, not the coarser regional approximation.
            // Stored station grades remain authoritative in the neighboring district.
            float RouteGround(Vector2 p)=>Contains(p)?CoverageHeight(p):View.Data.SmoothHeight(p.x/side+.5f,p.y/side+.5f)*terrainData.size.y;
            // Drawing back uphill from a downstream endpoint makes an incoming tributary,
            // rather than forcing an extension to excavate the entire hill at the old level.
            if(stream&&joined!=null&&RouteGround(raw[raw.Count-1])>joined.playerChannelLevels[joined.playerChannelLevels.Length-1]+.4f)joined=null;
            if(joined==null&&total>=64)return "This region already has 64 added waterways.";
            float EndGrade(Vector2 p){var hit=PlayerChannelGeometry.SnapStream(View.Data,p,side,12,out var q);return hit!=null?PlayerChannelGeometry.Level(hit,q,side)-.15f:RouteGround(p)-.2f;}
            if(stream&&joined==null&&EndGrade(raw[raw.Count-1])>EndGrade(raw[0])+.2f)raw.Reverse();
            if(stream){foreach(int index in new[]{0,raw.Count-1}){var hit=PlayerChannelGeometry.SnapStream(View.Data,raw[index],side,12,out var q);if(hit!=null)raw[index]=q;}}
            bool crossing=View.ChannelBoundary(raw[raw.Count-1],out var edge,out var outward);
            if(crossing){raw[raw.Count-1]=edge-outward*.15f;raw.Add(edge+outward*64);}
            var curve=canal?raw.ToArray():PointWireGeometry.RoundOpen(raw);var samples=new List<Vector2>{curve[0]};for(int i=1;i<curve.Length;i++){float length=Vector2.Distance(curve[i-1],curve[i]);int n=Mathf.Max(1,Mathf.CeilToInt(length/(stream?1f:4f)));for(int j=1;j<=n;j++){var p=Vector2.Lerp(curve[i-1],curve[i],j/(float)n);if(Vector2.Distance(samples[samples.Count-1],p)>.05f)samples.Add(p);}}
            if(samples.Count<2||Vector2.Distance(samples[0],samples[samples.Count-1])<10)return "Draw a longer open route (at least 10 m).";
            float half=canal?4:stream?4:9,bank=canal?1.2f:stream?1.5f:6;
            if(joined!=null){half=joined.halfWidthNormalized*side;bank=joined.bankWidthNormalized*side;}var centerline=new List<Vector2>();var levels=new List<float>();
            if(joined!=null){centerline.AddRange(joined.centerline);levels.AddRange(joined.playerChannelLevels);}
            float last=joined!=null?levels[levels.Count-1]:float.PositiveInfinity;
            for(int i=0;i<samples.Count;i++){
                var p=samples[i];var uv=p/side+Vector2.one*.5f;
                if(!Contains(p)&&(!crossing||Vector2.Dot(p-edge,outward)<-.01f||Vector2.Distance(p,edge)>64.1f))return "Keep the route inside the district until its final boundary connection.";
                if(Mathf.Abs(p.x)>side*.5f-half-bank||Mathf.Abs(p.y)>side*.5f-half-bank)return "Keep the waterway inside the regional map.";
                if(View.InCampClearing(p,half+bank+8))return "Keep the waterway clear of camps.";
                if(Contains(p)&&LakeRoadDistance(p)<Baseline.roadWidth+half+bank)return "Keep the waterway clear of roads.";
                foreach(var b in View.Data.WaterBodies){if(b==joined||stream&&b.playerChannel&&b.isStream)continue;if(b.playerChannel){if(PlayerChannelGeometry.Sampler(b,side).Query(p,out _,out _)<half+bank+b.halfWidthNormalized*side)return "Start at the downstream end to extend an existing waterway.";}else if(b.Contains(uv))return "Connecting to existing lakes or natural waterways is not available yet.";}
                float ground=stream?RouteGround(p):View.Data.SmoothHeight(uv.x,uv.y)*terrainData.size.y;float level=Mathf.Min(last,ground-.20f);
                if(!stream&&ground-level>12)return "Draw downstream over flatter ground; this route would need a deep trench.";
                if(level<Mathf.Max(stream?1:3,View.SeaLevel+.3f))return "Choose higher ground for this channel.";
                if(joined!=null&&i==0)continue;
                centerline.Add(uv);levels.Add(level);last=level;
            }
            // Blend tributaries into the existing wet surface, without rewriting the receiver.
            if(stream){
                int first=joined!=null?joined.centerline.Length:0;
                for(int k=first;k<centerline.Count;k++){
                    var p=(centerline[k]-Vector2.one*.5f)*side;
                    var hit=PlayerChannelGeometry.SnapStream(View.Data,p,side,12,out var q);
                    if(hit==null||hit==joined)continue;
                    float halfWet=PlayerStreamWidth.AtPoint(hit,q,side);
                    float blend=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(halfWet,halfWet+5,Vector2.Distance(p,q)));
                    levels[k]=Mathf.Lerp(levels[k],PlayerChannelGeometry.Level(hit,q,side)-.15f,blend);
                }
            }
            if(centerline.Count>4096)return "This waterway is too long to extend further.";
            int stationTotal=centerline.Count;foreach(var b in View.Data.WaterBodies)if(b.playerChannel&&b!=joined)stationTotal+=b.centerline.Length;if(stationTotal>65536)return "The region has reached its waterway detail limit.";
            if(joined!=null&&!stream){float traveled=0;for(int i=1;i<samples.Count;i++){traveled+=Vector2.Distance(samples[i-1],samples[i]);if(traveled<(half+bank)*3)continue;for(int j=0;j<joined.centerline.Length-1;j++)if(Vector2.Distance(samples[i],(joined.centerline[j]-Vector2.one*.5f)*side)<(half+bank)*1.5f)return "Extend away from the existing route so its banks do not overlap.";}}
            // Nonadjacent reaches must leave enough space for their banks; reject loops/folds.
            for(int i=0;!stream&&i<samples.Count;i++)for(int j=i+1;j<samples.Count;j++){if(j-i<3)continue;float along=0;for(int k=i+1;k<=j&&along<(half+bank)*3;k++)along+=Vector2.Distance(samples[k-1],samples[k]);if(along>(half+bank)*3&&Vector2.Distance(samples[i],samples[j])<(half+bank)*1.5f)return "Separate the bends so the banks do not fold over each other.";}
            var body=new RegionWaterBody{id=joined?.id??"player-channel-"+Guid.NewGuid().ToString("N"),kind=WaterBodyKind.River,isStream=stream,playerChannel=true,centerline=centerline.ToArray(),playerChannelLevels=levels.ToArray(),playerChannelGuides=raw.ToArray(),halfWidthNormalized=half/side,bankWidthNormalized=bank/side,playerChannelDepth=canal?1.6f:stream?.65f:2,appearanceProfile=canal?"player-canal-v1":stream?"player-stream-v1":"player-river-v1"};
            body.playerStreamWidthVersion=joined?.playerStreamWidthVersion??(stream?1:0);
            body.playerStreamWidthVariation=joined?.playerStreamWidthVariation??(stream?(int)widthVariation:0);
            if(joined!=null)body.playerChannelDepth=joined.playerChannelDepth;
            else if(stream)body.playerChannelDepth=.45f;
            if (!playerLakeBaseTerrain)
                CaptureCoverageGround(terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution));
            View.RecordPlayerChannel(body,joined?.id);
            var heights=playerLakeBaseTerrain.GetHeights(0,0,terrainData.heightmapResolution,terrainData.heightmapResolution);ApplyPlayerLakeBasins(heights);ApplyPlayerChannels(heights);terrainData.SetHeights(0,0,heights);bodyShores.Clear();BuildShoreField();
            var removed=new List<Vector2>();var kept=new List<TreeInstance>();foreach(var tree in CanonicalTreeInstances){var p=Origin+new Vector2(tree.position.x,tree.position.z)*Side;if(View.InPlayerChannelClearing(p))removed.Add(p);else kept.Add(tree);}if(removed.Count>0){View.RemoveForestAdditions(removed,.6f);if(MixedForest)MixedForest.RemoveWithin(removed,.6f);fullTreeInstances=kept.ToArray();RefreshEditedForest();}
            if (Geography)
            {
                Geography.RefreshWater();
                var bounds = body.Bounds;
                Geography.Invalidate(TerrainGeography.Pad(new Rect(
                    (bounds.min - Vector2.one * .5f) * View.MapSide, bounds.size * View.MapSide), 55f));
            }
            foreach(var old in GetComponentsInChildren<LakeWaterRenderer>())if(Array.Exists(View.Data.WaterBodies,b=>b.playerChannel&&b.id==old.BodyId)){old.gameObject.SetActive(false);Destroy(old.gameObject);}BuildPlayerChannels();if(groundCover)groundCover.InvalidateWaterEdits();DisplayTerrain.Flush();View.ReleaseLoadedMeadow();return null;
        }
        void ApplyPlayerChannels(float[,] heights){foreach(var b in View.Data.WaterBodies)if(b.playerChannel){CaptureCoverageGround(heights);PlayerChannelGeometry.Apply(b,heights,Origin,Side,terrainData.size.y,View.MapSide);}}
        void BuildPlayerChannels(){var rect=new Rect(Origin/View.MapSide+Vector2.one*.5f,Vector2.one*Side/View.MapSide);foreach(var b in View.Data.WaterBodies)if(b.playerChannel&&b.Bounds.Overlaps(rect)){bool exists=false;foreach(var r in GetComponentsInChildren<LakeWaterRenderer>())if(r.BodyId==b.id){exists=true;break;}if(exists)continue;PaintPlayerChannelBanks(b);var go=new GameObject((b.isStream?"Player stream — ":"Player river — ")+b.id);go.transform.SetParent(transform,false);go.AddComponent<LakeWaterRenderer>().InitializePlayerChannel(this,b);
                if(b.appearanceProfile=="player-canal-v1"){var banks=new GameObject("Canal masonry banks");banks.transform.SetParent(go.transform,false);banks.AddComponent<CanalBankRenderer>().Initialize(this,b);}
                if(b.isStream){var falls=new GameObject("Stream waterfalls");falls.transform.SetParent(go.transform,false);falls.AddComponent<StreamWaterfallRenderer>().Initialize(this,b);var setup=go.AddComponent<GeneratedStreamAppearance>().InitializeStages(this,b);while(setup.MoveNext()){}(setup as IDisposable)?.Dispose();}}}
        int playerStreamBedOffset=-1;
        int EnsurePlayerStreamBedLayer(int wetOffset)
        {
            if(playerStreamBedOffset>=0)return playerStreamBedOffset;
            var layers=terrainData.terrainLayers;playerStreamBedOffset=layers.Length;
            var pebbles=Own(Instantiate(layers[wetOffset]));pebbles.name="Player stream coarse wet pebbles";
            var pebbleStyle=CityForge.Lots.LotPathMaterial.Find("compacted-gravel-pebble");
            if(pebbleStyle?.Texture)pebbles.diffuseTexture=pebbleStyle.Texture;
            pebbles.tileSize=new Vector2(2.5f,2.5f);pebbles.normalScale=.7f;
            pebbles.diffuseRemapMax=new Vector4(.34f,.32f,.28f,1);
            Array.Resize(ref layers,layers.Length+1);layers[playerStreamBedOffset]=pebbles;terrainData.terrainLayers=layers;
            return playerStreamBedOffset;
        }
        void PaintPlayerChannelBanks(RegionWaterBody b)
        {
            int offset=EnsureLakeBedLayers(),coarse=b.isStream?EnsurePlayerStreamBedLayer(offset):offset,n=terrainData.alphamapResolution;float side=View.MapSide,bank=b.playerStreamWidthVersion==1?b.bankWidthNormalized*side+1.5f:Mathf.Max(10,b.bankWidthNormalized*side+6);
            var min=(b.Bounds.min-Vector2.one*.5f)*side-Vector2.one*bank;var max=(b.Bounds.max-Vector2.one*.5f)*side+Vector2.one*bank;
            int x0=Mathf.Clamp(Mathf.FloorToInt((min.x-Origin.x)/Side*(n-1)),0,n-1),x1=Mathf.Clamp(Mathf.CeilToInt((max.x-Origin.x)/Side*(n-1)),0,n-1),z0=Mathf.Clamp(Mathf.FloorToInt((min.y-Origin.y)/Side*(n-1)),0,n-1),z1=Mathf.Clamp(Mathf.CeilToInt((max.y-Origin.y)/Side*(n-1)),0,n-1);var maps=terrainData.GetAlphamaps(x0,z0,x1-x0+1,z1-z0+1);var sampler=PlayerChannelGeometry.Sampler(b,side);
            PlayerChannelGeometry.RasterRanges(b,side,Origin,Vector2.one*(Side/(n-1)),n,n,bank,out var first,out var last);
            for(int z=z0;z<=z1;z++)for(int x=Mathf.Max(x0,first[z]);x<=Mathf.Min(x1,last[z]);x++){var p=Origin+new Vector2(x/(float)(n-1),z/(float)(n-1))*Side;if(!Contains(p))continue;float distance=sampler.Query(p,out _,out var current);float d=distance-PlayerStreamWidth.AtStation(b,current.x/side)*side;if(d>bank)continue;float amount=d<0?1:Mathf.Lerp(.68f,.84f,Noise(p,17,812))*(1-Mathf.SmoothStep(0,1,d/bank));
                // Preserve the authored/natural dry bank textures; only paint the stream bed.
                if(b.isStream&&d>=0)continue;
                amount*=RiverSedimentWeight(p/side+Vector2.one*.5f);
                if(amount<=0)continue;
                int zz=z-z0,xx=x-x0;for(int l=0;l<maps.GetLength(2);l++)maps[zz,xx,l]=(l<lakeBaseAlphamaps.GetLength(2)?lakeBaseAlphamaps[z,x,l]:0)*(1-amount);if(b.isStream){
                    float coarseShare=Mathf.Lerp(.12f,.85f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,.9f,distance/Mathf.Max(.01f,distance-d))));
                    maps[zz,xx,coarse]+=amount*coarseShare;maps[zz,xx,offset]+=amount*(.85f-coarseShare);maps[zz,xx,offset+1]+=amount*.12f;maps[zz,xx,offset+2]+=amount*.03f;
                }else{maps[zz,xx,offset]+=amount*.3f;maps[zz,xx,offset+1]+=amount*.1f;maps[zz,xx,offset+2]+=amount*.6f;}}
            terrainData.SetAlphamaps(x0,z0,maps);
        }
    }
}
