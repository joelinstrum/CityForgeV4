Shader "CityForge/Campfire Flame" {
SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" } Pass {
Blend SrcAlpha One
ZWrite Off
Cull Off
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct A {float4 positionOS:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
struct V {float4 pos:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
V vert(A a){V o;o.pos=TransformObjectToHClip(a.positionOS.xyz);o.color=a.color;o.uv=a.uv;return o;}
half4 frag(V i):SV_Target {
 float y=i.uv.y;float width=lerp(.85,.10,y);float x=(i.uv.x-.5)*2/width;
 float alpha=saturate(1-x*x)*sin(saturate(y)*3.14159);
 return half4(i.color.rgb*1.7,i.color.a*alpha*alpha);
}
ENDHLSL
} } }
