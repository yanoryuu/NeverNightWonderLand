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
    //Grayscale用のScriptableRendererFeature
    public class GrayScaleRenderFeature : ScriptableRendererFeatureSimpleShaderBase<GrayScaleRenderFeature.GrayscaleRenderPass>
    {
#if URP_17_OR_NEWER
        protected override string ShaderGraphPath => ("Utage/PostEffect/Grayscale Shader Graph");
#endif
        protected override string ShaderPath => ("Utage/PostEffect/GrayScale");
        
        protected override GrayscaleRenderPass CreatePass()
        {
            return new GrayscaleRenderPass(GetShader());
        }

        public class GrayscaleRenderPass : ScriptableRenderPassSimpleBase<GrayScaleVolume>
        {

            public GrayscaleRenderPass(Shader shader)
                : base(shader, nameof(GrayscaleRenderPass))
            {
            }

#if URP_17_OR_NEWER
            public class PassData : MainPassDataBase<GrayScaleVolume>
            {
                protected override void ApplyVolumeComponent(RasterGraphContext context, MaterialPropertyBlock propertyBlock)
                {
                    var volume = this.GetVolumeComponent();
                    propertyBlock.SetFloat(ShaderPropertyID.Strength, volume.Strength);
                }
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                RecordRenderGraphSub<PassData>(renderGraph, frameData);
            }
#endif
            protected override string CommandBufferName => nameof(GrayscaleRenderPass);
            protected override void SetCommandBuffer(ScriptableRenderContext context, ref RenderingData renderingData, GrayScaleVolume volume, CommandBuffer commandBuffer)
            {
                Material.SetFloat(ShaderPropertyID.Strength, volume.Strength);
                ApplySimpleMaterial(context, ref renderingData, commandBuffer, Material);
            }
        }
    }
}

#endif
