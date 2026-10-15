using UnityEngine;

namespace CityForge.World
{
    [CreateAssetMenu(menuName = "CityForge/Landscape Profile")]
    public sealed class LandscapeProfile : ScriptableObject
    {
        [Min(1)] public int generationVersion = 1;
        [Header("Ground — distances in rendered world meters")]
        public Texture2D grassAlbedo, grassNormal, soilAlbedo, litterAlbedo;
        public Color grassTint = new Color(.68f,.87f,.58f,1);
        public Color calmGrass = new Color(.135f,.205f,.072f,1);
        public Color oliveGrass = new Color(.158f,.197f,.087f,1);
        public Color soilTint = new Color(.82f,.88f,.75f,1);
        public Color litterTint = new Color(.58f,.52f,.42f,1);
        [Min(.1f)] public float grassScale = 4, soilScale = 4, litterScale = 4;
        [Range(0,5)] public float grassMipBias = 2.5f;
        [Range(0,1)] public float closeSmoothing = .48f, distantSmoothing = .78f;
        [Min(.1f)] public float smoothingStart = 20, smoothingRange = 90;
        [Range(.1f,4)] public float groundBrightness = 2.2f;
        [Range(0,1)] public float normalStrength = .22f;
        [Min(1)] public float broadScale = 96.66f;
        [Range(0,.2f)] public float broadVariation = .075f, secondaryVariation = .012f;
        [Range(0,1)] public float litterStrength = .85f, shorelineSoilStrength = .65f;
        [Min(.1f)] public float shorelineWidth = 5;
        [Header("Pass 2 configuration — reserved; not consumed by current streaming")]
        public GameObject[] meadowPlants, shorelinePlants, understoryPlants;
        [Range(0,1)] public float baseVegetationDensity = .65f, closestZoomDensity = .7f;
        [Min(.1f)] public float roadFeatherWidth = 2, shorelineClusterSpacing = 8;

        public void Bind(int seed, float mapScale)
        {
            Shader.SetGlobalTexture("_LandscapeGrass", grassAlbedo);
            Shader.SetGlobalTexture("_LandscapeNormal", grassNormal);
            Shader.SetGlobalTexture("_LandscapeSoil", soilAlbedo);
            Shader.SetGlobalTexture("_LandscapeLitter", litterAlbedo);
            // Color fields are authored as linear surface reflectance, matching the showcase HLSL.
            Shader.SetGlobalFloat("_LandscapeBrightness", groundBrightness);
            Shader.SetGlobalVector("_LandscapeGrassTint", (Vector4)grassTint);
            Shader.SetGlobalVector("_LandscapeCalm", (Vector4)calmGrass);
            Shader.SetGlobalVector("_LandscapeOlive", (Vector4)oliveGrass);
            Shader.SetGlobalVector("_LandscapeSoilTint", (Vector4)soilTint);
            Shader.SetGlobalVector("_LandscapeLitterTint", (Vector4)litterTint);
            Shader.SetGlobalVector("_LandscapeScales", new Vector4(Mathf.Max(.1f,grassScale),Mathf.Max(.1f,soilScale),Mathf.Max(.1f,litterScale),grassMipBias));
            Shader.SetGlobalVector("_LandscapeSmoothing", new Vector4(closeSmoothing,distantSmoothing,smoothingStart,Mathf.Max(.1f,smoothingRange)));
            Shader.SetGlobalVector("_LandscapeVariation", new Vector4(Mathf.Max(1,broadScale),broadVariation,secondaryVariation,normalStrength));
            Shader.SetGlobalVector("_LandscapeCoverage", new Vector4(litterStrength,shorelineSoilStrength,Mathf.Max(.1f,shorelineWidth),mapScale));
            var random = new System.Random(seed);
            Shader.SetGlobalVector("_LandscapeSeed", new Vector4((float)random.NextDouble()*1000,(float)random.NextDouble()*1000,0,0));
        }
    }
}
