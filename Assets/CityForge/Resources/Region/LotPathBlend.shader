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
        _EdgeBlend("Edge Blend in Meters", Float) = 0
        _Recessed("Recessed Edge Shadow", Float) = 0
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

            float _EdgeBlend;
            float _Recessed;

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
                float sideDistance = min(input.ribbon.x, input.ribbon.y - input.ribbon.x);
                float edgeDistance = sideDistance;
                float opacity = _EdgeBlend > 0 ? smoothstep(0, _EdgeBlend, sideDistance) : 1;
                if (input.ribbon.w > 0)
                {
                    float endDistance = min(input.ribbon.z, input.ribbon.w - input.ribbon.z);
                    edgeDistance = min(edgeDistance, endDistance);
                    if (_EdgeBlend > 0)
                        opacity *= smoothstep(0, min(_EdgeBlend, input.ribbon.w * .45), endDistance);
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
                color.a = opacity;
                return color;
            }
            ENDHLSL
        }
    }
}
