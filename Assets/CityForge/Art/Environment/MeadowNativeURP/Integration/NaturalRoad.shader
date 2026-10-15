Shader "CityForge/Study/Natural Road"
{
 Properties { _BaseMap("Road albedo",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST; half4 _BaseColor;
   CBUFFER_END
   struct Input {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
   struct Output {float4 position:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float2 uv:TEXCOORD2;float edge:TEXCOORD3;half fog:TEXCOORD4;};
   Output Vert(Input v) {Output o;VertexPositionInputs p=GetVertexPositionInputs(v.vertex.xyz);o.position=p.positionCS;o.world=p.positionWS;o.normal=TransformObjectToWorldNormal(v.normal);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);o.edge=v.uv.y;o.fog=ComputeFogFactor(p.positionCS.z);return o;}
   half4 Frag(Output i):SV_Target
   {
    half4 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;
    half3 n=normalize(i.normal);if(n.y<0)n=-n;
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
    // The study has no baked probes; retain sky fill on road meshes in tree shadows.
    half3 lighting=max(SampleSH(n),half3(.28,.3,.32))+sun.color*saturate(dot(n,sun.direction))*sun.shadowAttenuation;
    half alpha=smoothstep(0,.08,i.edge)*smoothstep(0,.08,1-i.edge);
    return half4(MixFog(albedo.rgb*lighting,i.fog),alpha);
   }
   ENDHLSL
  }
 }
}
