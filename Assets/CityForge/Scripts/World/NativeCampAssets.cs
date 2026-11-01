using UnityEngine;
namespace CityForge.World
{
    public sealed class NativeCampAssets:ScriptableObject
    {
        public GameObject teepee,totem;
        public Material[] teepeeMaterials;
        public NpcDefinition chief,warrior,resident;
    }
}
