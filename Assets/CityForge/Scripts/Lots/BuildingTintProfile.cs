using System;
using UnityEngine;
namespace CityForge.Lots
{
    [Serializable] public sealed class BuildingTintBinding
    {
        public string region;
        public Material material;
        public Texture2D neutralAlbedo;
    }
    // Explicit reviewed regions only. Original keeps the exact source material inputs.
    public sealed class BuildingTintProfile : MonoBehaviour
    {
        public BuildingTintBinding[] bindings = Array.Empty<BuildingTintBinding>();
    }
}
