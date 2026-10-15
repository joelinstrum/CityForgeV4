using UnityEngine;
namespace CityForge.World
{
    public sealed class RegionTerrainKit : ScriptableObject
    {
        public Material terrainMaterial;
        public Texture2DArray albedos;
        public Texture2DArray normals;
        public Texture2D meadowDetail;
        public Texture2D meadowDetailNormal;
        // Metalness (R), ambient occlusion (G), optional height (B), smoothness (A).
        public Texture2DArray masks;
        public void Bind(Texture2D field)
        {
            Shader.SetGlobalTexture("_RegionAlbedos", albedos);
            Shader.SetGlobalTexture("_RegionNormals", normals);
            Shader.SetGlobalTexture("_RegionMasks", masks);
            Shader.SetGlobalTexture("_RegionField", field);
            Shader.SetGlobalTexture("_MeadowDetail", meadowDetail);
            Shader.SetGlobalTexture("_MeadowDetailNormal", meadowDetailNormal);
        }
    }
}
