Shader "CityForge/Layered Natural Ground" { Properties { _L0C("Layer 0 C",2D)="white"{} _L0N("Layer 0 N",2D)="bump"{} _L0AO("Layer 0 AO",2D)="white"{} _L0S("Layer 0 S",2D)="white"{} _L1C("Layer 1 C",2D)="white"{} _L1N("Layer 1 N",2D)="bump"{} _L1AO("Layer 1 AO",2D)="white"{} _L1S("Layer 1 S",2D)="white"{} _L2C("Layer 2 C",2D)="white"{} _L2N("Layer 2 N",2D)="bump"{} _L2AO("Layer 2 AO",2D)="white"{} _L2S("Layer 2 S",2D)="white"{}  _Coverage("Coverage",2D)="black"{} _Shore("Shore",2D)="black"{} _SurfaceScale("Brightness",Float)=1 } SubShader {Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"} Pass {Tags {"LightMode"="UniversalForward"} HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
TEXTURE2D(_L0C);TEXTURE2D(_L0N);TEXTURE2D(_L0AO);TEXTURE2D(_L0S);TEXTURE2D(_L1C);TEXTURE2D(_L1N);TEXTURE2D(_L1AO);TEXTURE2D(_L1S);TEXTURE2D(_L2C);TEXTURE2D(_L2N);TEXTURE2D(_L2AO);TEXTURE2D(_L2S); TEXTURE2D(_Coverage);TEXTURE2D(_Shore);SAMPLER(sampler_linear_repeat);SAMPLER(sampler_linear_clamp);float _SurfaceScale;
struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 ws:TEXCOORD0;float3 n:TEXCOORD1;};V vert(A i){V o;o.ws=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.ws);o.n=TransformObjectToWorldNormal(i.n);return o;}
float2 hash(float2 p){return frac(sin(float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3))))*43758.5453);}
float noise(float2 p){float2 c=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(c).x,hash(c+float2(1,0)).x,f.x),lerp(hash(c+float2(0,1)).x,hash(c+1).x,f.x),f.y);}
struct Layer {half3 color;float3 normal;half ao;half smooth;};
Layer sampleLayer(TEXTURE2D_PARAM(col,smp),TEXTURE2D_PARAM(norm,nsmp),TEXTURE2D_PARAM(ao,asmp),TEXTURE2D_PARAM(smooth,ssmp),float2 p){Layer o=(Layer)0;float2 c=floor(p),f=smoothstep(0,1,frac(p));for(int z=0;z<2;z++)for(int x=0;x<2;x++){float2 cell=c+float2(x,z),h=hash(cell);float a=h.x*6.28318,cs=cos(a),sn=sin(a);float2 q=p-cell;float2 uv=float2(q.x*cs-q.y*sn,q.x*sn+q.y*cs)+h*17;float w=(x?f.x:1-f.x)*(z?f.y:1-f.y);o.color+=SAMPLE_TEXTURE2D(col,smp,uv).rgb*w;float3 n=UnpackNormal(SAMPLE_TEXTURE2D(norm,nsmp,uv));float2 rotated=float2(n.x*cs+n.y*sn,-n.x*sn+n.y*cs);o.normal+=float3(rotated.x,n.z,rotated.y)*w;o.ao+=SAMPLE_TEXTURE2D(ao,asmp,uv).r*w;o.smooth+=SAMPLE_TEXTURE2D(smooth,ssmp,uv).r*w;}return o;}
half4 frag(V i):SV_Target{float2 p=i.ws.xz;half3 mask=SAMPLE_TEXTURE2D(_Coverage,sampler_linear_clamp,p/256).rgb;half3 shore=SAMPLE_TEXTURE2D(_Shore,sampler_linear_clamp,p/256).rgb;
Layer g=sampleLayer(TEXTURE2D_ARGS(_L0C,sampler_linear_repeat),TEXTURE2D_ARGS(_L0N,sampler_linear_repeat),TEXTURE2D_ARGS(_L0AO,sampler_linear_repeat),TEXTURE2D_ARGS(_L0S,sampler_linear_repeat),p/3.5);Layer d=sampleLayer(TEXTURE2D_ARGS(_L1C,sampler_linear_repeat),TEXTURE2D_ARGS(_L1N,sampler_linear_repeat),TEXTURE2D_ARGS(_L1AO,sampler_linear_repeat),TEXTURE2D_ARGS(_L1S,sampler_linear_repeat),p/3);Layer l=sampleLayer(TEXTURE2D_ARGS(_L2C,sampler_linear_repeat),TEXTURE2D_ARGS(_L2N,sampler_linear_repeat),TEXTURE2D_ARGS(_L2AO,sampler_linear_repeat),TEXTURE2D_ARGS(_L2S,sampler_linear_repeat),p/2.5);
// Warped broad fields keep meadow variation irregular and gently connected.
float2 warp=float2(noise(p/21+17),noise(p/19+47))-.5;
float2 field=p+warp*12;
float lush=smoothstep(.22,.76,noise(field/23+9));
float spring=smoothstep(.32,.80,noise(field/16+51));
float dry=smoothstep(.52,.83,noise(field/29+81))*(1-lush*.65);
float broken=smoothstep(.59,.84,noise(field/9+31))*smoothstep(.35,.72,noise(field/27+68));
float bank=saturate(shore.r*.85+shore.g*.75);bank*=.55+.45*noise(p/3+7);
float sparse=broken*.38*(1-mask.r*.65)*(1-shore.b);
float dirt=saturate(max(mask.g,bank)+mask.b*.23+sparse+dry*.10);
float litter=mask.r*smoothstep(.12,.70,noise(field/7+12))*.70;
half3 meadowTint=lerp(half3(.91,1.02,.86),half3(.64,.79,.69),lush*.85);
meadowTint=lerp(meadowTint,half3(.92,1.09,.84),spring*.45*(1-lush*.5));
meadowTint=lerp(meadowTint,half3(1.02,1.01,.88),dry*.40);
half3 color=lerp(g.color*.67*meadowTint,l.color*.65,litter);color=lerp(color,d.color*.62,dirt);color*=lerp(.97,1.03,noise(p/37));color*=1-shore.r*.16;
float3 n=normalize(lerp(lerp(g.normal,l.normal,litter),d.normal,dirt));float3 terrainNormal=normalize(i.n);n=normalize(terrainNormal+float3(n.x,0,n.z)*.65);half ao=lerp(lerp(g.ao,l.ao,litter),d.ao,dirt);half smoothness=lerp(lerp(g.smooth,l.smooth,litter),d.smooth,dirt)*.12;
InputData input=(InputData)0;input.positionWS=i.ws;input.normalWS=n;input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.ws);input.shadowCoord=TransformWorldToShadowCoord(i.ws);input.bakedGI=SampleSH(n);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);SurfaceData s=(SurfaceData)0;s.albedo=color*_SurfaceScale;s.normalTS=half3(0,0,1);s.occlusion=lerp(.65,1,ao);s.smoothness=smoothness;s.alpha=1;return UniversalFragmentPBR(input,s);}
ENDHLSL } UsePass "Universal Render Pipeline/Lit/ShadowCaster" } }