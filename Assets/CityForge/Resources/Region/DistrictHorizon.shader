Shader "CityForge/District Horizon"
{
    Properties
    {
        _ClearColor ("District clear color", Color) = (.075, .105, .12, 1)
        _Visibility ("Backdrop visibility", Range(0, 1)) = 0
        _Night ("Night darkness", Range(0,1)) = 0
        _StarVisibility ("Stars and moon", Range(0,1)) = 0
        _Hour ("Hour", Float) = 15
        _CloudTime ("Cloud drift time", Float) = 0
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
                half _Visibility, _Night, _StarVisibility, _Hour;
                float _CloudTime;
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
                half3 haze = half3(.25, .44, .64);
                half3 zenith = half3(.14, .32, .57);
                half3 sky = lerp(haze, zenith, smoothstep(-.15, .36, elevation));

                float cloudOpacity=0;
                UNITY_BRANCH
                if (elevation > .055)
                {
                    float2 cloudUV = direction.xz * (1.4 / (elevation + .12)) + float2(_CloudTime * .028, _CloudTime * .006);
                    float cloud = Noise(cloudUV) * .65 + Noise(cloudUV * 2.1) * .25 + Noise(cloudUV * 4.3) * .10;
                    float opacity = smoothstep(.55, .76, cloud) * smoothstep(.055, .20, elevation);
                    cloudOpacity=opacity;

                }

                sky=lerp(sky,sky*half3(.035,.055,.11),_Night);
                float westGlow=pow(saturate(dot(normalize(direction.xz),normalize(float2(-1,.3)))*.5+.5),3);
                float lowSky=exp(-max(0,elevation)*4);
                // Continuous ordered sunset palette: blue, soft orange, orange, red, violet.
                float warmUp=smoothstep(16.5,17.75,_Hour);
                half3 sunsetHorizon=lerp(half3(.96,.51,.23),half3(.98,.37,.08),smoothstep(17.75,18.75,_Hour));
                sunsetHorizon=lerp(sunsetHorizon,half3(.77,.17,.09),smoothstep(18.75,19.5,_Hour));
                sunsetHorizon=lerp(sunsetHorizon,half3(.27,.10,.36),smoothstep(19.5,20.5,_Hour));
                half3 sunsetZenith=lerp(half3(.23,.35,.56),half3(.13,.055,.23),smoothstep(18.75,20.5,_Hour));
                half3 sunsetSky=lerp(sunsetZenith,sunsetHorizon,lowSky);
                float duskFade=1-smoothstep(20.5,22,_Hour);
                float sunsetStrength=warmUp*duskFade*lerp(.65,1,westGlow);
                sky=lerp(sky,sunsetSky,sunsetStrength);
                float dawn=smoothstep(5,6,_Hour)*(1-smoothstep(6,8,_Hour));
                float eastGlow=pow(saturate(dot(normalize(direction.xz),normalize(float2(1,-.3)))*.5+.5),3);
                sky=lerp(sky,lerp(half3(.24,.15,.32),half3(.70,.39,.28),eastGlow),dawn*lowSky*.65);
                // Keep clouds visible and warm after applying the dusk/dawn sky palette.
                half3 cloudTint=lerp(half3(.84,.87,.88),half3(.055,.065,.11),_Night);
                half3 duskCloud=lerp(half3(.90,.56,.36),half3(.18,.11,.24),smoothstep(18.5,20.5,_Hour));
                cloudTint=lerp(cloudTint,duskCloud,sunsetStrength*.85);
                sky=lerp(sky,cloudTint,cloudOpacity*.72);
                if(_StarVisibility>0&&elevation>0){
                    float2 starUV=float2(atan2(direction.x,direction.z)/TWO_PI+.5,asin(clamp(direction.y,-1,1))/PI+.5)*float2(420,210);
                    float2 cell=floor(starUV);float random=Hash(cell);float2 center=float2(Hash(cell+19.3),Hash(cell+43.7))*.7+.15;
                    float radius=length(frac(starUV)-center);float aa=max(fwidth(starUV.x),fwidth(starUV.y));
                    float stars=(1-smoothstep(.065,.065+aa*.65,radius))*step(.985,random);
                    stars*=lerp(.55,1,Hash(cell+77))*(.87+.13*sin(_Time.y*1.2+random*100));
                    sky+=stars*half3(1.8,2.0,2.4)*_StarVisibility*(1-cloudOpacity*.9)*smoothstep(0,.12,elevation);
                    float phase=frac((_Hour-20+24)/24)*TWO_PI;
                    float3 moonDirection=normalize(float3(-.48*cos(phase),.12+.7*sin(phase),.65));
                    float disc=dot(direction,moonDirection);float moon=smoothstep(cos(.025),cos(.022),disc);
                    float glow=pow(saturate(disc),650)*.055;
                    sky+=(moon*half3(.72,.76,.68)*(.72+.28*Noise((direction.xz-moonDirection.xz)*900))+glow*half3(.38,.47,.65))*_StarVisibility*(1-cloudOpacity*.75);
                }


                // Fade below the horizon into the dark space surrounding the slab.
                sky = lerp(_ClearColor.rgb, sky, smoothstep(-.29, -.06, elevation));
                return half4(lerp(_ClearColor.rgb, sky, _Visibility), 1);
            }
            ENDHLSL
        }
    }
}
