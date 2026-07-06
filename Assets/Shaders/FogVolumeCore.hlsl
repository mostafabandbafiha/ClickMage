#ifndef FOG_VOLUME_CORE
#define FOG_VOLUME_CORE

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

// ── Textures ─────────────────────────────────────────────────────────
TEXTURE2D(_TexA); SAMPLER(sampler_TexA);
TEXTURE2D(_TexB); SAMPLER(sampler_TexB);

// ── Constant buffer ───────────────────────────────────────────────────
CBUFFER_START(UnityPerMaterial)
    float4 _ColorBottom;
    float4 _ColorTop;
    float  _HeightGradPow;

    float4 _TexA_ST;
    float4 _TexB_ST;
    float  _TexBlend;

    float  _NoiseScale;
    float4 _Speed1;
    float4 _Speed2;
    float  _SecondaryScale;

    float  _Contrast;
    float  _Opacity;
    float  _EdgeFadeX;
    float  _EdgeFadeY;
    float  _EdgeFadeZ;
    float  _DepthSoftness;
CBUFFER_END

// ── Structs ───────────────────────────────────────────────────────────
struct Attributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS  : SV_POSITION;
    float3 positionWS  : TEXCOORD0;
    // Local-space stored in [-0.5, 0.5] — Unity's built-in cube uses this range
    float3 positionLS  : TEXCOORD1;
    float4 positionNDC : TEXCOORD2;
    UNITY_VERTEX_OUTPUT_STEREO
};

// ── Helpers ───────────────────────────────────────────────────────────

// Smooth axis-aligned edge fade
// localT is 0..1 inside the box on one axis, fade is the fraction to fade over
float AxisFade(float localT, float fade)
{
    float lo = smoothstep(0.0, fade, localT);
    float hi = smoothstep(1.0, 1.0 - fade, localT);
    return lo * hi;
}

// Sample one noise texture with two drifting layers, multiply for wispiness
float SampleFogTex(TEXTURE2D_PARAM(tex, smp), float2 worldUV)
{
    float2 uv1 = worldUV + _Time.y * _Speed1.xy;
    float2 uv2 = worldUV * _SecondaryScale + _Time.y * _Speed2.xy + float2(17.3, 5.9);

    float n1 = SAMPLE_TEXTURE2D(tex, smp, uv1).r;
    float n2 = SAMPLE_TEXTURE2D(tex, smp, uv2).r;

    // Multiply creates patches; remap from [0..1] * [0..1] = [0..1]
    return saturate(n1 * n2 * 2.0);
}

// ── Vertex ────────────────────────────────────────────────────────────
Varyings Vert(Attributes IN)
{
    UNITY_SETUP_INSTANCE_ID(IN);
    Varyings OUT;
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

    VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
    OUT.positionCS  = vpi.positionCS;
    OUT.positionWS  = vpi.positionWS;
    OUT.positionLS  = IN.positionOS.xyz;   // Unity cube: -0.5 to +0.5
    OUT.positionNDC = vpi.positionNDC;
    return OUT;
}

// ── Fragment ──────────────────────────────────────────────────────────
float4 Frag(Varyings IN) : SV_Target
{
    // ── World XZ UV (camera-independent) ──────────────────────────
    float2 worldUV = IN.positionWS.xz * _NoiseScale;

    // ── Sample both textures, blend for day/night ──────────────────
    float noiseA = SampleFogTex(TEXTURE2D_ARGS(_TexA, sampler_TexA), worldUV);
    float noiseB = SampleFogTex(TEXTURE2D_ARGS(_TexB, sampler_TexB), worldUV);
    float noise  = lerp(noiseA, noiseB, _TexBlend);

    // Stylized contrast — sharper edges for non-realistic look
    noise = saturate(pow(abs(noise), _Contrast));

    // ── Local-space 0..1 for volume fades ─────────────────────────
    float3 localUV = IN.positionLS + 0.5;   // remap to [0, 1]

    // Per-axis edge softness — independent control per dimension
    float fadeX = AxisFade(localUV.x, _EdgeFadeX);
    float fadeY = AxisFade(localUV.y, _EdgeFadeY);
    float fadeZ = AxisFade(localUV.z, _EdgeFadeZ);

    // Separate height gradient: fog is always denser at the bottom
    // of the volume regardless of edge fade
    float heightGrad = pow(1.0 - localUV.y, _HeightGradPow);

    float volumeAlpha = fadeX * fadeY * fadeZ * heightGrad;

    // ── Soft depth intersection ────────────────────────────────────
    float2 screenUV      = IN.positionNDC.xy / IN.positionNDC.w;
    float  rawDepth      = SampleSceneDepth(screenUV);
    float  sceneDepth    = LinearEyeDepth(rawDepth, _ZBufferParams);
    float  fragDepth     = IN.positionNDC.w;
    float  depthFade     = saturate((sceneDepth - fragDepth) / _DepthSoftness);
    depthFade            = depthFade * depthFade;   // ease-in curve

    // ── Combine ────────────────────────────────────────────────────
    float alpha = noise * volumeAlpha * depthFade * _Opacity;

    // ── Color: vertical gradient in local space ────────────────────
    float4 color = lerp(_ColorBottom, _ColorTop, saturate(localUV.y));

    return float4(color.rgb, alpha);
}

#endif