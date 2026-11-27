#ifndef CITYFORGE_WEATHERING_PAVERS
#define CITYFORGE_WEATHERING_PAVERS
// One sample of the existing bluestone image, using only stone-face regions.
// The joint grid is drawn once procedurally; no misaligned masonry images are blended.
void WeatherPaverSurface(float2 world,out SurfaceData surface)
{
    float seed=_WeatherVariation.z+_WeatherMaterialParams.w;
    float row=floor(world.y*_WeatherPavers.z);
    float rowHash=WeatherHash(float2(row,seed+719));
    float width=(1/_WeatherPavers.y)*lerp(.82,1.18,rowHash);
    float height=1/_WeatherPavers.z;
    float2 grid=float2(world.x/width+WeatherHash(float2(row,seed+811)),world.y/height);
    float2 cell=floor(grid),local=frac(grid);
    float h=WeatherHash(cell+float2(seed,31.79));
    float h2=WeatherHash(cell.yx+float2(139.7,seed+53));
    int face=(int)floor(h*5);
    float4 rect=float4(.11,.54,.44,.25);
    if(face==1)rect=float4(.06,.035,.30,.16);
    else if(face==2)rect=float4(.66,.54,.31,.24);
    else if(face==3)rect=float4(.025,.28,.24,.18);
    else if(face==4)rect=float4(.34,.30,.38,.17);
    float2 flip=float2(h2>.5?-1:1,frac(h2*7.13)>.5?-1:1);
    float2 faceLocal=lerp(1-local,local,step(0,flip));
    float crop=lerp(.76,.96,WeatherHash(cell+float2(71,seed)));
    float2 cropOffset=float2(WeatherHash(cell+13.7),WeatherHash(cell+91.3))*(1-crop);
    float2 uv=rect.xy+(faceLocal*crop+cropOffset)*rect.zw;
    float2 dx=ddx(world)*float2(1/width,1/height)*flip*crop*rect.zw;
    float2 dy=ddy(world)*float2(1/width,1/height)*flip*crop*rect.zw;
    float footprint=max(length(ddx(world)),length(ddy(world)));
    float fade=WeatherFeatureFade(footprint,1/min(width,height));
    // At unresolved distances, the highest mip averages the original stone color;
    // the hash-selected face and tone no longer introduce subpixel flicker.
    dx=lerp(float2(1,0),dx,fade);dy=lerp(float2(0,1),dy,fade);
    half3 stone=SAMPLE_TEXTURE2D_GRAD(_BaseMap,sampler_BaseMap,uv,dx,dy).rgb*_BaseColor.rgb;
    float tone=(WeatherHash(cell+float2(seed+173,57.1))*2-1)*_WeatherPaverTone.x*fade;
    float tint=(WeatherHash(cell+float2(13.1,seed+257))*2-1)*_WeatherPaverTone.y*fade;
    stone*=max(.5,1+tone)*half3(1+tint*.35,1+tint*.10,1-tint*.25);
    float2 edges=min(local,1-local)*float2(width,height);
    float aa=max(.001,footprint*.55);
    float joint=1-smoothstep(_WeatherPavers.w*.5-aa*.5,_WeatherPavers.w*.5+aa*.5,min(edges.x,edges.y));
    float bevel=(1-smoothstep(_WeatherPavers.w*.65,_WeatherPavers.w*2.5+aa,min(edges.x,edges.y)))*fade;
    stone*=1-bevel*.13;
    float averageJoint=saturate(_WeatherPavers.w*(_WeatherPavers.y+_WeatherPavers.z));
    joint=lerp(averageJoint,joint,fade);
    surface=(SurfaceData)0;
    surface.albedo=lerp(stone,half3(.035,.032,.027),joint);
    surface.alpha=_BaseColor.a;surface.metallic=_Metallic;surface.specular=0;
    surface.smoothness=lerp(_Smoothness,.025,joint);
    surface.normalTS=half3(0,0,1);surface.occlusion=1;
}
#endif
