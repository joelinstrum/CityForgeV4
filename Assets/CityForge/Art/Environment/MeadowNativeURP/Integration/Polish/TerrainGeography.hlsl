#ifndef CITYFORGE_TERRAIN_GEOGRAPHY
#define CITYFORGE_TERRAIN_GEOGRAPHY

TEXTURE2D(_GeographyWeights0);
SAMPLER(sampler_GeographyWeights0);
TEXTURE2D(_GeographyWeights1);
float4 _GeographyGrid;
float _GeographyMode;

TEXTURE2D(_GeographyAlbedo0); TEXTURE2D(_GeographyNormal0); float4 _GeographySurface0; half3 _GeographyAlbedoBias0; half3 _GeographyAlbedoMean0;
TEXTURE2D(_GeographyAlbedo1); TEXTURE2D(_GeographyNormal1); float4 _GeographySurface1; half3 _GeographyAlbedoBias1; half3 _GeographyAlbedoMean1;
TEXTURE2D(_GeographyAlbedo2); TEXTURE2D(_GeographyNormal2); float4 _GeographySurface2; half3 _GeographyAlbedoBias2; half3 _GeographyAlbedoMean2;
TEXTURE2D(_GeographyAlbedo3); TEXTURE2D(_GeographyNormal3); float4 _GeographySurface3; half3 _GeographyAlbedoBias3; half3 _GeographyAlbedoMean3;
TEXTURE2D(_GeographyAlbedo4); TEXTURE2D(_GeographyNormal4); float4 _GeographySurface4; half3 _GeographyAlbedoBias4; half3 _GeographyAlbedoMean4;
TEXTURE2D(_GeographyAlbedo5); TEXTURE2D(_GeographyNormal5); float4 _GeographySurface5; half3 _GeographyAlbedoBias5; half3 _GeographyAlbedoMean5;

// Share the terrain's repeat sampler. A zero texture weight skips its sampling.
#define CITYFORGE_GEOGRAPHY_SURFACE(I,W) \
    if (W > .018h) { \
        float4 surface = _GeographySurface##I; \
        if (surface.x > 0) { \
            float2 texUV = world * surface.x; \
            half3 color = SAMPLE_TEXTURE2D_GRAD(_GeographyAlbedo##I,sampler_Splat0,texUV,worldDx*(surface.x*albedoMipScale),worldDy*(surface.x*albedoMipScale)).rgb; \
            half3 mean = _GeographyAlbedoMean##I; \
            if (mean.g > 0) color = lerp(mean, color, textureDetail); \
            blendColor += W * max(0, color * surface.w + _GeographyAlbedoBias##I); \
            half3 layerNormal = half3(0,0,1); \
            if (surface.z > 0 && normalDetail > 0) { \
                half3 encodedNormal = SAMPLE_TEXTURE2D_GRAD(_GeographyNormal##I,sampler_Splat0,texUV,worldDx*surface.x,worldDy*surface.x).xyz * 2 - 1; \
                layerNormal = normalize(half3(encodedNormal.xy * (surface.z * normalDetail), max(.01,encodedNormal.z))); \
            } \
            blendNormal += W * layerNormal; \
            blendSmoothness += W * surface.y; \
            blended += W; \
        } \
    }

void ApplyTerrainGeography(float2 world, half coverage, inout half3 albedo, inout half3 normalTS, inout half smoothness)
{
    float2 worldDx = ddx(world), worldDy = ddy(world);
    float footprint = max(length(worldDx),length(worldDy));
    half normalDetail = 1 - smoothstep(.09,.23,footprint);
    half textureDetail = lerp(1,.26,smoothstep(.06,.24,footprint));
    // Preserve more detail at the closest zoom without adding an overlay sample.
    float albedoMipScale = lerp(.80,1,smoothstep(.045,.12,footprint));
    if (_GeographyMode < 1 || coverage <= .001) return;
    float2 uv = (world - _GeographyGrid.xy) * _GeographyGrid.z + _GeographyGrid.w;
    half4 a = SAMPLE_TEXTURE2D(_GeographyWeights0,sampler_GeographyWeights0,uv);
    half2 b = SAMPLE_TEXTURE2D(_GeographyWeights1,sampler_GeographyWeights0,uv).rg;
    half sum = dot(a,half4(1,1,1,1)) + b.x + b.y;
    if (sum <= .001) return;
    a /= sum;
    b /= sum;

    if (_GeographyMode > 1.5)
    {
        half3 mapColor = a.x*half3(.19,.72,.13) + a.y*half3(.88,.58,.065) +
            a.z*half3(.39,.10,.63) + a.w*half3(.025,.64,.84) +
            b.x*half3(.80,.22,.08) + b.y*half3(.54,.55,.56);
        albedo = lerp(albedo,mapColor,coverage);
        return;
    }
    half3 blendColor = 0, blendNormal = 0;
    half blendSmoothness = 0, blended = 0;
    CITYFORGE_GEOGRAPHY_SURFACE(0,a.x)
    CITYFORGE_GEOGRAPHY_SURFACE(1,a.y)
    CITYFORGE_GEOGRAPHY_SURFACE(2,a.z)
    CITYFORGE_GEOGRAPHY_SURFACE(3,a.w)
    CITYFORGE_GEOGRAPHY_SURFACE(4,b.x)
    CITYFORGE_GEOGRAPHY_SURFACE(5,b.y)
    half remaining = saturate(1 - blended * coverage);
    albedo = albedo * remaining + blendColor * coverage;
    normalTS = normalize(normalTS * remaining + blendNormal * coverage);
    smoothness = smoothness * remaining + blendSmoothness * coverage;
}

#endif
