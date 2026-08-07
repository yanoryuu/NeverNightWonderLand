#if UTAGE_URP
#if URP_17_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Utage.RenderPipeline.Urp
{
    //セピア用のScriptableRendererFeature
    public class SepiaRenderFeature : ScriptableRendererFeatureSimpleShaderBase<SepiaRenderFeature.SepiaRenderPass>
    {
#if URP_17_OR_NEWER
        protected override string ShaderGraphPath => ("Utage/PostEffect/Sepia Shader Graph");
#endif
        protected override string ShaderPath => ("Utage/PostEffect/Sepia");

        protected override SepiaRenderPass CreatePass()
        {
            return new SepiaRenderPass(GetShader());
        }

        public class SepiaRenderPass : ScriptableRenderPassSimpleBase<SepiaVolume>
        {
            public SepiaRenderPass(Shader shader)
                : base(shader, nameof(SepiaRenderPass))
            {
            }

#if URP_17_OR_NEWER
            public class PassData : MainPassDataBase<SepiaVolume>
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
            protected override string CommandBufferName => nameof(SepiaRenderPass);
            protected override void SetCommandBuffer(ScriptableRenderContext context, ref RenderingData renderingData, SepiaVolume volume, CommandBuffer commandBuffer)
            {
                Material.SetFloat(ShaderPropertyID.Strength, volume.Strength);
                ApplySimpleMaterial(context, ref renderingData, commandBuffer, Material);
            }
        }
    }
}

#endif
