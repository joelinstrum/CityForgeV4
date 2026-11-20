Shader "CityForge/Stream Waterfall"
{
 Properties { _PreviewTime("Preview time (-1 realtime)",Float)=-1 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+5"}
  Pass {
   Blend One OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float _PreviewTime;
   CBUFFER_END
   struct A{float4 position:POSITION;float2 uv:TEXCOORD0;float2 kind:TEXCOORD1;};
   struct V{float4 position:SV_POSITION;float2 uv:TEXCOORD0;float kind:TEXCOORD1;};
   V Vert(A i){V o;o.position=TransformObjectToHClip(i.position.xyz);o.uv=i.uv;o.kind=i.kind.x;return o;}
   float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float Noise(float2 p){float2 q=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(q),Hash(q+float2(1,0)),f.x),lerp(Hash(q+float2(0,1)),Hash(q+1),f.x),f.y);}
   half4 Frag(V i):SV_Target{
    float seconds=_PreviewTime>=0?_PreviewTime:_Time.y;
    float edge=smoothstep(0,.09,i.uv.x)*smoothstep(0,.09,1-i.uv.x);
    float grain=Noise(float2(i.uv.x*35,i.uv.y*1.7-seconds*4))*.65+Noise(float2(i.uv.x*71,i.uv.y*3.1-seconds*7))*.35;
    float alpha=edge*(.48+grain*.42);
    half3 color=lerp(half3(.28,.48,.49),half3(.86,.94,.91),grain);
    if(i.kind>.5&&i.kind<1.5){
     float2 p=(i.uv-.5)*2;float radius=length(p);
     float foam=Noise(p*7-seconds*.8);
     alpha=(1-smoothstep(.35,1,radius))*(.22+foam*.48);color=half3(.73,.86,.83);
    }else if(i.kind>1.5){alpha=edge*.65;color=lerp(half3(.12,.29,.29),half3(.59,.76,.73),grain*.65);}
    return half4(color*alpha,alpha);
   }
   ENDHLSL
  }
 }
}
