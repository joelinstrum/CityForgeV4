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
        public float[] weights={.15384616f,.15384616f,0,.15384616f,.15384616f,0,.28846154f,.09615385f};
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
        // Keep stable variant slots; Chestnut03 and Locust were removed in the player's review.
        public static bool VariantAllowed(int variant) => variant is 0 or 1 or 3 or 4 or 6 or 7;
        public int ResolveVariant(int requested, uint hash) => VariantAllowed(requested) ? requested : Pick(hash);
        static readonly int[] KeptChestnuts = { 0, 1, 3, 4 };
        public int PickChestnut(uint hash) => KeptChestnuts[hash % 4];
        public int Pick(uint hash)
        {
            float total = 0; for (int i = 0; i < weights.Length; i++) if (VariantAllowed(i)) total += weights[i];
            if (total <= 0) throw new InvalidOperationException("The reviewed forest has no available trees.");
            float value = (hash & 0xffffff) / 16777216f * total;
            int last = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                if (!VariantAllowed(i) || weights[i] <= 0) continue;
                last = i; value -= weights[i]; if (value < 0) return i;
            }
            return last;
        }
    }
}
