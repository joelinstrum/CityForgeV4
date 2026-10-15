#ifndef CITYFORGE_PROFILE_GROUND
#define CITYFORGE_PROFILE_GROUND
TEXTURE2D(_LandscapeGrass); SAMPLER(sampler_LandscapeGrass);
TEXTURE2D(_LandscapeNormal); SAMPLER(sampler_LandscapeNormal);
TEXTURE2D(_LandscapeSoil); SAMPLER(sampler_LandscapeSoil);
TEXTURE2D(_LandscapeLitter); SAMPLER(sampler_LandscapeLitter);
float _LandscapeEnabled, _LandscapeBrightness;
float4 _LandscapeGrassTint, _LandscapeCalm, _LandscapeOlive, _LandscapeSoilTint, _LandscapeLitterTint;
float4 _LandscapeScales, _LandscapeSmoothing, _LandscapeVariation, _LandscapeCoverage, _LandscapeSeed;
void ProfileGround(float3 position, float3 worldNormal, float4 field, float2 woodland,
 out float3 albedo, out float3 normalTS, out float smoothness, out float ao)
{
 float2 p=position.xz;
 float2 q=p+_LandscapeSeed.xy;
 float broad=cfnoise(q/_LandscapeVariation.x)*2-1;
 float secondary=cfnoise(q/9+37)*2-1;
 float3 calm=lerp(_LandscapeCalm.rgb,_LandscapeOlive.rgb,saturate(broad)*.22)
             *(1+broad*_LandscapeVariation.y+secondary*_LandscapeVariation.z);
 float2 rotated=mul(float2x2(.8,-.6,.6,.8),p)/(_LandscapeScales.x*1.71)+float2(.37,.61);
 float3 grass=(SAMPLE_TEXTURE2D_BIAS(_LandscapeGrass,sampler_LandscapeGrass,p/_LandscapeScales.x,_LandscapeScales.w).rgb*.7
              +SAMPLE_TEXTURE2D_BIAS(_LandscapeGrass,sampler_LandscapeGrass,rotated,_LandscapeScales.w).rgb*.3)*_LandscapeGrassTint.rgb;
 // Screen footprint works in orthographic gameplay; camera-to-ground distance does not track zoom reliably.
 float footprint=max(length(ddx(p)),length(ddy(p)));
 float distant=unity_OrthoParams.w > .5 ? smoothstep(.15,1.2,footprint) : saturate((distance(position,_WorldSpaceCameraPos)-_LandscapeSmoothing.z)/_LandscapeSmoothing.w);
 albedo=lerp(grass,calm,lerp(_LandscapeSmoothing.x,_LandscapeSmoothing.y,distant));
 float3 litter=SAMPLE_TEXTURE2D_BIAS(_LandscapeLitter,sampler_LandscapeLitter,p/_LandscapeScales.z,1.5).rgb*_LandscapeLitterTint.rgb;
 float3 soil=SAMPLE_TEXTURE2D_BIAS(_LandscapeSoil,sampler_LandscapeSoil,p/_LandscapeScales.y,1.5).rgb*_LandscapeSoilTint.rgb;
 // Existing water-distance encoding is geographic; convert once into rendered meters.
 float bankDistance=field.r*2000*_LandscapeCoverage.w;
 float pockets=smoothstep(.48,.72,cfnoise(q/13));
 float bank=(1-smoothstep(0,_LandscapeCoverage.z,bankDistance))*pockets*_LandscapeCoverage.y;
 float canopy=smoothstep(.12,.9,woodland.r)*_LandscapeCoverage.x;
 albedo=lerp(albedo,litter,canopy);
 albedo=lerp(albedo,soil,bank*(1-canopy*.5));
 // Keep established rock/coastal substrates, but remove their fine grass noise contribution.
 float rock=smoothstep(.0015,.016,1-saturate(worldNormal.y))*.72;
 float beach=(1-smoothstep(18,125,field.r*2000))*(1-smoothstep(.39,.61,field.g));
 albedo=saturate(albedo*_LandscapeBrightness);
 albedo=lerp(albedo,cfcover(p,4),rock);
 albedo=lerp(albedo,cfcover(p,5),beach);
 normalTS=UnpackNormal(SAMPLE_TEXTURE2D(_LandscapeNormal,sampler_LandscapeNormal,p/_LandscapeScales.x));
 normalTS.xy*=_LandscapeVariation.w*(1-smoothstep(.05,.4,footprint))*(1-canopy);
 normalTS=normalize(normalTS);
 smoothness=lerp(.07,.15,bank); ao=lerp(1,.94,canopy);
}
#endif
