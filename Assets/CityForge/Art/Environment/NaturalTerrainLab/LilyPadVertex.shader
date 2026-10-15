Shader "CityForge/Lily Pad Vertex Color" {SubShader {Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Pass {Tags {"LightMode"="UniversalForward"} Cull Off HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
struct A{float4 p:POSITION;float3 n:NORMAL;half4 c:COLOR;};struct V{float4 p:SV_POSITION;float3 ws:TEXCOORD0;half4 c:COLOR;};V vert(A i){V o;o.ws=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.ws);o.c=i.c;return o;}half4 frag(V i):SV_Target{InputData d=(InputData)0;d.positionWS=i.ws;d.normalWS=float3(0,1,0);d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.ws);d.shadowCoord=TransformWorldToShadowCoord(i.ws);d.bakedGI=SampleSH(d.normalWS);SurfaceData s=(SurfaceData)0;s.albedo=i.c.rgb*.58;s.smoothness=.24;s.occlusion=1;s.alpha=1;s.normalTS=float3(0,0,1);return UniversalFragmentPBR(d,s);}
ENDHLSL } UsePass "Universal Render Pipeline/Lit/DepthOnly"}}