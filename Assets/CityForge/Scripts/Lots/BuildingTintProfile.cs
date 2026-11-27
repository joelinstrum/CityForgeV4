using System;
using UnityEngine;
namespace CityForge.Lots
{
    [Serializable] public sealed class BuildingWallFinish
    {
        public string id;
        public Texture2D albedo, calmAlbedo;
        public Color meanColor = Color.white;
        public bool customSurface;
        public float coursesPerTile;
        public Texture2D normal, packedSurface;
        public Vector4 uvTransform = new Vector4(1,1,0,0);
    }
    [Serializable] public sealed class BuildingTintBinding
    {
        public string region;
        public Material material;
        public Texture2D neutralAlbedo;
        public BuildingWallFinish[] finishes = Array.Empty<BuildingWallFinish>();
    }
    // Explicit reviewed regions only. Original keeps the exact source material inputs.
    public sealed class BuildingTintProfile : MonoBehaviour
    {
        public string wallLabel = "Walls";
        public BuildingTintBinding[] bindings = Array.Empty<BuildingTintBinding>();
    }
}
