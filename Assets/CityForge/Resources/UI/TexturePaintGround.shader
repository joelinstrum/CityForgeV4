Shader "CityForge/Texture Paint Ground" {
Properties { _WeightsA("Weights A",2D)="black"{} _WeightsB("Weights B",2D)="black"{}
_Layer0("Layer 0",2D)="white"{}
_Tint0("Tint 0",Color)=(1,1,1,1)
_Repeat0("Repeat 0",Vector)=(1,1,0,0)
_Layer1("Layer 1",2D)="white"{}
_Tint1("Tint 1",Color)=(1,1,1,1)
_Repeat1("Repeat 1",Vector)=(1,1,0,0)
_Layer2("Layer 2",2D)="white"{}
_Tint2("Tint 2",Color)=(1,1,1,1)
_Repeat2("Repeat 2",Vector)=(1,1,0,0)
_Layer3("Layer 3",2D)="white"{}
_Tint3("Tint 3",Color)=(1,1,1,1)
_Repeat3("Repeat 3",Vector)=(1,1,0,0)
_Layer4("Layer 4",2D)="white"{}
_Tint4("Tint 4",Color)=(1,1,1,1)
_Repeat4("Repeat 4",Vector)=(1,1,0,0)
_Layer5("Layer 5",2D)="white"{}
_Tint5("Tint 5",Color)=(1,1,1,1)
_Repeat5("Repeat 5",Vector)=(1,1,0,0)
_Layer6("Layer 6",2D)="white"{}
_Tint6("Tint 6",Color)=(1,1,1,1)
_Repeat6("Repeat 6",Vector)=(1,1,0,0)
_Layer7("Layer 7",2D)="white"{}
_Tint7("Tint 7",Color)=(1,1,1,1)
_Repeat7("Repeat 7",Vector)=(1,1,0,0)
}
SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-30" "RenderType"="Transparent" }
Pass { Tags { "LightMode"="UniversalForward" } Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _SHADOWS_SOFT
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
TEXTURE2D(_WeightsA); SAMPLER(sampler_WeightsA); TEXTURE2D(_WeightsB); SAMPLER(sampler_WeightsB);
TEXTURE2D(_Layer0); SAMPLER(sampler_Layer0); float4 _Tint0, _Repeat0;
TEXTURE2D(_Layer1); SAMPLER(sampler_Layer1); float4 _Tint1, _Repeat1;
TEXTURE2D(_Layer2); SAMPLER(sampler_Layer2); float4 _Tint2, _Repeat2;
TEXTURE2D(_Layer3); SAMPLER(sampler_Layer3); float4 _Tint3, _Repeat3;
TEXTURE2D(_Layer4); SAMPLER(sampler_Layer4); float4 _Tint4, _Repeat4;
TEXTURE2D(_Layer5); SAMPLER(sampler_Layer5); float4 _Tint5, _Repeat5;
TEXTURE2D(_Layer6); SAMPLER(sampler_Layer6); float4 _Tint6, _Repeat6;
TEXTURE2D(_Layer7); SAMPLER(sampler_Layer7); float4 _Tint7, _Repeat7;
struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; float2 localUV:TEXCOORD1; };
struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; float2 uv:TEXCOORD2; float2 localUV:TEXCOORD3; };
V vert(A input) { V o; o.world=TransformObjectToWorld(input.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.world); o.normal=TransformObjectToWorldNormal(input.normalOS); o.uv=input.uv; o.localUV=input.localUV; return o; }
half4 frag(V input):SV_Target {
float4 a=SAMPLE_TEXTURE2D(_WeightsA,sampler_WeightsA,input.uv); float4 b=SAMPLE_TEXTURE2D(_WeightsB,sampler_WeightsB,input.uv);
float alpha=dot(a,1)+dot(b,1); clip(alpha-.001); float3 color=0;
color += SAMPLE_TEXTURE2D(_Layer0,sampler_Layer0, input.localUV * _Repeat0.xy).rgb * _Tint0.rgb * a[0];
color += SAMPLE_TEXTURE2D(_Layer1,sampler_Layer1, input.localUV * _Repeat1.xy).rgb * _Tint1.rgb * a[1];
color += SAMPLE_TEXTURE2D(_Layer2,sampler_Layer2, input.localUV * _Repeat2.xy).rgb * _Tint2.rgb * a[2];
color += SAMPLE_TEXTURE2D(_Layer3,sampler_Layer3, input.localUV * _Repeat3.xy).rgb * _Tint3.rgb * a[3];
color += SAMPLE_TEXTURE2D(_Layer4,sampler_Layer4, input.localUV * _Repeat4.xy).rgb * _Tint4.rgb * b[0];
color += SAMPLE_TEXTURE2D(_Layer5,sampler_Layer5, input.localUV * _Repeat5.xy).rgb * _Tint5.rgb * b[1];
color += SAMPLE_TEXTURE2D(_Layer6,sampler_Layer6, input.localUV * _Repeat6.xy).rgb * _Tint6.rgb * b[2];
color += SAMPLE_TEXTURE2D(_Layer7,sampler_Layer7, input.localUV * _Repeat7.xy).rgb * _Tint7.rgb * b[3];
color/=max(alpha,.001); float3 normal=normalize(input.normal); Light sun=GetMainLight(TransformWorldToShadowCoord(input.world));
float3 lighting=SampleSH(normal)+sun.color*saturate(dot(normal,sun.direction))*sun.shadowAttenuation;
return half4(color*lighting,saturate(alpha)); }
ENDHLSL
} } }
