Shader "Hidden/DisplacementFade"
{
    Properties
    {
        _MainTex    ("Prev Frame", 2D)  = "black" {}
        _NewFrame   ("New Frame",  2D)  = "black" {}
        _FadeAmount ("Fade",       Float) = 0.97
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex;
            sampler2D _NewFrame;
            float     _FadeAmount;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 prev  = tex2D(_MainTex, i.uv) * _FadeAmount;
                fixed4 fresh = tex2D(_NewFrame, i.uv);
                return max(prev, fresh);
            }
            ENDHLSL
        }
    }
}
