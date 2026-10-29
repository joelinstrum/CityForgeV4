// Authored CityForge source. Texture/mesh assets are intentionally local.
// Approved refined palette C, October 28, 2026. Configure via ApprovedForestColors.
Shader "CityForge/Trees/Approved Forest Canopy" {
Properties { _BaseMap("Base",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1) _InstanceTint("Instance tint",Color)=(1,1,1,1) _Cutoff("Cutout",Range(0,1))=.4 _Leaf("Leaf",Float)=1 _Smoothness("Smoothness",Range(0,1))=.12 _OcclusionStrength("Geometric ambient occlusion",Range(0,1))=0 _PaletteMix("Palette remap",Float)=0 _CrownRefine("Subtle crown variation",Float)=0 }
SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"} Cull Off
Pass { Name "ForwardLit" Tags {"LightMode"="UniversalForward"}
HLSLPROGRAM
#pragma target 3.5
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile _ LOD_FADE_CROSSFADE
#pragma multi_compile_instancing
#pragma instancing_options assumeuniformscaling lodfade
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
#define _SPECULAR_SETUP 1
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;float4 _BaseMap_ST;float _Cutoff,_Leaf,_Smoothness,_OcclusionStrength,_PaletteMix,_CrownRefine;
CBUFFER_END
UNITY_INSTANCING_BUFFER_START(TreeProps)
UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceTint)
UNITY_DEFINE_INSTANCED_PROP(float4, _PaletteTarget)
UNITY_INSTANCING_BUFFER_END(TreeProps)
struct A {UNITY_VERTEX_INPUT_INSTANCE_ID float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;float4 color:COLOR;};
struct V {UNITY_VERTEX_INPUT_INSTANCE_ID float4 pos:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float2 uv:TEXCOORD2;half4 color:COLOR;};
V vert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_TRANSFER_INSTANCE_ID(a,o);o.world=TransformObjectToWorld(a.positionOS.xyz);o.pos=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(a.normalOS);o.uv=TRANSFORM_TEX(a.uv,_BaseMap);o.color=a.color;return o;}
half4 frag(V i,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target {
UNITY_SETUP_INSTANCE_ID(i);
#if defined(LOD_FADE_CROSSFADE)
LODFadeCrossFade(i.pos);
#endif
 half4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);clip(tex.a-_Cutoff);
 half3 n=normalize(i.normal)*IS_FRONT_VFACE(face,1,-1);half3 albedo=tex.rgb*i.color.rgb*_BaseColor.rgb*UNITY_ACCESS_INSTANCED_PROP(TreeProps,_InstanceTint).rgb;
 if(_Leaf>.5 && _PaletteMix>.001){
 // Local chroma adjustment in linear RGB; project remains Gamma.
 float3 source=SRGBToLinear(max(float3(albedo),0));
 float luminance=dot(source,float3(.2126,.7152,.0722));
 float4 palette=UNITY_ACCESS_INSTANCED_PROP(TreeProps,_PaletteTarget);
 float3 recolored=luminance*palette.rgb;
 if(_CrownRefine>.5){
 // Direction and existing leaf AO vary color within a crown; no noise or geometry changes.
 float recess=saturate((.55-i.color.a)/.30);
 float upper=saturate((n.y-.10)*1.15)*saturate(dot(n,GetMainLight().direction))*(1-recess);
 recolored*=lerp(float3(1,1,1),float3(1.045,1.015,.965),upper);
 float y=dot(recolored,float3(.2126,.7152,.0722));
 float3 neutralGreen=float3(.82,1.10,.70);neutralGreen/=dot(neutralGreen,float3(.2126,.7152,.0722));
 recolored=lerp(recolored,y*neutralGreen,.07*recess)*(1-.015*recess);
 }
 albedo=LinearToSRGB(max(lerp(source,recolored,_PaletteMix)*palette.a,0));
 }
 InputData d=(InputData)0;d.positionWS=i.world;d.normalWS=n;d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.world);d.shadowCoord=TransformWorldToShadowCoord(i.world);d.bakedGI=SampleSH(n);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.pos);d.shadowMask=half4(1,1,1,1);
 SurfaceData s=(SurfaceData)0;s.albedo=albedo;s.metallic=0;s.specular=lerp(half3(.04,.04,.04),half3(.0176,.0176,.0176),_Leaf);s.smoothness=_Smoothness;s.normalTS=half3(0,0,1);s.occlusion=lerp(1,i.color.a,_OcclusionStrength);s.alpha=1;
 half4 c=UniversalFragmentPBR(d,s);Light l=GetMainLight(d.shadowCoord);c.rgb+=albedo*l.color*saturate(dot(-n,l.direction))*.16*_Leaf*l.shadowAttenuation;return c;
}
ENDHLSL
}
Pass { Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"} ZWrite On ZTest LEqual ColorMask 0 Cull Off
HLSLPROGRAM
#pragma target 3.5
#pragma vertex shadowVert
#pragma fragment shadowFrag
#pragma multi_compile _ LOD_FADE_CROSSFADE
#pragma multi_compile_instancing
#pragma instancing_options assumeuniformscaling lodfade
#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;float4 _BaseMap_ST;float _Cutoff,_Leaf,_Smoothness,_OcclusionStrength,_PaletteMix,_CrownRefine;
CBUFFER_END
UNITY_INSTANCING_BUFFER_START(TreeProps)
UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceTint)
UNITY_DEFINE_INSTANCED_PROP(float4, _PaletteTarget)
UNITY_INSTANCING_BUFFER_END(TreeProps)
float3 _LightDirection;float3 _LightPosition;
struct A{UNITY_VERTEX_INPUT_INSTANCE_ID float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
struct V{UNITY_VERTEX_INPUT_INSTANCE_ID float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
V shadowVert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_TRANSFER_INSTANCE_ID(a,o);float3 p=TransformObjectToWorld(a.positionOS.xyz);float3 n=TransformObjectToWorldNormal(a.normalOS);
#ifdef _CASTING_PUNCTUAL_LIGHT_SHADOW
float3 l=normalize(_LightPosition-p);
#else
float3 l=_LightDirection;
#endif
o.pos=TransformWorldToHClip(ApplyShadowBias(p,n,l));
#if UNITY_REVERSED_Z
o.pos.z=min(o.pos.z,UNITY_NEAR_CLIP_VALUE*o.pos.w);
#else
o.pos.z=max(o.pos.z,UNITY_NEAR_CLIP_VALUE*o.pos.w);
#endif
o.uv=TRANSFORM_TEX(a.uv,_BaseMap);return o;}
half4 shadowFrag(V i):SV_Target{
UNITY_SETUP_INSTANCE_ID(i);
#if defined(LOD_FADE_CROSSFADE)
LODFadeCrossFade(i.pos);
#endif
clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a-_Cutoff);return 0;}
ENDHLSL
}
Pass { Name "DepthOnly" Tags {"LightMode"="DepthOnly"} ZWrite On ColorMask R Cull Off
HLSLPROGRAM
#pragma target 3.5
#pragma vertex depthVert
#pragma fragment depthFrag
#pragma multi_compile _ LOD_FADE_CROSSFADE
#pragma multi_compile_instancing
#pragma instancing_options assumeuniformscaling lodfade
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;float4 _BaseMap_ST;float _Cutoff,_Leaf,_Smoothness,_OcclusionStrength,_PaletteMix,_CrownRefine;
CBUFFER_END
UNITY_INSTANCING_BUFFER_START(TreeProps)
UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceTint)
UNITY_DEFINE_INSTANCED_PROP(float4, _PaletteTarget)
UNITY_INSTANCING_BUFFER_END(TreeProps)
struct A{UNITY_VERTEX_INPUT_INSTANCE_ID float4 positionOS:POSITION;float2 uv:TEXCOORD0;};struct V{UNITY_VERTEX_INPUT_INSTANCE_ID float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
V depthVert(A a){V o;UNITY_SETUP_INSTANCE_ID(a);UNITY_TRANSFER_INSTANCE_ID(a,o);o.pos=TransformObjectToHClip(a.positionOS.xyz);o.uv=TRANSFORM_TEX(a.uv,_BaseMap);return o;}
half4 depthFrag(V i):SV_Target{
UNITY_SETUP_INSTANCE_ID(i);
#if defined(LOD_FADE_CROSSFADE)
LODFadeCrossFade(i.pos);
#endif
clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a-_Cutoff);return i.pos.z;}
ENDHLSL
}
} }
