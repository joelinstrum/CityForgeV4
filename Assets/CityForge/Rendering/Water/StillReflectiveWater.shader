Shader "CityForge/Still Reflective Water"
{
 Properties {
  _DepthMask("Depth and footprint",2D)="black"{}
  _Reflection("Planar reflection",2D)="gray"{}
  _Shallow("Shallow tint",Color)=(.12,.28,.29,1)
  _Deep("Deep tint",Color)=(.025,.105,.16,1)
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
  Pass {
   Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_DepthMask); SAMPLER(sampler_DepthMask);
   TEXTURE2D(_Reflection); SAMPLER(sampler_Reflection);
   float4x4 _ReflectionVP;float4 _RegionOriginScale;half4 _Shallow,_Deep;
   struct A {float4 position:POSITION;};
   struct V {float4 position:SV_POSITION;float3 world:TEXCOORD0;};
   V Vert(A i){V o;o.world=TransformObjectToWorld(i.position.xyz);o.position=TransformWorldToHClip(o.world);return o;}
   half4 Frag(V i):SV_Target {
    float2 mask=SAMPLE_TEXTURE2D(_DepthMask,sampler_DepthMask,(i.world.xz-_RegionOriginScale.xy)*_RegionOriginScale.z).rg;
    clip(mask.y-.5);float depth=max(0,mask.x);clip(depth-.005);
    float3 view=GetWorldSpaceNormalizeViewDir(i.world);
    float fresnel=.04+.96*pow(1-saturate(view.y),5);
    float4 reflected=mul(_ReflectionVP,float4(i.world,1));
    float2 uv=reflected.xy/reflected.w*.5+.5;
    half3 reflection=SAMPLE_TEXTURE2D(_Reflection,sampler_Reflection,uv).rgb;
    half3 tint=lerp(_Shallow.rgb,_Deep.rgb,smoothstep(.15,4,depth));
    half3 color=lerp(tint,reflection*half3(.8,.9,1),saturate(.48+fresnel*.45));
    float alpha=lerp(.4,.97,smoothstep(.1,2,depth))*smoothstep(0,.35,depth);
    return half4(color,alpha);
   }
   ENDHLSL
  }
 }
}
