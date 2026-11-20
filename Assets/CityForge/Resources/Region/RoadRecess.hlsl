#ifndef CITYFORGE_ROAD_RECESS_INCLUDED
#define CITYFORGE_ROAD_RECESS_INCLUDED
TEXTURE2D(_RecessFootprint); SAMPLER(sampler_RecessFootprint);
float4 _RecessFootprintRect;
float _RecessUseFootprint;
float RoadRecessDistance(float2 world) {
    return SAMPLE_TEXTURE2D_LOD(_RecessFootprint, sampler_RecessFootprint,
        (world - _RecessFootprintRect.xy) * _RecessFootprintRect.zw, 0).r;
}
float RoadRecessHit(float2 world, float2 ray, out float edgeDistance) {
    edgeDistance = max(0, RoadRecessDistance(world));
    float bed = RoadRecessDistance(world + ray);
    return bed >= 0 ? 1 : saturate(edgeDistance / max(.00001, edgeDistance - bed));
}
#endif
