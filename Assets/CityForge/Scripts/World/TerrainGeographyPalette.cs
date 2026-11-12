using System;
using UnityEngine;

namespace CityForge.World
{
    [CreateAssetMenu(menuName = "CityForge/Terrain Geography Palette")]
    public sealed class TerrainGeographyPalette : ScriptableObject
    {
        [Serializable]
        public sealed class Surface
        {
            public Texture2D albedo;
            [Tooltip("Linear RGB tangent-space normal texture, using the existing RegionPBR format.")] public Texture2D normal;
            [Min(1f)] public float tileMeters = 9f;
            [Range(0f,1f)] public float smoothness = .04f;
            [Range(0f,1f)] public float normalStrength = .3f;
            [Tooltip("Optional linear mean of the albedo. It softens texture contrast when viewed from far away.")]
            public Vector3 distantAlbedoMean;
            [Tooltip("Linear RGB correction applied after albedo scaling.")] public Vector3 albedoBias;
            [Range(.05f,2f)] public float albedoScale = 1f;
        }

        [Tooltip("Meadow, dry grass, woodland, moist grass, disturbed soil, rocky ground. An empty meadow slot uses the existing terrain grass.")]
        public Surface[] surfaces = new Surface[6];
    }
}
