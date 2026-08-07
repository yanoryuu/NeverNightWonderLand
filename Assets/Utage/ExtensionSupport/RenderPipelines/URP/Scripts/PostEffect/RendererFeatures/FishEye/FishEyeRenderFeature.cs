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
    public class FishEyeRenderFeature : ScriptableRendererFeatureSimpleShaderBase<FishEyeRenderFeature.FishEyeRenderPass>
    {
#if URP_17_OR_NEWER
        protected override string ShaderGraphPath => ("Utage/PostEffect/FishEye Shader Graph");
#endif
        protected override string ShaderPath => ("Utage/PostEffect/FishEye");

        protected override FishEyeRenderPass CreatePass()
        {
            return new FishEyeRenderPass(GetShader());
        }

        public class FishEyeRenderPass : ScriptableRenderPassSimpleBase<FishEyeVolume>
        {
            public FishEyeRenderPass(Shader shader)
                : base(shader, nameof(FishEyeRenderPass))
            {
            }

#if URP_17_OR_NEWER

            public class PassData : MainPassDataBase<FishEyeVolume>
            {
                public float CameraAspect { get; set; }
                protected override void ApplyVolumeComponent(RasterGraphContext context, MaterialPropertyBlock propertyBlock)
                {
                    var volume = this.GetVolumeComponent();
                    float size = volume.Strength * volume.BaseSize;
                    propertyBlock.SetFloat(ShaderPropertyID.IntensityX, size * volume.IntensityX);
                    propertyBlock.SetFloat(ShaderPropertyID.IntensityY, size * volume.IntensityY * CameraAspect);
                }

                public override void RecordPassData(IScriptableRenderPassSimple renderPass, IRasterRenderGraphBuilder builder, RenderGraph renderGraph, ContextContainer frameData)
                {
                    base.RecordPassData(renderPass, builder, renderGraph, frameData);
                    var cam = frameData.Get<UniversalCameraData>().camera;
                    CameraAspect = (cam.scaledPixelWidth * 1.0f) / (cam.scaledPixelHeight * 1.0f);
                }
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                RecordRenderGraphSub<PassData>(renderGraph, frameData);
            }

#endif
            protected override string CommandBufferName => nameof(FishEyeRenderPass);
            protected override void SetCommandBuffer(ScriptableRenderContext context, ref RenderingData renderingData, FishEyeVolume volume, CommandBuffer commandBuffer)
            {
                var cam = renderingData.cameraData.camera; 
                float ar = (cam.scaledPixelWidth * 1.0f) / (cam.scaledPixelHeight * 1.0f);
                float size = volume.Strength * volume.BaseSize;
                Material.SetFloat(ShaderPropertyID.IntensityX, size * volume.IntensityX);
                Material.SetFloat(ShaderPropertyID.IntensityY, size * volume.IntensityY * ar);
                ApplySimpleMaterial(context, ref renderingData, commandBuffer, Material);
            }
        }
    }
}

#endif
