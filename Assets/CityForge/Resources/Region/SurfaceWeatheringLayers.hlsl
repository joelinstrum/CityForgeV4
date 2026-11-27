#ifndef CITYFORGE_WEATHERING_LAYERS
#define CITYFORGE_WEATHERING_LAYERS
struct WeatherLayers { float micro, microFade, medium, macro, footprint; };
float2 WeatherLayerRotate(float2 p,float2 sinCos)
{ return float2(sinCos.y*p.x-sinCos.x*p.y,sinCos.x*p.x+sinCos.y*p.y); }
float WeatherFeatureFade(float footprint,float inverseScale)
{ return 1-smoothstep(.35,1.25,footprint*inverseScale); }
float WeatherLayerSample(float2 world,float4 layer,float2 offset,float salt)
{
    float seed=_WeatherVariation.z+_WeatherMaterialParams.w;
    return WeatherNoise((WeatherLayerRotate(world,layer.zw)+offset)*layer.y+float2(seed*.071+salt,seed*.133-salt));
}
WeatherLayers WeatherGetLayers(float2 world)
{
    WeatherLayers value;value.micro=value.medium=value.macro=.5;value.microFade=0;value.footprint=0;
    UNITY_BRANCH if(_WeatherMaterialParams.z<.5)return value;
    value.footprint=max(length(ddx(world)),length(ddy(world)));
    if(_WeatherMicro.x>0 || _WeatherMaterialParams.y==1){
        // Fade fine physical features before they become subpixel. No camera/time-based phase.
        value.microFade=WeatherFeatureFade(value.footprint,_WeatherMicro.y);
        if(value.microFade>0)value.micro=WeatherLayerSample(world,_WeatherMicro,_WeatherLayerOffsets.xy,17.71);
    }
    if(_WeatherMedium.x>0 || _WeatherMaterialParams.y==2)
    {float fade=WeatherFeatureFade(value.footprint,_WeatherMedium.y);if(fade>0)value.medium=lerp(.5,WeatherLayerSample(world,_WeatherMedium,_WeatherLayerOffsets.zw,43.19),fade);}
    if(_WeatherMacro.x>0 || _WeatherMaterialParams.y==3)
    {float fade=WeatherFeatureFade(value.footprint,_WeatherMacro.y);if(fade>0)value.macro=lerp(.5,WeatherLayerSample(world,_WeatherMacro,_WeatherMacroParams.xy,89.37),fade);}
    return value;
}
float4 WeatherGutterFields(float2 p,float footprint)
{
    float seed=_WeatherVariation.z+_WeatherMaterialParams.w;
    float4 n=.5;
    float4 fade=1-smoothstep(.35,1.25,footprint*_WeatherGutterFrequencies);
    // Four independent 2D fields sample the actual physical left/right edge positions.
    // They do not reuse one longitudinal pattern or a ribbon side flag that resets at joins.
    if(_WeatherPattern.x>0 && _WeatherGutterVariations.x>0 && fade.x>0)n.x=WeatherNoise(float2(.9205*p.x-.3907*p.y,.3907*p.x+.9205*p.y)*_WeatherGutterFrequencies.x+float2(seed*.193+37.1,19.7));
    if(_WeatherPattern.x>0 && _WeatherGutterVariations.y>0 && fade.y>0)n.y=WeatherNoise(float2(.1219*p.x-.9925*p.y,.9925*p.x+.1219*p.y)*_WeatherGutterFrequencies.y+float2(103.9,seed*.271+61.3));
    if(_WeatherPattern.x>0 && _WeatherGutterVariations.z>0 && fade.z>0)n.z=WeatherNoise(float2(-.6561*p.x-.7547*p.y,.7547*p.x-.6561*p.y)*_WeatherGutterFrequencies.z+float2(seed*.317-71.9,127.1));
    if(_WeatherPattern.x>0 && _WeatherGutterVariations.w>0 && fade.w>0)n.w=WeatherNoise(float2(-.7986*p.x+.6018*p.y,-.6018*p.x-.7986*p.y)*_WeatherGutterFrequencies.w+float2(211.3,seed*.419-39.7));
    return lerp(float4(.5,.5,.5,.5),n,fade);
}
float4 WeatherGutterFields(float2 p){return WeatherGutterFields(p,0);}
void WeatherApplyLayers(inout SurfaceData surface,WeatherLayers layers)
{
    UNITY_BRANCH if(_WeatherMaterialParams.z<.5)return;
    float master=_WeatherIntensity;
    float macro=(layers.macro*2-1)*_WeatherMacro.x*master;
    surface.albedo*=max(.5,1+macro*_WeatherMacroParams.z);
    surface.albedo*=lerp(half3(1,1,1),_WeatherMacroTint.rgb,saturate(layers.macro*_WeatherMacro.x*_WeatherMacroParams.w*master));
    surface.smoothness=saturate(surface.smoothness-macro*_WeatherMaterialParams.x);
    surface.albedo*=1+(layers.micro-.5)*layers.microFade*_WeatherMicro.x*master;
    float medium=smoothstep(.25,.75,layers.medium)*_WeatherMedium.x*master;
    surface.albedo*=1-medium;
    surface.albedo*=lerp(half3(1,1,1),_WeatherSedimentTint.rgb,saturate(medium*.25));
}
#endif
