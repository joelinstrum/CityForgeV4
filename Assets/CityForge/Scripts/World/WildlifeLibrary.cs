using System;
using UnityEngine;
namespace CityForge.World
{
    public enum WildlifeHabitat { Woodland, WoodlandEdge, Freshwater, River, FreshwaterBank, WoodlandBird, FlyingBird, RiverBankShallows }
    [Serializable] public sealed class WildlifeSpecies
    {
        public string id;public GameObject prefab;public WildlifeHabitat habitat;
        public int weight=1;public float speed=.6f;
    }
    /// <summary>Local licensed prefabs; the public project ships only the integration code.</summary>
    public sealed class WildlifeLibrary:ScriptableObject
    {
        public WildlifeSpecies[] species=Array.Empty<WildlifeSpecies>();
    }
}
