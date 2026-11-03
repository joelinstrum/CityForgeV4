Shader "Crest/Inputs/Foam/CityForge Inland Exclusion"
{
 Properties { _FoamLimit("Coastal foam limit", 2D)="white" {} }
 SubShader {
  Tags { "RenderType"="Opaque" }
  Blend Zero OneMinusSrcAlpha Cull Off ZTest Always ZWrite Off
  Pass {
   CGPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "UnityCG.cginc"
   sampler2D _FoamLimit;
   CBUFFER_START(CrestPerWaterInput)
   float3 _Crest_DisplacementAtInputPosition;
   half _Crest_Weight;
   CBUFFER_END
   struct Attributes { float3 positionOS:POSITION; float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; };
   Varyings Vert(Attributes v) {
    Varyings o;float3 p=mul(unity_ObjectToWorld,float4(v.positionOS,1)).xyz;
    p.xz-=_Crest_DisplacementAtInputPosition.xz;
    o.positionCS=mul(UNITY_MATRIX_VP,float4(p,1));o.uv=v.uv;return o;
   }
   float4 Frag(Varyings i):SV_Target {
    // Multiply persistent foam by the coastal allowance, without adding negative foam.
    return float4(0,0,0,(1-saturate(tex2D(_FoamLimit,i.uv).r))*_Crest_Weight);
   }
   ENDCG
  }
 }
}
