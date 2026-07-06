using UnityEngine.Rendering.Universal;
using UnityEngine;

public class LUTBlenderFeature : ScriptableRendererFeature
{
    public Shader lutBlendShader;
    public Shader lutApplyShader;

    private LUTBlenderPass _pass;

    public RenderTexture GetBlendedLUT() => _pass?.BlendedLUT;

    public override void Create()
    {
        // ✨ Add null checks for shaders
        if (lutBlendShader == null || lutApplyShader == null)
        {
            Debug.LogError("LUTBlenderFeature: Shaders not assigned!");
            return;
        }

        _pass = new LUTBlenderPass
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
        };

        _pass.Setup(lutBlendShader, lutApplyShader);
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        // ✨ Add null check before adding pass
        if (_pass == null)
        {
            Debug.LogWarning("LUTBlenderFeature: Pass is null, skipping");
            return;
        }

        renderer.EnqueuePass(_pass);
    }

    public override void SetupRenderPasses(
    ScriptableRenderer renderer,
    in RenderingData renderingData)
    {
        if (_pass == null) return;

        // ✨ Only pass source — destination is handled internally via temp RT
        _pass.Setup(
            renderer.cameraColorTargetHandle,
            renderer.cameraColorTargetHandle  // ignored now
        );
    }

    protected override void Dispose(bool disposing)
    {
        _pass?.Dispose();
    }
}
