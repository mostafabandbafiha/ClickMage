Shader "Custom/GridHighlight"
{
    Properties
    {
        _Color ("Color", Color) = (0, 1, 0, 0.8)
        _CornerSize ("Corner Size", Range(0.01, 0.49)) = 0.15
        _Thickness ("Thickness", Range(0.01, 0.1)) = 0.04
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }

        Pass
        {
            ZTest LEqual
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _Color;
            float _CornerSize;
            float _Thickness;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                float left   = uv.x;
                float right  = 1.0 - uv.x;
                float bottom = uv.y;
                float top    = 1.0 - uv.y;

                float nearLeft   = step(left,   _Thickness);
                float nearRight  = step(right,  _Thickness);
                float nearBottom = step(bottom, _Thickness);
                float nearTop    = step(top,    _Thickness);

                float inCornerX = step(left, _CornerSize) + step(right, _CornerSize);
                float inCornerY = step(bottom, _CornerSize) + step(top, _CornerSize);

                float hBracket = (nearBottom + nearTop) * inCornerX;
                float vBracket = (nearLeft + nearRight) * inCornerY;

                float mask = saturate(hBracket + vBracket);
                return half4(_Color.rgb, mask * _Color.a);
            }
            ENDHLSL
        }
    }
}
