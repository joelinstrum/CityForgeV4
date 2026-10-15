#ifndef CITYFORGE_REGION_SURFACE
#define CITYFORGE_REGION_SURFACE
TEXTURE2D_ARRAY(_RegionAlbedos); SAMPLER(sampler_RegionAlbedos);
TEXTURE2D_ARRAY(_RegionNormals); SAMPLER(sampler_RegionNormals);
TEXTURE2D_ARRAY(_RegionMasks); SAMPLER(sampler_RegionMasks);
TEXTURE2D(_MeadowDetail); SAMPLER(sampler_MeadowDetail);
TEXTURE2D(_MeadowDetailNormal); SAMPLER(sampler_MeadowDetailNormal);
TEXTURE2D(_MeadowCoverField); SAMPLER(sampler_MeadowCoverField);
TEXTURE2D(_WoodlandCoverField); SAMPLER(sampler_WoodlandCoverField);
float _RegionSide;
float _RegionMeadowDetailEnabled;
TEXTURE2D(_RegionField); SAMPLER(sampler_RegionField);
float cfhash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
float cfnoise(float2 p) {
 float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
 return lerp(lerp(cfhash(i),cfhash(i+float2(1,0)),f.x),lerp(cfhash(i+float2(0,1)),cfhash(i+1),f.x),f.y);
}
float3 cfcover(float2 p, float layer) {
 // Physical ground texture scale, independent of the selected region size.
 // Rotated, differently sized samples break repetition; mipmaps filter distant detail.
 float2 a=p/24.0;
 float2 b=mul(float2x2(.8,-.6,.6,.8),p)/41.0+float2(.31,.67);
 float2 c=mul(float2x2(.36,.93,-.93,.36),p)/173.0+float2(.72,.18);
 return SAMPLE_TEXTURE2D_ARRAY(_RegionAlbedos,sampler_RegionAlbedos,a,layer).rgb*.55
       +SAMPLE_TEXTURE2D_ARRAY(_RegionAlbedos,sampler_RegionAlbedos,b,layer).rgb*.30
       +SAMPLE_TEXTURE2D_ARRAY(_RegionAlbedos,sampler_RegionAlbedos,c,layer).rgb*.15;
}
void RegionSurface_float(float3 WorldPosition, float3 WorldNormal, out float3 Albedo,
 out float3 NormalTS, out float Smoothness, out float AO) {
 float2 p=WorldPosition.xz;
 float2 uv=(p+_RegionSide*.5)/_RegionSide;
 float4 field=SAMPLE_TEXTURE2D(_RegionField,sampler_RegionField,uv);
 // Existing geographic bank/coast fields retain their source-scale encoding.
 float distance=field.r*2000.0;
 float moisture=saturate(field.b*.6+.4*(1-smoothstep(150,1800,distance)));
 float slope=1-saturate(WorldNormal.y);
 float coastal=1-smoothstep(.39,.61,field.g);
 float beach=(1-smoothstep(18,125,distance))*coastal;
 float wet=(1-smoothstep(30,450,distance))*(1-beach);
 float rock=smoothstep(.0015,.016,slope)*.72;
 float elevation=smoothstep(80,280,WorldPosition.y);
 // Warp patch boundaries, then layer broad meadow coverage and smaller bare spots.
 float2 warp=float2(cfnoise(p/137+19),cfnoise(p/149-31))-.5;
 float2 q=p+warp*74;
 float broad=cfnoise(q/290+8);
 float patches=cfnoise(q/67+24);
 float small=cfnoise(q/14-13);
 float4 meadow=SAMPLE_TEXTURE2D(_MeadowCoverField,sampler_MeadowCoverField,uv);
 // Coverage follows a few continuous vegetation bands, not independent color-cloud noise.
 float dry=meadow.b*.74*(1-wet*.7);
 float bare=meadow.a*.75*(1-wet*.5);
 float2 woodland=SAMPLE_TEXTURE2D(_WoodlandCoverField,sampler_WoodlandCoverField,uv).rg;
 float forest=woodland.r*.48*(1-rock)*(1-wet);
 float fertile=saturate(bare+moisture*.035+wet*.70);
 float w[6];w[0]=1;w[1]=dry;w[2]=fertile;w[3]=forest;
 w[4]=saturate(rock+elevation*.08);w[5]=beach;
 float total=0;for(int i=1;i<6;i++)total+=w[i];
 w[0]=max(.06,1-total);total+=w[0];
 Albedo=0;Smoothness=0;AO=0;NormalTS=0;
 for(int j=0;j<6;j++) {
   float weight=w[j]/total;
   float3 cover=cfcover(p,j);
   // Muted meadow greens and straw; soil/rock/coast retain their material colors.
   if(j==0) cover*=float3(.84,.98,.88);
   if(j==1) cover*=float3(.96,.95,.86);
   Albedo+=cover*weight;
   float4 mask=SAMPLE_TEXTURE2D_ARRAY(_RegionMasks,sampler_RegionMasks,p/24.0,j);
   AO+=mask.g*weight;Smoothness+=mask.a*weight;
   float3 n=SAMPLE_TEXTURE2D_ARRAY(_RegionNormals,sampler_RegionNormals,p/24.0,j).xyz*2-1;
   NormalTS+=n*weight;
 }
 // Gentle changes in cover tone, rather than dark patches suggesting fake elevation.
 float tone=lerp(.985,1.015,broad)*lerp(.99,1.01,patches);
 Albedo*=lerp(tone,1.0,saturate(beach+rock));
 // Fine normal detail fades as its footprint becomes smaller than a screen pixel.
 float footprint=max(length(ddx(p)),length(ddy(p)));
 NormalTS.xy*=.14*(1-smoothstep(.5,3.5,footprint));
 // Rich turf lives at a few meters per repeat; the regional layer supplies distant cover.
 float detailWeight=1-smoothstep(.18,.85,footprint);
 UNITY_BRANCH if (_RegionMeadowDetailEnabled > .5)
 {
 float2 grassUV=p/8.5;
 float2 grassUV2=mul(float2x2(.8,-.6,.6,.8),p)/13.7+float2(.37,.61);
 float3 turf=SAMPLE_TEXTURE2D(_MeadowDetail,sampler_MeadowDetail,grassUV).rgb*.72
             +SAMPLE_TEXTURE2D(_MeadowDetail,sampler_MeadowDetail,grassUV2).rgb*.28;
 turf*=lerp(.96,1.03,cfnoise(p/2.7+4));
 float grassCoverage=saturate((w[0]+w[1]*.45+w[3]*.5)/total);
 float3 greenTurf=turf*float3(1.02,1.18,1.25);
 // Distinct surface structure: compact clover turf, coarser mixed meadow, straw and soil.
 float3 roughDetail=SAMPLE_TEXTURE2D(_MeadowDetail,sampler_MeadowDetail,
       mul(float2x2(.36,.93,-.93,.36),p)/17.0).rgb*float3(.99,1.15,1.19);
 roughDetail=lerp(roughDetail,cfcover(p*.65,3)*float3(.95,1.10,.92),.06);
 float3 dryDetail=SAMPLE_TEXTURE2D_ARRAY(_RegionAlbedos,sampler_RegionAlbedos,p/9.0,1).rgb;
 dryDetail=lerp(dryDetail,turf*float3(1.25,1.18,.88),.38);
 float3 soilDetail=SAMPLE_TEXTURE2D_ARRAY(_RegionAlbedos,sampler_RegionAlbedos,p/6.0,2).rgb;
 float soilBreak=smoothstep(.34,.66,cfnoise(p/4.3+71));
 soilDetail=lerp(greenTurf*.85,soilDetail,soilBreak*.80);
 float3 vegetation=greenTurf*meadow.r+roughDetail*meadow.g+dryDetail*meadow.b+soilDetail*meadow.a;
 // Canopy litter is anchored to actual trunks; meadow roughness follows the outer edge.
 vegetation=lerp(vegetation,roughDetail,woodland.g*(1-woodland.r)*.30);
 float3 litter=cfcover(p*.9,3)*float3(.94,1.00,.86);
 vegetation=lerp(vegetation,lerp(greenTurf*.78,litter,.35),woodland.r*.65);
 float landCoverage=1-saturate(beach+rock+wet*.7);
 Albedo=lerp(Albedo,vegetation*tone,landCoverage*detailWeight*.88);
 float3 bladeNormal=SAMPLE_TEXTURE2D(_MeadowDetailNormal,sampler_MeadowDetailNormal,grassUV).rgb*2-1;
 NormalTS.xy+=bladeNormal.xy*grassCoverage*detailWeight*lerp(.8,1.2,meadow.g);
 }
 NormalTS=normalize(NormalTS);Smoothness=lerp(Smoothness,.24,wet*.5);
}
#endif
