Shader "CityForge/Night Window Glass"
{
    Properties { _EmissionColor("Window light", Color) = (0,0,0,1) _PaneStrength("Pane strength", Float) = 0.60784314 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+5" }
        Pass
        {
            ZWrite On Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _EmissionColor;
                half _PaneStrength;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float fog:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings vert(Attributes input)
            {
                Varyings output; UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS=TransformObjectToHClip(input.positionOS.xyz);
                output.fog=ComputeFogFactor(output.positionCS.z); return output;
            }
            half4 frag(Varyings input):SV_Target { return half4(MixFog(_EmissionColor.rgb*_PaneStrength,input.fog),1); }
            ENDHLSL
        }
    }
}
