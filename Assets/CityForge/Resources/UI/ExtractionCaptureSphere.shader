Shader "CityForge/UI/Extraction Capture Sphere"
{
 Properties { _Color("Capture color",Color)=(.1,.9,.3,.16) _Fill("Fill",Float)=.5 _Rim("Rim",Float)=.5 _Viewer("Viewer",Vector)=(0,0,0,0) }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+40" "RenderType"="Transparent" }
  Pass {
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off ZTest LEqual Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
   struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;};
   CBUFFER_START(UnityPerMaterial)
   float4 _Color,_Viewer;float _Fill,_Rim;
   CBUFFER_END
   V Vert(A input){V output;output.world=TransformObjectToWorld(input.positionOS.xyz);output.positionCS=TransformWorldToHClip(output.world);output.normal=TransformObjectToWorldNormal(input.normalOS);return output;}
   half4 Frag(V input):SV_Target{
    clip(.2-distance(_WorldSpaceCameraPos,_Viewer.xyz));
    float rim=pow(1-abs(dot(normalize(input.normal),normalize(_WorldSpaceCameraPos-input.world))),2);
    return half4(_Color.rgb,_Color.a*saturate(_Fill+_Rim*rim));
   }
   ENDHLSL
  }
 }
}
