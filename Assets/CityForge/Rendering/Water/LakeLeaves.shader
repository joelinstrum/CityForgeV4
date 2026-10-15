Shader "CityForge/Lake Leaves" {
Properties { }
SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Pass {
Tags {"LightMode"="UniversalForward"} Cull Off
HLSLPROGRAM
#pragma vertex Vert
#pragma fragment Frag
#pragma multi_compile_instancing
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
struct A {float4 position:POSITION;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
struct V {float4 position:SV_POSITION;half4 color:COLOR;};
V Vert(A i){UNITY_SETUP_INSTANCE_ID(i);V o;o.position=TransformObjectToHClip(i.position.xyz);o.color=i.color;return o;}
half4 Frag(V i):SV_Target {Light light=GetMainLight();return half4(i.color.rgb*(.4+light.color*.6),1);}
ENDHLSL
} } }
