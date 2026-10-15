Shader "CityForge/Forest Foliage"
{
 Properties
 {
  _BaseMap("Leaf albedo and opacity",2D)="white" {}
  _BaseColor("Leaf tint",Color)=(.58,.78,.40,1)
  _Cutoff("Leaf cutoff",Range(0,1))=.3
  _CrownCenter("Crown center in mesh space",Vector)=(0,8,0,0)
  _CrownRadii("Crown radii",Vector)=(5,4,5,0)
  _CrownNormalBlend("Crown lighting",Range(0,1))=.8
  _SkyFill("Foliage sky fill",Range(0,2))=1.15
  _Transmission("Leaf light transmission",Range(0,1))=.18
  _ShadowFill("Foliage shadow fill",Range(0,1))=.14
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
  Cull Off
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
  CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST, _BaseColor, _CrownCenter, _CrownRadii;
   float _Cutoff, _CrownNormalBlend, _SkyFill, _Transmission, _ShadowFill;
  CBUFFER_END
  struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
  struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; float2 uv:TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
  Varyings Vert(Attributes v)
  {
   Varyings o=(Varyings)0; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
   o.positionWS=TransformObjectToWorld(v.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.positionWS);
   float3 radius=max(_CrownRadii.xyz,.0001);
   float3 crownNormal=SafeNormalize((v.positionOS.xyz-_CrownCenter.xyz)/(radius*radius));
   // Use the whole crown to guide diffuse lighting. Geometry and opacity remain the original leaves.
   o.normalWS=TransformObjectToWorldNormal(normalize(lerp(v.normalOS,crownNormal,_CrownNormalBlend)));
   o.uv=TRANSFORM_TEX(v.uv,_BaseMap); return o;
  }
  half4 Leaf(Varyings i) { half4 c=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor; clip(c.a-_Cutoff); return c; }
  half4 Frag(Varyings i):SV_Target
  {
   UNITY_SETUP_INSTANCE_ID(i); half4 c=Leaf(i); half3 n=normalize(i.normalWS);
   InputData lighting=(InputData)0;
   lighting.positionWS=i.positionWS; lighting.normalWS=n;
   lighting.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
   lighting.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
   lighting.bakedGI=SampleSH(n)*_SkyFill;
   lighting.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
   lighting.shadowMask=half4(1,1,1,1);
   SurfaceData surface=(SurfaceData)0;
   surface.albedo=c.rgb; surface.alpha=1; surface.smoothness=.08; surface.occlusion=1; surface.normalTS=half3(0,0,1);
   half4 result=UniversalFragmentPBR(lighting,surface);
   Light sun=GetMainLight(lighting.shadowCoord);
   half transmission=saturate(dot(-n,sun.direction)+.35)*_Transmission*lerp(.5,1,sun.shadowAttenuation);
   result.rgb+=c.rgb*sun.color*(transmission+_ShadowFill*(1-sun.shadowAttenuation));
   return half4(result.rgb,1);
  }
  half4 ClipOnly(Varyings i):SV_Target { UNITY_SETUP_INSTANCE_ID(i); Leaf(i); return 0; }
  ENDHLSL
  Pass
  {
   Name "Forward" Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile_instancing
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   ENDHLSL
  }
  Pass
  {
   Name "ShadowCaster" Tags { "LightMode"="ShadowCaster" } ZWrite On ColorMask 0
   HLSLPROGRAM
   #pragma vertex ShadowVert
   #pragma fragment ClipOnly
   #pragma multi_compile_instancing
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   float3 _LightDirection, _LightPosition;
   Varyings ShadowVert(Attributes v)
   {
    Varyings o=Vert(v); UNITY_SETUP_INSTANCE_ID(v);
    float3 lightDirection=_LightDirection;
    #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
     lightDirection=normalize(_LightPosition-o.positionWS);
    #endif
    o.positionCS=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(o.positionWS,TransformObjectToWorldNormal(v.normalOS),lightDirection)));
    return o;
   }
   ENDHLSL
  }
  Pass
  {
   Name "DepthOnly" Tags { "LightMode"="DepthOnly" } ZWrite On ColorMask R
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment ClipOnly
   #pragma multi_compile_instancing
   ENDHLSL
  }
 }
}
