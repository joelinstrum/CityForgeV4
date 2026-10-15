Shader "CityForge/Region Water"
{
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+10" }
 Pass {
 Tags { "LightMode"="UniversalForward" }
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #pragma multi_compile_fog
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 float _RegionSide;
TEXTURE2D(_RegionField); SAMPLER(sampler_RegionField);
 struct Attributes { float4 positionOS:POSITION; };
 struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float fog:TEXCOORD1; };
 Varyings Vert(Attributes i) { Varyings o; o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.fog=ComputeFogFactor(o.positionCS.z);return o; }
 half4 Frag(Varyings i):SV_Target {
 float4 field=SAMPLE_TEXTURE2D(_RegionField,sampler_RegionField,(i.world.xz+_RegionSide*.5)/_RegionSide);
 float depth=max(0,12-field.a*600);
 float offshore=saturate((.5-field.g)*4);
 float deep=saturate(smoothstep(0,12,depth)+offshore*.45);
 float3 base=lerp(float3(.16,.32,.31),float3(.018,.067,.096),deep);
 float2 p=i.world.xz;
 float t=_Time.y;
 float3 n=normalize(float3(.022*sin(p.x/85+p.y/140+t*.19)+.01*sin(p.y/33-t*.12),1,.018*cos(p.y/110-p.x/170+t*.15)));
 float3 v=GetWorldSpaceNormalizeViewDir(i.world);
 float fresnel=.025+.975*pow(1-saturate(dot(n,v)),5);
 float3 reflected=reflect(-v,n);
 float3 sky=lerp(float3(.23,.32,.38),float3(.39,.48,.56),saturate(reflected.y));
 Light sun=GetMainLight();
 float spec=pow(saturate(dot(n,normalize(v+sun.direction))),280)*.06;
 float foam=(1-smoothstep(.2,1.5,depth))*.09*(.6+.4*sin(p.x/70+p.y/80+t*.3));
 float3 color=lerp(base,sky,fresnel*.7)+sun.color*spec+foam;
 return half4(MixFog(color,i.fog),1);
 }
 ENDHLSL
 }
 }
}
