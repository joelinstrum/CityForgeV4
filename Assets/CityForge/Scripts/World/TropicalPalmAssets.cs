using System;
using UnityEngine;
namespace CityForge.World
{
    // Private meshes and textures are prepared locally, never published with source.
    public sealed class TropicalPalmAssets:ScriptableObject
    {
        [Serializable] public sealed class Variant { public Mesh[] lods; }
        public Variant[] variants;
        public Material material;
    }
}
