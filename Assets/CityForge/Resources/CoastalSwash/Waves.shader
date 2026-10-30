Shader "Crest/Inputs/Animated Waves/CityForge Shallow Swash" {
 Properties { _SwashTime("Swash time",Float)=0 }
 SubShader { Blend One One Cull Off ZWrite Off ZTest Always
 Pass { CGPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "UnityCG.cginc"
 #include "Swash.hlsl"
 CBUFFER_START(CrestPerWaterInput)
 float _Crest_Weight;
 float _Crest_SimDeltaTime;
 float3 _Crest_DisplacementAtInputPosition;
 CBUFFER_END
 struct Vertex {float4 position:POSITION;float2 shore:TEXCOORD0;float2 ground:TEXCOORD1;};
 struct Pixel {float4 position:SV_POSITION;float2 shore:TEXCOORD0;float ground:TEXCOORD1;};
 Pixel Vert(Vertex v){Pixel p;float4 world=mul(unity_ObjectToWorld,v.position);world.xz-=_Crest_DisplacementAtInputPosition.xz;p.position=mul(UNITY_MATRIX_VP,world);p.shore=v.shore;p.ground=v.ground.x;return p;}
 float4 Frag(Pixel i):SV_Target {float height=SwashHeight(i.shore);
    return float4(0,height*_Crest_Weight,0,0);
 }
 ENDCG }
 }
}
