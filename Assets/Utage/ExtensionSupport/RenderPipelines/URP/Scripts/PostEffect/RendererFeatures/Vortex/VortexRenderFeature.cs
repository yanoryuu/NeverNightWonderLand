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
    public class VortexRenderFeature : ScriptableRendererFeatureSimpleShaderBase<VortexRenderFeature.VortexRenderPass>
    {
#if URP_17_OR_NEWER
        protected override string ShaderGraphPath => ("Utage/PostEffect/Vortex Shader Graph");
#endif
        protected override string ShaderPath => ("Utage/PostEffect/Vortex");

        protected override VortexRenderPass CreatePass()
        {
            return new VortexRenderPass(GetShader());
        }

        public class VortexRenderPass : ScriptableRenderPassSimpleBase<VortexVolume>
        {
            public VortexRenderPass(Shader shader)
                : base(shader, nameof(VortexRenderPass))
            {
            }
#if URP_17_OR_NEWER
            public class PassData : MainPassDataBase<VortexVolume>
            {
                protected override void ApplyVolumeComponent(RasterGraphContext context, MaterialPropertyBlock propertyBlock)
                {
                    var volume = this.GetVolumeComponent();
                    float angle = volume.Angle;
                    var center = volume.Center;
                    var radius = volume.Radius;
                    propertyBlock.SetVector(ShaderPropertyID.Center, center);
                    propertyBlock.SetVector(ShaderPropertyID.Radius, radius);
                    propertyBlock.SetFloat(ShaderPropertyID.Angle, angle * Mathf.Deg2Rad);
                }
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                RecordRenderGraphSub<PassData>(renderGraph, frameData);
            }

#endif
            protected override string CommandBufferName => nameof(VortexRenderPass);
            protected override void SetCommandBuffer(ScriptableRenderContext context, ref RenderingData renderingData, VortexVolume volume, CommandBuffer commandBuffer)
            {
                float angle = volume.Angle;
                var center = volume.Center;
                var radius = volume.Radius;
                SetDistortionMaterial(Material, angle, center, radius );
                ApplySimpleMaterial(context, ref renderingData, commandBuffer, Material);
            }
        }
    }
}
#endif
