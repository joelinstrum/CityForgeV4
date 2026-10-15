Shader "CityForge/Meadow Grass"
{
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
  Cull Off
  Pass
  {
   Name "Meadow" Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "RegionSurface.hlsl"
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float2 uv:TEXCOORD1; half4 color:COLOR; };
   Varyings Vert(Attributes v)
   {
    Varyings o; o.positionWS=TransformObjectToWorld(v.positionOS.xyz);
    o.positionCS=TransformWorldToHClip(o.positionWS); o.uv=v.uv; o.color=v.color; return o;
   }
   half4 Frag(Varyings i):SV_Target
   {
    // Match the ground's shared vegetation coverage, retaining irregular gaps within stands.
    float2 p=i.positionWS.xz;
    float4 cover=SAMPLE_TEXTURE2D(_MeadowCoverField,sampler_MeadowCoverField,(p+_RegionSide*.5)/_RegionSide);
    clip(cfnoise(p/3.1+51)-lerp(.12,.48,cover.a));
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
    half3 color=i.color.rgb;
    color*=lerp(.62,1.16,i.uv.y);
    half3 light=SampleSH(half3(0,1,0))+sun.color*(.35+.65*saturate(sun.direction.y))*sun.shadowAttenuation;
    return half4(color*light,1);
   }
   ENDHLSL
  }
 }
}
