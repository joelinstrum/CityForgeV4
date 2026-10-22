#ifndef CITYFORGE_RIVER_GLINT_RESPONSE
#define CITYFORGE_RIVER_GLINT_RESPONSE
// Used only by the local generated Crest study shader. Original SDK files are untouched.
float _CityForgeGlintStart;
float _CityForgeGlintEnd;
float _CityForgeGlintGain;
float _CityForgeRippleEvolution;
float _CityForgeRippleEvolutionSpeed;

// Traveling, interfering surface slopes change reflection alignment continuously.
// This evolves normals, rather than fading painted highlights or blinking pixels.
float3 CityForgeEvolvingNormal(float3 normalWS, float2 positionXZ)
{
    float t = _Time.y * _CityForgeRippleEvolutionSpeed;
    float2 d0 = normalize(float2(.81, .59));
    float2 d1 = normalize(float2(-.38, .92));
    float2 d2 = normalize(float2(.96, -.28));
    float2 slope = d0 * cos(dot(positionXZ, d0) * 3.69 - t * 3.2)
        + .65 * d1 * cos(dot(positionXZ, d1) * 2.03 + t * 2.3 + 1.7)
        + .4 * d2 * cos(dot(positionXZ, d2) * 1.34 - t * 1.65 + 3.1);
    return normalize(normalWS + float3(slope.x, 0, slope.y) * _CityForgeRippleEvolution);
}

half4 CityForgeRiverLighting(InputData inputData, SurfaceData surface)
{
    inputData.normalWS = CityForgeEvolvingNormal(inputData.normalWS, inputData.positionWS.xz);
    half4 full = UniversalFragmentPBR(inputData, surface);
#if defined(_SPECULAR_SETUP)
    SurfaceData withoutSpecular = surface;
    withoutSpecular.specular = 0;
    half4 base = UniversalFragmentPBR(inputData, withoutSpecular);
    float3 highlight = max(0, (float3)full.rgb - base.rgb);
    float brightness = max(highlight.r, max(highlight.g, highlight.b));
    // Preserve the approved31-degree response. Ease down coverage and intensity
    // above it, using camera orientation so the orthographic switch cannot reset it.
    float cameraPitch = asin(saturate(abs(UNITY_MATRIX_V[2].y))) * 57.2957795;
    float highTilt = smoothstep(31.0, 35.26439, cameraPitch);
    float gateStart = _CityForgeGlintStart + .20 * highTilt;
    float gateEnd = _CityForgeGlintEnd + .35 * highTilt;
    float retained = smoothstep(gateStart, max(gateStart + .001, gateEnd), brightness);
    float angleGain = lerp(1.0, .20, highTilt);
    full.rgb = base.rgb + highlight * retained * _CityForgeGlintGain * angleGain;
#endif
    return full;
}
#define UniversalFragmentPBR CityForgeRiverLighting
#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/PBRForwardPass.hlsl"
#undef UniversalFragmentPBR
#endif
