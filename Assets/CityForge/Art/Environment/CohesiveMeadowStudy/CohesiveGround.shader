Shader "CityForge/Cohesive Meadow Ground" {
Properties { _T0("Patch texture 0",2D)="white"{} _T1("Patch texture 1",2D)="white"{} _T2("Patch texture 2",2D)="white"{} _T3("Patch texture 3",2D)="white"{} _T4("Patch texture 4",2D)="white"{} _T5("Patch texture 5",2D)="white"{} _T6("Patch texture 6",2D)="white"{} _T7("Patch texture 7",2D)="white"{} _T8("Patch texture 8",2D)="white"{} _T9("Patch texture 9",2D)="white"{} _T10("Patch texture 10",2D)="white"{} _Weights0("Patch weights 0",2D)="black"{} _Weights1("Patch weights 1",2D)="black"{} _Weights2("Patch weights 2",2D)="black"{} _Grass("Meadow",2D)="white"{} _Forest("Leaf litter",2D)="white"{} _Dirt("Dirt",2D)="white"{} _Coverage("Coverage",2D)="black"{} _Variation("Dry soil and stone masks",2D)="black"{} _Shore("Damp bank, gravel and lakebed",2D)="black"{} _DryGrass("Dry meadow",2D)="white"{} _Stone("Stone",2D)="white"{} _BaseMap("Shadow fallback",2D)="white"{} _BaseColor("Shadow tint",Color)=(1,1,1,1) _Cutoff("Cutoff",Float)=0.5 _SurfaceScale("Surface brightness",Float)=1 }
SubShader { Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" } Pass {
Tags { "LightMode"="UniversalForward" }
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
TEXTURE2D(_Grass);SAMPLER(sampler_linear_repeat);TEXTURE2D(_Forest);SAMPLER(sampler_Forest);TEXTURE2D(_Dirt);SAMPLER(sampler_Dirt);TEXTURE2D(_Coverage);SAMPLER(sampler_Coverage);
TEXTURE2D(_Variation);SAMPLER(sampler_Variation);TEXTURE2D(_DryGrass);SAMPLER(sampler_DryGrass);TEXTURE2D(_Stone);SAMPLER(sampler_Stone);
TEXTURE2D(_Shore);SAMPLER(sampler_Shore);
TEXTURE2D(_T0);
TEXTURE2D(_T1);
TEXTURE2D(_T2);
TEXTURE2D(_T3);
TEXTURE2D(_T4);
TEXTURE2D(_T5);
TEXTURE2D(_T6);
TEXTURE2D(_T7);
TEXTURE2D(_T8);
TEXTURE2D(_T9);
TEXTURE2D(_T10);
TEXTURE2D(_Weights0);
TEXTURE2D(_Weights1);
TEXTURE2D(_Weights2);
float _SurfaceScale;
struct A {float4 p:POSITION;float3 n:NORMAL;};struct V {float4 p:SV_POSITION;float3 ws:TEXCOORD0;float3 n:TEXCOORD1;};
V vert(A i){V o;o.ws=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.ws);o.n=TransformObjectToWorldNormal(i.n);return o;}
float2 hash(float2 p){return frac(sin(float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3))))*43758.5453);}
float noise(float2 p){float2 c=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(c).x,hash(c+float2(1,0)).x,f.x),lerp(hash(c+float2(0,1)).x,hash(c+1).x,f.x),f.y);}
float2 sampleUV(float2 p,float2 cell){float2 r=hash(cell);float angle=r.x*6.28318;float2 q=p-cell;return float2(q.x*cos(angle)-q.y*sin(angle),q.x*sin(angle)+q.y*cos(angle))+r*17;}
// Crossfade independently rotated and offset patches in world space.
half3 stochastic(TEXTURE2D_PARAM(tex,smp),float2 p){float2 c=floor(p),f=frac(p);f=smoothstep(0,1,f);half3 a=SAMPLE_TEXTURE2D(tex,smp,sampleUV(p,c)).rgb;half3 b=SAMPLE_TEXTURE2D(tex,smp,sampleUV(p,c+float2(1,0))).rgb;half3 d=SAMPLE_TEXTURE2D(tex,smp,sampleUV(p,c+float2(0,1))).rgb;half3 e=SAMPLE_TEXTURE2D(tex,smp,sampleUV(p,c+1)).rgb;return lerp(lerp(a,b,f.x),lerp(d,e,f.x),f.y);}
half4 frag(V i):SV_Target {float2 p=i.ws.xz;half3 mask=SAMPLE_TEXTURE2D(_Coverage,sampler_Coverage,p/256).rgb;float broad=noise(p/43+11);float fine=noise(p/9+7);half3 grassTex=stochastic(TEXTURE2D_ARGS(_Grass,sampler_linear_repeat),p/10);half grassDetail=dot(grassTex,half3(.2126,.7152,.0722));half3 meadow=lerp(half3(.17,.245,.09),half3(.265,.33,.14),broad);meadow*=.72+grassDetail*1.3;meadow=grassTex*(.48+broad*.13);
half3 litterTex=stochastic(TEXTURE2D_ARGS(_Forest,sampler_Forest),p/7);half luminance=dot(litterTex,half3(.2126,.7152,.0722));half3 litter=lerp(half3(.12,.125,.065),half3(.235,.21,.125),broad)*(.6+luminance*1.65);half3 dirtTex=stochastic(TEXTURE2D_ARGS(_Dirt,sampler_Dirt),p/5);half dirtLum=dot(dirtTex,half3(.2126,.7152,.0722));half3 dirt=lerp(half3(.27,.225,.155),half3(.35,.30,.215),fine)*(.7+dirtLum*1.3);half3 variation=SAMPLE_TEXTURE2D(_Variation,sampler_Variation,p/256).rgb;
half3 dryTex=stochastic(TEXTURE2D_ARGS(_DryGrass,sampler_DryGrass),p/8);half dryLum=dot(dryTex,half3(.2126,.7152,.0722));
half3 dry=lerp(half3(.23,.245,.125),half3(.30,.285,.17),broad)*(.72+dryLum*1.1);
meadow=lerp(meadow,dry,variation.r);
half4 w0=SAMPLE_TEXTURE2D(_Weights0,sampler_Coverage,p/256);half4 w1=SAMPLE_TEXTURE2D(_Weights1,sampler_Coverage,p/256);half4 w2=SAMPLE_TEXTURE2D(_Weights2,sampler_Coverage,p/256);half3 patch=0;
patch+=stochastic(TEXTURE2D_ARGS(_T0,sampler_linear_repeat),p/6)*w0.r*0.55*half3(0.84,1.04,0.77);
patch+=stochastic(TEXTURE2D_ARGS(_T1,sampler_linear_repeat),p/7)*w0.g*0.55*half3(0.85,1.12,0.74);
patch+=stochastic(TEXTURE2D_ARGS(_T2,sampler_linear_repeat),p/5)*w0.b*0.55*half3(0.76,1.05,0.76);
patch+=stochastic(TEXTURE2D_ARGS(_T3,sampler_linear_repeat),p/8)*w0.a*0.55*half3(0.65,0.84,0.66);
patch+=stochastic(TEXTURE2D_ARGS(_T4,sampler_linear_repeat),p/6)*w1.r*0.55*half3(0.84,1.03,0.75);
patch+=stochastic(TEXTURE2D_ARGS(_T5,sampler_linear_repeat),p/5)*w1.g*0.55*half3(0.8,1.04,0.77);
patch+=stochastic(TEXTURE2D_ARGS(_T6,sampler_linear_repeat),p/9)*w1.b*0.55*half3(0.79,1.08,0.72);
patch+=stochastic(TEXTURE2D_ARGS(_T7,sampler_linear_repeat),p/6)*w1.a*0.55*half3(0.72,0.98,0.68);
patch+=stochastic(TEXTURE2D_ARGS(_T8,sampler_linear_repeat),p/7)*w2.r*0.55*half3(0.64,0.84,0.64);
patch+=stochastic(TEXTURE2D_ARGS(_T9,sampler_linear_repeat),p/4)*w2.g*0.55*half3(0.83,1.06,0.74);
patch+=stochastic(TEXTURE2D_ARGS(_T10,sampler_linear_repeat),p/5)*w2.b*0.42*half3(1,1,1);
patch=lerp(patch,half3(.19,.27,.105),.28);litter=litterTex*.30;dirt=dirtTex*.42;patch*=lerp(1.0,.78,mask.r);half3 color=lerp(patch,litter,mask.r*.43);color=lerp(color,dirt,variation.g*.65);
half3 stoneTex=stochastic(TEXTURE2D_ARGS(_Stone,sampler_Stone),p/4);half stoneLum=dot(stoneTex,half3(.2126,.7152,.0722));
half3 stone=lerp(half3(.26,.27,.245),half3(.39,.39,.345),fine)*(.72+stoneLum*.7);color=lerp(color,stone,variation.b);color=lerp(color,dirt,mask.b*.42);color=lerp(color,dirt,mask.g);
half3 shore=SAMPLE_TEXTURE2D(_Shore,sampler_Shore,p/256).rgb;
half3 gravel=lerp(half3(.26,.25,.20),half3(.36,.335,.255),fine)*(.78+stoneLum*.5);
half3 damp=lerp(half3(.11,.105,.075),half3(.18,.17,.115),fine)*(.8+dirtLum*.65);
gravel=stoneTex*.32;damp=dirtTex*.18;float bankBreak=.35+.65*smoothstep(.25,.75,noise(p/4));color=lerp(color,gravel,shore.g*.45*bankBreak);color=lerp(color,damp,shore.r*.32*bankBreak);color=lerp(color,gravel*.73,shore.b);
InputData input=(InputData)0;input.positionWS=i.ws;input.normalWS=normalize(i.n);input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.ws);input.shadowCoord=TransformWorldToShadowCoord(i.ws);input.bakedGI=SampleSH(input.normalWS);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);SurfaceData surface=(SurfaceData)0;surface.albedo=color*_SurfaceScale;surface.normalTS=half3(0,0,1);surface.smoothness=.07;surface.occlusion=1;surface.alpha=1;return UniversalFragmentPBR(input,surface);}
ENDHLSL }
UsePass "Universal Render Pipeline/Lit/ShadowCaster"
} }
