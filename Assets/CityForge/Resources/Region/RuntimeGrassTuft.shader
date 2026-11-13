Shader "CityForge/Runtime Grass Tuft"
{
    Properties
    {
        _BaseMap("Blade texture", 2D) = "white" {}
        _Cutoff("Blade cutoff", Range(0,1)) = .2
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        Cull Off
        Pass
        {
            Name "GrassTuft" Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
                float _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                float sway = sin(_Time.y * 1.7 + dot(world.xz, float2(.27,.19)));
                world.xz += float2(1,.6) * sway * saturate(input.positionOS.y * 2) * .035;
                output.positionWS = world;
                output.positionCS = TransformWorldToHClip(world);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half4 blade = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                clip(blade.a - _Cutoff);
                // A tuft reads as a patch of meadow at gameplay scale. Vertical cards
                // share ground-oriented lighting while still receiving nearby shadows.
                half3 up = half3(0,1,0);
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half3 light = max(0, SampleSH(up)) +
                    sun.color * saturate(sun.direction.y) * lerp(.22,1,sun.shadowAttenuation);
                half textureDetail = lerp(.68, 1.12, saturate(Luminance(blade.rgb) * 3.0));
                // A warmer olive keeps sunlit blades close to the surrounding meadow.
                half3 meadow = half3(.077, .108, .029) * textureDetail;
                return half4(meadow * light,1);
            }
            ENDHLSL
        }
    }
}
