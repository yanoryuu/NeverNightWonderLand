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
    public class MotionBlurRenderFeature : ScriptableRendererFeatureSimpleShaderBase<MotionBlurRenderFeature.MotionBlurRenderPass>
    {
#if URP_17_OR_NEWER
        protected override string ShaderGraphPath => ("Utage/PostEffect/MotionBlur Shader Graph");
#endif
        protected override string ShaderPath => ("Utage/PostEffect/MotionBlur");
        protected override MotionBlurRenderPass CreatePass()
        {
            return new MotionBlurRenderPass(GetShader());
        }

        public class MotionBlurRenderPass : ScriptableRenderPassSimpleBase<MotionBlurVolume>
        {
            public MotionBlurRenderPass(Shader shader)
                : base(shader, nameof(MotionBlurRenderPass))
            {
            }
#if URP_17_OR_NEWER
            public class PassData : MainPassDataBase<MotionBlurVolume>
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
            protected override string CommandBufferName => nameof(MotionBlurRenderPass);
            protected override void SetCommandBuffer(ScriptableRenderContext context, ref RenderingData renderingData, MotionBlurVolume volume, CommandBuffer commandBuffer)
            {
                Material.SetFloat(ShaderPropertyID.Strength, volume.Strength);
                ApplySimpleMaterial(context, ref renderingData, commandBuffer, Material);
            }
        }
    }
}

#endif
