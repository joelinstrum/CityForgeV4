using System;
using System.Collections.Generic;
using UnityEngine;
namespace CityForge.World
{
    /// <summary>Append-only species slots. Never reorder: saved regions store these indices.</summary>
    public static class ForestTreeMix
    {
        public const int BaselineCount=15, EvergreenStart=15, BroadleafStart=19, SpeciesCount=8, PrototypeCount=27;
        public static readonly string[] SpeciesNames={"SilverMapleA","SilverMapleB","CanyonLiveOakA","CanyonLiveOakB","SouthernLiveOak","AngelOak","OakPilotSummer","OakPilotVariantBSummer"};
        static readonly Dictionary<RegionSeason,GameObject[]> palettes=new();
        public const int AllSpeciesMask=(1<<SpeciesCount)-1;
        const string SpeciesPreference="CityForge.Forest.SpeciesMask";
        static int? enabledMask;
        // Lazy: save validation also calls this class on a background worker.
        public static int EnabledSpeciesMask=>enabledMask??=(PlayerPrefs.GetInt(SpeciesPreference,0)&AllSpeciesMask);
        public static void SetEnabledSpecies(int mask,bool remember=false){enabledMask=mask&AllSpeciesMask;if(remember){PlayerPrefs.SetInt(SpeciesPreference,enabledMask.Value);PlayerPrefs.Save();}}
        public static bool SpeciesEnabled(int species)=>(EnabledSpeciesMask&(1<<species))!=0;
        static GameObject LoadPrefab(int species,RegionSeason season)
        {
            var variant=species>=2&&species<=5?RegionSeason.Summer:season;
            if(!palettes.TryGetValue(variant,out var result))palettes[variant]=result=new GameObject[SpeciesCount];
            if(!result[species])result[species]=Resources.Load<GameObject>("Trees/ForestBroadleaf/"+SpeciesNames[species]+"/"+variant);
            if(!result[species])throw new InvalidOperationException("Missing forest tree: "+SpeciesNames[species]+" / "+variant);
            return result[species];
        }
        public static bool IsValidPrototype(int index,bool mountain)=>index>=0&&(index<BaselineCount||(mountain&&index<BroadleafStart)||(index>=BroadleafStart&&index<PrototypeCount));
        public static GameObject[] LoadPrefabs(RegionSeason season)
        {
            var result=new GameObject[SpeciesCount];for(int i=0;i<result.Length;i++)result[i]=LoadPrefab(i,season);return result;
        }
        public static TreePrototype[] Prototypes(GameObject[] baseline,GameObject[] evergreens,RegionSeason season)
        {
            if(baseline.Length!=BaselineCount||evergreens.Length!=MountainEvergreenMix.SpeciesCount)throw new InvalidOperationException("Forest prototype layout mismatch");
            var result=new TreePrototype[PrototypeCount];
            for(int i=0;i<baseline.Length;i++)result[i]=new TreePrototype{prefab=baseline[i]};
            for(int i=0;i<evergreens.Length;i++)result[EvergreenStart+i]=new TreePrototype{prefab=evergreens[i]};
            for(int i=0;i<SpeciesCount;i++)result[BroadleafStart+i]=new TreePrototype{prefab=SpeciesEnabled(i)?LoadPrefab(i,season):baseline[3+i]};return result;
        }
        public static TreeInstance[] Presentation(TreeInstance[] canonical,MeadowRuntimeBaseline baseline)
        {
            TreeInstance[] result=null;int mask=EnabledSpeciesMask;
            for(int i=0;i<canonical.Length;i++){
                var tree=canonical[i];int species=tree.prototypeIndex-BroadleafStart;
                if(species<0||species>=SpeciesCount||(mask&(1<<species))!=0)continue;
                if(result==null)result=(TreeInstance[])canonical.Clone();
                int native=3+species;float size=Mathf.InverseLerp(.8f,1.12f,tree.heightScale);
                tree.prototypeIndex=native;tree.widthScale=baseline.treeWidths[native]*Mathf.Lerp(.85f,1.15f,size);tree.heightScale=baseline.treeHeights[native]*Mathf.Lerp(.85f,1.15f,size);result[i]=tree;
            }
            return result??canonical;
        }
        public static int Apply(TerrainData terrain,int seed)
        {
            var trees=terrain.treeInstances;int count=0;
            for(int i=0;i<trees.Length;i++){
                var tree=trees[i];
                if(tree.prototypeIndex>=BroadleafStart&&tree.prototypeIndex<PrototypeCount){count++;continue;}
                if(tree.prototypeIndex<3||tree.prototypeIndex>=11)continue;
                uint hash=Hash(tree.position,seed);if((hash&65535)/65535f>=.24f)continue;
                tree.prototypeIndex=BroadleafStart+(int)((hash>>16)%SpeciesCount);
                tree.widthScale=tree.heightScale=Mathf.Lerp(.8f,1.12f,((hash>>8)&65535)/65535f);
                trees[i]=tree;count++;
            }
            terrain.SetTreeInstances(trees,false);return count;
        }
        static uint Hash(Vector3 p,int seed){unchecked{uint h=(uint)(Mathf.RoundToInt(p.x*100000)*73856093^Mathf.RoundToInt(p.z*100000)*19349663^seed^0x3ac76915);h^=h>>16;h*=0x7feb352d;h^=h>>15;return h;}}
    }
}
