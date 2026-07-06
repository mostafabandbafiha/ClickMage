Shader "Custom/GrayscaleItem"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _GrayscaleAmount ("Grayscale Amount", Range(0, 1)) = 0
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
        }
        
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _GrayscaleAmount;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv);
                
                // Convert to grayscale using luminance formula
                float grayscale = dot(color.rgb, float3(0.299, 0.587, 0.114));
                fixed3 grayscaleColor = fixed3(grayscale, grayscale, grayscale);
                
                // Blend between original and grayscale
                color.rgb = lerp(color.rgb, grayscaleColor, _GrayscaleAmount);
                
                // Apply vertex color
                color *= i.color;
                
                return color;
            }
            ENDCG
        }
    }
    
    Fallback "Sprites/Default"
}