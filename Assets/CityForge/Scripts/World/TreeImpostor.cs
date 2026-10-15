using UnityEngine;
namespace CityForge.World
{
    /// <summary>Eight baked azimuth views of this tree at the region camera's fixed pitch.</summary>
    public sealed class TreeImpostor : MonoBehaviour
    {
        public Material material;
        public Vector3 center;
        public float span;
    }
}
