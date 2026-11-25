// Rendering-only neutral albedo for the approved Marquee trim region. Source atlas stays unchanged.
Shader "Hidden/CityForge/BuildingTrimNeutralize"
{
    Properties { _MainTex ("Original albedo", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 frag(v2f_img i) : SV_Target
            {
                float3 albedo=tex2D(_MainTex,i.uv).rgb;
                // Linear-light reference of the approved sage panels; retain baked relief/shadow.
                float neutral=saturate(dot(albedo,float3(.2126,.7152,.0722))/.22);
                return float4(neutral,neutral,neutral,1);
            }
            ENDHLSL
        }
    }
}
