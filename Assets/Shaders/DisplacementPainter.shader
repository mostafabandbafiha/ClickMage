Shader "Custom/DisplacementPainter"
{
    Properties
    {
        _MoveDir ("Move Direction", Vector) = (0,0,0,0)
        _Strength ("Strength", Float) = 1.0
        _EdgeSoftness ("Edge Softness", Float) = 0.15
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend One One
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _MoveDir;
            float _Strength;
            float _EdgeSoftness;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // remap uv from [0,1] to [-1,1]
                float2 centered = i.uv * 2.0 - 1.0;
                float dist = length(centered);

                // smooth circle falloff, discard outside radius
                float circle = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0, dist);
                if (circle <= 0.001) discard;

                float2 dir = normalize(_MoveDir.xy + float2(0.0001, 0.0001));
                return float4(dir.x * 0.5 + 0.5, dir.y * 0.5 + 0.5, _Strength * circle, circle);
            }
            ENDCG
        }
    }
}
