Shader "Hidden/CityForge/RiverNormalBlend" {
Properties { _MainTex("Source normal",2D)="bump"{} _Blend("Blend",Range(0,1))=1 }
SubShader { Cull Off ZWrite Off ZTest Always
Pass { HLSLPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#include "UnityCG.cginc"
sampler2D _MainTex;float _Blend;
float4 frag(v2f_img input):SV_Target {
 float2 uv=input.uv;
 float2 a=UnpackNormal(tex2D(_MainTex,uv)).xy;
 float2 b=UnpackNormal(tex2D(_MainTex,float2(-uv.y,uv.x)+float2(.371,.613))).xy;b=float2(b.y,-b.x);
 float2 c=UnpackNormal(tex2D(_MainTex,-uv*2+float2(.719,.283))).xy;c=-c;
 float3 weights=float3(1-.55*_Blend,.35*_Blend,.2*_Blend);
 float2 slope=(a*weights.x+b*weights.y+c*weights.z)/sqrt(dot(weights,weights));
 float3 normal=normalize(float3(slope,sqrt(max(.01,1-dot(slope,slope)))));
 return float4(normal*.5+.5,1);
}
ENDHLSL }
} }
