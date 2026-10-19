using System;
using UnityEngine;
namespace CityForge.World
{
    /// <summary>Stable experimental evergreen slots appended after the approved seasonal library.</summary>
    public static class MountainEvergreenMix
    {
        public const string ResourcePath="Trees/MountainEvergreen/EvergreenPilot";
        public const int SpeciesCount=4;
        public static readonly string[] SpeciesNames={"EvergreenPilot","BalsamFir","FraserFir","CilicianFir"};
        public static GameObject[] LoadPrefabs(){
            var result=new GameObject[SpeciesCount];
            for(int i=0;i<result.Length;i++){
                result[i]=Resources.Load<GameObject>("Trees/MountainEvergreen/"+SpeciesNames[i]);
                if(!result[i])throw new InvalidOperationException("Missing mountain tree: "+SpeciesNames[i]);
            }
            return result;
        }
        public static TreePrototype[] Prototypes(GameObject[] seasonal,GameObject[] evergreens)
        {
            var result=new TreePrototype[seasonal.Length+(evergreens?.Length??0)];
            for(int i=0;i<seasonal.Length;i++)result[i]=new TreePrototype{prefab=seasonal[i]};
            if(evergreens!=null)for(int i=0;i<evergreens.Length;i++)result[seasonal.Length+i]=new TreePrototype{prefab=evergreens[i]};return result;
        }
        public static int Apply(TerrainData terrain,int seasonalCount,int seed)
        {
            var trees=terrain.treeInstances;int count=0;
            for(int i=0;i<trees.Length;i++){
                var tree=trees[i];
                if(tree.prototypeIndex>=seasonalCount&&tree.prototypeIndex<seasonalCount+SpeciesCount){count++;continue;}
                // Replace only mature poplars; retain approved understory and every placement.
                if(tree.prototypeIndex<3||tree.prototypeIndex>=11)continue;
                float cluster=Mathf.PerlinNoise(tree.position.x*8+seed*.001f,tree.position.z*8+31);
                uint hash=Hash(tree.position,seed);float chance=(hash&65535)/65535f;
                float originalThreshold=Mathf.Lerp(.04f,.28f,Mathf.InverseLerp(.35f,.7f,cluster));
                if(chance>originalThreshold+.12f)continue;
                // Retain the original pilot selection; new firs occupy a separate stable band.
                tree.prototypeIndex=seasonalCount+(chance<=originalThreshold?0:1+(int)((hash>>8)%3));
                tree.widthScale=tree.heightScale=Mathf.Lerp(.78f,1.15f,((hash>>16)&65535)/65535f);
                trees[i]=tree;count++;
            }
            terrain.SetTreeInstances(trees,false);return count;
        }
        static uint Hash(Vector3 p,int seed)
        {
            unchecked{uint h=(uint)(Mathf.RoundToInt(p.x*100000)*73856093^Mathf.RoundToInt(p.z*100000)*19349663^seed);h^=h>>16;h*=0x7feb352d;h^=h>>15;return h;}
        }
    }
}
