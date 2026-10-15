Shader "CityForge/Calm Meadow Pond" {
Properties { _DepthMask("Shallow depth",2D)="black"{} _Reflection("Reflections",2D)="gray"{} _Shallow("Shallow water",Color)=(.12,.22,.17,1) _Deep("Deep water",Color)=(.025,.075,.083,1) }
SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"} Pass {
Tags {"LightMode"="UniversalForward"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
TEXTURE2D(_DepthMask);SAMPLER(sampler_DepthMask);TEXTURE2D(_Reflection);SAMPLER(sampler_Reflection);
float4x4 _ReflectionVP;half4 _Shallow,_Deep;
struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 ws:TEXCOORD0;};
V vert(A i){V o;o.ws=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.ws);return o;}
half4 frag(V i):SV_Target{float depth=SAMPLE_TEXTURE2D(_DepthMask,sampler_DepthMask,i.ws.xz/256).r*3;clip(depth-.004);
float2 p=i.ws.xz;float t=_Time.y;float3 normal=normalize(float3(.0008*sin(p.x*.42+p.y*.17+t*.62)+.0004*sin(p.y*.89-t*.45),1,.0006*cos(p.y*.37-p.x*.19+t*.51)));
float3 view=GetWorldSpaceNormalizeViewDir(i.ws);float fresnel=.06+.94*pow(1-saturate(dot(view,normal)),5);
float4 rp=mul(_ReflectionVP,float4(i.ws,1));float2 uv=rp.xy/rp.w*.5+.5;uv+=normal.xz*.006;
half3 reflection=SAMPLE_TEXTURE2D(_Reflection,sampler_Reflection,uv).rgb;
half3 base=lerp(half3(.12,.30,.37),half3(.045,.16,.30),smoothstep(.05,1.5,depth));Light sun=GetMainLight(TransformWorldToShadowCoord(i.ws));float spec=pow(saturate(dot(normal,normalize(view+sun.direction))),420)*.65;
// Crisp planar reflections dominate; a blue depth tint remains beneath them.
half3 blueReflection=reflection*half3(.48,.54,.61);
half3 color=lerp(base,blueReflection,saturate(.76+fresnel*.22));
color+=sun.color*spec*sun.shadowAttenuation;
float alpha=lerp(.42,.99,smoothstep(.01,.45,depth))*smoothstep(0,.15,depth);return half4(color,alpha);}
ENDHLSL
} } }
