Shader "CityForge/UI/Forest Removal Circle"
{
 Properties { _Color("Color",Color)=(1,.12,.08,.9) }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" "RenderType"="Transparent" }
  Pass {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off ZTest Always Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A {float4 positionOS:POSITION;};struct V {float4 positionCS:SV_POSITION;};
   float4 _Color;
   V Vert(A input){V output;output.positionCS=TransformObjectToHClip(input.positionOS.xyz);return output;}
   half4 Frag(V input):SV_Target{return _Color;}
   ENDHLSL
  }
 }
}
