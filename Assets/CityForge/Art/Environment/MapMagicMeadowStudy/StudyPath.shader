Shader "CityForge/Study Dirt Path" {
Properties { _BaseMap("Dirt",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1) }
SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" } Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
Pass { Tags { "LightMode"="UniversalForward" } HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap); float4 _BaseColor;
struct A { float4 p:POSITION;float2 uv:TEXCOORD0;};struct V { float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 ws:TEXCOORD1;};
V vert(A a){V o;o.ws=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.ws);o.uv=a.uv;return o;}
half4 frag(V i):SV_Target {Light l=GetMainLight(TransformWorldToShadowCoord(i.ws));half3 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;float edge=min(i.uv.x,1-i.uv.x);float alpha=smoothstep(0,.22,edge);return half4(c*(SampleSH(float3(0,1,0))+l.color*saturate(l.direction.y)*l.shadowAttenuation),alpha);}
ENDHLSL } } }
