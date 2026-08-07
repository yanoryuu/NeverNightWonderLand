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
    public class TwirlRenderFeature : ScriptableRendererFeatureSimpleShaderBase<TwirlRenderFeature.TwirlRenderPass>
    {
#if URP_17_OR_NEWER
        protected override string ShaderGraphPath => "Utage/PostEffect/Twirl Shader Graph";
#endif
        protected override string ShaderPath => ("Utage/PostEffect/Twirl");

        protected override TwirlRenderPass CreatePass()
        {
            return new TwirlRenderPass(GetShader());
        }
        public class TwirlRenderPass : ScriptableRenderPassSimpleBase<TwirlVolume>
        {
            public TwirlRenderPass(Shader shader)
                : base(shader, nameof(TwirlRenderPass))
            {
            }

#if URP_17_OR_NEWER
            public class PassData : MainPassDataBase<TwirlVolume>
            {
                protected override void ApplyVolumeComponent(RasterGraphContext context, MaterialPropertyBlock propertyBlock)
                {
                    var volume = this.GetVolumeComponent();
                    propertyBlock.SetVector(ShaderPropertyID.Center, volume.Center);
                    propertyBlock.SetVector(ShaderPropertyID.Radius, volume.Radius);
                    float angle = volume.Angle;
                    Matrix4x4 rotationMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, angle), Vector3.one);
                    propertyBlock.SetMatrix(ShaderPropertyID.RotationMatrix, rotationMatrix);
                }
            }
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                RecordRenderGraphSub<PassData>(renderGraph, frameData);
            }

#endif
            protected override string CommandBufferName => nameof(TwirlRenderPass);
            protected override void SetCommandBuffer(ScriptableRenderContext context, ref RenderingData renderingData, TwirlVolume volume, CommandBuffer commandBuffer)
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
