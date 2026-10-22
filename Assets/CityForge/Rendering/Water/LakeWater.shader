Shader "CityForge/Calm Lake Water"
{
 Properties {
  [HideInInspector] _DepthRegion("Depth mapping",Vector)=(0,0,1,1)
  [HideInInspector] _EnvironmentHDR("Environment decode",Vector)=(1,1,0,0)
  [HideInInspector] _CapturePosition("Capture position",Vector)=(0,0,0,0)
  [HideInInspector] _CaptureBoxMin("Capture minimum",Vector)=(0,0,0,0)
  [HideInInspector] _CaptureBoxMax("Capture maximum",Vector)=(0,0,0,0)
  [HideInInspector] _EnvironmentReady("Environment ready",Float)=0
  _RiverSurface("Bounded river surface",Float)=0
  _RiverFlow("River current coordinates",2D)="black"{}
  [Normal] _RiverNormal("River water normal",2D)="bump"{}
  _RippleScale("River normal tile metres",Float)=80
  _RippleStrength("River normal strength",Float)=0.65
  _RiverRoughness("River reflection roughness",Float)=0.32
  _SunStrength("Maximum river sun glint",Float)=0.03
  _CurrentSpeed("Lengthwise current speed",Float)=1.1
  _CurrentPreviewTime("Current preview time (-1 realtime)",Float)=-1
  _RiverSceneDepth("River depth texture available",Float)=0
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
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_RiverNormal); SAMPLER(sampler_RiverNormal);
   TEXTURE2D(_RiverFlow); SAMPLER(sampler_RiverFlow);
   TEXTURE2D(_DepthMask); SAMPLER(sampler_DepthMask);
   TEXTURECUBE(_Environment); SAMPLER(sampler_Environment);
   CBUFFER_START(UnityPerMaterial)
   float4 _DepthRegion, _EnvironmentHDR, _CapturePosition, _CaptureBoxMin, _CaptureBoxMax; half4 _Deep, _Shallow; float _EnvironmentReady,_ReflectionMip,_ReflectionStrength,_NormalDetail,_RiverSurface,_CurrentSpeed,_CurrentPreviewTime,_RiverSceneDepth,_RippleScale,_RippleStrength,_RiverRoughness,_SunStrength;
   CBUFFER_END
   struct A {float4 position:POSITION;};
   struct V {float4 position:SV_POSITION;float3 world:TEXCOORD0;};
   V Vert(A i){V o;o.world=TransformObjectToWorld(i.position.xyz);o.position=TransformWorldToHClip(o.world);return o;}
   float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float Noise(float2 p){float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(q),Hash(q+float2(1,0)),f.x),lerp(Hash(q+float2(0,1)),Hash(q+1),f.x),f.y);}
   float2 FlowingNormal(float2 world,float2 downstream,float seconds,float tile){
    // Two overlapping phases advect a mipmapped authored normal field without a reset jump.
    float phase=frac(seconds*_CurrentSpeed*.5/6);
    float other=frac(phase+.5);
    float weight=smoothstep(0,1,abs(phase-.5)*2);
    float2 a=(world-downstream*(phase*6))/tile;
    float2 b=(world-downstream*(other*6))/tile;
    float2 n0=UnpackNormal(SAMPLE_TEXTURE2D(_RiverNormal,sampler_RiverNormal,a)).xy;
    float2 n1=UnpackNormal(SAMPLE_TEXTURE2D(_RiverNormal,sampler_RiverNormal,b)).xy;
    return lerp(n0,n1,weight);
   }
   half4 Frag(V i):SV_Target {
    float2 uv=(i.world.xz-_DepthRegion.xy)*_DepthRegion.zw;
    float3 field=SAMPLE_TEXTURE2D(_DepthMask,sampler_DepthMask,uv).rgb;
    clip(field.y-.5);float depth=max(0,field.x);clip(depth-.008);
    float2 p=i.world.xz*.7;
    float3 normal=normalize(float3((Noise(p)-.5)*_NormalDetail,1,(Noise(p+31)-.5)*_NormalDetail));
    if(_RiverSurface>.5){
     float4 flow=SAMPLE_TEXTURE2D(_RiverFlow,sampler_RiverFlow,uv);
     float seconds=_CurrentPreviewTime>=0?_CurrentPreviewTime:_Time.y;
     float2 downstream=normalize(flow.zw+float2(.00001,0));
     float2 primary=FlowingNormal(i.world.xz,downstream,seconds,max(4,_RippleScale));
     float2 rotated=float2(.9063*i.world.x+.4226*i.world.z,-.4226*i.world.x+.9063*i.world.z);
     float2 rotatedFlow=float2(.9063*downstream.x+.4226*downstream.y,-.4226*downstream.x+.9063*downstream.y);
     float2 secondary=FlowingNormal(rotated+71,rotatedFlow,seconds,max(4,_RippleScale)*2.13);
     secondary=float2(.9063*secondary.x-.4226*secondary.y,.4226*secondary.x+.9063*secondary.y);
     float2 slope=(primary+secondary*.35)*_RippleStrength;
     normal=normalize(float3(slope.x,1,slope.y));
    }
    float3 view=GetWorldSpaceNormalizeViewDir(i.world);
    float cosine=max(.15,saturate(dot(view,normal)));
    float fresnel=.025+.975*pow(1-cosine,5);
    float opticalDepth=depth;
    if(_RiverSurface>.5&&_RiverSceneDepth>.5){
     float2 screenUV=GetNormalizedScreenSpaceUV(i.position.xy);
     float sceneEye=LinearEyeDepth(SampleSceneDepth(screenUV),_ZBufferParams);
     if(unity_OrthoParams.w>.5){
      float raw=SampleSceneDepth(screenUV);
      #if UNITY_REVERSED_Z
       raw=1-raw;
      #endif
      sceneEye=lerp(_ProjectionParams.y,_ProjectionParams.z,raw);
     }
     float surfaceEye=-TransformWorldToView(i.world).z;
     opticalDepth=min(depth,max(0,sceneEye-surfaceEye)*cosine);
    }
    float path=opticalDepth/cosine*max(.5,field.b);
    float absorption=1-exp(-path*.42);
    half3 scattering=lerp(_Shallow.rgb,_Deep.rgb,1-exp(-depth*.6))*(.992+.016*Noise(i.world.xz*1.8));
    float3 direction=reflect(-view,normal);
    // Approximate finite capture volume: orthographic view directions alone are constant across a flat lake.
    float3 skyDirection=direction;
    float3 safeDirection=(step(0,direction)*2-1)*max(abs(direction),.0001);
    float3 distances=(lerp(_CaptureBoxMin.xyz,_CaptureBoxMax.xyz,step(0,direction))-i.world)/safeDirection;
    float travel=max(0,min(distances.x,min(distances.y,distances.z)));
    direction=i.world+direction*travel-_CapturePosition.xyz;
    half3 environment=half3(.19,.28,.32);
    if(_EnvironmentReady>.5) environment=DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(_Environment,sampler_Environment,direction,_ReflectionMip),_EnvironmentHDR);
    if(_RiverSurface>.5)environment=GlossyEnvironmentReflection(skyDirection,i.world,_RiverRoughness,1,GetNormalizedScreenSpaceUV(i.position.xy));
    // Premultiplied absorption leaves the real bed visible; Fresnel adds soft, approximate environment reflection.
    float reflectionWeight=(_RiverSurface>.5?saturate(.025+fresnel*.8):saturate(fresnel+_ReflectionStrength))*smoothstep(0,.25,depth);
    float edge=smoothstep(0,.12,depth);
    float alpha=(absorption+(1-absorption)*reflectionWeight)*edge;
    half3 color=(scattering*absorption*(1-reflectionWeight)+environment*reflectionWeight)*edge;
    if(_RiverSurface>.5){
     Light sun=GetMainLight();
     float3 halfDirection=normalize(view+sun.direction);
     float nh=saturate(dot(normal,halfDirection));float nl=saturate(dot(normal,sun.direction));
     float variance=dot(ddx(normal),ddx(normal))+dot(ddy(normal),ddy(normal));
     float roughness=sqrt(_RiverRoughness*_RiverRoughness+min(.3,variance*4));
     float a2=roughness*roughness*roughness*roughness;
     float denominator=nh*nh*(a2-1)+1;
     float distribution=a2/max(.00001,3.14159265*denominator*denominator);
     float vh=saturate(dot(view,halfDirection));float specularFresnel=.025+.975*pow(1-vh,5);
     float glint=min(_SunStrength,distribution*specularFresnel*.12)*nl;
     color+=sun.color*glint*edge;
    }
    return half4(color,alpha);
   }
   ENDHLSL
  }
 }
}
