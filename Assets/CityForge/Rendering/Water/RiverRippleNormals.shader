Shader "Hidden/CityForge/RiverRippleNormals"
{
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Periodic height derivatives: no painted white crests, and no external artwork.
            float4 frag(v2f_img input) : SV_Target
            {
                float2 slope = 0;
                [unroll] for (int i = 0; i < 32; i++)
                {
                    float seed = i + 1;
                    float angle = seed * 2.39996323;
                    float frequency = 5 + fmod(seed * 7, 27);
                    float2 wave = round(float2(cos(angle), sin(angle)) * frequency);
                    float phase = frac(sin(seed * 127.1) * 43758.5453) * 6.2831853;
                    float weight = .19 * lerp(.7, 1.0, abs(cos(angle)));
                    slope += normalize(wave) * cos(dot(input.uv, wave) * 6.2831853 + phase) * weight;
                }
                float3 normal = normalize(float3(slope, 1));
                return float4(normal * .5 + .5, 1);
            }
            ENDHLSL
        }
    }
}
