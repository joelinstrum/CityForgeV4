Shader "CityForge/UI/Town Button Artwork"
{
    Properties
    {
        [PerRendererData] _MainTex ("Button Sheet", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 art = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half brightest = max(art.r, max(art.g, art.b));
                // Remove the near-black sheet matte while retaining antialiased edges.
                #if defined(UNITY_COLORSPACE_GAMMA)
                    art.a *= smoothstep(.015h, .06h, brightest);
                #else
                    art.a *= smoothstep(.0012h, .005h, brightest);
                #endif
                return art * input.color;
            }
            ENDHLSL
        }
    }
}
