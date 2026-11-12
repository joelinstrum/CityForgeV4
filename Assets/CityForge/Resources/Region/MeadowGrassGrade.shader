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
                // Prepare quieter diffuse textures once; retain the original alpha for terrain blending.
                float4 source=tex2D(_MainTex,input.uv);
                float3 softGrass=tex2Dlod(_MainTex,float4(input.uv,0,4.5)).rgb;
                float3 averageGrass=tex2Dlod(_MainTex,float4(input.uv,0,9)).rgb;
                // Preserve the authored grade in display space before writing the sRGB texture.
                if (!IsGammaSpace())
                {
                    softGrass=LinearToGammaSpace(softGrass);
                    averageGrass=LinearToGammaSpace(averageGrass);
                }
                source.rgb=lerp(averageGrass,softGrass,0.18);
                if (!IsGammaSpace())
                {
                    float3 finerGrass=LinearToGammaSpace(tex2Dlod(_MainTex,float4(input.uv,0,2.5)).rgb);
                    source.rgb=lerp(source.rgb,finerGrass,.22);
                }
                float3 color=source.rgb*_SourceTint.rgb;
                float luminance=dot(color,float3(.2126,.7152,.0722));
                color=max(0,lerp(luminance.xxx,color,_Saturation));
                // Normalize the tint so its hue adjustment does not silently change exposure.
                float3 tint = IsGammaSpace() ? _GrassTint.rgb : LinearToGammaSpace(_GrassTint.rgb);
                color*=tint/max(.001,dot(tint,float3(.2126,.7152,.0722)));
                color=GammaToLinearSpace(LinearToGammaSpace(color)*_Brightness);
                if (!IsGammaSpace()) color=GammaToLinearSpace(color);
                return float4(color,source.a);
            }
            ENDHLSL
        }
    }
}
