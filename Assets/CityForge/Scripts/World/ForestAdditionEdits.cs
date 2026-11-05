using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace CityForge.World
{
    [Serializable] public sealed class ForestAdditionRecord
    {
        public float x,z,width,height,rotation;
        public int district,prototype,variant;
    }
    public sealed partial class PrototypeRegionView
    {
        public ForestAdditionRecord[] ForestAdditions {get;private set;}=Array.Empty<ForestAdditionRecord>();
        Dictionary<Vector2Int,ForestAdditionRecord> additionIndex;
        public static Vector2Int ForestPointKey(Vector2 point)=>new Vector2Int(Mathf.RoundToInt(point.x*100),Mathf.RoundToInt(point.y*100));
        public ForestAdditionRecord ForestAddition(Vector2 point)
        {
            if(ForestAdditions.Length==0)return null;
            if(additionIndex==null){additionIndex=new Dictionary<Vector2Int,ForestAdditionRecord>();foreach(var tree in ForestAdditions)additionIndex[ForestPointKey(new Vector2(tree.x,tree.z))]=tree;}
            return additionIndex.TryGetValue(ForestPointKey(point),out var record)?record:null;
        }
        internal void RecordForestAdditions(List<ForestAdditionRecord> records)
        {
            var all=new List<ForestAdditionRecord>(ForestAdditions);all.AddRange(records);ForestAdditions=all.ToArray();additionIndex=null;WoodlandRevision++;
        }
        internal void RemoveForestAdditions(IReadOnlyList<Vector2> centers,float radius)
        {
            var kept=new List<ForestAdditionRecord>();foreach(var tree in ForestAdditions)if(!InActiveDistrict(new Vector2(tree.x,tree.z))||!ForestRemovalValidation.InBrush(new Vector2(tree.x,tree.z),centers,radius*radius))kept.Add(tree);
            ForestAdditions=kept.ToArray();additionIndex=null;
        }
    }
    public static class ForestAdditionValidation
    {
        public const int Limit=65536;
        public static void Validate(ForestAdditionRecord[] records,int grid,float mapSide)
        {
            if(records==null)return;if(records.Length>Limit)throw new InvalidDataException("Too many planted trees.");var keys=new HashSet<Vector2Int>();
            foreach(var t in records){
                if(t==null||!float.IsFinite(t.x)||!float.IsFinite(t.z)||Mathf.Abs(t.x)>mapSide*.5f||Mathf.Abs(t.z)>mapSide*.5f||t.district<0||t.district>=grid*grid
                    ||t.prototype<ForestTreeMix.BroadleafStart||t.prototype>=ForestTreeMix.PrototypeCount||t.variant<0||t.variant>7||!float.IsFinite(t.width)||!float.IsFinite(t.height)||!float.IsFinite(t.rotation)||t.width<=0||t.height<=0||t.width>10||t.height>10||!keys.Add(PrototypeRegionView.ForestPointKey(new Vector2(t.x,t.z))))throw new InvalidDataException("Invalid planted tree.");
                var bounds=RegionBiomeMap.DistrictBounds(t.district,grid);var uv=new Vector2(t.x/mapSide+.5f,t.z/mapSide+.5f);if(!bounds.Contains(uv))throw new InvalidDataException("Planted tree outside its district.");
            }
        }
    }
    public sealed partial class MeadowDistrictLandscape
    {
        TreeInstance PlantedInstance(ForestAdditionRecord record)=>new TreeInstance{position=new Vector3((record.x-Origin.x)/Side,Height(new Vector2(record.x,record.z))/terrainData.size.y,(record.z-Origin.y)/Side),prototypeIndex=record.prototype,widthScale=record.width,heightScale=record.height,rotation=record.rotation,color=Color.white,lightmapColor=Color.white};
        TreeInstance[] MergePlantedForest(TreeInstance[] source)
        {
            if(View.ForestAdditions.Length==0)return source;var result=new List<TreeInstance>(source.Length);
            foreach(var tree in source){var point=Origin+new Vector2(tree.position.x,tree.position.z)*Side;if(View.ForestAddition(point)==null)result.Add(tree);}
            foreach(var tree in View.ForestAdditions)if(Contains(new Vector2(tree.x,tree.z))&&!View.InPlayerLakeClearing(new Vector2(tree.x,tree.z))&&!View.InPlayerChannelClearing(new Vector2(tree.x,tree.z)))result.Add(PlantedInstance(tree));return result.ToArray();
        }
        void RefreshEditedForest()
        {
            var mature=new List<TreeInstance>();canopy.Clear();
            foreach(var tree in fullTreeInstances){if(!IsSmallPlant(tree.prototypeIndex))mature.Add(tree);if(tree.prototypeIndex>=3){var p=Origin+new Vector2(tree.position.x,tree.position.z)*Side;var key=Vector2Int.FloorToInt(p/16);if(!canopy.TryGetValue(key,out var list))canopy[key]=list=new List<Vector2>();list.Add(p);}}
            matureTreeInstances=mature.ToArray();SetNativePresentation(SmallPlantsEnabled?fullTreeInstances:matureTreeInstances);DisplayTerrain.Flush();
        }
        public int AddForest(Vector2 center,float radius,int species=0)=>AddForest(new[]{center},radius,species);
        public int AddForest(IReadOnlyList<Vector2> centers,float radius,int species=0)
        {
            if(IsRegionOverview||centers==null||centers.Count==0||!TreesEnabled||View.DistrictLoading||View.IsSaving||!float.IsFinite(radius)||radius<=0||radius>1000)return 0;
            int capacity=Mathf.Min(ForestAdditionValidation.Limit-View.ForestAdditions.Length,200000-CanonicalTreeInstances.Length);if(capacity<=0)return 0;
            var valid=new List<Vector2>();foreach(var p in centers)if(float.IsFinite(p.x)&&float.IsFinite(p.y)&&Contains(p))valid.Add(p);if(valid.Count==0)return 0;
            const float separation=4.2f;var occupied=new Dictionary<Vector2Int,List<Vector2>>();
            void Index(Vector2 point){var key=Vector2Int.FloorToInt(point/separation);if(!occupied.TryGetValue(key,out var list))occupied[key]=list=new List<Vector2>();list.Add(point);}
            foreach(var tree in CanonicalTreeInstances)if(ForestTree(tree.prototypeIndex))Index(Origin+new Vector2(tree.position.x,tree.position.z)*Side);
            var candidates=new Dictionary<Vector2Int,(Vector2 point,uint hash)>();float radius2=radius*radius;
            foreach(int district in View.ActiveDistrictMembers){
                var origin=View.DistrictOrigin(district);float side=View.DistrictSide;int n=Mathf.Max(1,Mathf.RoundToInt(side/DistrictTreeCoverage.Spacing));float step=side/n;
                foreach(var center in valid){
                    int minX=Mathf.Clamp(Mathf.FloorToInt((center.x-radius-origin.x)/step)-1,0,n-1),maxX=Mathf.Clamp(Mathf.CeilToInt((center.x+radius-origin.x)/step)+1,0,n-1);
                    int minZ=Mathf.Clamp(Mathf.FloorToInt((center.y-radius-origin.y)/step)-1,0,n-1),maxZ=Mathf.Clamp(Mathf.CeilToInt((center.y+radius-origin.y)/step)+1,0,n-1);
                    for(int z=minZ;z<=maxZ;z++)for(int x=minX;x<=maxX;x++){
                        uint hash=DistrictTreeCoverage.Hash(x,z,Seed^district*19349663);float jx=((hash&65535)/65535f-.5f)*.55f,jz=(((hash>>16)&65535)/65535f-.5f)*.55f;
                        var p=origin+new Vector2(x+.5f+jx,z+.5f+jz)*step;p=new Vector2(Mathf.Round(p.x*100)/100,Mathf.Round(p.y*100)/100);
                        if((p-center).sqrMagnitude<=radius2)candidates[PrototypeRegionView.ForestPointKey(p)]=(p,hash);
                    }
                }
            }
            var ordered=new List<(Vector2 point,uint hash)>(candidates.Values);ordered.Sort((a,b)=>a.hash.CompareTo(b.hash));var additions=new List<ForestAdditionRecord>();var native=new List<TreeInstance>();var library=MixedForest?MixedForestLibrary.Load():null;
            foreach(var candidate in ordered){
                if(additions.Count>=capacity)break;var p=candidate.point;
                if(!Contains(p)||View.InPlayerLakeClearing(p)||View.InPlayerChannelClearing(p)||View.InCampClearing(p,8)||WaterDistance(p)<9||RoadDistance(p)<5||Height(p)<View.SeaLevel+.2f||MountainSlope(p)>25)continue;
                bool near=false;var key=Vector2Int.FloorToInt(p/separation);
                for(int z=-1;z<=1&&!near;z++)for(int x=-1;x<=1&&!near;x++)if(occupied.TryGetValue(key+new Vector2Int(x,z),out var nearby))foreach(var other in nearby)if((other-p).sqrMagnitude<separation*separation){near=true;break;}
                if(near)continue;uint hash=MixedDistrictForest.PointHash(p,Seed);int variant=species==1?(int)(hash%5):species==2?5:species==3?(hash%4==0?7:6):library?library.Pick(hash):(int)(hash%8);
                var record=new ForestAdditionRecord{x=p.x,z=p.y,district=View.DistrictAtWorld(new Vector3(p.x,0,p.y)),variant=variant,prototype=ForestTreeMix.BroadleafStart+(int)(candidate.hash%8),width=Mathf.Lerp(.8f,1.12f,((candidate.hash>>8)&65535)/65535f),height=Mathf.Lerp(.8f,1.12f,((candidate.hash>>8)&65535)/65535f),rotation=(candidate.hash&65535)/65535f*Mathf.PI*2};
                additions.Add(record);native.Add(PlantedInstance(record));Index(p);
            }
            if(additions.Count==0)return 0;View.RecordForestAdditions(additions);var all=new List<TreeInstance>(CanonicalTreeInstances);all.AddRange(native);fullTreeInstances=all.ToArray();
            if(MixedForest)MixedForest.AddTrees(native);RefreshEditedForest();return additions.Count;
        }
    }
}
