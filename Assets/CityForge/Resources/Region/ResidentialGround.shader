Shader "CityForge/Residential Ground"
{
    Properties
    {
        _Albedo("Albedo", 2D) = "white" {}
        _Mask("Wear Mask", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-1" }
        Pass
        {
            Tags { "LightMode"="UniversalForwardOnly" }
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #define _SURFACE_TYPE_TRANSPARENT 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Albedo);
            SAMPLER(sampler_Albedo);
            TEXTURE2D(_Mask);
            SAMPLER(sampler_Mask);

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 sampleUV : TEXCOORD0;
                float2 footprintUV : TEXCOORD1;
                float4 settings : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 sampleUV : TEXCOORD2;
                float2 footprintUV : TEXCOORD3;
                half4 settings : TEXCOORD4;
                half fog : TEXCOORD5;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.sampleUV = input.sampleUV;
                output.footprintUV = input.footprintUV;
                output.settings = input.settings;
                output.fog = ComputeFogFactor(position.positionCS.z);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 border = min(input.footprintUV, 1 - input.footprintUV);
                float edge = min(border.x, border.y);
                if (input.settings.z > .5 && edge < .022)
                    return half4(.95, .73, .22, .9);

                half mask = SAMPLE_TEXTURE2D(_Mask, sampler_Mask, input.sampleUV).r;
                // Compact lots have little exposed yard, so concentrate the fade at the edge.
                half compact = input.settings.w;
                half fadeWidth = lerp(lerp(.13, .30, 1 - mask), lerp(.055, .13, 1 - mask), compact);
                half feather = smoothstep(0, fadeWidth, edge);
                half coverage = lerp(mask, sqrt(mask), compact);
                half3 soil = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, input.sampleUV).rgb;
                half luminance = dot(soil, half3(.2126, .7152, .0722));
                soil = lerp(soil, luminance.xxx, .17) * input.settings.y;
                half3 normal = NormalizeNormalPerPixel(input.normalWS);
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half diffuse = saturate(dot(normal, sun.direction)) * sun.shadowAttenuation * sun.distanceAttenuation;
                half3 lit = soil * (SampleSH(normal) + sun.color * diffuse);
                return half4(MixFog(lit, input.fog), coverage * feather * input.settings.x);
            }
            ENDHLSL
        }
    }
}
