Shader "CityForge/Distant Tree"
{
 Properties { _BaseMap("Baked tree views", 2D) = "white" {} _BaseColor("Stand lighting tint",Color)=(1,1,1,1) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
  Cull Off
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
  CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
  CBUFFER_END
  struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
  struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
  Varyings Vert(Attributes v) { Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv; return o; }
  half4 Frag(Varyings i):SV_Target { half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv); clip(c.a-.25); return half4(c.rgb*_BaseColor.rgb,1); }
  ENDHLSL
  Pass
  {
   Name "Forward" Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   ENDHLSL
  }
  Pass
  {
   Name "ShadowCaster" Tags { "LightMode"="ShadowCaster" }
   ZWrite On ColorMask 0
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   ENDHLSL
  }
  Pass
  {
   Name "DepthOnly" Tags { "LightMode"="DepthOnly" }
   ZWrite On ColorMask 0
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   ENDHLSL
  }
 }
}
