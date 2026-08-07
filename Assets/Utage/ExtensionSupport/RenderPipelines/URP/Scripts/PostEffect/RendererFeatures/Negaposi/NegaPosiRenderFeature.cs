#if UTAGE_URP
#if URP_17_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Utage.RenderPipeline.Urp
{
    //カラーフェード用のScriptableRendererFeature
    public class NegaPosiRenderFeature : ScriptableRendererFeatureSimpleShaderBase<NegaPosiRenderFeature.NegaposiRenderPass>
    {
#if URP_17_OR_NEWER
        protected override string ShaderGraphPath => ("Utage/PostEffect/Negaposi Shader Graph");
#endif
        protected override string ShaderPath => ("Utage/PostEffect/NegaPosi");
        
        protected override NegaposiRenderPass CreatePass()
        {
            return new NegaposiRenderPass(GetShader());
        }

        public class NegaposiRenderPass : ScriptableRenderPassSimpleBase<NegaPosiVolume>
        {
            public NegaposiRenderPass(Shader shader)
                : base(shader, nameof(NegaposiRenderPass))
            {
            }
#if URP_17_OR_NEWER
            public class PassData : MainPassDataBase<NegaPosiVolume>
            {
                protected override void ApplyVolumeComponent(RasterGraphContext context, MaterialPropertyBlock propertyBlock)
                {
                }
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                RecordRenderGraphSub<PassData>(renderGraph, frameData);
            }

#endif
            protected override string CommandBufferName => nameof(NegaposiRenderPass);
            protected override void SetCommandBuffer(ScriptableRenderContext context, ref RenderingData renderingData, NegaPosiVolume volume, CommandBuffer commandBuffer)
            {
                ApplySimpleMaterial(context, ref renderingData, commandBuffer, Material);
            }
        }
    }
}

#endif
