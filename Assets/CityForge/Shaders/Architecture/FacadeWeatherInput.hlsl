// URP 17.1 LitInput derivative, Unity Companion License.
#ifndef UNIVERSAL_LIT_INPUT_INCLUDED
#define UNIVERSAL_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"

#if defined(_DETAIL_MULX2) || defined(_DETAIL_SCALED)
#define _DETAIL
#endif

// NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
CBUFFER_START(UnityPerMaterial)
float4 _CFFacadeA, _CFFacadeB, _CFMaterialResponse; float _CFFacadeEnabled;
float4 _CFBounds, _CFWeatherParams; float4x4 _CFWorldToBuilding; float _CFWeatherIntensity, _CFWeatherUseBuilding;
float4 _BaseMap_ST;
float4 _BaseMap_TexelSize;
float4 _DetailAlbedoMap_ST;
half4 _BaseColor;
half4 _SpecColor;
half4 _EmissionColor;
half _Cutoff;
half _Smoothness;
half _Metallic;
half _BumpScale;
half _Parallax;
half _OcclusionStrength;
half _ClearCoatMask;
half _ClearCoatSmoothness;
half _DetailAlbedoMapScale;
half _DetailNormalMapScale;
half _Surface;
UNITY_TEXTURE_STREAMING_DEBUG_VARS;
CBUFFER_END

// NOTE: Do not ifdef the properties for dots instancing, but ifdef the actual usage.
// Otherwise you might break CPU-side as property constant-buffer offsets change per variant.
// NOTE: Dots instancing is orthogonal to the constant buffer above.
#ifdef UNITY_DOTS_INSTANCING_ENABLED

UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _SpecColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic)
    UNITY_DOTS_INSTANCED_PROP(float , _BumpScale)
    UNITY_DOTS_INSTANCED_PROP(float , _Parallax)
    UNITY_DOTS_INSTANCED_PROP(float , _OcclusionStrength)
    UNITY_DOTS_INSTANCED_PROP(float , _ClearCoatMask)
    UNITY_DOTS_INSTANCED_PROP(float , _ClearCoatSmoothness)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailAlbedoMapScale)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailNormalMapScale)
    UNITY_DOTS_INSTANCED_PROP(float , _Surface)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

// Here, we want to avoid overriding a property like e.g. _BaseColor with something like this:
// #define _BaseColor UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor0)
//
// It would be simpler, but it can cause the compiler to regenerate the property loading code for each use of _BaseColor.
//
// To avoid this, the property loads are cached in some static values at the beginning of the shader.
// The properties such as _BaseColor are then overridden so that it expand directly to the static value like this:
// #define _BaseColor unity_DOTS_Sampled_BaseColor
//
// This simple fix happened to improve GPU performances by ~10% on Meta Quest 2 with URP on some scenes.
static float4 unity_DOTS_Sampled_BaseColor;
static float4 unity_DOTS_Sampled_SpecColor;
static float4 unity_DOTS_Sampled_EmissionColor;
static float  unity_DOTS_Sampled_Cutoff;
static float  unity_DOTS_Sampled_Smoothness;
static float  unity_DOTS_Sampled_Metallic;
static float  unity_DOTS_Sampled_BumpScale;
static float  unity_DOTS_Sampled_Parallax;
static float  unity_DOTS_Sampled_OcclusionStrength;
static float  unity_DOTS_Sampled_ClearCoatMask;
static float  unity_DOTS_Sampled_ClearCoatSmoothness;
static float  unity_DOTS_Sampled_DetailAlbedoMapScale;
static float  unity_DOTS_Sampled_DetailNormalMapScale;
static float  unity_DOTS_Sampled_Surface;

