#ifndef CITYFORGE_SURFACE_WEATHERING
#define CITYFORGE_SURFACE_WEATHERING
#ifndef CITYFORGE_SURFACE_PROPERTIES_BOUND
#include "SurfaceWeatheringProperties.hlsl"
CITYFORGE_WEATHERING_PROPERTIES
#endif
TEXTURE2D(_WeatherGrimeMask); SAMPLER(sampler_WeatherGrimeMask);
TEXTURE2D(_WeatherSediment); SAMPLER(sampler_WeatherSediment);
TEXTURE2D(_WeatherDebris); SAMPLER(sampler_WeatherDebris);
// Stable world-space value noise: no road ID, UV reset, time or saved state.
float WeatherHash(float2 p)
{
    float3 q=frac(float3(p.xyx)*.1031);
    q+=dot(q,q.yzx+33.33);
    return frac((q.x+q.y)*q.z);
}
float WeatherNoise(float2 p)
{
    float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(WeatherHash(cell),WeatherHash(cell+float2(1,0)),f.x),
        lerp(WeatherHash(cell+float2(0,1)),WeatherHash(cell+1),f.x),f.y);
}
float2 WeatherRotate(float2 p,float turn)
{
    if(turn<1)return p;
    if(turn<2)return float2(-p.y,p.x);
    if(turn<3)return -p;
    return float2(p.y,-p.x);
}
half4 WeatherSample(TEXTURE2D_PARAM(map,mapSampler),float2 uv,float phase,float salt,bool alpha)
{
    UNITY_BRANCH if(_WeatherVariation.x<.5)
        return SAMPLE_TEXTURE2D(map,mapSampler,uv);
    float index=floor(phase),weight=smoothstep(.15,.85,frac(phase));
    float2 dx=ddx(uv),dy=ddy(uv);
    float2 keyA=float2(index+_WeatherVariation.z,salt),keyB=keyA+float2(1,0);
    float turnA=floor(WeatherHash(keyA)*4),turnB=floor(WeatherHash(keyB)*4);
    float2 shiftA=float2(WeatherHash(keyA+17.3),WeatherHash(keyA+41.7))*17;
    float2 shiftB=float2(WeatherHash(keyB+17.3),WeatherHash(keyB+41.7))*17;
    half4 a=SAMPLE_TEXTURE2D_GRAD(map,mapSampler,WeatherRotate(uv,turnA)+shiftA,
        WeatherRotate(dx,turnA),WeatherRotate(dy,turnA));
    half4 b=SAMPLE_TEXTURE2D_GRAD(map,mapSampler,WeatherRotate(uv,turnB)+shiftB,
        WeatherRotate(dx,turnB),WeatherRotate(dy,turnB));
    if(alpha){a.rgb*=a.a;b.rgb*=b.a;}
    half4 result=lerp(a,b,weight);
    if(alpha)result.rgb/=max(.0001,result.a);
    return result;
}
#include "SurfaceWeatheringLayers.hlsl"
half3 ApplySurfaceWeathering(float3 world, float4 ribbon, float knownBoundaryDistance, inout SurfaceData surface, inout half3 normalWS)
{
    UNITY_BRANCH if (_WeatherIntensity <= 0) return half3(0,0,0);
    WeatherLayers layers=WeatherGetLayers(world.xz);
    WeatherApplyLayers(surface,layers);
    bool sidewalk = _WeatherGrime.w > .5;
    float distance = max(0, min(ribbon.x, ribbon.y - ribbon.x));
    if (_RecessUseFootprint > .5 || _WeatherUseBoundary > .5)
        distance = knownBoundaryDistance < 999 ? abs(knownBoundaryDistance) : abs(RoadRecessDistance(world.xz));
    float phase=0;
    UNITY_BRANCH if(_WeatherVariation.x>.5)
        phase=WeatherNoise(world.xz*_WeatherVariation.y+_WeatherVariation.z*.137)*12;
    // World-space pockets vary along the connected exterior boundary, never per segment.
    half pocket = layers.medium;
    UNITY_BRANCH if (_WeatherPattern.x > 0 && _WeatherMaterialParams.z < .5)
    {
        UNITY_BRANCH if(_WeatherVariation.x>.5)
            pocket=smoothstep(.25,.75,WeatherNoise(world.xz*_WeatherPattern.y*3+_WeatherVariation.z*.319));
        else
            pocket=smoothstep(.40,.60,SAMPLE_TEXTURE2D(_WeatherGrimeMask,sampler_WeatherGrimeMask,
                world.xz*_WeatherPattern.y+float2(.173,.619)).r);
    }
    float4 independentGutter=float4(1,1,1,1);
    if(_WeatherMaterialParams.z>.5){
        float4 raw=WeatherGutterFields(world.xz,layers.footprint);
        independentGutter=lerp(float4(1,1,1,1),lerp(float4(.15,.12,.05,.03),float4(1.85,1.8,1.9,1.9),raw),saturate(_WeatherGutterVariations)*saturate(_WeatherPattern.x));
    }
    float patchWidth = lerp(1,lerp(.18,1.4,pocket),saturate(_WeatherPattern.x));
    if(_WeatherMaterialParams.z>.5)patchWidth=independentGutter.x;
    float physicalWidth=clamp(_WeatherGutter.x*patchWidth,.05,1.5);
    float gutter = 1 - smoothstep(0, physicalWidth, distance);
    float outerEdge = sidewalk && _WeatherUseBoundary > .5 && _WeatherPattern.w > 0
        ? 1-smoothstep(0,.22,abs(distance-_WeatherPattern.w)) : 0;
    float accumulation = gutter * lerp(1,lerp(.05,1,pocket),saturate(_WeatherPattern.x));
    if(_WeatherMaterialParams.z>.5)accumulation=gutter;
    float sedimentAmount=0;
    // Screen derivatives recover a world XZ gradient without changing the mesh or base tangent normal.
    float2 dx = ddx(world.xz), dy = ddy(world.xz);
    float determinant = dx.x * dy.y - dx.y * dy.x;
    if (!sidewalk && _WeatherCamber > 0 && abs(determinant) > 1e-10)
    {
        float2 gradient = float2(ddx(distance) * dy.y - ddy(distance) * dx.y,
            dx.x * ddy(distance) - dy.x * ddx(distance)) / determinant;
        float slope = _WeatherIntensity * _WeatherCamber * (1 - saturate(distance / max(.1, min(ribbon.y,8) * .5)));
        normalWS = normalize(normalWS - float3(gradient.x,0,gradient.y) * slope);
    }
    float contact = (1 - smoothstep(0,.18,distance)) * _WeatherGutter.z;
    // Contact needs a road/sidewalk boundary; standalone lot paths keep only their gutter controls.
    contact *= (sidewalk || _RecessBorder > .5) ? 1 : 0;
    surface.albedo *= 1 - _WeatherIntensity * contact;
    // Shared world samples do not restart per road; a rotated broad sample breaks the tile period.
    float2 grimeUV = world.xz * _WeatherGrime.y;
    half grime = 0;
    UNITY_BRANCH if (_WeatherGrime.x > 0 || _WeatherGrime.z > 0)
    {
        half fine = WeatherSample(TEXTURE2D_ARGS(_WeatherGrimeMask,sampler_WeatherGrimeMask),grimeUV,phase,1,false).r;
        half broad = WeatherSample(TEXTURE2D_ARGS(_WeatherGrimeMask,sampler_WeatherGrimeMask),
            float2(-grimeUV.y,grimeUV.x) * .371 + float2(.37,.61),phase,7,false).r;
        grime = saturate(fine * .7 + broad * .3);
        surface.albedo *= 1 - _WeatherIntensity * _WeatherGrime.x * grime * lerp(.35,1,gutter);
        surface.smoothness = saturate(surface.smoothness + _WeatherIntensity * _WeatherGrime.z * grime * .12);
    }
    UNITY_BRANCH if (_WeatherAccumulation.x > 0)
    {
        half3 sediment = WeatherSample(TEXTURE2D_ARGS(_WeatherSediment,sampler_WeatherSediment),world.xz*_WeatherAccumulation.y,phase,13,false).rgb;
        float amount = _WeatherIntensity * _WeatherAccumulation.x * (accumulation * independentGutter.z + outerEdge * (_WeatherMaterialParams.z>.5?layers.medium:pocket) * .3) * lerp(.45,1,grime);
        sedimentAmount=saturate(amount);
        surface.albedo = lerp(surface.albedo,sediment * _WeatherSedimentTint.rgb,saturate(amount));
    }
    UNITY_BRANCH if (_WeatherAccumulation.z > 0)
    {
        half4 debris = WeatherSample(TEXTURE2D_ARGS(_WeatherDebris,sampler_WeatherDebris),world.xz*_WeatherAccumulation.w,phase,23,true);
        UNITY_BRANCH if (_WeatherPattern.z > 0)
        {
            // A second offset sample adds irregular leaf groups without another mesh or pass.
            half4 scattered = WeatherSample(TEXTURE2D_ARGS(_WeatherDebris,sampler_WeatherDebris),
                float2(-world.z,world.x) * _WeatherAccumulation.w * 1.17 + float2(.317,.731),phase,37,true);
            half weight = scattered.a * _WeatherPattern.z * (1-debris.a);
            half alpha = debris.a + weight;
            debris.rgb = (debris.rgb * debris.a + scattered.rgb * weight) / max(.0001,alpha);
            debris.a = alpha;
        }
        float leafBand = 1 - smoothstep(0,physicalWidth*1.6,distance);
        float leafAccumulation = leafBand * lerp(1,lerp(.08,1,pocket),saturate(_WeatherPattern.x));
        if(_WeatherMaterialParams.z>.5)leafAccumulation=leafBand*independentGutter.w;
        float coverage = _WeatherIntensity * _WeatherAccumulation.z * leafAccumulation * saturate(debris.a * 1.3);
        surface.albedo = lerp(surface.albedo,debris.rgb * _WeatherDebrisTint.rgb,saturate(coverage));
    }
    surface.albedo *= 1 - _WeatherIntensity * saturate(accumulation * independentGutter.y + outerEdge * (_WeatherMaterialParams.z>.5?layers.medium:pocket) * .6) * (sidewalk ? _WeatherGutter.w : _WeatherGutter.y);
    int debugMode=(int)_WeatherMaterialParams.y;
    if(debugMode==1)return ((layers.micro-.5)*layers.microFade+.5).xxx;
    if(debugMode==2)return layers.medium.xxx;
    if(debugMode==3)return layers.macro.xxx;
    if(debugMode==4)return gutter.xxx;
    if(debugMode==5)return sedimentAmount.xxx;
    return surface.albedo;
}
#endif
