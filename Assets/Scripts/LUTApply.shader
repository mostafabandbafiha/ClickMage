Shader "Hidden/LUTApply"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlendedLUT ("Blended LUT", 2D) = "white" {}
        _Intensity ("Intensity", Range(0, 1)) = 1
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "LUT Apply"
            ZTest Always ZWrite Off Cull Off
            
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
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            TEXTURE2D(_BlendedLUT);
            SAMPLER(sampler_BlendedLUT);
            
            float _Intensity;
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            
            float3 ApplyLUT(float3 color, TEXTURE2D(lut), SAMPLER(samplerLut))
            {
                const float lutSize = 32.0;
                const float lutWidth = 1024.0;
                
                // Scale color to LUT space
                float3 scaledColor = saturate(color) * (lutSize - 1.0);
                
                // Calculate blue slice position
                float blueSlice = floor(scaledColor.b);
                float blueOffset = scaledColor.b - blueSlice;
                
                // Calculate UV for first slice
                float2 uv1;
                uv1.x = (blueSlice * lutSize + scaledColor.r + 0.5) / lutWidth;
                uv1.y = (scaledColor.g + 0.5) / lutSize;
                
                // Calculate UV for second slice
                float2 uv2;
                uv2.x = ((blueSlice + 1.0) * lutSize + scaledColor.r + 0.5) / lutWidth;
                uv2.y = uv1.y;
                
                // Sample and interpolate
                float3 color1 = SAMPLE_TEXTURE2D(lut, samplerLut, uv1).rgb;
                float3 color2 = SAMPLE_TEXTURE2D(lut, samplerLut, uv2).rgb;
                
                return lerp(color1, color2, blueOffset);
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample original color
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Apply LUT
                float3 gradedColor = ApplyLUT(color.rgb, _BlendedLUT, sampler_BlendedLUT);
                
                // Blend with original based on intensity
                color.rgb = lerp(color.rgb, gradedColor, _Intensity);
                
                return color;
            }
            ENDHLSL
        }
    }
}
