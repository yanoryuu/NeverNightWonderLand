#if UTAGE_URP
#if URP_17_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Utage.RenderPipeline.Urp
{
    //Mosaic用のScriptableRendererFeature
    public class MosaicRenderFeature : ScriptableRendererFeatureSimpleShaderBase<MosaicRenderFeature.MosaicRenderPass>
    {
#if URP_17_OR_NEWER
        protected override string ShaderGraphPath => ("Utage/PostEffect/Mosaic Shader Graph");
#endif
        protected override string ShaderPath => ("Utage/PostEffect/Mosaic");
        
        protected override MosaicRenderPass CreatePass()
        {
            return new MosaicRenderPass(GetShader());
        }

        public class MosaicRenderPass : ScriptableRenderPassSimpleBase<MosaicVolume>
        {
            public MosaicRenderPass(Shader shader)
                : base(shader, nameof(MosaicRenderPass))
            {
            }
#if URP_17_OR_NEWER
            public class PassData : MainPassDataBase<MosaicVolume>
            {
                public float CameraScaledPixelWidth { get; set; }
                public float CameraScaledPixelHeight { get; set; }

                public override void RecordPassData(IScriptableRenderPassSimple renderPass, IRasterRenderGraphBuilder builder,
                    RenderGraph renderGraph, ContextContainer frameData)
                {
                    base.RecordPassData(renderPass, builder, renderGraph, frameData);
                    var cam = frameData.Get<UniversalCameraData>().camera;
                    CameraScaledPixelWidth = (cam.scaledPixelWidth * 1.0f);
                    CameraScaledPixelHeight = (cam.scaledPixelHeight * 1.0f);
                }

                protected override void ApplyVolumeComponent(RasterGraphContext context,
                    MaterialPropertyBlock propertyBlock)
                {
                    var volume = this.GetVolumeComponent();
                    float scaleSize = Mathf.Min(CameraScaledPixelWidth / volume.ReferenceResolution.x, CameraScaledPixelHeight / volume.ReferenceResolution.y);
                    propertyBlock.SetFloat(ShaderPropertyID.Size, Mathf.CeilToInt(volume.Size * scaleSize));
                }
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                RecordRenderGraphSub<PassData>(renderGraph, frameData);
            }

#endif
            protected override string CommandBufferName => nameof(MosaicRenderPass);
           
            protected override void SetCommandBuffer(ScriptableRenderContext context, ref RenderingData renderingData, MosaicVolume volume, CommandBuffer commandBuffer)
            {
                //現在のカメラの描画サイズと、実際のスクリーンの描画ピクセルサイズに合わせて
                //モザイクのサイズをかえる
                var camera = renderingData.cameraData.camera;
                float scaleSize = Mathf.Min(camera.scaledPixelWidth / volume.ReferenceResolution.x, camera.scaledPixelHeight / volume.ReferenceResolution.y);
                Material.SetFloat(ShaderPropertyID.Size, Mathf.CeilToInt( volume.Size * scaleSize));
                ApplySimpleMaterial(context, ref renderingData, commandBuffer, Material);
            }
        }
    }
}

#endif
