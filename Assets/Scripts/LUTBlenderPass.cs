using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;

public class LUTBlenderPass : ScriptableRenderPass
{
    private LUTBlenderComponent settings;
    private Material blendMaterial;
    private Material applyMaterial;
    private RenderTexture blendedLUT;
    private RTHandle source;

    public RenderTexture BlendedLUT => blendedLUT;

    public void Setup(Shader blendShader, Shader applyShader)
    {
        if (blendShader != null)
            blendMaterial = CoreUtils.CreateEngineMaterial(blendShader);
        if (applyShader != null)
            applyMaterial = CoreUtils.CreateEngineMaterial(applyShader);
    }

    public void Setup(RTHandle source, RTHandle destination)
    {
        this.source = source;
    }

    public override void Execute(
        ScriptableRenderContext context,
        ref RenderingData renderingData)
    {
        if (blendMaterial == null || applyMaterial == null)
        {
            Debug.LogWarning("LUTBlenderPass: Materials not initialized");
            return;
        }

        var stack = VolumeManager.instance.stack;
        settings = stack.GetComponent<LUTBlenderComponent>();

        if (settings == null || !settings.IsActive()) return;
        if (settings.lut1.value == null) return;

        var cmd = CommandBufferPool.Get("LUT Blending");

        // ── Step 1: Bake blended LUT into RT ──────────────────────
        if (blendedLUT == null)
        {
            blendedLUT = new RenderTexture(1024, 32, 0, RenderTextureFormat.ARGBHalf)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            blendedLUT.Create();
        }

        // If lut2 is missing, blend = 0 (just use lut1)
        Texture lut1Tex = settings.lut1.value;
        Texture lut2Tex = settings.lut2.value != null ? settings.lut2.value : lut1Tex;
        float blendAmount = settings.lut2.value != null ? settings.blend.value : 0f;

        blendMaterial.SetTexture("_LUT1", lut1Tex);
        blendMaterial.SetTexture("_LUT2", lut2Tex);
        blendMaterial.SetFloat("_BlendAmount", blendAmount);

        cmd.Blit(null, blendedLUT, blendMaterial);

        // ── Step 2: Apply LUT using a TEMP buffer ─────────────────
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;

        var tempRT = RenderTexture.GetTemporary(
            desc.width,
            desc.height,
            0,
            desc.colorFormat
        );

        // Copy camera → temp
        cmd.Blit(source, tempRT);

        // Apply LUT: temp → camera
        applyMaterial.SetTexture("_BlendedLUT", blendedLUT);
        applyMaterial.SetFloat("_Intensity", settings.intensity.value);

        cmd.Blit(tempRT, source, applyMaterial);

        // Release temp immediately after use
        RenderTexture.ReleaseTemporary(tempRT);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        if (blendedLUT != null)
        {
            blendedLUT.Release();
            Object.Destroy(blendedLUT);
            blendedLUT = null;
        }

        CoreUtils.Destroy(blendMaterial);
        CoreUtils.Destroy(applyMaterial);
    }
}
