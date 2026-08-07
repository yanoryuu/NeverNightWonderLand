#if UTAGE_URP
#if URP_17_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Utage.RenderPipeline.Urp
{
    //キャプチャ用のScriptableRendererFeature
    public class CaptureRenderFeature : ScriptableRendererFeatureBase<CaptureRenderFeature.CaptureRenderPass>
    {

        protected override CaptureRenderPass CreatePass()
        {
            return new CaptureRenderPass();
        }

        public class CaptureRenderPass : ScriptableRenderPassBase<CaptureVolume>
        {
            public override bool EnablePass() => true;
            public CaptureRenderPass()
                :base(nameof(CaptureRenderPass))
            {
            }
#if URP_17_OR_NEWER

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
//                renderGraph.AddCopyPass(resourcesData.activeColorTexture,renderGraph.ImportTexture(RTHandles.Alloc(captureVolumeController.CaptureTextureToWrite)));

                // パスデータの登録
                using var builder = renderGraph.AddRasterRenderPass<CapturePassData>(nameof(CapturePassData), out var passData, profilingSampler);
                passData.RecordPassData(this, builder, renderGraph, frameData);
                builder.SetRenderFunc(static (CapturePassData data, RasterGraphContext context) => data.RenderFunction(context));
            }

            // キャプチャ用のパスデータ
            public class CapturePassData
            {
                TextureHandle InputTexture { get; set; }
                TextureHandle OutputTexture { get; set; }
                CaptureVolumeController CaptureVolumeController { get; set; }
                public virtual void RecordPassData(ScriptableRenderPassBase renderPass, IRasterRenderGraphBuilder builder, RenderGraph renderGraph, ContextContainer frameData)
                {
                    var camera = frameData.Get<UniversalCameraData>().camera;
                    CaptureVolumeController = camera.GetComponentInChildren<CaptureVolumeController>(true);
                    if (CaptureVolumeController == null)
                    {
                        Debug.LogError("CaptureVolumeController is not found", camera);
                        return;
                    }

                    if (CaptureVolumeController.CaptureTextureToWrite == null)
                    {
                        Debug.LogError("CaptureTextureToWrite is not found", CaptureVolumeController);
                        return;
                    }

                    UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
                    InputTexture = resourcesData.activeColorTexture;
                    builder.UseTexture(InputTexture, AccessFlags.Read);
                    OutputTexture = renderGraph.ImportTexture(RTHandles.Alloc(CaptureVolumeController.CaptureTextureToWrite));
                    builder.SetRenderAttachment(OutputTexture, 0, AccessFlags.Write);
                }

                public void RenderFunction(RasterGraphContext context)
                {
                    Blitter.BlitTexture(context.cmd, InputTexture, new Vector4(1, 1, 0, 0), 0.0f, false);
                    CaptureVolumeController.OnCaptured();
                }
            }
#endif
            protected override string CommandBufferName => nameof(CaptureRenderPass);

            protected override void SetCommandBuffer(ScriptableRenderContext context, ref RenderingData renderingData, CaptureVolume volume, CommandBuffer commandBuffer)
            {
                //コマンドバッファを登録
                var camera = renderingData.cameraData.camera;
                CaptureVolumeController captureCamera = camera.GetComponentInChildren<CaptureVolumeController>(true);
                commandBuffer.Blit(RenderTarget, captureCamera.CaptureTextureToWrite);
            }
          
            protected override void ExecuteInActive(ScriptableRenderContext context, ref RenderingData renderingData, CaptureVolume volume)
            {
                base.ExecuteInActive(context, ref renderingData, volume);
                var camera = renderingData.cameraData.camera;
                CaptureVolumeController captureCamera = camera.GetComponentInChildren<CaptureVolumeController>(true);
                captureCamera.OnCaptured();
            }
        }
    }
}

#endif
