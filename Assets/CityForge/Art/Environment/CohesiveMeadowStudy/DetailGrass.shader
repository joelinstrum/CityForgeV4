Shader "CityForge/Detail Meadow Grass" {Properties {_BaseColor("Grass",Color)=(.2,.3,.1,1)} SubShader {Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Pass {Tags {"LightMode"="UniversalForward"} Cull Off HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
float4 _BaseColor;struct A{float4 p:POSITION;float4 c:COLOR;};struct V{float4 p:SV_POSITION;float3 ws:TEXCOORD0;float4 c:COLOR;};V vert(A i){V o;o.ws=TransformObjectToWorld(i.p.xyz);float wind=sin(o.ws.x*.35+o.ws.z*.22+_Time.y*.9)*.025+sin(o.ws.z*1.1+_Time.y*1.3)*.012;o.ws.xz+=float2(wind,wind*.55)*i.c.a;o.p=TransformWorldToHClip(o.ws);o.c=i.c;return o;}
half4 frag(V i):SV_Target{InputData input=(InputData)0;input.positionWS=i.ws;input.normalWS=half3(0,1,0);input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.ws);input.shadowCoord=TransformWorldToShadowCoord(i.ws);input.bakedGI=SampleSH(input.normalWS);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);SurfaceData s=(SurfaceData)0;s.albedo=_BaseColor.rgb*i.c.rgb*lerp(.90,1.12,i.c.a);s.normalTS=half3(0,0,1);s.occlusion=1;s.alpha=1;s.smoothness=.02;return UniversalFragmentPBR(input,s);}
ENDHLSL } } }