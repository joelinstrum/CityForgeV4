Shader "CityForge/Calm Lake Water"
{
 Properties {
  _ReflectionMip("Reflection softness",Float)=0.25
  _ReflectionStrength("Reflection visibility",Float)=0.20
  _NormalDetail("Static normal detail",Float)=0.008
  _DepthMask("Cached depth and footprint",2D)="black"{}
  _Environment("One-time local environment",Cube)=""{}
  _Deep("Deep teal scattering",Color)=(.035,.15,.18,1)
  _Shallow("Shallow teal scattering",Color)=(.13,.28,.25,1)
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
  Pass {
   Tags {"LightMode"="UniversalForward"}
   Blend One OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
   TEXTURE2D(_DepthMask); SAMPLER(sampler_DepthMask);
   TEXTURECUBE(_Environment); SAMPLER(sampler_Environment);
   CBUFFER_START(UnityPerMaterial)
   float4 _DepthRegion, _EnvironmentHDR, _CapturePosition, _CaptureBoxMin, _CaptureBoxMax; half4 _Deep, _Shallow; float _EnvironmentReady,_ReflectionMip,_ReflectionStrength,_NormalDetail;
   CBUFFER_END
   struct A {float4 position:POSITION;};
   struct V {float4 position:SV_POSITION;float3 world:TEXCOORD0;};
   V Vert(A i){V o;o.world=TransformObjectToWorld(i.position.xyz);o.position=TransformWorldToHClip(o.world);return o;}
   float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float Noise(float2 p){float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(q),Hash(q+float2(1,0)),f.x),lerp(Hash(q+float2(0,1)),Hash(q+1),f.x),f.y);}
   half4 Frag(V i):SV_Target {
    float2 uv=(i.world.xz-_DepthRegion.xy)*_DepthRegion.zw;
    float3 field=SAMPLE_TEXTURE2D(_DepthMask,sampler_DepthMask,uv).rgb;
    clip(field.y-.5);float depth=max(0,field.x);clip(depth-.008);
    float2 p=i.world.xz*.7;
    float3 normal=normalize(float3((Noise(p)-.5)*_NormalDetail,1,(Noise(p+31)-.5)*_NormalDetail));
    float3 view=GetWorldSpaceNormalizeViewDir(i.world);
    float cosine=max(.15,saturate(dot(view,normal)));
    float fresnel=.025+.975*pow(1-cosine,5);
    float path=depth/cosine*max(.5,field.b);
    float absorption=1-exp(-path*.42);
    half3 scattering=lerp(_Shallow.rgb,_Deep.rgb,1-exp(-depth*.6))*(.992+.016*Noise(i.world.xz*1.8));
    float3 direction=reflect(-view,normal);
    // Approximate finite capture volume: orthographic view directions alone are constant across a flat lake.
    float3 safeDirection=(step(0,direction)*2-1)*max(abs(direction),.0001);
    float3 distances=(lerp(_CaptureBoxMin.xyz,_CaptureBoxMax.xyz,step(0,direction))-i.world)/safeDirection;
    float travel=max(0,min(distances.x,min(distances.y,distances.z)));
    direction=i.world+direction*travel-_CapturePosition.xyz;
    half3 environment=half3(.19,.28,.32);
    if(_EnvironmentReady>.5) environment=DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(_Environment,sampler_Environment,direction,_ReflectionMip),_EnvironmentHDR);
    // Premultiplied absorption leaves the real bed visible; Fresnel adds soft, approximate environment reflection.
    float reflectionWeight=saturate(fresnel+_ReflectionStrength)*smoothstep(0,.25,depth);
    float edge=smoothstep(0,.12,depth);
    float alpha=(absorption+(1-absorption)*reflectionWeight)*edge;
    half3 color=(scattering*absorption*(1-reflectionWeight)+environment*reflectionWeight)*edge;
    return half4(color,alpha);
   }
   ENDHLSL
  }
 }
}
