using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CityForge.World
{
    /// <summary>Pass17 assets and presentation sampled along saved regional stream routes.</summary>
    public sealed class GeneratedStreamAppearance:MonoBehaviour
    {
        struct Batch {public Mesh mesh;public Material material;public Matrix4x4[] instances;}
        readonly List<Batch> batches=new();readonly List<Vector3> clusterSeats=new();Bounds bounds;
        public int PatchCount {get;private set;}
        public int StoneCount {get;private set;}
        public int ShrubCount {get;private set;}
        public static bool Applies(PrototypeRegionView view)
        {if(view?.Data?.WaterBodies!=null)foreach(var body in view.Data.WaterBodies)if(body.isStream&&body.channelVersion>=2)return true;return false;}
        public static bool Near(PrototypeRegionView view,Vector2 world,out Vector2 studyPoint,out float distance)
        {
            studyPoint=default;distance=float.PositiveInfinity;float side=view.MapSide;var uv=world/side+Vector2.one*.5f;
            foreach(var body in view.Data.WaterBodies){
                if(!body.isStream||body.channelVersion<2)continue;
                var area=body.Bounds;float margin=5/side;
                if(uv.x<area.xMin-margin||uv.x>area.xMax+margin||uv.y<area.yMin-margin||uv.y>area.yMax+margin)continue;
                float along=body.channelAlongX?uv.x:uv.y;
                int segments=body.centerline.Length-1;float reach=(5f/side)+body.halfWidthNormalized;
                int first=Mathf.Clamp(Mathf.FloorToInt((along-reach)*segments),0,segments-1),last=Mathf.Clamp(Mathf.FloorToInt((along+reach)*segments),0,segments-1);
                for(int i=first;i<=last;i++){
                    var a=body.centerline[i];var delta=body.centerline[i+1]-a;
                    float t=Mathf.Clamp01(Vector2.Dot(uv-a,delta)/delta.sqrMagnitude);var offset=(uv-a-delta*t)*side;
                    float d=offset.magnitude-body.halfWidthNormalized*side;if(d>=distance||d>5)continue;
                    float station=(i+t)*side/segments;var normal=new Vector2(delta.y,-delta.x).normalized;
                    studyPoint=new Vector2(StreamRockTestRegion.Center(station)+Vector2.Dot(offset,normal),station);distance=d;
                }
            }
            return distance<=5;
        }
        public static bool StreamOnlyDistrict(MeadowDistrictLandscape land)
        {
            bool stream=false;var rect=new Rect(land.Origin/land.View.MapSide+Vector2.one*.5f,Vector2.one*land.Side/land.View.MapSide);
            foreach(var body in land.View.Data.WaterBodies)if(body.kind==WaterBodyKind.River&&body.Bounds.Overlaps(rect)){if(!body.isStream)return false;stream=true;}
            return stream;
        }
        public static Vector2 Cover(MeadowDistrictLandscape land,Vector2 world,float distance,Vector2 studyPoint)
            =>StreamBankBlend.VariedCover(land,world,distance,studyPoint);
        void AddBatch(RiverBedRockAssets assets,int type,List<Matrix4x4> matrices)
        {
            for(int i=0;i<matrices.Count;i+=1023)batches.Add(new Batch{mesh=assets.meshes[type],material=assets.materials[type],instances=matrices.GetRange(i,Mathf.Min(1023,matrices.Count-i)).ToArray()});
        }
        public IEnumerator InitializeStages(MeadowDistrictLandscape land)
        {
            var routes=new List<RegionWaterBody>();var rect=new Rect(land.Origin/land.View.MapSide+Vector2.one*.5f,Vector2.one*land.Side/land.View.MapSide);
            foreach(var body in land.View.Data.WaterBodies)if(body.isStream&&body.channelVersion>=2&&body.Bounds.Overlaps(rect))routes.Add(body);
            if(routes.Count==0)yield break;
            var patches=Resources.Load<RiverBedRockAssets>("StreamRocks/Rocks");var stones=Resources.Load<RiverBedRockAssets>("IndividualStreamRocks/Stones");
            if(!patches||patches.meshes.Length==0||!stones||stones.meshes.Length!=10)throw new InvalidOperationException("Approved Pass17 stream rock assets are missing. Prepare the existing private StreamRocks/Rocks and IndividualStreamRocks/Stones assets.");
            var patchMatrices=new List<Matrix4x4>();var stoneMatrices=new List<Matrix4x4>[10];for(int i=0;i<10;i++)stoneMatrices[i]=new List<Matrix4x4>();
            var trees=new List<TreeInstance>(land.DisplayTerrain.terrainData.treeInstances);var shrubTypes=new List<int>();
            var library=Resources.Load<SeasonalTreeLibrary>("Seasons/SeasonalTrees");var prefabs=library?library.Prefabs(land.View.VisualSeason):null;
            if(prefabs!=null)for(int i=0;i<prefabs.Length&&i<land.DisplayTerrain.terrainData.treePrototypes.Length;i++)if(prefabs[i]&&(prefabs[i].name.ToLowerInvariant().Contains("bush")||prefabs[i].name.ToLowerInvariant().Contains("willow")))shrubTypes.Add(i);
            bool Fits(Vector2 p,float radius)=>land.Contains(p+Vector2.one*radius)&&land.Contains(p-Vector2.one*radius)&&land.Contains(p+new Vector2(radius,-radius))&&land.Contains(p+new Vector2(-radius,radius));
            bool haveBounds=false;
            void Seat(Vector2 p,float radius,float y){var b=new Bounds(new Vector3(p.x,y,p.y),Vector3.one*(radius*2+4));if(!haveBounds){bounds=b;haveBounds=true;}else bounds.Encapsulate(b);}
            foreach(var body in routes){
                // Seed per route, independent of selected district and combined footprint.
                int salt=17;foreach(char c in body.id)salt=unchecked(salt*31+c);var random=new System.Random(land.View.Data.Seed^salt);float R()=>(float)random.NextDouble();
                float half=body.halfWidthNormalized*land.View.MapSide;
                void Frame(float station,out Vector2 p,out Vector2 normal){int segments=body.centerline.Length-1;float row=Mathf.Clamp01(station/land.View.MapSide)*segments;int i=Mathf.Min(segments-1,(int)row);var a=body.centerline[i];var delta=body.centerline[i+1]-a;p=(a+delta*(row-i)-Vector2.one*.5f)*land.View.MapSide;normal=new Vector2(delta.y,-delta.x).normalized;}
                bool Usable(Vector2 p){if(!land.Contains(p)||land.View.InCampClearing(p,2))return false;var uv=p/land.View.MapSide+Vector2.one*.5f;foreach(var water in land.View.Data.WaterBodies)if(water.kind==WaterBodyKind.Ocean&&water.Contains(uv))return false;return true;}
                // Retain long irregular gaps between scanned patches, as in Pass17.
                for(float station=7;station<land.View.MapSide-7;station+=6.3f){
                    float s=station+(R()-.5f)*2,sign=R()<.5f?-1:1,scale=Mathf.Lerp(.60f,.98f,R()),heading=R()*360;
                    Frame(s,out var p,out var normal);p+=normal*sign*half*.70f;
                    if(!Usable(p)||Mathf.Sin(s*.055f)+.55f*Mathf.Sin(s*.137f)<-.2f)continue;
                    float radius=patches.meshes[0].bounds.extents.magnitude;
                    scale=Mathf.Min(scale,half*.55f/Mathf.Max(.01f,radius));radius*=scale;if(!Fits(p,radius))continue;
                    float y=land.Height(p)-.10f;patchMatrices.Add(Matrix4x4.TRS(new Vector3(p.x,y,p.y),Quaternion.Euler(0,heading,0),Vector3.one*scale));clusterSeats.Add(new Vector3(p.x,p.y,radius));PatchCount++;Seat(p,radius,y);
                }
                for(float station=6;station<land.View.MapSide-6;station+=1.15f){
                    for(int j=0;j<3;j++){
                        float s=station+(R()-.5f)*1.15f,across=(R()*2-1)*half*.90f,band=R();
                        float diameter=band<.62f?Mathf.Lerp(.16f,.40f,R()):band<.92f?Mathf.Lerp(.40f,.75f,R()):Mathf.Lerp(.85f,1.5f,R());
                        int type=random.Next(10);float heading=R()*360;Frame(s,out var p,out var normal);p+=normal*across;if(!Usable(p)||!Fits(p,diameter))continue;
                        bool overlap=false;foreach(var seat in clusterSeats)if(Vector2.Distance(p,new Vector2(seat.x,seat.y))<seat.z+diameter*.71f+.12f){overlap=true;break;}if(overlap)continue;
                        float ground=land.Height(p);var uv=p/land.View.MapSide+Vector2.one*.5f;
                        if(ground>RegionalRiverNetwork.GroundWaterLevel(land.View.Data,uv.x,uv.y)-.04f)continue;
                        float y=ground-diameter*.12f;stoneMatrices[type].Add(Matrix4x4.TRS(new Vector3(p.x,y,p.y),Quaternion.Euler(0,heading,0),Vector3.one*diameter));StoneCount++;Seat(p,diameter,y);
                    }
                    if(((int)(station/1.15f))%128==0)yield return null;
                }
                for(float station=14;station<land.View.MapSide-14;station+=11){
                    float sign=R()<.5f?-1:1,offset=Mathf.Lerp(-.2f,1.7f,R()),scale=Mathf.Lerp(.5f,.85f,R()),rotation=R()*Mathf.PI*2;int type=shrubTypes.Count>0?shrubTypes[random.Next(shrubTypes.Count)]:-1;
                    Frame(station,out var p,out var normal);p+=normal*sign*(half+offset);if(type<0||!Usable(p))continue;
                    float h=land.Height(p);var uv=p/land.View.MapSide+Vector2.one*.5f;if(h<RegionalRiverNetwork.GroundWaterLevel(land.View.Data,uv.x,uv.y)-.12f)continue;
                    var position=new Vector3((p.x-land.Origin.x)/land.Side,(h-.06f)/land.DisplayTerrain.terrainData.size.y,(p.y-land.Origin.y)/land.Side);
                    if(!trees.Exists(t=>t.prototypeIndex==type&&Mathf.Abs(t.position.x-position.x)<.000001f&&Mathf.Abs(t.position.z-position.z)<.000001f)){trees.Add(new TreeInstance{position=position,prototypeIndex=type,widthScale=scale,heightScale=scale,rotation=rotation,color=Color.white,lightmapColor=Color.white});ShrubCount++;}
                }
                yield return null;
            }
            AddBatch(patches,0,patchMatrices);for(int i=0;i<10;i++)AddBatch(stones,i,stoneMatrices[i]);
            land.DisplayTerrain.terrainData.SetTreeInstances(trees.ToArray(),false);
        }
        void LateUpdate(){foreach(var batch in batches){var parameters=new RenderParams(batch.material){worldBounds=bounds,shadowCastingMode=ShadowCastingMode.On,receiveShadows=true};Graphics.RenderMeshInstanced(parameters,batch.mesh,0,batch.instances);}}
    }
}
