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
        public int TreeCount {get;private set;}
        public string PlacementSignature {get;private set;}
        public bool Ready {get;private set;}
        public static bool Applies(MeadowDistrictLandscape landscape)=>landscape&&!landscape.IsRegionOverview
            &&landscape.View.Climate==RegionClimate.Temperate&&landscape.View.CreationSettings?.forestAppearanceVersion==1;
        public static bool ReplacedPrototype(int index)=>(index>=3&&index<=10)||(index>=ForestTreeMix.BroadleafStart&&index<ForestTreeMix.PrototypeCount);
        struct Candidate { public TreeInstance native; public Vector2 point; public uint hash; }
        static uint Hash(Vector2 point,int seed)
        {
            unchecked {uint h=(uint)(Mathf.RoundToInt(point.x*100)*73856093^Mathf.RoundToInt(point.y*100)*19349663^seed);h^=h>>16;h*=0x7feb352d;h^=h>>15;return h;}
        }
        public IEnumerator InitializeStages(MeadowDistrictLandscape source)
        {
            land=source;var library=MixedForestLibrary.Load();
            var cells=new Dictionary<Vector2Int,Candidate>();
            foreach(var tree in land.CanonicalTreeInstances){
                if(!ReplacedPrototype(tree.prototypeIndex))continue;
                var p=land.Origin+new Vector2(tree.position.x,tree.position.z)*land.Side;
                if(!land.Contains(p)||land.View.InCampClearing(p,8)||land.WaterDistance(p)<9||land.RoadDistance(p)<5
                    ||land.Height(p)<land.View.SeaLevel+.2f||land.MountainSlope(p)>25||land.Woodland(p)<.2f)continue;
                uint hash=Hash(p,land.Seed);var key=Vector2Int.FloorToInt(p/CellSide);
                if(!cells.TryGetValue(key,out var previous)||hash<previous.hash)cells[key]=new Candidate{native=tree,point=p,hash=hash};
            }
            var candidates=new List<Candidate>(cells.Values);candidates.Sort((a,b)=>a.hash.CompareTo(b.hash));
            holder=new GameObject("Approved mixed woodland");holder.transform.SetParent(transform,false);holder.SetActive(false);
            long started=System.Diagnostics.Stopwatch.GetTimestamp();ulong signature=1469598103934665603UL;
            foreach(var candidate in candidates){
                int variant=library.Pick(candidate.hash);var template=library.prefabs[variant];
                var tree=Instantiate(template,holder.transform,false);tree.name=template.name;
                tree.transform.position=new Vector3(candidate.point.x,land.Height(candidate.point),candidate.point.y);
                tree.transform.rotation=Quaternion.Euler(0,candidate.native.rotation*Mathf.Rad2Deg,0)*template.transform.localRotation;
                tree.transform.localScale=template.transform.localScale*Mathf.Lerp(.88f,1.12f,((candidate.hash>>8)&65535)/65535f);
                unchecked {signature=(signature^candidate.hash)*1099511628211UL;signature=(signature^(uint)variant)*1099511628211UL;}
                TreeCount++;
                if((System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000.0/System.Diagnostics.Stopwatch.Frequency>=2){yield return null;started=System.Diagnostics.Stopwatch.GetTimestamp();}
            }
            PlacementSignature=signature.ToString("x16");
            var renderer=holder.AddComponent<SpatialForestRenderer>();renderer.viewCamera=land.View.GetComponentInChildren<Camera>();
            renderer.shadowSources=library.shadowSources;renderer.shadowReplacements=library.shadowReplacements;
            renderer.simplifiedDistantShadows=true;renderer.minimumInstancedViewSize=200;renderer.instanced=true;
            renderer.Capture();Ready=true;RefreshVisibility();
        }
        public void RefreshVisibility(){if(holder&&land)holder.SetActive(land.TreesEnabled&&land.View.VisualSeason==RegionSeason.Summer);}
        public TreeInstance[] NativePresentation(TreeInstance[] source)
        {
            if(!land||land.View.VisualSeason!=RegionSeason.Summer)return source;
            var result=new List<TreeInstance>(source.Length);foreach(var tree in source)if(!ReplacedPrototype(tree.prototypeIndex))result.Add(tree);return result.ToArray();
        }
    }
}