void SetupDOTSLitMaterialPropertyCaches()
{
    unity_DOTS_Sampled_BaseColor            = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _BaseColor);
    unity_DOTS_Sampled_SpecColor            = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _SpecColor);
    unity_DOTS_Sampled_EmissionColor        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float4, _EmissionColor);
    unity_DOTS_Sampled_Cutoff               = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Cutoff);
    unity_DOTS_Sampled_Smoothness           = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Smoothness);
    unity_DOTS_Sampled_Metallic             = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Metallic);
    unity_DOTS_Sampled_BumpScale            = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _BumpScale);
    unity_DOTS_Sampled_Parallax             = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Parallax);
    unity_DOTS_Sampled_OcclusionStrength    = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _OcclusionStrength);
    unity_DOTS_Sampled_ClearCoatMask        = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _ClearCoatMask);
    unity_DOTS_Sampled_ClearCoatSmoothness  = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _ClearCoatSmoothness);
    unity_DOTS_Sampled_DetailAlbedoMapScale = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailAlbedoMapScale);
    unity_DOTS_Sampled_DetailNormalMapScale = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _DetailNormalMapScale);
    unity_DOTS_Sampled_Surface              = UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float , _Surface);
}

#undef UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES
#define UNITY_SETUP_DOTS_MATERIAL_PROPERTY_CACHES() SetupDOTSLitMaterialPropertyCaches()

#define _BaseColor              unity_DOTS_Sampled_BaseColor
#define _SpecColor              unity_DOTS_Sampled_SpecColor
#define _EmissionColor          unity_DOTS_Sampled_EmissionColor
#define _Cutoff                 unity_DOTS_Sampled_Cutoff
#define _Smoothness             unity_DOTS_Sampled_Smoothness
#define _Metallic               unity_DOTS_Sampled_Metallic
#define _BumpScale              unity_DOTS_Sampled_BumpScale
#define _Parallax               unity_DOTS_Sampled_Parallax
#define _OcclusionStrength      unity_DOTS_Sampled_OcclusionStrength
#define _ClearCoatMask          unity_DOTS_Sampled_ClearCoatMask
#define _ClearCoatSmoothness    unity_DOTS_Sampled_ClearCoatSmoothness
#define _DetailAlbedoMapScale   unity_DOTS_Sampled_DetailAlbedoMapScale
#define _DetailNormalMapScale   unity_DOTS_Sampled_DetailNormalMapScale
#define _Surface                unity_DOTS_Sampled_Surface

#endif

TEXTURE2D(_ParallaxMap);        SAMPLER(sampler_ParallaxMap);
TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_DetailMask);         SAMPLER(sampler_DetailMask);
TEXTURE2D(_DetailAlbedoMap);    SAMPLER(sampler_DetailAlbedoMap);
TEXTURE2D(_DetailNormalMap);    SAMPLER(sampler_DetailNormalMap);
TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);
TEXTURE2D(_ClearCoatMap);       SAMPLER(sampler_ClearCoatMap);

#ifdef _SPECULAR_SETUP
    #define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv)
#else
    #define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv)
#endif

half4 SampleMetallicSpecGloss(float2 uv, half albedoAlpha)
{
    half4 specGloss;

#ifdef _METALLICSPECGLOSSMAP
    specGloss = half4(SAMPLE_METALLICSPECULAR(uv));
    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        specGloss.a = albedoAlpha * _Smoothness;
    #else
        specGloss.a *= _Smoothness;
    #endif
#else // _METALLICSPECGLOSSMAP
    #if _SPECULAR_SETUP
        specGloss.rgb = _SpecColor.rgb;
    #else
        specGloss.rgb = _Metallic.rrr;
    #endif

    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        specGloss.a = albedoAlpha * _Smoothness;
    #else
        specGloss.a = _Smoothness;
    #endif
#endif

    return specGloss;
}

half SampleOcclusion(float2 uv)
{
    #ifdef _OCCLUSIONMAP
        half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
        return LerpWhiteTo(occ, _OcclusionStrength);
    #else
        return half(1.0);
    #endif
}


// Returns clear coat parameters
// .x/.r == mask
// .y/.g == smoothness
half2 SampleClearCoat(float2 uv)
{
#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half2 clearCoatMaskSmoothness = half2(_ClearCoatMask, _ClearCoatSmoothness);

#if defined(_CLEARCOATMAP)
    clearCoatMaskSmoothness *= SAMPLE_TEXTURE2D(_ClearCoatMap, sampler_ClearCoatMap, uv).rg;
#endif

    return clearCoatMaskSmoothness;
#else
    return half2(0.0, 1.0);
#endif  // _CLEARCOAT
}

