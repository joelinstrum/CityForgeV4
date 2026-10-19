Shader "CityForge/Regional Relief"
{
 Properties { _BaseMap("Relief atlas",2D)="white"{} _Grid("District grid",Float)=8 }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
 Pass {
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A {float4 positionOS:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
 struct V {float4 positionCS:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
 CBUFFER_START(UnityPerMaterial)
 float _Grid;
 CBUFFER_END
 TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
 V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.color=i.color;o.uv=i.uv;return o;}
 half4 Frag(V i):SV_Target {float2 q=i.uv*_Grid;float2 edge=min(frac(q),1-frac(q));float2 width=max(fwidth(q),.0001);float gridLine=1-saturate(min(edge.x/width.x,edge.y/width.y));return half4(lerp(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb,half3(.86,.85,.74),gridLine*.32),1);}
 ENDHLSL
 }
 }
}
