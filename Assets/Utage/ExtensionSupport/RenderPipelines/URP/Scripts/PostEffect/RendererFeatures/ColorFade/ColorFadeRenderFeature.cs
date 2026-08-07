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
    public class ColorFadeRenderFeature : ScriptableRendererFeatureSimpleShaderBase<ColorFadeRenderFeature.ColorFadeRenderPass>
    {
#if URP_17_OR_NEWER
        protected override string ShaderGraphPath => ("Utage/PostEffect/ColorFade Shader Graph");
#endif
        protected override string ShaderPath => ("Utage/PostEffect/ColorFade");

        protected override ColorFadeRenderPass CreatePass()
        {
            return new ColorFadeRenderPass(GetShader());
        }

        public class ColorFadeRenderPass : ScriptableRenderPassSimpleBase<ColorFadeVolume>
        {
            public ColorFadeRenderPass(Shader shader)
                : base(shader, nameof(ColorFadeRenderPass))
            {
            }
#if URP_17_OR_NEWER
            public class PassData : MainPassDataBase<ColorFadeVolume>
            {
                protected override void ApplyVolumeComponent(RasterGraphContext context, MaterialPropertyBlock propertyBlock)
                {
                    var volume = this.GetVolumeComponent();
                    propertyBlock.SetFloat(ShaderPropertyID.Strength, volume.Strength);
                    propertyBlock.SetColor(ShaderPropertyID.Color, volume.Color);
                }
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                RecordRenderGraphSub<PassData>(renderGraph, frameData);
            }
#endif
            protected override string CommandBufferName => nameof(ColorFadeRenderPass);
           
            protected override void SetCommandBuffer(ScriptableRenderContext context, ref RenderingData renderingData, ColorFadeVolume volume, CommandBuffer commandBuffer)
            {
                Material.SetFloat(ShaderPropertyID.Strength, volume.Strength);
                Material.color = volume.Color;
                ApplySimpleMaterial(context, ref renderingData, commandBuffer, Material);
            }
        }
    }
}

#endif
