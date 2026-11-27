using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityForge.World
{
    public enum SurfaceWeatheringDebug { None, Micro, Medium, Macro, GutterMask, SedimentAccumulation, CombinedWeathering }
    [CreateAssetMenu(menuName = "CityForge/Surface Weathering Profile")]
    public sealed class SurfaceWeatheringProfile : ScriptableObject
    {
        [Range(0,1)] public float intensity = .45f;
        [Tooltip("Physical gutter width in metres; the shared boundary field resolves up to 2m.")]
        [Range(.05f,1.5f)] public float gutterWidth = .45f;
        [Range(0,1)] public float gutterDarkness = .20f;
        [Range(0,1)] public float grimeStrength = .12f;
        [Min(.1f)] public float grimeScale = 8f;
        [Range(0,1)] public float sedimentStrength = .15f;
        public Color sedimentTint = Color.white;
        [Min(.1f)] public float sedimentScale = 1.7f;
        [Range(0,1)] public float organicDebrisStrength = .25f;
        [Min(.1f)] public float debrisScale = 3f;
        [Range(0,.15f)] public float syntheticCamberStrength = 0f;
        [Range(0,1)] public float moistureVariation = .08f;
        [Range(0,1)] public float contactShading = .12f;
        [Range(0,1)] public float sidewalkEdgeWeathering = .18f;
        [Header("Irregular edge accumulation")]
        [Range(0,1)] public float edgeBreakup = .8f;
        [Min(.1f)] public float edgePatchScale = 3f;
        [Range(0,1)] public float debrisScatter = .6f;
        [ColorUsage(false,true)] public Color debrisTint = Color.white;
        [Header("Stable variation along long streets")]
        public bool removeRepetition = true;
        [Min(4)] public float variationScale = 24f;
        [Tooltip("Stable pattern seed; changing it redistributes weathering without editing roads.")]
        public int variationSeed = 137;
        [Header("Multi-scale weathering (physical feature sizes in metres)")]
        public bool multiScaleWeathering = true;
        [Range(0,.25f)] public float microIntensity = .055f;
        [Range(.05f,.30f)] public float microScale = .17f;
        public Vector2 microOffset = new(13.7f,41.3f);
        [Range(0,360)] public float microRotation = 17f;
        [Range(0,.4f)] public float mediumIntensity = .12f;
        [Range(1,4)] public float mediumScale = 2.37f;
        public Vector2 mediumOffset = new(83.1f,7.9f);
        [Range(0,360)] public float mediumRotation = 67f;
        [Range(0,1)] public float macroIntensity = .65f;
        [Range(8,25)] public float macroScale = 18.7f;
        public Vector2 macroOffset = new(-21.5f,103.9f);
        [Range(0,360)] public float macroRotation = 113f;
        [Range(0,.4f)] public float macroBrightness = .24f;
        [Range(0,1)] public float macroTintStrength = .25f;
        [ColorUsage(false,true)] public Color macroTint = new(1.06f,1.01f,.95f,1);
        [Range(0,.3f)] public float roughnessVariation = .10f;
        [Header("Independent gutter fields (sampled at each physical edge)")]
        [Range(0,1)] public float gutterWidthVariation = .85f;
        [Range(0,1)] public float gutterDarknessVariation = .8f;
        [Range(0,1)] public float gutterSedimentVariation = .9f;
        [Range(0,1)] public float gutterDebrisVariation = .95f;
        [Range(1,8)] public float gutterWidthScale = 3.1f;
        [Range(1,8)] public float gutterDarknessScale = 2.13f;
        [Range(1,8)] public float gutterSedimentScale = 1.73f;
        [Range(1,8)] public float gutterDebrisScale = 3.77f;
        [Header("Unlit diagnostic views (None for normal gameplay)")]
        public SurfaceWeatheringDebug debugView;
        [Header("Bluestone face shuffle (RGB stone only)")]
        public bool paverFaceVariation = true;
        [Range(.4f,1.5f)] public float paverWidth = .85f;
        [Range(.25f,.8f)] public float paverHeight = .5f;
        [Range(.005f,.04f)] public float paverJointWidth = .018f;
        [Range(0,.4f)] public float paverToneVariation = .20f;
        [Range(0,.3f)] public float paverTintVariation = .09f;
        public Texture2D grimeMask, gutterSediment, organicDebris;
        internal static int Revision;
        void OnValidate() { unchecked { Revision++; } }
        public void Apply(Material material, bool sidewalk)
        {
            if (!material || !material.HasProperty("_WeatherIntensity")) return;
            if (intensity > 0) material.EnableKeyword("_SURFACE_WEATHERING");
            else material.DisableKeyword("_SURFACE_WEATHERING");
            material.SetFloat("_WeatherIntensity", Mathf.Clamp01(intensity));
            material.SetVector("_WeatherGutter", new Vector4(Mathf.Clamp(gutterWidth,.05f,1.5f), gutterDarkness, contactShading, sidewalkEdgeWeathering));
            material.SetVector("_WeatherGrime", new Vector4(grimeStrength, 1 / Mathf.Max(.1f,grimeScale), moistureVariation, sidewalk ? 1 : 0));
            material.SetVector("_WeatherAccumulation", new Vector4(sedimentStrength, 1 / Mathf.Max(.1f,sedimentScale), organicDebrisStrength, 1 / Mathf.Max(.1f,debrisScale)));
            material.SetFloat("_WeatherCamber", sidewalk ? 0 : syntheticCamberStrength);
            material.SetColor("_WeatherSedimentTint", sedimentTint);
            material.SetVector("_WeatherPattern", new Vector4(edgeBreakup, 1 / Mathf.Max(.1f,edgePatchScale), debrisScatter, sidewalk ? CityForge.Lots.LotPathSurface.ResidentialSidewalkWidth : 0));
            material.SetColor("_WeatherDebrisTint", debrisTint);
            material.SetVector("_WeatherVariation",new Vector4(removeRepetition?1:0,1/Mathf.Max(4,variationScale),variationSeed,0));
            Vector4 Layer(float strength,float scale,float rotation){float angle=rotation*Mathf.Deg2Rad;return new Vector4(strength,1/Mathf.Max(.01f,scale),Mathf.Sin(angle),Mathf.Cos(angle));}
            material.SetVector("_WeatherMicro",Layer(microIntensity,microScale,microRotation));
            material.SetVector("_WeatherMedium",Layer(mediumIntensity,mediumScale,mediumRotation));
            material.SetVector("_WeatherMacro",Layer(macroIntensity,macroScale,macroRotation));
            material.SetVector("_WeatherLayerOffsets",new Vector4(microOffset.x,microOffset.y,mediumOffset.x,mediumOffset.y));
            material.SetVector("_WeatherMacroParams",new Vector4(macroOffset.x,macroOffset.y,macroBrightness,macroTintStrength));
            material.SetColor("_WeatherMacroTint",macroTint);
            material.SetVector("_WeatherMaterialParams",new Vector4(roughnessVariation,(int)debugView,multiScaleWeathering?1:0,sidewalk?811:0));
            material.SetVector("_WeatherGutterVariations",new Vector4(gutterWidthVariation,gutterDarknessVariation,gutterSedimentVariation,gutterDebrisVariation));
            material.SetVector("_WeatherGutterFrequencies",new Vector4(1/Mathf.Max(1,gutterWidthScale),1/Mathf.Max(1,gutterDarknessScale),1/Mathf.Max(1,gutterSedimentScale),1/Mathf.Max(1,gutterDebrisScale)));
            bool rgbStone=sidewalk&&paverFaceVariation&&!material.IsKeywordEnabled("_NORMALMAP")&&!material.IsKeywordEnabled("_METALLICSPECGLOSSMAP")&&!material.IsKeywordEnabled("_PARALLAXMAP")&&!material.IsKeywordEnabled("_EMISSION")&&!material.IsKeywordEnabled("_OCCLUSIONMAP");
            material.SetVector("_WeatherPavers",new Vector4(rgbStone?1:0,1/Mathf.Max(.1f,paverWidth),1/Mathf.Max(.1f,paverHeight),paverJointWidth));
            material.SetVector("_WeatherPaverTone",new Vector4(paverToneVariation,paverTintVariation,0,0));
            material.SetTexture("_WeatherGrimeMask", grimeMask ? grimeMask : Texture2D.grayTexture);
            material.SetTexture("_WeatherSediment", gutterSediment ? gutterSediment : Texture2D.whiteTexture);
            material.SetTexture("_WeatherDebris", organicDebris ? organicDebris : Texture2D.blackTexture);
            // Missing color assets must not paint white sediment or opaque black debris.
            if (!gutterSediment || !organicDebris)
                material.SetVector("_WeatherAccumulation", new Vector4(gutterSediment ? sedimentStrength : 0, 1 / Mathf.Max(.1f,sedimentScale), organicDebris ? organicDebrisStrength : 0, 1 / Mathf.Max(.1f,debrisScale)));
        }
    }

}