void ApplyPerPixelDisplacement(half3 viewDirTS, inout float2 uv)
{
#if defined(_PARALLAXMAP)
    uv += ParallaxMapping(TEXTURE2D_ARGS(_ParallaxMap, sampler_ParallaxMap), viewDirTS, _Parallax, uv);
#endif
}

// Used for scaling detail albedo. Main features:
// - Depending if detailAlbedo brightens or darkens, scale magnifies effect.
// - No effect is applied if detailAlbedo is 0.5.
half3 ScaleDetailAlbedo(half3 detailAlbedo, half scale)
{
    // detailAlbedo = detailAlbedo * 2.0h - 1.0h;
    // detailAlbedo *= _DetailAlbedoMapScale;
    // detailAlbedo = detailAlbedo * 0.5h + 0.5h;
    // return detailAlbedo * 2.0f;

    // A bit more optimized
    return half(2.0) * detailAlbedo * scale - scale + half(1.0);
}

half3 ApplyDetailAlbedo(float2 detailUv, half3 albedo, half detailMask)
{
#if defined(_DETAIL)
    half3 detailAlbedo = SAMPLE_TEXTURE2D(_DetailAlbedoMap, sampler_DetailAlbedoMap, detailUv).rgb;

    // In order to have same performance as builtin, we do scaling only if scale is not 1.0 (Scaled version has 6 additional instructions)
#if defined(_DETAIL_SCALED)
    detailAlbedo = ScaleDetailAlbedo(detailAlbedo, _DetailAlbedoMapScale);
#else
    detailAlbedo = half(2.0) * detailAlbedo;
#endif

    return albedo * LerpWhiteTo(detailAlbedo, detailMask);
#else
    return albedo;
#endif
}

half3 ApplyDetailNormal(float2 detailUv, half3 normalTS, half detailMask)
{
#if defined(_DETAIL)
#if BUMP_SCALE_NOT_SUPPORTED
    half3 detailNormalTS = UnpackNormal(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, detailUv));
#else
    half3 detailNormalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, detailUv), _DetailNormalMapScale);
#endif

    // With UNITY_NO_DXT5nm unpacked vector is not normalized for BlendNormalRNM
    // For visual consistancy we going to do in all cases
    detailNormalTS = normalize(detailNormalTS);

    return lerp(normalTS, BlendNormalRNM(normalTS, detailNormalTS), detailMask); // todo: detailMask should lerp the angle of the quaternion rotation, not the normals
#else
    return normalTS;
#endif
}

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);

    half4 specGloss = SampleMetallicSpecGloss(uv, albedoAlpha.a);
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    outSurfaceData.albedo = AlphaModulate(outSurfaceData.albedo, outSurfaceData.alpha);

#if _SPECULAR_SETUP
    outSurfaceData.metallic = half(1.0);
    outSurfaceData.specular = specGloss.rgb;
#else
    outSurfaceData.metallic = specGloss.r;
    outSurfaceData.specular = half3(0.0, 0.0, 0.0);
#endif

    outSurfaceData.smoothness = specGloss.a;
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    outSurfaceData.occlusion = SampleOcclusion(uv);
    outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));

#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half2 clearCoat = SampleClearCoat(uv);
    outSurfaceData.clearCoatMask       = clearCoat.r;
    outSurfaceData.clearCoatSmoothness = clearCoat.g;
#else
    outSurfaceData.clearCoatMask       = half(0.0);
    outSurfaceData.clearCoatSmoothness = half(0.0);
#endif

#if defined(_DETAIL)
    half detailMask = SAMPLE_TEXTURE2D(_DetailMask, sampler_DetailMask, uv).a;
    float2 detailUv = uv * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
    outSurfaceData.albedo = ApplyDetailAlbedo(detailUv, outSurfaceData.albedo, detailMask);
    outSurfaceData.normalTS = ApplyDetailNormal(detailUv, outSurfaceData.normalTS, detailMask);
#endif
}

