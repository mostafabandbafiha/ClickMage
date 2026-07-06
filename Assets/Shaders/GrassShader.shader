Shader "Custom/Grass"
{
    Properties
    {
        // --- Base Colors ---
        _BaseColor          ("Base Color",          Color)          = (0.2, 0.8, 0.2, 1)
        _TipColor           ("Tip Color",           Color)          = (0.1, 0.6, 0.1, 1)

        // --- World-Space XZ Gradient ---
        // Color at the "start" end of the gradient direction
        _GradientBottomColor ("Gradient Start Color",  Color)       = (0.04, 0.12, 0.04, 1)
        // Color at the "end" end of the gradient direction
        _GradientTopColor    ("Gradient End Color",    Color)       = (0.45, 0.95, 0.35, 1)
        // Arrow direction in XZ — e.g. (1,0,1,0) = diagonal NE
        _GradientDirection   ("Gradient Direction XZ", Vector)      = (1, 0, 1, 0)
        // World-space center of the gradient band
        _GradientCenter      ("Gradient Center XZ",    Vector)      = (0, 0, 0, 0)
        // Width of the gradient band in world units
        _GradientWidth       ("Gradient Width",        Float)       = 20.0

        // --- Wind ---
        _WindStrength       ("Wind Strength",       Range(0, 1))    = 0.2
        _WindSpeed          ("Wind Speed",          Range(0, 50))   = 1
        _WindDirection      ("Wind Direction",      Vector)         = (1, 0, 0.6, 0)
        _WindNoiseScale     ("Wind Noise Scale",    Range(0.01, 1)) = 0.05
        _WindTintStrength   ("Wind Tint Strength",  Range(0, 1))    = 0.4
        _WindTint           ("Wind Tint",           Color)          = (0.9, 0.75, 0.3, 1)

        // --- Displacement ---
        _DisplacementStrength ("Displacement Strength", Range(0, 2)) = 1.0
        _RTResolution         ("RT Resolution",         Float)       = 1024

        // --- Lighting ---
        _AmbientStrength    ("Ambient Strength",    Range(0, 1))    = 0.3
        _LightWrap          ("Light Wrap",          Range(0, 1))    = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"            = "Transparent"
            "Queue"                 = "Transparent"
            "RenderPipeline"        = "UniversalPipeline"
            "IgnoreProjector"       = "True"
        }
        LOD 200

        // =========================================================
        //  PASS 1 — URP Forward Lit  (receives + casts shadows)
        // =========================================================
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            // URP shadow keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ----- Properties (CBUFFER required in URP) -----
            CBUFFER_START(UnityPerMaterial)
                half4   _BaseColor;
                half4   _TipColor;

                half4   _GradientBottomColor;
                half4   _GradientTopColor;
                float4  _GradientDirection;
                float4  _GradientCenter;
                float   _GradientWidth;

                float   _WindStrength;
                float   _WindSpeed;
                float4  _WindDirection;
                float   _WindNoiseScale;
                float   _WindTintStrength;
                half4   _WindTint;

                float   _DisplacementStrength;
                float   _RTResolution;

                float   _AmbientStrength;
                float   _LightWrap;
            CBUFFER_END

            // Displacement texture — declared outside CBUFFER (samplers can't go inside)
            TEXTURE2D(_DisplacementTex);
            SAMPLER(sampler_DisplacementTex);

            // Set globally by GrassDisplacementManager — keep outside CBUFFER
            float4 _DisplacementBoundsCenter;
            float4 _DisplacementBoundsSize;

            // ----- Structs -----
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 color        : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float  heightT      : TEXCOORD0;
                float  windAmt      : TEXCOORD1;
                float2 worldXZ      : TEXCOORD2;    // XZ position for diagonal gradient
                float3 normalWS     : TEXCOORD3;
                float3 positionWS   : TEXCOORD4;   // needed for shadow coord in URP
            };

            // ----- Helpers -----
            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash2(i + float2(0,0)).x;
                float b = hash2(i + float2(1,0)).x;
                float c = hash2(i + float2(0,1)).x;
                float d = hash2(i + float2(1,1)).x;
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // ----- Vertex -----
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 worldPos         = TransformObjectToWorld(IN.positionOS.xyz);
                float3 originalWorldPos = worldPos;
                float  mask             = IN.color.r;   // 0=root, 1=tip

                // --- Displacement ---
                float2 dispUV = (originalWorldPos.xz - _DisplacementBoundsCenter.xz)
                                / _DisplacementBoundsSize.x + 0.5;
                dispUV = saturate(dispUV);

                float texelSize = 1.0 / _RTResolution;
                float center = SAMPLE_TEXTURE2D_LOD(_DisplacementTex, sampler_DisplacementTex, dispUV,                              0).r;
                float left   = SAMPLE_TEXTURE2D_LOD(_DisplacementTex, sampler_DisplacementTex, dispUV + float2(-texelSize, 0), 0).r;
                float right  = SAMPLE_TEXTURE2D_LOD(_DisplacementTex, sampler_DisplacementTex, dispUV + float2( texelSize, 0), 0).r;
                float down   = SAMPLE_TEXTURE2D_LOD(_DisplacementTex, sampler_DisplacementTex, dispUV + float2(0, -texelSize), 0).r;
                float up     = SAMPLE_TEXTURE2D_LOD(_DisplacementTex, sampler_DisplacementTex, dispUV + float2(0,  texelSize), 0).r;

                float2 grad    = float2(right - left, up - down);
                float  gradLen = length(grad);
                float2 pushDir = gradLen > 0.001 ? -normalize(grad) : float2(0, 0);

                float dispStrength = smoothstep(0.0, 0.3, center) * center;
                float3 dispOffset  = float3(pushDir.x, -0.2, pushDir.y)
                                     * dispStrength * _DisplacementStrength * mask;
                worldPos += dispOffset;

                // --- Wind ---
                float2 windDir2D = normalize(_WindDirection.xz);
                float2 windUV    = originalWorldPos.xz * _WindNoiseScale
                                   + windDir2D * _Time.y * _WindSpeed * 0.15;
                float2 windUV2   = originalWorldPos.xz * _WindNoiseScale * 2.3
                                   + windDir2D * _Time.y * _WindSpeed * 0.09
                                   + float2(3.7, 1.3);

                float noise1   = valueNoise(windUV);
                float noise2   = valueNoise(windUV2);
                float combined = noise1 * 0.7 + noise2 * 0.3;

                float windOffset = (combined * 2.0 - 1.0) * _WindStrength * mask;
                worldPos.xz += IN.normalOS.xz * windOffset;

                OUT.positionHCS = TransformWorldToHClip(worldPos);
                OUT.positionWS  = worldPos;
                OUT.heightT     = mask;
                OUT.windAmt     = saturate(combined) * mask;
                OUT.worldXZ     = worldPos.xz;          // pass XZ for gradient
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);

                return OUT;
            }

            // ----- Fragment -----
            half4 frag(Varyings IN) : SV_Target
            {
                // 1. Blade-height gradient (local, root→tip)
                half4 col = lerp(_BaseColor, _TipColor, IN.heightT);

                // 2. World-space XZ diagonal gradient
                //    Project the fragment's XZ offset from center onto the gradient direction
                float2 gradDir  = normalize(_GradientDirection.xz);
                float2 offsetXZ = IN.worldXZ - _GradientCenter.xz;
                float  proj     = dot(offsetXZ, gradDir);           // signed distance along direction
                float  gradT    = saturate(proj / max(_GradientWidth, 0.001) + 0.5);
                col.rgb *= lerp(_GradientBottomColor.rgb, _GradientTopColor.rgb, gradT);

                // 3. Fake AO — roots darker
                col.rgb *= lerp(_AmbientStrength, 1.0, IN.heightT);

                // 4. URP main light + shadow
                //    GetMainLight(shadowCoord) returns light.color and light.shadowAttenuation
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light  mainLight   = GetMainLight(shadowCoord);

                float3 normalWS   = normalize(IN.normalWS);
                float  NdotL      = dot(normalWS, mainLight.direction);
                float  halfLambert = saturate(NdotL * (1.0 - _LightWrap) + _LightWrap);

                // shadowAttenuation is 0 (full shadow) .. 1 (fully lit)
                col.rgb *= halfLambert * mainLight.color * mainLight.shadowAttenuation;

                // 5. Wind tint
                col.rgb = lerp(col.rgb, col.rgb * _WindTint.rgb,
                               IN.windAmt * _WindTintStrength);

                col.a = 0.85;
                return col;
            }

            ENDHLSL
        }

        // =========================================================
        //  PASS 2 — Shadow Caster  (grass casts shadows onto ground)
        // =========================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float   _WindStrength;
                float   _WindSpeed;
                float4  _WindDirection;
                float   _WindNoiseScale;
                float   _DisplacementStrength;
                float   _RTResolution;
                // unused in shadow pass but must match the CBUFFER layout
                half4   _BaseColor;
                half4   _TipColor;
                half4   _GradientBottomColor;
                half4   _GradientTopColor;
                float4  _GradientDirection;
                float4  _GradientCenter;
                float   _GradientWidth;
                float   _WindTintStrength;
                half4   _WindTint;
                float   _AmbientStrength;
                float   _LightWrap;
            CBUFFER_END

            TEXTURE2D(_DisplacementTex);
            SAMPLER(sampler_DisplacementTex);
            float4 _DisplacementBoundsCenter;
            float4 _DisplacementBoundsSize;

            // URP shadow caster needs this built-in
            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float2 hash2s(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }
            float valueNoiseS(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = hash2s(i + float2(0,0)).x;
                float b = hash2s(i + float2(1,0)).x;
                float c = hash2s(i + float2(0,1)).x;
                float d = hash2s(i + float2(1,1)).x;
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings vertShadow(Attributes IN)
            {
                Varyings OUT;

                float3 worldPos         = TransformObjectToWorld(IN.positionOS.xyz);
                float3 originalWorldPos = worldPos;
                float  mask             = IN.color.r;

                // --- Displacement (mirror main pass) ---
                float2 dispUV = (originalWorldPos.xz - _DisplacementBoundsCenter.xz)
                                / _DisplacementBoundsSize.x + 0.5;
                dispUV = saturate(dispUV);
                float texelSize = 1.0 / _RTResolution;
                float center = SAMPLE_TEXTURE2D_LOD(_DisplacementTex, sampler_DisplacementTex, dispUV,                              0).r;
                float left   = SAMPLE_TEXTURE2D_LOD(_DisplacementTex, sampler_DisplacementTex, dispUV + float2(-texelSize, 0), 0).r;
                float right  = SAMPLE_TEXTURE2D_LOD(_DisplacementTex, sampler_DisplacementTex, dispUV + float2( texelSize, 0), 0).r;
                float down   = SAMPLE_TEXTURE2D_LOD(_DisplacementTex, sampler_DisplacementTex, dispUV + float2(0, -texelSize), 0).r;
                float up     = SAMPLE_TEXTURE2D_LOD(_DisplacementTex, sampler_DisplacementTex, dispUV + float2(0,  texelSize), 0).r;

                float2 grad    = float2(right - left, up - down);
                float  gradLen = length(grad);
                float2 pushDir = gradLen > 0.001 ? -normalize(grad) : float2(0, 0);
                float  dispStr = smoothstep(0.0, 0.3, center) * center;
                worldPos += float3(pushDir.x, -0.2, pushDir.y) * dispStr * _DisplacementStrength * mask;

                // --- Wind (mirror main pass so shadow matches blade) ---
                float2 windDir2D = normalize(_WindDirection.xz);
                float2 windUV    = originalWorldPos.xz * _WindNoiseScale
                                   + windDir2D * _Time.y * _WindSpeed * 0.15;
                float2 windUV2   = originalWorldPos.xz * _WindNoiseScale * 2.3
                                   + windDir2D * _Time.y * _WindSpeed * 0.09
                                   + float2(3.7, 1.3);
                float combined   = valueNoiseS(windUV) * 0.7 + valueNoiseS(windUV2) * 0.3;
                float windOffset = (combined * 2.0 - 1.0) * _WindStrength * mask;
                worldPos.xz += IN.normalOS.xz * windOffset;

                // URP shadow caster clip position — applies shadow bias automatically
                float3 normalWS     = TransformObjectToWorldNormal(IN.normalOS);
                float4 positionHCS  = TransformWorldToHClip(
                    ApplyShadowBias(worldPos, normalWS, _LightDirection)
                );

                // Clamp depth so shadow doesn't clip on near plane
                #if UNITY_REVERSED_Z
                    positionHCS.z = min(positionHCS.z, positionHCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionHCS.z = max(positionHCS.z, positionHCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                OUT.positionHCS = positionHCS;
                return OUT;
            }

            half4 fragShadow(Varyings IN) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
