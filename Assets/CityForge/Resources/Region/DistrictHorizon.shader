Shader "CityForge/District Horizon"
{
    Properties
    {
        _ClearColor ("District clear color", Color) = (.075, .105, .12, 1)
        _Visibility ("Backdrop visibility", Range(0, 1)) = 0
        _Skyline ("Regional skyline", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "RenderPipeline"="UniversalPipeline" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ClearColor;
                half _Visibility;
            CBUFFER_END
            TEXTURE2D(_Skyline);
            SAMPLER(sampler_Skyline);

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
            };

            Varyings Vert(float3 positionOS : POSITION)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(positionOS);
                output.direction = positionOS;
                return output;
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float Noise(float2 p)
            {
                float2 cell = floor(p);
                float2 blend = frac(p);
                blend = blend * blend * (3.0 - 2.0 * blend);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), blend.x),
                    lerp(Hash(cell + float2(0, 1)), Hash(cell + 1), blend.x), blend.y);
            }

            float Ridge(float elevation, float profile, float base, float height)
            {
                float summit = base + profile * height;
                return smoothstep(.025, .15, profile) * smoothstep(base - .045, base, elevation) *
                    (1.0 - smoothstep(summit - .003, summit + .004, elevation));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                float elevation = direction.y;
                half3 haze = half3(.43, .59, .73);
                half3 zenith = half3(.09, .29, .63);
                half3 sky = lerp(haze, zenith, smoothstep(-.02, .43, elevation));

                UNITY_BRANCH
                if (elevation > .055)
                {
                    float2 cloudUV = direction.xz * (1.4 / (elevation + .12)) + float2(_Time.y * .012, 0);
                    float cloud = Noise(cloudUV) * .65 + Noise(cloudUV * 2.1) * .25 + Noise(cloudUV * 4.3) * .10;
                    float opacity = smoothstep(.55, .76, cloud) * smoothstep(.055, .20, elevation);
                    sky = lerp(sky, half3(.84, .87, .88), opacity * .72);
                }

                UNITY_BRANCH
                if (elevation > -.12 && elevation < .20)
                {
                    float bearing = atan2(direction.x, direction.z) / TWO_PI + .5;
                    half2 profile = SAMPLE_TEXTURE2D(_Skyline, sampler_Skyline, float2(bearing, .5)).rg;
                    sky = lerp(sky, half3(.44, .54, .58), Ridge(elevation, profile.g, -.005, .14) * .78);
                    sky = lerp(sky, half3(.34, .45, .49), Ridge(elevation, profile.r, -.028, .15) * .82);
                }

                // Fade below the horizon into the dark space surrounding the slab.
                sky = lerp(_ClearColor.rgb, sky, smoothstep(-.29, -.06, elevation));
                return half4(lerp(_ClearColor.rgb, sky, _Visibility), 1);
            }
            ENDHLSL
        }
    }
}
