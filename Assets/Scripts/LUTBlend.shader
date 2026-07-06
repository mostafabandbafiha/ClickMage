Shader "Hidden/LUTBlend"
{
    Properties
    {
        _LUT1        ("LUT 1", 2D)   = "white" {}
        _LUT2        ("LUT 2", 2D)   = "white" {}
        _BlendAmount ("_BlendAmount", Float) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings   { float4 positionHCS : SV_POSITION; float2 uv : TEXCOORD0; };

            TEXTURE2D(_LUT1); SAMPLER(sampler_LUT1);
            TEXTURE2D(_LUT2); SAMPLER(sampler_LUT2);
            float _BlendAmount;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 c1 = SAMPLE_TEXTURE2D(_LUT1, sampler_LUT1, IN.uv);
                float4 c2 = SAMPLE_TEXTURE2D(_LUT2, sampler_LUT2, IN.uv);
                return lerp(c1, c2, _BlendAmount);
            }
            ENDHLSL
        }
    }
}
