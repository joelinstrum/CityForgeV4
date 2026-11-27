using System;
using UnityEngine;
namespace CityForge.Lots
{
    [Serializable] public sealed class BuildingWindowFrameBinding
    {
        public Material original;
        // Same input properties as original, with an explicit frame-only mask shader.
        public Material customizable;
    }
    [DisallowMultipleComponent] public sealed class BuildingWindowFrameProfile : MonoBehaviour
    {
        public BuildingWindowFrameBinding[] bindings = Array.Empty<BuildingWindowFrameBinding>();
    }
}
