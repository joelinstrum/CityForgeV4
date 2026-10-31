Shader "CityForge/Regional Relief"
{
 Properties { _BaseMap("Relief atlas",2D)="white"{} _Grid("District grid",Float)=8 _DistrictIds("District groups",2D)="black"{} _Grouped("Grouped",Float)=0 }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" } Pass {
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A {float4 positionOS:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
 struct V {float4 positionCS:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
 CBUFFER_START(UnityPerMaterial)
 float _Grid,_Grouped;
 CBUFFER_END
 TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
 TEXTURE2D(_DistrictIds); SAMPLER(sampler_DistrictIds);
 V Vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.color=i.color;o.uv=i.uv;return o;}
 half4 Frag(V i):SV_Target {
 float2 q=i.uv*_Grid,cell=floor(min(q,_Grid-.001)),f=frac(q),edge=min(f,1-f),width=max(fwidth(q),.0001);
 float2 lines=1-saturate(edge/width);half3 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
 if(_Grouped<.5)return half4(lerp(color,half3(.86,.85,.74),max(lines.x,lines.y)*.32),1);
 float2 center=(cell+.5)/_Grid;float4 a=SAMPLE_TEXTURE2D(_DistrictIds,sampler_DistrictIds,center);
 float2 sx=float2(f.x<.5?-1:1,0),sy=float2(0,f.y<.5?-1:1);
 float4 b=SAMPLE_TEXTURE2D(_DistrictIds,sampler_DistrictIds,center+sx/_Grid),c=SAMPLE_TEXTURE2D(_DistrictIds,sampler_DistrictIds,center+sy/_Grid);
 float outX=(cell.x+sx.x<0||cell.x+sx.x>=_Grid)?1:0,outY=(cell.y+sy.y<0||cell.y+sy.y>=_Grid)?1:0;
 float2 boundary=float2(abs(a.r-b.r)>.001||outX>0,abs(a.r-c.r)>.001||outY>0);
 // Selected cells form one outline even before Apply; applied groups share their stable ID.
 float2 selectedBoundary=float2(abs(a.g-b.g)>.5||(outX>0&&a.g>.5),abs(a.g-c.g)>.5||(outY>0&&a.g>.5));
 if(a.g>.5){if(b.g>.5)boundary.x=0;if(c.g>.5)boundary.y=0;}
 float grid=max(lines.x*boundary.x,lines.y*boundary.y);color=lerp(color,half3(.86,.85,.74),grid*.32);
 float2 goldLines=1-saturate(edge/(width*2.3));
 float gold=max(goldLines.x*selectedBoundary.x,goldLines.y*selectedBoundary.y);
 return half4(lerp(color,half3(1,.70,.16),gold*.98),1);
 }
 ENDHLSL
 } }
}
