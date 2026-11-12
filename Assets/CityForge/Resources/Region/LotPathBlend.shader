Shader "CityForge/Lot Path Blend"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _Smoothness("Smoothness", Range(0,1)) = 0.08
        _Metallic("Metallic", Range(0,1)) = 0
        _MetallicGlossMap("Metallic Smoothness", 2D) = "white" {}
        _SpecColor("Specular", Color) = (0.2,0.2,0.2,1)
        _SpecGlossMap("Specular Smoothness", 2D) = "white" {}
        _BumpMap("Normal", 2D) = "bump" {}
        _BumpScale("Normal Strength", Float) = 1
        _ParallaxMap("Height", 2D) = "black" {}
        _Parallax("Relief", Float) = 0.01
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0,1)) = 1
        _EmissionMap("Emission", 2D) = "white" {}
        _EmissionColor("Emission Color", Color) = (0,0,0,0)
        _RoadShoulderMask("Road Shoulder Mask", 2D) = "gray" {}
        _RoadWearMap("Road Wear", 2D) = "white" {}
        [HideInInspector] _DirtEndFade("Standalone Dirt Ends", Vector) = (0,0,0,0)
        _EdgeBlend("Edge Blend in Meters", Float) = 0
        _Recessed("Recessed Edge Shadow", Float) = 0
        _PathOpacity("Path Opacity", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex PathVertex
            #pragma fragment PathFragment
            #pragma multi_compile_local_fragment _ _ROAD_WEAR
            #pragma multi_compile_local _ _NORMALMAP
            #pragma multi_compile_local_fragment _ _PARALLAXMAP
            #pragma multi_compile_local_fragment _ _METALLICSPECGLOSSMAP
            #pragma multi_compile_local_fragment _ _OCCLUSIONMAP
            #pragma multi_compile_local_fragment _ _SPECULAR_SETUP
            #pragma multi_compile_local_fragment _ _SPECULARHIGHLIGHTS_OFF
            #pragma multi_compile_local_fragment _ _ENVIRONMENTREFLECTIONS_OFF
            #pragma multi_compile_local_fragment _ _EMISSION
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog
            #define _SURFACE_TYPE_TRANSPARENT 1
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            float4 _DirtEndFade;
            float _DirtJunctionCount;
            float4 _DirtJunctions[32];
            float4 _DirtJunctionAxes[32];
            float _EdgeBlend;
            float _Recessed;
            float _PathOpacity;
            TEXTURE2D(_RoadWearMap);
            SAMPLER(sampler_RoadWearMap);
            TEXTURE2D(_RoadShoulderMask);
            SAMPLER(sampler_RoadShoulderMask);

            #if defined(_ROAD_WEAR)
            float DirtHash(float2 cell)
            {
                float3 p = frac(cell.xyx * .1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float DirtNoise(float2 samplePosition)
            {
                float2 cell = floor(samplePosition);
                float2 f = frac(samplePosition);
                f = f * f * (3 - 2 * f);
                return lerp(lerp(DirtHash(cell), DirtHash(cell + float2(1, 0)), f.x),
                    lerp(DirtHash(cell + float2(0, 1)), DirtHash(cell + 1), f.x), f.y);
            }
            #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float4 ribbon : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float4 ribbon : TEXCOORD4;
                half fog : TEXCOORD5;
            };

            Varyings PathVertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs basis = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = basis.normalWS;
                output.tangentWS = half4(basis.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.ribbon = input.ribbon;
                output.fog = ComputeFogFactor(position.positionCS.z);
                return output;
            }

            half4 PathFragment(Varyings input) : SV_Target
            {
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float2 uv = input.uv;
                #if defined(_PARALLAXMAP)
                    ApplyPerPixelDisplacement(GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, view), uv);
                #endif
                SurfaceData surface;
                InitializeStandardLitSurfaceData(uv, surface);
                float sideDistance = min(input.ribbon.x, input.ribbon.y - input.ribbon.x);
                #if defined(_ROAD_WEAR)
                    // Road-local UVs keep the ruts aligned to the ribbon through turns.
                    float across = saturate(input.ribbon.x / input.ribbon.y);
                    float repeatLength = 4.5;
                    if (input.ribbon.w < 0)
                    {
                        float loopLength = -input.ribbon.w;
                        // Mirrored repeat needs an even number of repeats around a loop.
                        repeatLength = loopLength / (2 * max(1, round(loopLength / (2 * repeatLength))));
                    }
                    float2 roadWorld = input.positionWS.xz;
                    float compaction = DirtNoise(roadWorld * .14);
                    float patches = DirtNoise(roadWorld * .62);
                    float grit = DirtNoise(roadWorld * 6.5);
                    float pixelsAcross = input.ribbon.y / max(fwidth(input.ribbon.x), .001);
                    float closeDetail = smoothstep(7, 22, pixelsAcross);

                    float2 wearUV = float2(lerp(.22, .78, across), input.ribbon.z / repeatLength);
                    half4 wear = SAMPLE_TEXTURE2D(_RoadWearMap, sampler_RoadWearMap, wearUV);
                    // A rotated second sample breaks the base tile pattern without changing
                    // the scale of embedded grit or the longitudinal orientation of the ruts.
                    float2 alternateUV = float2(-uv.y, uv.x) + float2(.37, .61);
                    half3 alternateSoil = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, alternateUV).rgb * _BaseColor.rgb;
                    float earthPatch = DirtNoise(roadWorld * .23 + 8.7);
                    surface.albedo = lerp(surface.albedo, alternateSoil, smoothstep(.34, .71, earthPatch) * .65);
                    float wearBreak = DirtNoise(float2(input.ribbon.z * .27, 32.4));
                    if (input.ribbon.w < 0)
                    {
                        float angle = input.ribbon.z * (6.2831853 / -input.ribbon.w);
                        wearBreak = DirtNoise(float2(cos(angle), sin(angle)) * (-input.ribbon.w * .043) + 32.4);
                    }
                    float rutContinuity = lerp(.055, 1, smoothstep(.32, .67, wearBreak));
                    float junctionInfluence = 0;
                    float approachWear = 0;
                    float sharedRut = 0;
                    float sharedRutCoverage = 0;
                    float turningWear = 0;
                    float cornerDetail = 0;
                    float turnBreak = smoothstep(.26, .65, DirtNoise(roadWorld * 3.1 + 11.4));
                    for (int j = 0; j < (int)_DirtJunctionCount; j++)
                    {
                        float4 junction = _DirtJunctions[j];
                        float2 offset = roadWorld - junction.xy;
                        float distanceToJoin = length(offset);
                        junctionInfluence = max(junctionInfluence, 1 - smoothstep(junction.z, junction.w, distanceToJoin));
                        approachWear = max(approachWear, 1 - smoothstep(junction.w + 2, junction.w + 5, distanceToJoin));
                        if (distanceToJoin >= junction.w || closeDetail <= 0) continue;

                        float4 axes = _DirtJunctionAxes[j];
                        float2 axisLengths = float2(length(axes.xy), length(axes.zw));
                        if (min(axisLengths.x, axisLengths.y) < .1) continue;
                        float2 a = axes.xy / axisLengths.x;
                        float2 b = axes.zw / axisLengths.y;
                        float determinant = a.x * b.y - a.y * b.x;
                        float2 along = float2(offset.x * b.y - offset.y * b.x,
                            a.x * offset.y - a.y * offset.x) / determinant;
                        // Both overlapping meshes shade the same tracks so draw order cannot hide a crossing.
                        float2 acrossAxis = abs(along.yx * determinant);
                        float2 trackWidths = 100 * (axisLengths - lerp(.5, 1, step(.75, axisLengths)));
                        float2 trackDistance = abs(acrossAxis / trackWidths - .17);
                        float2 tracks = 1 - smoothstep(.006, lerp(.052, .099, patches), trackDistance);
                        // A terminal arm softens into the junction instead of projecting past it.
                        tracks *= max(step(.75, axisLengths), smoothstep(.10, 1.05, along));
                        float trackCoverage = 1 - smoothstep(junction.w * .72, junction.w, distanceToJoin);
                        sharedRut = max(sharedRut, max(tracks.x, tracks.y) * trackCoverage);
                        sharedRutCoverage = max(sharedRutCoverage, trackCoverage);
                        // Reflect across through roads only. Terminal arms have a single direction.
                        if (axisLengths.x > .75) along.x = abs(along.x);
                        if (axisLengths.y > .75) along.y = abs(along.y);
                        float reach = junction.w * .78;
                        float2 corner = reach - along;
                        float sector = smoothstep(.06, .28, min(along.x, along.y))
                            * smoothstep(0, .32, min(corner.x, corner.y));
                        float radius = reach - junction.z * 1.13;
                        float arc = abs(length(corner) - radius + (patches - .5) * .11);
                        turningWear = max(turningWear, (1 - smoothstep(.035, .15, arc)) * sector * turnBreak);
                        float cornerPatch = 1 - smoothstep(.17, .58, length(along - reach * .70));
                        cornerDetail = max(cornerDetail, cornerPatch * sector);
                    }
                    // Keep the approaches readable even where the normal wear pattern has a gap.
                    rutContinuity = lerp(max(rutContinuity, .78 * approachWear), .9, sharedRutCoverage);
                    float crossingWear = 1 - .35 * junctionInfluence;
                    float packedJunction = smoothstep(.12, .85, junctionInfluence)
                        * lerp(.76, 1, patches);
                    surface.albedo = lerp(surface.albedo, wear.rgb, wear.a * lerp(.16, .36, compaction) * rutContinuity * (1 - sharedRutCoverage) * crossingWear);
                    float trackTexture = DirtNoise(roadWorld * 1.35);
                    float rutDistance = min(abs(across - .33), abs(across - .67));
                    float rut = 1 - smoothstep(.006, lerp(.052, .099, patches), rutDistance);
                    rut = rut * (1 - sharedRutCoverage) + sharedRut;
                    float rutWear = rut * rutContinuity * crossingWear * lerp(.13, .30, trackTexture) * lerp(.68, 1.16, grit);
                    half soilValue = dot(surface.albedo, half3(.2126, .7152, .0722));
                    half3 neutralSoil = lerp(surface.albedo, soilValue.xxx, .21);
                    float soilVariation = lerp(.90, 1.07, compaction) * lerp(.89, 1.10, earthPatch);
                    soilVariation = lerp(soilVariation, lerp(.97, 1.02, compaction), packedJunction * .36);
                    surface.albedo = neutralSoil * half3(.66, .62, .55) * soilVariation;
                    float looseShoulder = 1 - smoothstep(.10, .55, sideDistance);
                    float shoulderGrain = looseShoulder * lerp(-.07, .14, grit);
                    // Broken arcs and a few grit highlights around the turning corners.
                    float disturbedSoil = cornerDetail * ((patches - .5) * .10 + smoothstep(.72, .94, grit) * .15);
                    surface.albedo *= lerp(.85, 1 - rutWear + shoulderGrain - turningWear * .13 + disturbedSoil, closeDetail);
                    surface.smoothness = lerp(.035, .065, max(rut * (1 - junctionInfluence), packedJunction * .55) * closeDetail);
                #endif

                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.positionCS = input.positionCS;
                lighting.viewDirectionWS = view;
                half3 bitangent = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
                lighting.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(surface.normalTS,
                    half3x3(input.tangentWS.xyz, bitangent, input.normalWS)));
                lighting.bakedGI = SampleSH(lighting.normalWS);
                lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.shadowMask = half4(1,1,1,1);

                // x/y are across the ribbon in meters. z/w describe its open ends.
                float edgeDistance = sideDistance;
                float opacity = _EdgeBlend > 0 ? smoothstep(0, _EdgeBlend, sideDistance) : 1;
                #if defined(_ROAD_WEAR)
                    // Alpha lets the actual lit terrain show through. Limit the patch
                    // breakup to the shoulder so the wheel bed stays continuously dirt.
                    float shoulder = lerp(.26, .38, patches);
                    float maskU = lerp(.09, .43, saturate(sideDistance / .50));
                    maskU = across < .5 ? maskU : 1 - maskU;
                    float2 maskUV = float2(maskU, input.ribbon.z / repeatLength);
                    float mask = SAMPLE_TEXTURE2D(_RoadShoulderMask, sampler_RoadShoulderMask, maskUV).r;
                    float tufts = DirtNoise(roadWorld * 2.7);
                    float grassShape = tufts * .63 + patches * .20 + (1 - mask) * .17;
                    float shoulderPatch = DirtNoise(roadWorld * .43 + 21.8);
                    float patchCoverage = smoothstep(.32, .59, shoulderPatch);
                    float grassReach = lerp(.15, .30, tufts);
                    float intrusion = smoothstep(.37, .61, grassShape)
                        * (1 - smoothstep(grassReach, grassReach + .10, sideDistance));
                    float roughness = (grit - .5) * .045 * closeDetail;
                    float shoulderCoverage = smoothstep(0, shoulder, sideDistance + roughness);
                    // Small pockets can reveal grass within the shoulder even where
                    // the outer edge remains intact.
                    float pocketNoise = DirtNoise(roadWorld * 3.4 + 17.6);
                    float pocketBand = smoothstep(.07, .14, sideDistance)
                        * (1 - smoothstep(.25, lerp(.33, .49, patches), sideDistance));
                    float pockets = smoothstep(.55, .81, pocketNoise) * pocketBand;
                    float grassCoverage = max(intrusion, pockets) * patchCoverage;
                    opacity = shoulderCoverage * (1 - grassCoverage * lerp(.23, .98, closeDetail));
                #endif
                if (input.ribbon.w > 0)
                {
                    float endDistance = min(input.ribbon.z, input.ribbon.w - input.ribbon.z);
                    edgeDistance = min(edgeDistance, endDistance);
                    if (_EdgeBlend > 0)
                    {
                        float fade = min(_EdgeBlend, input.ribbon.w * .45);
                        #if defined(_ROAD_WEAR)
                            float naturalFade = min(lerp(.42, .68, patches), input.ribbon.w * .45);
                            float startFade = lerp(fade, naturalFade, _DirtEndFade.x);
                            float finishFade = lerp(fade, naturalFade, _DirtEndFade.y);
                            opacity *= smoothstep(0, startFade, input.ribbon.z)
                                * smoothstep(0, finishFade, input.ribbon.w - input.ribbon.z);
                        #else
                            opacity *= smoothstep(0, fade, endDistance);
                        #endif
                    }
                }
                half4 color = UniversalFragmentPBR(lighting, surface);
                if (_Recessed > 0)
                {
                    // Move the contact shadow inward as the feather widens so it remains visible.
                    float lip = min(_EdgeBlend * .65, input.ribbon.w > 0 ? input.ribbon.w * .28 : _EdgeBlend);
                    float falloff = min(.40, input.ribbon.y * .24);
                    float shadow = 1 - smoothstep(lip, lip + falloff, edgeDistance);
                    color.rgb *= 1 - .20 * shadow;
                }
                color.rgb = MixFog(color.rgb, input.fog);
                color.a = opacity * _PathOpacity;
                return color;
            }
            ENDHLSL
        }
    }
}
