#ifndef CITYFORGE_TROPICAL_PALM_WIND
#define CITYFORGE_TROPICAL_PALM_WIND
// Authored procedural wind. Original URP lighting and texture sampling remain authoritative.
float _PalmWindWeight, _PalmWindTime, _PalmHeight;
float3 PalmWindPosition(float3 position, float2 uv)
{
    if (_PalmWindWeight <= 0) return position;
    float height = max(1, _PalmHeight);
    float h = saturate(position.y / height);
    float2 anchor = TransformObjectToWorld(float3(0, 0, 0)).xz;
    float phase = dot(anchor, float2(.071, .053));
    float time = _PalmWindTime;
    float sway = sin(time * .85 + phase) + .22 * sin(time * 1.47 + phase * 1.3);
    // Atlas columns 4, 5 and 7 contain fronds. Bark does not flutter.
    float column = floor(saturate(uv.x) * 8);
    float leaf = (column == 4 || column == 5 || column == 7) ? 1 : 0;
    float crown = smoothstep(.58, .95, h) * leaf;
    float flutter = sin(time * 2.1 + phase + dot(position.xz, float2(.56, .39)));
    float3 wind = TransformWorldToObjectDir(float3(.92, 0, .39), false);
    float bend = .20 * h * h * sway + .095 * crown * flutter;
    position += wind * bend * _PalmWindWeight;
    position.y += .035 * crown * flutter * _PalmWindWeight;
    return position;
}
void PalmWindDeform(inout float3 position, inout float3 normal, float2 uv)
{
    if (_PalmWindWeight <= 0) return;
    float height = max(1, _PalmHeight);
    float h = saturate(position.y / height);
    float phase = dot(TransformObjectToWorld(float3(0,0,0)).xz, float2(.071,.053));
    float sway = sin(_PalmWindTime * .85 + phase) + .22 * sin(_PalmWindTime * 1.47 + phase * 1.3);
    float3 wind = TransformWorldToObjectDir(float3(.92,0,.39), false);
    // Inverse-transpose of the small trunk-bend shear; avoids frozen trunk highlights.
    normal.y -= dot(normal.xz, wind.xz) * (.40 * h / height) * sway * _PalmWindWeight;
    normal = normalize(normal);
    position = PalmWindPosition(position, uv);
}
#endif
