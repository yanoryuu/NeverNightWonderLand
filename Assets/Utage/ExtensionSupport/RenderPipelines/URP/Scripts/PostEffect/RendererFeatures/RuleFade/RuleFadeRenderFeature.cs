#if UTAGE_URP
#if URP_17_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Utage.RenderPipeline.Urp
{
    //カラーフェード用のScriptableRendererFeature
    public class RuleFadeRenderFeature : ScriptableRendererFeatureSimpleShaderBase<RuleFadeRenderFeature.RuleFadeRenderPass>
    {
#if URP_17_OR_NEWER
        protected override string ShaderGraphPath => ("Utage/PostEffect/RuleFade Shader Graph");
#endif
        protected override string ShaderPath => ("Utage/PostEffect/RuleFade");

        protected override RuleFadeRenderPass CreatePass()
        {
            return new RuleFadeRenderPass(GetShader());
        }

        public class RuleFadeRenderPass : ScriptableRenderPassSimpleBase<RuleFadeVolume>
        {
            public RuleFadeRenderPass(Shader shader)
                : base(shader, nameof(RuleFadeRenderPass))
            {
            }
#if URP_17_OR_NEWER
            public class PassData : MainPassDataBase<RuleFadeVolume>
            {
                protected override void ApplyVolumeComponent(RasterGraphContext context, MaterialPropertyBlock propertyBlock)
                {
                    var volume = this.GetVolumeComponent();
                    propertyBlock.SetFloat(ShaderPropertyID.Strength, volume.Strength);
                    propertyBlock.SetFloat(ShaderPropertyID.Vague, volume.Vague);
                    propertyBlock.SetTexture(ShaderPropertyID.RuleTex, volume.RuleTexture);
                    propertyBlock.SetColor(ShaderPropertyID.Color, volume.Color);
                }
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                RecordRenderGraphSub<PassData>(renderGraph, frameData);
            }
#endif
            protected override string CommandBufferName => nameof(RuleFadeRenderPass);
            protected override void SetCommandBuffer(ScriptableRenderContext context, ref RenderingData renderingData, RuleFadeVolume volume, CommandBuffer commandBuffer)
            {
                Material.SetFloat(ShaderPropertyID.Strength, volume.Strength);
                Material.SetFloat(ShaderPropertyID.Vague, volume.Vague);
                Material.SetTexture(ShaderPropertyID.RuleTex, volume.RuleTexture);
                Material.color = volume.Color;
                ApplySimpleMaterial(context, ref renderingData, commandBuffer, Material);
            }
        }
    }
}

#endif
