#ifndef CITYFORGE_PATH_MATERIAL_INPUT_INCLUDED
#define CITYFORGE_PATH_MATERIAL_INPUT_INCLUDED
// Load LitInput dependencies first so extending its single UnityPerMaterial buffer
// cannot inject properties into another package buffer. No package source is copied.
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#include "SurfaceWeatheringProperties.hlsl"
// URP 17 LitInput contains one material buffer. Keep one invariant layout across variants.
#undef CBUFFER_END
#define CBUFFER_END float4 _DirtEndFade; float _EdgeBlend, _Recessed, _PathOpacity, _VertexOpacity, _NaturalFootpath, _RecessDepth, _RecessBorder, _BorderSides, _BorderTextured; float4 _RecessFootprintRect; float _RecessUseFootprint; CITYFORGE_WEATHERING_PROPERTIES };
#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
#undef CBUFFER_END
#define CBUFFER_END };
#define CITYFORGE_SURFACE_PROPERTIES_BOUND 1
#endif
