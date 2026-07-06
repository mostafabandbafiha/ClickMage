Shader "Custom/FogVolumeRaymarched"
{
    Properties
    {
        [HDR] _ColorBottom  ("Color Bottom",        Color)            = (0.45,0.65,0.90,1)
        [HDR] _ColorTop     ("Color Top",           Color)            = (0.75,0.88,1,1)
        _HeightGradPow      ("Height Gradient",     Range(0.1,4))     = 1.5

        _NoiseScale         ("Noise Scale",         Float)            = 0.3
        _Speed1             ("Drift Speed 1",       Vector)           = (0.018,0.009,0,0)
        _Speed2             ("Drift Speed 2",       Vector)           = (-0.011,0.016,0,0)
        _SecondaryScale     ("Secondary Scale",     Float)            = 1.6
        _Contrast           ("Contrast",            Range(1,8))       = 2.5
        _Opacity            ("Opacity",             Range(0,1))       = 0.55

        _EdgeFade           ("Edge Fade",           Range(0.01,0.49)) = 0.18
        _GroundFade         ("Ground Fade",         Range(0.01,0.49)) = 0.22
        _DepthSoftness      ("Depth Softness",      Range(0.01,4))    = 0.8

        [Header(Vertex Wave)]
        _WaveHeight         ("Wave Height",         Range(0,0.3))     = 0.06
        _WaveSpeed          ("Wave Speed",          Float)            = 0.6
        _WaveScale          ("Wave Scale",          Float)            = 1.5

        [Header(Raymarching)]
        _Steps              ("March Steps",         Range(8,64))      = 32
        _StepSize           ("Step Size",           Range(0.005,0.1)) = 0.02
        // How much density accumulated per step — controls the
        // hollow-light / thick-fog feel. Low = wispy, high = opaque.
        _DensityMult        ("Density Multiplier",  Range(0.1,4))     = 1.2

        [Header(Debug)]
        _IgnoreDepth        ("Debug Ignore Depth",  Float)            = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"  = "UniversalPipeline"
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent+1"
            "IgnoreProjector" = "True"
        }

        // Single pass — ray marches from back to front internally
        Pass
        {
            Name "FogRaymarch"
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Front          // render back faces so we can enter the volume

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorBottom, _ColorTop;
                float  _HeightGradPow;
                float  _NoiseScale;
                float4 _Speed1, _Speed2;
                float  _SecondaryScale;
                float  _Contrast, _Opacity;
                float  _EdgeFade, _GroundFade;
                float  _DepthSoftness;
                float  _WaveHeight, _WaveSpeed, _WaveScale;
                float  _Steps, _StepSize;
                float  _DensityMult;
                float  _IgnoreDepth;
            CBUFFER_END

            // ── Noise ───────────────────────────────────────────────

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453);
            }
            float ValueNoise(float2 p)
            {
                float2 i=floor(p), f=frac(p), u=f*f*(3.0-2.0*f);
                return lerp(lerp(Hash(i),           Hash(i+float2(1,0)),u.x),
                            lerp(Hash(i+float2(0,1)),Hash(i+float2(1,1)),u.x),u.y);
            }
            float VNoise2(float2 p)
            {
                return ValueNoise(p)*0.65 + ValueNoise(p*2.1+5.7)*0.35;
            }

            float2 GradHash(float2 p)
            {
                p = float2(dot(p,float2(127.1,311.7)),dot(p,float2(269.5,183.3)));
                return -1.0 + 2.0*frac(sin(p)*43758.5453);
            }
            float GradNoise(float2 p)
            {
                float2 i=floor(p), f=frac(p), u=f*f*(3.0-2.0*f);
                float a=dot(GradHash(i),f), b=dot(GradHash(i+float2(1,0)),f-float2(1,0));
                float c=dot(GradHash(i+float2(0,1)),f-float2(0,1));
                float d=dot(GradHash(i+float2(1,1)),f-float2(1,1));
                return saturate(lerp(lerp(a,b,u.x),lerp(c,d,u.x),u.y)*0.5+0.5);
            }
            float FBM(float2 p)
            {
                float v=0, a=0.5;
                float2x2 rot=float2x2(0.8,-0.6,0.6,0.8);
                [unroll] for(int i=0;i<3;i++){ v+=a*GradNoise(p); p=mul(rot,p)*2.1; a*=0.5; }
                return saturate(v);
            }

            // Density at a given world-space point inside the volume.
            // This is what you sample at every raymarch step.
            float SampleDensity(float3 worldPos, float3 uvw)
            {
                float2 uv1 = worldPos.xz * _NoiseScale + _Time.y * _Speed1.xy;
                float2 uv2 = worldPos.xz * _NoiseScale * _SecondaryScale
                             + _Time.y * _Speed2.xy + float2(17.3,5.9);

                float n = saturate(FBM(uv1) * FBM(uv2) * 2.0);
                      n = saturate(pow(abs(n), _Contrast));

                // Volume shape: edges fade to 0, ground fades up, top thins
                float fadeXZ   = smoothstep(0.0, _EdgeFade, uvw.x)
                               * smoothstep(1.0, 1.0-_EdgeFade, uvw.x)
                               * smoothstep(0.0, _EdgeFade, uvw.z)
                               * smoothstep(1.0, 1.0-_EdgeFade, uvw.z);
                float fadeY    = smoothstep(0.0, _GroundFade, uvw.y)
                               * pow(abs(1.0 - uvw.y), _HeightGradPow);

                return n * fadeXZ * fadeY;
            }

            // ── AABB ray-box intersection ────────────────────────────
            // Returns the entry and exit distances along the ray.
            // box is a unit cube in local space: min=-0.5, max=0.5
            // We transform the ray into local space to keep it simple.
            bool RayBox(float3 rayOriginLS, float3 rayDirLS,
                        out float tMin, out float tMax)
            {
                float3 invDir = 1.0 / rayDirLS;
                float3 t0 = (-0.5 - rayOriginLS) * invDir;
                float3 t1 = ( 0.5 - rayOriginLS) * invDir;
                float3 tNear = min(t0, t1);
                float3 tFar  = max(t0, t1);
                tMin = max(max(tNear.x, tNear.y), tNear.z);
                tMax = min(min(tFar.x,  tFar.y),  tFar.z);
                return tMax > max(tMin, 0.0);
            }

            // ── Structs ─────────────────────────────────────────────

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv0        : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                // entry point on the back face in world space
                float3 entryWS     : TEXCOORD1;
                float4 positionNDC : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── Vertex: wave only, no distortion ────────────────────

            Varyings Vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 worldBase = TransformObjectToWorld(IN.positionOS.xyz);
                float3 worldUp   = normalize(TransformObjectToWorldDir(float3(0,1,0)));

                float2 waveUV = worldBase.xz * _WaveScale;
                float  wave   = VNoise2(waveUV + _Time.y * _WaveSpeed);
                       wave   = (wave * 2.0 - 1.0) * _WaveHeight;

                float3 displaced = worldBase + worldUp * wave;
                float3 posOS     = TransformWorldToObject(displaced);

                VertexPositionInputs vpi = GetVertexPositionInputs(posOS);

                OUT.positionCS  = vpi.positionCS;
                OUT.positionWS  = vpi.positionWS;
                OUT.entryWS     = vpi.positionWS; // back face = entry point
                OUT.positionNDC = vpi.positionNDC;
                return OUT;
            }

            // ── Fragment: the actual raymarch ────────────────────────

            float4 Frag(Varyings IN) : SV_Target
                {
                    float2 screenUV = IN.positionNDC.xy / IN.positionNDC.w;
                    float  rawDepth = SampleSceneDepth(screenUV);
                    float3 sceneWS  = ComputeWorldSpacePosition(screenUV, rawDepth, UNITY_MATRIX_I_VP);
                    float  sceneDepth = distance(_WorldSpaceCameraPos, sceneWS);

                    float3 camPos  = _WorldSpaceCameraPos;
                    float3 rayDir  = normalize(IN.entryWS - camPos);

                    float4x4 w2o      = UNITY_MATRIX_I_M;
                    float3 rayOriginLS = mul(w2o, float4(camPos, 1)).xyz;
                    float3 rayDirLS    = normalize(mul((float3x3)w2o, rayDir));

                    float tMin, tMax;
                    if (!RayBox(rayOriginLS, rayDirLS, tMin, tMax))
                        discard;

                    tMin = max(tMin, 0.001);

                    int   steps    = (int)clamp(_Steps, 8, 64);
                    float stepSize = _StepSize;

                    // ── Jitter: offset start by a random fraction of one step ────────
                    // Hash screen pixel position — cheap, no texture needed.
                    // Each pixel gets a different sub-step offset so adjacent pixels
                    // cover different sample positions, breaking up banding at low
                    // step counts without any extra loop iterations.
                    float2 px     = screenUV * _ScreenParams.xy;
                    float jitter = frac(52.9829189 * frac(dot(px, float2(0.06711056, 0.00583715))));
                    float  t      = tMin + jitter * stepSize;   // ← was: float t = tMin;
                    // ─────────────────────────────────────────────────────────────────

                    float3 accumColor = 0;
                    float  accumAlpha = 0;

                    float worldScale = length(mul((float3x3)UNITY_MATRIX_M, float3(1,0,0)));

                    [loop]
                    for (int i = 0; i < steps; i++)
                    {
                        if (t >= tMax) break;
                        if (accumAlpha >= 0.99) break;

                        float3 posLS   = rayOriginLS + rayDirLS * t;
                        float3 posWS   = TransformObjectToWorld(posLS);

                        float rayDepth = distance(camPos, posWS);
                        if (_IgnoreDepth < 0.5 && rayDepth > sceneDepth)
                            break;

                        float3 uvw    = posLS + 0.5;
                        float density = SampleDensity(posWS, uvw) * _DensityMult * stepSize * worldScale;

                        float transmittance = exp(-density * 8.0);
                        float alpha         = 1.0 - transmittance;
                        float3 stepColor    = lerp(_ColorBottom.rgb, _ColorTop.rgb, saturate(uvw.y));

                        accumColor += (1.0 - accumAlpha) * alpha * stepColor;
                        accumAlpha += (1.0 - accumAlpha) * alpha;

                        t += stepSize;
                    }

                    accumAlpha *= _Opacity;
                    return float4(accumColor, saturate(accumAlpha));
                }

            ENDHLSL
        }
    }
}