// All per-instance weather state is instanced. Material response and masks stay shared.
#ifdef UNITY_INSTANCING_ENABLED
UNITY_INSTANCING_BUFFER_START(CFArchitecturalInstance)
    UNITY_DEFINE_INSTANCED_PROP(float4, _CFFacadeA)
    UNITY_DEFINE_INSTANCED_PROP(float4, _CFFacadeB)
    UNITY_DEFINE_INSTANCED_PROP(float, _CFFacadeEnabled)
    UNITY_DEFINE_INSTANCED_PROP(float4, _CFBounds)
    UNITY_DEFINE_INSTANCED_PROP(float4x4, _CFWorldToBuilding)
    UNITY_DEFINE_INSTANCED_PROP(float, _CFWeatherUseBuilding)
    UNITY_DEFINE_INSTANCED_PROP(float, _CFWeatherIntensity)
UNITY_INSTANCING_BUFFER_END(CFArchitecturalInstance)
    #define _CFFacadeA UNITY_ACCESS_INSTANCED_PROP(CFArchitecturalInstance,_CFFacadeA)
    #define _CFFacadeB UNITY_ACCESS_INSTANCED_PROP(CFArchitecturalInstance,_CFFacadeB)
    #define _CFFacadeEnabled UNITY_ACCESS_INSTANCED_PROP(CFArchitecturalInstance,_CFFacadeEnabled)
    #define _CFBounds UNITY_ACCESS_INSTANCED_PROP(CFArchitecturalInstance,_CFBounds)
    #define _CFWorldToBuilding UNITY_ACCESS_INSTANCED_PROP(CFArchitecturalInstance,_CFWorldToBuilding)
    #define _CFWeatherUseBuilding UNITY_ACCESS_INSTANCED_PROP(CFArchitecturalInstance,_CFWeatherUseBuilding)
    #define _CFWeatherIntensity UNITY_ACCESS_INSTANCED_PROP(CFArchitecturalInstance,_CFWeatherIntensity)
