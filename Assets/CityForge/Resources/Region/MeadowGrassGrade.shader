Shader "Hidden/CityForge/Meadow Grass Grade"
{
    Properties
    {
        _MainTex("Source grass",2D)="white" {}
        _SourceTint("Previous terrain tint",Vector)=(1,1,1,1)
        _GrassTint("Grass tint",Color)=(1,1,1,1)
        _Saturation("Grass saturation",Float)=1
        _Brightness("Grass perceived brightness",Float)=1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _SourceTint, _GrassTint;
            float _Saturation, _Brightness;
            float4 frag(v2f_img input) : SV_Target
            {
                float4 source=tex2D(_MainTex,input.uv);
                float3 color=source.rgb*_SourceTint.rgb;
                float luminance=dot(color,float3(.2126,.7152,.0722));
                color=max(0,lerp(luminance.xxx,color,_Saturation));
                // Normalize the tint so its hue adjustment does not silently change exposure.
                color*=_GrassTint.rgb/max(.001,dot(_GrassTint.rgb,float3(.2126,.7152,.0722)));
                color=GammaToLinearSpace(LinearToGammaSpace(color)*_Brightness);
                return float4(color,source.a);
            }
            ENDHLSL
        }
    }
}
