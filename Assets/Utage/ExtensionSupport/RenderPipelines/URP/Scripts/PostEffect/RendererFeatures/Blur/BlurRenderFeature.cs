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
    public class BlurRenderFeature : ScriptableRendererFeatureBase<BlurRenderFeature.BlurRenderPass>
    {
        protected Shader ShaderDownSample => shaderDownSample;
        protected Shader ShaderGauss => shaderGauss;
        protected Shader ShaderSgxGauss => shaderSgxGauss;
        [SerializeField] Shader shaderDownSample;
        [SerializeField] Shader shaderGauss;
        [SerializeField] Shader shaderSgxGauss;

 #if URP_17_OR_NEWER
        protected Shader ShaderGraphDownSample => shaderGraphDownSample;
        [SerializeField] Shader shaderGraphDownSample;
#endif
        
        public virtual void Reset()
        {
            InitializeShader();
        }
        public override void Create()
        {
            InitializeShader();
            base.Create();
        }
        protected void InitializeShader()
        {
            shaderDownSample = Shader.Find("Utage/PostEffect/BlurDownSample");
            shaderGauss = Shader.Find("Utage/PostEffect/BlurGauss");
            shaderSgxGauss = Shader.Find("Utage/PostEffect/BlurSgxGauss");
#if URP_17_OR_NEWER
            if (shaderGraphDownSample == null)
            {
                shaderGraphDownSample = Shader.Find("Utage/PostEffect/BlurDownSample Shader Graph");
            }
#endif
        }


        protected override BlurRenderPass CreatePass()
        {
            return new BlurRenderPass(this);
        }

        public class BlurRenderPass : ScriptableRenderPassBase<BlurVolume>
        {
            Material MaterialDownSample { get; }
            Material MaterialGauss { get; }
            Material MaterialSgxGauss { get; }
#if URP_17_OR_NEWER
            Material MaterialDownSampleGraph { get; }
#endif

            public BlurRenderPass(BlurRenderFeature renderFeature)
                :base(nameof(BlurRenderPass))
            {
                MaterialDownSample = CoreUtils.CreateEngineMaterial(renderFeature.ShaderDownSample);
                MaterialGauss = CoreUtils.CreateEngineMaterial(renderFeature.ShaderGauss);
                MaterialSgxGauss = CoreUtils.CreateEngineMaterial(renderFeature.ShaderSgxGauss);
#if URP_17_OR_NEWER
                MaterialDownSampleGraph = CoreUtils.CreateEngineMaterial(renderFeature.ShaderGraphDownSample);
#endif
            }

            Material GetGaussMaterial(BlurVolume volume)
            {
                switch (volume.Type)
                {
                    case BlurVolume.BlurType.SgxGauss:
                        return this.MaterialSgxGauss;
                    case BlurVolume.BlurType.StandardGauss:
                    default:
                        return this.MaterialGauss;
                }
            }

            public override bool EnablePass()
            {
                bool result = MaterialDownSample != null && MaterialGauss != null && MaterialSgxGauss != null;
#if URP_17_OR_NEWER
                result &= MaterialDownSampleGraph != null;
#endif
                return result;
            }
            
#if URP_17_OR_NEWER
            class BlurPassData
            {
                TextureHandle SourceTexture { get; set; }
                TextureHandle DownSampleTexture0 { get; set; }
                TextureHandle DownSampleTexture1 { get; set; }
                TextureHandle OutTexture { get; set; }
                Material MaterialDownSample { get; set; }
                Material MaterialGauss { get; set; }
                Vector2 TextureSize { get; set; }
                float DownSampleScale { get; set; }
                float BlurSize { get; set; }
                int BlurIterations { get; set; }

                public void RecordPassData(BlurRenderPass renderPass, IUnsafeRenderGraphBuilder builder, RenderGraph renderGraph, ContextContainer frameData)
                {
                    var volume = renderPass.GetVolumeComponent();
                    var cameraData = frameData.Get<UniversalCameraData>();
                    UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
                    MaterialDownSample = renderPass.MaterialDownSampleGraph;
                    MaterialGauss = renderPass.GetGaussMaterial(volume);

                    SourceTexture = resourcesData.activeColorTexture;
                    builder.UseTexture(SourceTexture, AccessFlags.Read);
                    OutTexture = resourcesData.activeColorTexture;
                    builder.UseTexture(OutTexture, AccessFlags.Write);

                    int iDownSample = volume.DownSample;
                    DownSampleScale = 1.0f / (1.0f * (1 << iDownSample));
                    BlurSize = volume.BlurSize;
                    BlurIterations = volume.BlurIterations;
                    
                    // ダウンサンプル処理用の低解像度テクスチャを作成
                    var cam = cameraData.camera;
                    int rtW = cam.scaledPixelWidth >> iDownSample;
                    int rtH = cam.scaledPixelHeight >> iDownSample;
                    TextureSize = new Vector2(rtW, rtH); 
                    var downSampleTextureDesc = renderGraph.GetTextureDesc(SourceTexture);
                    downSampleTextureDesc.width = rtW;
                    downSampleTextureDesc.height = rtH;
                    DownSampleTexture0 = RenderGraphUtil.CreateRenderGraphTexture(renderGraph, downSampleTextureDesc, nameof(DownSampleTexture0));
                    builder.UseTexture(DownSampleTexture0, AccessFlags.ReadWrite);
                    DownSampleTexture1 = RenderGraphUtil.CreateRenderGraphTexture(renderGraph, downSampleTextureDesc, nameof(DownSampleTexture1));
                    builder.UseTexture(DownSampleTexture1, AccessFlags.ReadWrite);
                }

                public void RenderFunction(UnsafeGraphContext context)
                {
                    var unsafeCmd = context.cmd;
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(unsafeCmd);
                    
                    // MaterialDownSampleを適用して、作業用テクスチャに書き込み
                    unsafeCmd.SetRenderTarget(DownSampleTexture0);
                    MaterialDownSample.SetVector(ShaderPropertyID.TextureSize, TextureSize);
                    Blitter.BlitTexture(cmd, SourceTexture, Vector2.one, MaterialDownSample, 0);
                    
                    //ガウスブラー処理
                    for(int i = 0; i < BlurIterations; i++) {
                        float iterationOffs = (i*1.0f);
                        float p = BlurSize * DownSampleScale + iterationOffs;

                        cmd.SetGlobalFloat(ShaderPropertyID.Parameter, p);

                        // 縦方向のブラー
                        cmd.SetGlobalVector(ShaderPropertyID.UvOffset, new Vector4(0, 1, 0, -1));
                        cmd.Blit(DownSampleTexture0, DownSampleTexture1, MaterialGauss);

                        // 横方向のブラー
                        cmd.SetGlobalVector(ShaderPropertyID.UvOffset, new Vector4(1, 0, -1, 0));
                        cmd.Blit(DownSampleTexture1, DownSampleTexture0, MaterialGauss);
                    }
                  
                    //最終出力
                    unsafeCmd.SetRenderTarget(OutTexture);
                    Blitter.BlitTexture(cmd, DownSampleTexture0, Vector2.one, 0, true);
                }
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                //解像度が異なるテクスチャを扱うので、UnsafePassを使用
                using (var builder = renderGraph.AddUnsafePass<BlurPassData>(nameof(BlurPassData), out var passData, profilingSampler))
                {
                passData.RecordPassData(this, builder, renderGraph, frameData);
                //描画ロジックを設定
                    builder.SetRenderFunc(static (BlurPassData data, UnsafeGraphContext context) =>
                        data.RenderFunction(context));
                }

            }
#endif
            protected override string CommandBufferName => nameof(BlurRenderPass);

            protected override void SetCommandBuffer(ScriptableRenderContext context, ref RenderingData renderingData, BlurVolume volume, CommandBuffer commandBuffer)
            {
                int iDownSample = volume.DownSample;
                var cam = renderingData.cameraData.camera; 

                int rtW = cam.scaledPixelWidth >> iDownSample;
                int rtH = cam.scaledPixelHeight >> iDownSample;

                // ダウンサンプル処理（低解像度テクスチャを作成して書き込み）
                commandBuffer.GetTemporaryRT(ShaderPropertyID.TemporaryRT, rtW, rtH, 0, FilterMode.Bilinear);
                // RenderTargetの内容を、Materialを適用して、作業用テクスチャに書き込み
                commandBuffer.Blit(RenderTarget, ShaderPropertyID.TemporaryRT, MaterialDownSample, 0);
                
                float widthMod = 1.0f / (1.0f * (1<<iDownSample));
                float blurSize = volume.BlurSize;

                int blurIterations = volume.BlurIterations;
                
                Material gaussMaterial = GetGaussMaterial(volume);
                
                commandBuffer.GetTemporaryRT(ShaderPropertyID.TemporaryRT1, rtW, rtH, 0, FilterMode.Bilinear);
                for(int i = 0; i < blurIterations; i++) {
                    float iterationOffs = (i*1.0f);
                    float p = blurSize * widthMod + iterationOffs;
                    commandBuffer.SetGlobalFloat(ShaderPropertyID.Parameter, p);

                    // 縦方向のブラー(コマンドバッファ中にマテリアルのパラメーターを複数回描きかえるなら、commandBuffer.SetGlobalを使う)
                    commandBuffer.SetGlobalVector(ShaderPropertyID.UvOffset, new Vector4(0, 1, 0, -1));
                    commandBuffer.Blit(ShaderPropertyID.TemporaryRT, ShaderPropertyID.TemporaryRT1, gaussMaterial);

                    // 横方向のブラー
                    commandBuffer.SetGlobalVector(ShaderPropertyID.UvOffset, new Vector4(1, 0, -1,0));
                    commandBuffer.Blit(ShaderPropertyID.TemporaryRT1, ShaderPropertyID.TemporaryRT, gaussMaterial);
                }
                
                // 作業用テクスチャを、RenderTargetに書き込みしなおす
                commandBuffer.Blit(ShaderPropertyID.TemporaryRT, RenderTarget);
                //作業の書き込み先テクスチャの解放
                commandBuffer.ReleaseTemporaryRT(ShaderPropertyID.TemporaryRT1);
                commandBuffer.ReleaseTemporaryRT(ShaderPropertyID.TemporaryRT);
            }
        }
    }
}

#endif
