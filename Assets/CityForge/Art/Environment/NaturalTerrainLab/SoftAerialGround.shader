Shader "CityForge/Soft Aerial Ground" { Properties { _L0C("Layer 0 C",2D)="white"{} _L0N("Layer 0 N",2D)="bump"{} _L0AO("Layer 0 AO",2D)="white"{} _L0S("Layer 0 S",2D)="white"{} _L1C("Layer 1 C",2D)="white"{} _L1N("Layer 1 N",2D)="bump"{} _L1AO("Layer 1 AO",2D)="white"{} _L1S("Layer 1 S",2D)="white"{} _L2C("Layer 2 C",2D)="white"{} _L2N("Layer 2 N",2D)="bump"{} _L2AO("Layer 2 AO",2D)="white"{} _L2S("Layer 2 S",2D)="white"{}  _Coverage("Coverage",2D)="black"{} _Shore("Shore",2D)="black"{} _SurfaceScale("Brightness",Float)=1 _WaterLevel("Water elevation",Float)=6.8 _ShoreField("Signed water distance",2D)="white"{} _FieldBounds("Field origin and size",Vector)=(0,0,256,256) _ShoreEnabled("Natural shoreline",Float)=1 } 
SubShader {
 Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass {
  Name "NaturalGround" Tags {"LightMode"="UniversalForward"}
  HLSLPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
  #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  TEXTURE2D(_L0C);TEXTURE2D(_L0N);TEXTURE2D(_L0AO);TEXTURE2D(_L0S);
  TEXTURE2D(_L1C);TEXTURE2D(_L1N);TEXTURE2D(_L1AO);TEXTURE2D(_L1S);
  TEXTURE2D(_L2C);TEXTURE2D(_L2N);TEXTURE2D(_L2AO);TEXTURE2D(_L2S);
  TEXTURE2D(_Coverage);TEXTURE2D(_Shore);TEXTURE2D(_ShoreField);
  SAMPLER(sampler_linear_repeat);SAMPLER(sampler_linear_clamp);
  float _SurfaceScale,_WaterLevel,_ShoreEnabled;float4 _FieldBounds;
  struct A {float4 p:POSITION;float3 n:NORMAL;};
  struct V {float4 p:SV_POSITION;float3 ws:TEXCOORD0;float3 n:TEXCOORD1;};
  V vert(A i){V o;o.ws=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.ws);o.n=TransformObjectToWorldNormal(i.n);return o;}
  float2 hash(float2 p){return frac(sin(float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3))))*43758.5453);}
  float noise(float2 p){float2 c=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(c).x,hash(c+float2(1,0)).x,f.x),lerp(hash(c+float2(0,1)).x,hash(c+1).x,f.x),f.y);}
  // Random cell rotation/offset removes recognizable repeats. Explicit derivatives
  // avoid discontinuous random UVs selecting the wrong mip at cell boundaries.
  half3 cover(TEXTURE2D_PARAM(tex,smp),float2 p,float footprint) {
    float2 c=floor(p),f=smoothstep(0,1,frac(p));
    float2 dx=ddx(p),dy=ddy(p);half3 outColor=0;
    for(int z=0;z<2;z++)for(int x=0;x<2;x++){
      float2 cell=c+float2(x,z),h=hash(cell),q=p-cell;
      float a=h.x*6.28318,cs=cos(a),sn=sin(a);
      float2x2 rotation=float2x2(cs,-sn,sn,cs);
      float2 uv=mul(rotation,q)+h*17;
      float w=(x?f.x:1-f.x)*(z?f.y:1-f.y);
      outColor+=SAMPLE_TEXTURE2D_GRAD(tex,smp,uv,mul(rotation,dx)*1.8,mul(rotation,dy)*1.8).rgb*w;
    }
    half3 mean=SAMPLE_TEXTURE2D_LOD(tex,smp,float2(.5,.5),12).rgb;
    return lerp(mean,outColor,(1-smoothstep(.08,.65,footprint))*.58);
  }
  half4 frag(V i):SV_Target {
    float2 p=i.ws.xz;
    float footprint=max(length(ddx(p)),length(ddy(p)));
    float close=1-smoothstep(.08,.38,footprint);
    float2 uv=(p-_FieldBounds.xy)/_FieldBounds.zw;
    half3 mask=SAMPLE_TEXTURE2D(_Coverage,sampler_linear_clamp,uv).rgb;
    float2 warp=float2(noise(p/43+17),noise(p/57+47))-.5;
    float2 field=p+warp*16;
    float broad=smoothstep(.22,.78,noise(field/72+9)*.55+noise(field/28+41)*.45);
    float secondary=noise(field/8+23)*.7+noise(field/3.5+17)*.3;
    float dry=smoothstep(.43,.74,noise(field/34+81));
    half3 grass=cover(TEXTURE2D_ARGS(_L0C,sampler_linear_repeat),p/5.5,footprint);
    half3 dirt=cover(TEXTURE2D_ARGS(_L1C,sampler_linear_repeat),p/4.5,footprint);
    half3 litter=cover(TEXTURE2D_ARGS(_L2C,sampler_linear_repeat),p/5,footprint);
    half3 tint=lerp(half3(.78,.865,.65),half3(.89,.935,.70),broad);
    tint=lerp(tint,half3(1.05,.97,.70),dry*.08);
    half3 turf=grass*.67*tint;
    turf*=lerp(.97,1.03,secondary);
    float bare=smoothstep(.59,.79,noise(field/11+31))*.035*(1-mask.r*.7);
    float soil=max(mask.g+mask.b*.18,bare);
    float elevation=i.ws.y-_WaterLevel;
    float slope=1-saturate(normalize(i.n).y);
    float distance=SAMPLE_TEXTURE2D(_ShoreField,sampler_linear_clamp,uv).r*64-32;
    float moisture=saturate(noise(field/13+66)*.68+noise(field/4+11)*.32);
    float proximity=1-smoothstep(0,7.5,max(0,distance));
    float low=1-smoothstep(.15,1.8,max(0,elevation));
    // Dry/firm sectors keep grass at the edge; wet pockets and slopes expose soil.
    float wetPockets=smoothstep(.36,.63,moisture);
    float shoreSoil=proximity*low*saturate(wetPockets*.9+smoothstep(.012,.12,slope)*.55);
    if(_ShoreEnabled>.5)soil=max(soil,shoreSoil);
    else {
      half3 shore=SAMPLE_TEXTURE2D(_Shore,sampler_linear_clamp,uv).rgb;
      float bank=saturate(shore.r*.85+shore.g*.75)*smoothstep(.30,.70,noise(p/9+7))*(.55+.45*noise(p/3+7));
      float submerged=1-smoothstep(6.65,6.9+noise(p/7)*.4,i.ws.y);
      soil=max(soil,bank);soil=lerp(soil,1,submerged);
    }
    float canopy=mask.r*smoothstep(.12,.70,noise(field/7+12))*.60;
    half3 color=lerp(turf,litter*.61,canopy);
    half3 earth=dirt*lerp(half3(.62,.59,.50),half3(.44,.43,.37),wetPockets*proximity);
    color=lerp(color,earth,saturate(soil));
    // Preserve a dirt lake bed, but don't extend its height threshold onto dry land.
    float bed=1-smoothstep(lerp(-.80,-.32,wetPockets),lerp(-.25,.10,wetPockets),elevation);
    color=lerp(color,dirt*half3(.39,.36,.29)*lerp(.8,1.15,noise(p/2.3)),bed);
    float3 n=normalize(i.n);
    UNITY_BRANCH if(close>.01){
      float3 detail=UnpackNormal(SAMPLE_TEXTURE2D(_L0N,sampler_linear_repeat,p/5.5));
      n=normalize(n+float3(detail.x,0,detail.y)*.25*close*(1-soil));
    }
    InputData input=(InputData)0;
    input.positionWS=i.ws;input.normalWS=n;input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.ws);
    input.shadowCoord=TransformWorldToShadowCoord(i.ws);input.bakedGI=SampleSH(n);
    input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.p);
    SurfaceData surface=(SurfaceData)0;
    surface.albedo=color*_SurfaceScale;surface.normalTS=half3(0,0,1);
    surface.occlusion=1;surface.smoothness=.035;surface.alpha=1;
    return UniversalFragmentPBR(input,surface);
  }
  ENDHLSL
 }
 UsePass "Universal Render Pipeline/Lit/DepthOnly"
 UsePass "Universal Render Pipeline/Lit/ShadowCaster"
}
}
