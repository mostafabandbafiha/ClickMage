using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FPL
{
    internal class URP_EnableDepthNormals : ScriptableRendererFeature
    {

        private EnableDepthNormalsPass m_depthNormalsPass = null;

        public override void Create()
        {
            // Create the pass...
            if (m_depthNormalsPass == null) m_depthNormalsPass = new EnableDepthNormalsPass ();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            bool shouldAdd = m_depthNormalsPass.Setup (renderer);
            if (shouldAdd) renderer.EnqueuePass (m_depthNormalsPass);
        }

        private class EnableDepthNormalsPass : ScriptableRenderPass
        {
            internal bool Setup(ScriptableRenderer renderer)
            {
                ConfigureInput (ScriptableRenderPassInput.Normal);
                return true;
            }
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }

#if UNITY_6000_0_OR_NEWER
            public override void RecordRenderGraph(UnityEngine.Rendering.RenderGraphModule.RenderGraph renderGraph, ContextContainer frameData) { }
#endif
        }
    }
}
