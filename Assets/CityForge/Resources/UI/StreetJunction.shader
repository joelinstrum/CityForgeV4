Shader "CityForge/Street Junction" {
Properties {
_RecessDepth("Visual depth",Float)=0
_RecessBorder("Curb lining",Float)=0
_RecessDirt("Earth lining",2D)="white"{}
_RecessUseFootprint("Shared boundary",Float)=0
_RecessFootprint("Boundary field",2D)="black"{}
_RecessFootprintRect("Boundary bounds",Vector)=(0,0,1,1)
_Layer0("Street 0",2D)="white"{} _Tint0("Tint 0",Color)=(1,1,1,1)
_Layer1("Street 1",2D)="white"{} _Tint1("Tint 1",Color)=(1,1,1,1)
_Layer2("Street 2",2D)="white"{} _Tint2("Tint 2",Color)=(1,1,1,1)
_Layer3("Street 3",2D)="white"{} _Tint3("Tint 3",Color)=(1,1,1,1)
}
SubShader { Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-5" "RenderType"="Transparent"}
Pass { Tags {"LightMode"="UniversalForward"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
HLSLPROGRAM
#pragma target 3.5
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _SHADOWS_SOFT
#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#pragma multi_compile_fog
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
TEXTURE2D(_Layer0); SAMPLER(sampler_Layer0); float4 _Tint0;
TEXTURE2D(_Layer1); SAMPLER(sampler_Layer1); float4 _Tint1;
TEXTURE2D(_Layer2); SAMPLER(sampler_Layer2); float4 _Tint2;
TEXTURE2D(_Layer3); SAMPLER(sampler_Layer3); float4 _Tint3;
#include "../Region/RoadRecess.hlsl"
float _RecessDepth, _RecessBorder;
TEXTURE2D(_RecessDirt); SAMPLER(sampler_RecessDirt);
struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 weights:COLOR;float2 uv0:TEXCOORD0;float2 uv1:TEXCOORD1;float2 uv2:TEXCOORD2;float2 uv3:TEXCOORD3;float2 alpha:TEXCOORD4;};
struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float4 weights:COLOR;float4 uv01:TEXCOORD2;float4 uv23:TEXCOORD3;float alpha:TEXCOORD4;};
V vert(A v){V o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(v.normalOS);o.weights=v.weights;o.uv01=float4(v.uv0,v.uv1);o.uv23=float4(v.uv2,v.uv3);o.alpha=v.alpha.x;return o;}
half4 frag(V i):SV_Target {
float wall=0, edgeDistance=1000;
if(_RecessDepth>0 && _RecessUseFootprint>.5) {
    float3 view=GetWorldSpaceNormalizeViewDir(i.world);
    float2 ray=-view.xz*(_RecessDepth/max(.12,view.y));
    float hit=RoadRecessHit(i.world.xz,ray,edgeDistance);
    float2 dx=ddx(i.world.xz),dy=ddy(i.world.xz);
    float determinant=dx.x*dy.y-dx.y*dy.x;
    if(abs(determinant)>1e-10) {
        float2 pixels=float2(ray.x*dy.y-ray.y*dy.x,dx.x*ray.y-dx.y*ray.x)/determinant*hit;
        i.uv01+=ddx(i.uv01)*pixels.x+ddy(i.uv01)*pixels.y;
        i.uv23+=ddx(i.uv23)*pixels.x+ddy(i.uv23)*pixels.y;
    }
    wall=1-smoothstep(.96,1,hit);
}
float4 w=i.weights/max(dot(i.weights,1),.001);
float3 c=SAMPLE_TEXTURE2D(_Layer0,sampler_Layer0,i.uv01.xy).rgb*_Tint0.rgb*w.x;
c+=SAMPLE_TEXTURE2D(_Layer1,sampler_Layer1,i.uv01.zw).rgb*_Tint1.rgb*w.y;
c+=SAMPLE_TEXTURE2D(_Layer2,sampler_Layer2,i.uv23.xy).rgb*_Tint2.rgb*w.z;
c+=SAMPLE_TEXTURE2D(_Layer3,sampler_Layer3,i.uv23.zw).rgb*_Tint3.rgb*w.w;
if(_RecessDepth>0 && _RecessUseFootprint>.5) {
    float3 lining=SAMPLE_TEXTURE2D(_RecessDirt,sampler_RecessDirt,i.world.xz*2).rgb*float3(.783,.774,.801);
    lining=lerp(lining,float3(.39,.38,.35),saturate(_RecessBorder));
    c=lerp(c*.91,lining*.54,wall);
    c*=1-.20*(1-smoothstep(0,_RecessDepth*.85,edgeDistance));
}
// Match the receiving ribbons' URP lighting instead of adding a brighter,
// separately lit earth decal at the connection.
InputData lighting=(InputData)0;
lighting.positionWS=i.world;lighting.normalWS=normalize(i.normal);
lighting.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);
lighting.shadowCoord=TransformWorldToShadowCoord(i.world);
lighting.bakedGI=SampleSH(lighting.normalWS);lighting.shadowMask=half4(1,1,1,1);
lighting.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
SurfaceData surface=(SurfaceData)0;
surface.albedo=c;surface.normalTS=half3(0,0,1);surface.smoothness=.03;
surface.occlusion=1;surface.alpha=i.alpha;
half4 color=UniversalFragmentPBR(lighting,surface);
color.rgb=MixFog(color.rgb,ComputeFogFactor(TransformWorldToHClip(i.world).z));
color.a=i.alpha;return color;
}
ENDHLSL
} } }