#endif
TEXTURE2D(_CFWeatherMask); SAMPLER(sampler_CFWeatherMask);
TEXTURE2D(_CFFacadeMask); SAMPLER(sampler_CFFacadeMask);
float CFArchitectureHash(float3 q){q=frac(q*.1031);q+=dot(q,q.yzx+33.33);return frac((q.x+q.y)*q.z);}
float CFArchitectureNoise(float3 p){float3 c=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(CFArchitectureHash(c),CFArchitectureHash(c+float3(1,0,0)),f.x),lerp(CFArchitectureHash(c+float3(0,1,0)),CFArchitectureHash(c+float3(1,1,0)),f.x),f.y),lerp(lerp(CFArchitectureHash(c+float3(0,0,1)),CFArchitectureHash(c+float3(1,0,1)),f.x),lerp(CFArchitectureHash(c+float3(0,1,1)),CFArchitectureHash(c+1),f.x),f.y),f.z);}
void CFApplyFacadeWeather(float3 world,half3 normalWS,inout SurfaceData s)
{
    UNITY_BRANCH if(_CFWeatherIntensity<=0 && _CFFacadeEnabled<.5)return;
    float3 p=_CFWeatherUseBuilding>.5?mul(_CFWorldToBuilding,float4(world,1)).xyz:TransformWorldToObject(world);float3 n=_CFWeatherUseBuilding>.5?normalize(mul((float3x3)_CFWorldToBuilding,normalWS)):TransformWorldToObjectDir(normalWS);float3 q=p-float3(0,0,_CFBounds.z);
    float w=_CFBounds.x*2,d=_CFBounds.y*2,u=0;
    if(q.z>_CFBounds.y-.1 || abs(n.z)>=abs(n.x)&&n.z>0)u=_CFBounds.x-q.x;
    else if(abs(n.x)>abs(n.z)&&n.x<0)u=w+_CFBounds.y-q.z;
    else if(n.z<0)u=w+d+q.x+_CFBounds.x;
    else u=w+d+w+q.z+_CFBounds.y;
    float2 uv=float2(u/(2*w+2*d),p.y/_CFBounds.w);
    if(_CFFacadeEnabled>.5)
    {
        half4 architecture=SAMPLE_TEXTURE2D(_CFFacadeMask,sampler_CFFacadeMask,uv);
        // World derivatives filter fine layers; broad metre-scale fields survive gameplay zoom.
        float footprint=max(length(ddx(p)),length(ddy(p)));
        float seed=_CFFacadeB.z;
        float3 phase=float3(frac(seed*.137)*137,frac(seed*.319)*173,frac(seed*.719)*97);
        float macro=CFArchitectureNoise(p*.19+phase+13.7);
        float medium=CFArchitectureNoise(p*float3(.87,1.13,.91)+phase+31.3);
        float micro=CFArchitectureNoise(p*6.7+phase+61.9);
        float mediumWeight=1-smoothstep(.8,2.5,footprint);
        float microWeight=1-smoothstep(.035,.16,footprint);
        float patch=smoothstep(.34,.79,macro*.70+medium*.30);
        float wall=1-smoothstep(.45,.8,abs(n.y));
        float ground=(1-smoothstep(.1,2.1,p.y))*lerp(.25,1,patch)*wall;
        float rain=architecture.r*lerp(.35,1,medium)*wall;
        float soot=architecture.g*lerp(.30,1,macro)*wall;
        float recess=architecture.b*lerp(.40,1,medium)*wall;
        float damp=max(ground,rain*.75+recess*.25);
        float dark=_CFMaterialResponse.x*(patch*.28*_CFFacadeA.x+rain*.36*_CFFacadeA.y+soot*.40*_CFFacadeA.z+ground*.40*_CFFacadeA.w+recess*.12*_CFFacadeA.x);
        float tone=(macro-.5)*.22*_CFFacadeA.x+(medium-.5)*.09*_CFFacadeA.x*mediumWeight;
        s.albedo*=clamp(1+tone-dark,.38,1.15);
        // Fixed assembly exposure axis, not current sun lighting: no baked directional shadows.
        float exposure=lerp(.35,1,saturate(dot(normalize(float3(-.45,.15,.88)),n)*.5+.5));
        float fade=_CFFacadeB.x*_CFMaterialResponse.z*exposure*lerp(.3,1,macro)*.16;
        s.albedo=lerp(s.albedo,s.albedo*.90+half3(.16,.145,.12),fade);
        float mineral=architecture.a*smoothstep(.55,.78,medium)*_CFFacadeB.y*_CFMaterialResponse.y*.28;
        s.albedo=lerp(s.albedo,half3(.52,.49,.42),mineral);
        s.smoothness=saturate(s.smoothness+(damp*.07*_CFFacadeA.w)-(patch*.10*_CFFacadeA.x+soot*.08*_CFFacadeA.z+mineral*.3)+(micro-.5)*.04*microWeight*_CFFacadeA.x);
        s.normalTS=normalize(s.normalTS+half3((micro-.5)*_CFMaterialResponse.w*_CFFacadeA.x*microWeight,(medium-.5)*_CFMaterialResponse.w*_CFFacadeA.x*microWeight,0));
        return;
    }
    half streak=SAMPLE_TEXTURE2D(_CFWeatherMask,sampler_CFWeatherMask,uv).r;
    float broad=CFArchitectureNoise(p*.47+13.7),fine=CFArchitectureNoise(p*float3(4.8,.65,4.8)+31.3);
    float ground=(1-smoothstep(0,1.0,p.y))*lerp(.45,1,broad);
    float edge=min(_CFBounds.x-.18-abs(q.x),_CFBounds.y-.18-abs(q.z));
    float roof=(1-smoothstep(.05,.85,edge))*step(.7,n.y)*lerp(.3,1,fine);
    half dirt=ground*_CFWeatherParams.y+streak*_CFWeatherParams.z+roof*_CFWeatherParams.w;
    half variation=(broad-.5h)*_CFWeatherParams.x;
    s.albedo*=1+_CFWeatherIntensity*(variation-dirt);
    s.smoothness=saturate(s.smoothness-_CFWeatherIntensity*(dirt*.20h+(fine-.5h)*.08h));
}
#endif // UNIVERSAL_INPUT_SURFACE_PBR_INCLUDED
