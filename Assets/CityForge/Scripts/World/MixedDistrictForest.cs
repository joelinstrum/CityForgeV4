using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CityForge.World
{
    /// <summary>Versioned summer presentation of canonical woodland; stored ecology and terrain remain authoritative.</summary>
    public sealed class MixedDistrictForest : MonoBehaviour
    {
        const float CellSide=10;
        MeadowDistrictLandscape land;
        GameObject holder;
        readonly List<(Transform root,uint hash,int variant)> placements=new();
        public int TreeCount {get;private set;}
        public string PlacementSignature {get;private set;}
        public bool Ready {get;private set;}
        public static bool Applies(MeadowDistrictLandscape landscape)=>landscape&&!landscape.IsRegionOverview
            &&landscape.View.Climate==RegionClimate.Temperate&&landscape.View.CreationSettings?.forestAppearanceVersion==1;
        public static bool ReplacedPrototype(int index)=>(index>=3&&index<=10)||(index>=ForestTreeMix.BroadleafStart&&index<ForestTreeMix.PrototypeCount);
        struct Candidate { public TreeInstance native; public Vector2 point; public uint hash; }
        public static uint PointHash(Vector2 point,int seed)
        {
            unchecked {uint h=(uint)(Mathf.RoundToInt(point.x*100)*73856093^Mathf.RoundToInt(point.y*100)*19349663^seed);h^=h>>16;h*=0x7feb352d;h^=h>>15;return h;}
        }
        public IEnumerator InitializeStages(MeadowDistrictLandscape source)
        {
            land=source;var library=MixedForestLibrary.Load();
            var cells=new Dictionary<Vector2Int,Candidate>();bool coverage=DistrictTreeCoverage.Applies(land.View.CreationSettings);int unique=0;
            foreach(var tree in land.CanonicalTreeInstances){
                if(!ReplacedPrototype(tree.prototypeIndex))continue;
                var p=land.Origin+new Vector2(tree.position.x,tree.position.z)*land.Side;
                if(!land.Contains(p)||land.View.InCampClearing(p,8)||land.WaterDistance(p)<9||land.RoadDistance(p)<5
                    ||land.Height(p)<land.View.SeaLevel+.2f||land.MountainSlope(p)>25||(!coverage&&land.View.ForestAddition(p)==null&&land.Woodland(p)<.2f))continue;
                uint hash=PointHash(p,land.Seed);var key=coverage||land.View.ForestAddition(p)!=null?new Vector2Int(int.MinValue+unique++,0):Vector2Int.FloorToInt(p/CellSide);
                if(!cells.TryGetValue(key,out var previous)||hash<previous.hash)cells[key]=new Candidate{native=tree,point=p,hash=hash};
            }
            var candidates=new List<Candidate>(cells.Values);candidates.Sort((a,b)=>a.hash.CompareTo(b.hash));
            holder=new GameObject("Approved mixed woodland");holder.transform.SetParent(transform,false);holder.SetActive(false);
            long started=System.Diagnostics.Stopwatch.GetTimestamp();ulong signature=1469598103934665603UL;
            foreach(var candidate in candidates){
                int variant=land.View.ForestAddition(candidate.point)?.variant??library.Pick(candidate.hash);var template=library.prefabs[variant];
                var tree=Instantiate(template,holder.transform,false);tree.name=template.name;
                tree.transform.SetPositionAndRotation(new Vector3(candidate.point.x,land.Height(candidate.point),candidate.point.y),
                    Quaternion.Euler(0,candidate.native.rotation*Mathf.Rad2Deg,0)*template.transform.localRotation);
                tree.transform.localScale=template.transform.localScale*Mathf.Lerp(.88f,1.12f,((candidate.hash>>8)&65535)/65535f);
                unchecked {signature=(signature^candidate.hash)*1099511628211UL;signature=(signature^(uint)variant)*1099511628211UL;}
                placements.Add((tree.transform,candidate.hash,variant));TreeCount++;
                if((System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency>=8){yield return null;started=System.Diagnostics.Stopwatch.GetTimestamp();}
            }
            PlacementSignature=signature.ToString("x16");
            var renderer=holder.AddComponent<SpatialForestRenderer>();renderer.viewCamera=land.View.GetComponentInChildren<Camera>();
            renderer.shadowSources=library.shadowSources;renderer.shadowReplacements=library.shadowReplacements;
            renderer.simplifiedDistantShadows=true;renderer.minimumInstancedViewSize=200;renderer.instanced=true;
            renderer.Capture(prepareInstancedView:true);Ready=true;RefreshVisibility();
        }
        public int AddTrees(IReadOnlyList<TreeInstance> source)
        {
            if(!holder)return 0;var library=MixedForestLibrary.Load();var roots=new List<Transform>();
            foreach(var native in source){
                var point=land.Origin+new Vector2(native.position.x,native.position.z)*land.Side;uint hash=PointHash(point,land.Seed);
                int variant=land.View.ForestAddition(point)?.variant??library.Pick(hash);var template=library.prefabs[variant];var tree=Instantiate(template,holder.transform,false);tree.name=template.name;
                tree.transform.position=new Vector3(point.x,land.Height(point),point.y);tree.transform.rotation=Quaternion.Euler(0,native.rotation*Mathf.Rad2Deg,0)*template.transform.localRotation;
                tree.transform.localScale=template.transform.localScale*Mathf.Lerp(.88f,1.12f,((hash>>8)&65535)/65535f);placements.Add((tree.transform,hash,variant));roots.Add(tree.transform);
            }
            holder.GetComponent<SpatialForestRenderer>().Append(roots);placements.Sort((a,b)=>a.hash.CompareTo(b.hash));RefreshSignature();return roots.Count;
        }
        void RefreshSignature()
        {
            TreeCount=placements.Count;ulong signature=1469598103934665603UL;
            foreach(var p in placements)unchecked{signature=(signature^p.hash)*1099511628211UL;signature=(signature^(uint)p.variant)*1099511628211UL;}
            PlacementSignature=signature.ToString("x16");
        }
        public int RemoveWithin(Vector2 center,float radius)=>RemoveWithin(new[]{center},radius);
        public int RemoveWithin(IReadOnlyList<Vector2> centers,float radius)
        {
            if(!holder)return 0;var renderer=holder.GetComponent<SpatialForestRenderer>();int removed=renderer.RemoveWithin(centers,radius);if(removed==0)return 0;
            float radius2=radius*radius;placements.RemoveAll(p=>!p.root||ForestRemovalValidation.InBrush(new Vector2(p.root.position.x,p.root.position.z),centers,radius2));
            RefreshSignature();return removed;
        }
        public void RefreshVisibility(){if(holder&&land)holder.SetActive(land.TreesEnabled&&land.View.VisualSeason==RegionSeason.Summer);}
        public TreeInstance[] NativePresentation(TreeInstance[] source)
        {
            if(!land||land.View.VisualSeason!=RegionSeason.Summer)return source;
            var result=new List<TreeInstance>(source.Length);foreach(var tree in source)if(!ReplacedPrototype(tree.prototypeIndex))result.Add(tree);return result.ToArray();
        }
    }
}
