using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace CityForge.World
{
    [Serializable] public sealed class ForestRemovalStroke
    {
        public float x,z,radius;
        public int[] districts;
    }
    public sealed partial class PrototypeRegionView
    {
        public ForestRemovalStroke[] ForestRemovals {get;private set;}=Array.Empty<ForestRemovalStroke>();
        public bool IsForestRemoved(Vector2 point)
        {
            if(InPlayerLakeClearing(point)||InPlayerChannelClearing(point))return true;
            if(ForestAddition(point)!=null||ForestRemovals.Length==0)return false;int district=DistrictAtWorld(new Vector3(point.x,0,point.y));
            foreach(var stroke in ForestRemovals){if((point-new Vector2(stroke.x,stroke.z)).sqrMagnitude>stroke.radius*stroke.radius)continue;foreach(int member in stroke.districts)if(member==district)return true;}return false;
        }
        internal void RecordForestRemovals(IReadOnlyList<Vector2> points,float radius)
        {
            var strokes=new List<ForestRemovalStroke>(ForestRemovals);
            foreach(var point in points)strokes.Add(new ForestRemovalStroke{x=point.x,z=point.y,radius=radius,districts=(int[])ActiveDistrictMembers.Clone()});ForestRemovals=strokes.ToArray();WoodlandRevision++;
        }
    }
    public static class ForestRemovalValidation
    {
        public static bool InBrush(Vector2 point,IReadOnlyList<Vector2> centers,float radius2)
        {
            for(int i=0;i<centers.Count;i++)if((point-centers[i]).sqrMagnitude<=radius2)return true;return false;
        }
        public static void Validate(ForestRemovalStroke[] strokes,int grid)
        {
            if(strokes==null)return;if(strokes.Length>8192)throw new InvalidDataException("Too many forest removal edits.");
            foreach(var stroke in strokes){
                if(stroke==null||!float.IsFinite(stroke.x)||!float.IsFinite(stroke.z)||!float.IsFinite(stroke.radius)||stroke.radius<=0||stroke.radius>1000||stroke.districts==null||stroke.districts.Length<1||stroke.districts.Length>4)throw new InvalidDataException("Invalid forest removal edit.");
                var unique=new HashSet<int>();foreach(int cell in stroke.districts)if(cell<0||cell>=grid*grid||!unique.Add(cell))throw new InvalidDataException("Invalid forest removal district.");
            }
        }
    }
    public sealed partial class MeadowDistrictLandscape
    {
        bool ForestTree(int prototype)=>prototype<11||prototype>=Baseline.treePrefabs.Length;
        TreeInstance[] FilterForestEdits(TreeInstance[] source)
        {
            var kept=new List<TreeInstance>(source.Length);
            foreach(var tree in source){var point=Origin+new Vector2(tree.position.x,tree.position.z)*Side;if(!View.InPlayerLakeClearing(point)&&!View.InPlayerChannelClearing(point)&&(!ForestTree(tree.prototypeIndex)||!View.IsForestRemoved(point)))kept.Add(tree);}
            return MergePlantedForest(kept.ToArray());
        }

        void SetGeneratedForestTrees(List<TreeInstance> trees)
        {
            var filtered=FilterForestEdits(trees.ToArray());
            if(View.ForestRemovals.Length>0||View.ForestAdditions.Length>0||playerLakeBaseTerrain){canopy.Clear();foreach(var tree in filtered)if(tree.prototypeIndex>=3){var point=Origin+new Vector2(tree.position.x,tree.position.z)*Side;var key=Vector2Int.FloorToInt(point/16);if(!canopy.TryGetValue(key,out var list))canopy[key]=list=new List<Vector2>();list.Add(point);}}
            terrainData.SetTreeInstances(filtered,true);
        }
        public int RemoveForest(Vector2 center,float radius)=>RemoveForest(new[]{center},radius);
        public int RemoveForest(IReadOnlyList<Vector2> centers,float radius)
        {
            if(IsRegionOverview||centers==null||centers.Count==0||!TreesEnabled||View.DistrictLoading||View.IsSaving||!float.IsFinite(radius)||radius<=0||radius>1000)return 0;
            if(View.ForestRemovals.Length>=8192)return 0;
            var valid=new List<Vector2>();int available=8192-View.ForestRemovals.Length;
            foreach(var point in centers){if(valid.Count>=available)break;if(float.IsFinite(point.x)&&float.IsFinite(point.y)&&Contains(point))valid.Add(point);}if(valid.Count==0)return 0;
            var source=CanonicalTreeInstances;var kept=new List<TreeInstance>(source.Length);float radius2=radius*radius;
            foreach(var tree in source){var point=Origin+new Vector2(tree.position.x,tree.position.z)*Side;if(!ForestTree(tree.prototypeIndex)||!ForestRemovalValidation.InBrush(point,valid,radius2))kept.Add(tree);}
            // The edit mask also covers authored legacy crowns/palms and future cache regeneration.
            View.RecordForestRemovals(valid,radius);View.RemoveForestAdditions(valid,radius);int visibleRemoved=MixedForest?MixedForest.RemoveWithin(valid,radius):0;
            if(kept.Count==source.Length&&visibleRemoved==0)return 0;
            fullTreeInstances=kept.ToArray();
            RefreshEditedForest();
            return source.Length-fullTreeInstances.Length;
        }
    }
}
