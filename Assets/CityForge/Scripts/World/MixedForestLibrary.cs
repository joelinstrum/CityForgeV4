using System;
using UnityEngine;
namespace CityForge.World
{
    /// <summary>Private licensed art references. The public repository contains its builder, not the artwork.</summary>
    [CreateAssetMenu(menuName="CityForge/Approved Mixed Forest Library")]
    public sealed class MixedForestLibrary : ScriptableObject
    {
        public const string ResourcePath="Trees/ApprovedMixedForest/V1/Library";
        public GameObject[] prefabs;
        public float[] weights={.112f,.112f,.112f,.112f,.112f,.16f,.21f,.07f};
        public Mesh[] shadowSources,shadowReplacements;
        public void Validate()
        {
            if(prefabs==null||prefabs.Length!=8||weights==null||weights.Length!=8)
                throw new InvalidOperationException("Build the private approved mixed forest library using CityForge/Forest/Build approved mixed forest library.");
            float total=0;for(int i=0;i<8;i++){if(!prefabs[i]||!prefabs[i].GetComponent<LODGroup>()||weights[i]<0)throw new InvalidOperationException("Invalid mixed forest tree entry "+i);total+=weights[i];}
            if(Mathf.Abs(total-1)>.001f||shadowSources==null||shadowReplacements==null||shadowSources.Length!=shadowReplacements.Length||shadowSources.Length==0)
                throw new InvalidOperationException("Mixed forest palette or shadow library is incomplete.");
        }
        public static MixedForestLibrary Load()
        {
            var library=Resources.Load<MixedForestLibrary>(ResourcePath);
            if(!library)throw new InvalidOperationException("Private approved mixed forest art is missing. Build or restore the local mixed forest asset bundle.");
            library.Validate();return library;
        }
        public int Pick(uint hash)
        {
            float value=(hash&0xffffff)/16777216f;
            for(int i=0;i<weights.Length;i++){value-=weights[i];if(value<0)return i;}
            return weights.Length-1;
        }
    }
}
