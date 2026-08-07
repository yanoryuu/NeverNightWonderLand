#if UTAGE_URP


using System;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

#if URP_17_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
#endif

using UnityEngine.Rendering.Universal;


namespace Utage.RenderPipeline.Urp
{
    public abstract class ScriptableRenderPassBase : ScriptableRenderPass
    {
#if URP_13_OR_NEWER
        protected RTHandle RenderTarget { get; set; }
#else
        protected RenderTargetIdentifier RenderTarget { get; set; }
#endif
        public abstract bool EnablePass();
        public abstract bool IsActiveVolume();

        protected ScriptableRenderPassBase(string passName)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            profilingSampler = new ProfilingSampler(passName);
#if URP_17_OR_NEWER
            // PostProcessEffectの場合はtrue
            requiresIntermediateTexture = true;
#elif URP_13_OR_NEWER
            profilingSampler = new ProfilingSampler(passName);
#endif
        }

#if URP_17_OR_NEWER
#if !UNITY_6000_4_OR_NEWER
        [Obsolete("This is for compatibility mode only (when Render Graph is disabled)", false)]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
            RenderTarget = cameraData.renderer.cameraColorTargetHandle;
        }
#endif
#else
        public void Setup(ScriptableRenderer renderer)
        {
#if URP_13_OR_NEWER
            RenderTarget = renderer.cameraColorTargetHandle;
#else
            RenderTarget = renderer.cameraColorTarget;
#endif
        }
#endif
    }

    public abstract class ScriptableRenderPassBase<TVolume> : ScriptableRenderPassBase
        where TVolume : VolumeComponent, IPostProcessComponent
    {
        protected ScriptableRenderPassBase(string passName) : base(passName)
        {
        }
#if !UNITY_6000_4_OR_NEWER
#if URP_17_OR_NEWER
        [Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.", false)]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Volumeコンポーネントを取得
            var volume = GetVolumeComponent();
            //アクティブチェック
            if (!CheckActive(context, ref renderingData, volume)) return;

            //実行
            ExecuteInActive(context, ref renderingData, volume);
        }
#endif
        protected virtual void ExecuteInActive(ScriptableRenderContext context, ref RenderingData renderingData,
            TVolume volume)
        {
            var commandBuffer = CommandBufferPool.Get();
            using (new ProfilingScope(commandBuffer, profilingSampler))
            {
                //コマンドバッファを登録
                SetCommandBuffer(context, ref renderingData, volume, commandBuffer);
            }
            context.ExecuteCommandBuffer(commandBuffer);
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
//          context.Submit();
            commandBuffer.Clear();
            CommandBufferPool.Release(commandBuffer);
        }
        

        //コマンドバッファ名
        protected abstract string CommandBufferName { get; }
        //コマンドバッファを登録
        protected abstract void SetCommandBuffer(ScriptableRenderContext context, ref RenderingData renderingData, TVolume volume, CommandBuffer commandBuffer);

        //単純に、今の画面をマテリアルを適用して描きかえる
        protected virtual void ApplySimpleMaterial(ScriptableRenderContext context, ref RenderingData renderingData, CommandBuffer commandBuffer, Material material)
        {
            var cameraData = renderingData.cameraData;
            var w = cameraData.camera.scaledPixelWidth;
            var h = cameraData.camera.scaledPixelHeight;
            //作業用の書き込み先テクスチャ作成
            commandBuffer.GetTemporaryRT(ShaderPropertyID.TemporaryRT, w, h, 0, FilterMode.Bilinear);

            // RenderTargetの内容を、Materialを適用して、作業用テクスチャに書き込み
            commandBuffer.Blit(RenderTarget, ShaderPropertyID.TemporaryRT, material);
        
            // 作業用テクスチャを、RenderTargetに書き込みしなおす
            commandBuffer.Blit(ShaderPropertyID.TemporaryRT, RenderTarget);
                
            //作業の書き込み先テクスチャの解放
            commandBuffer.ReleaseTemporaryRT(ShaderPropertyID.TemporaryRT);
        }
        
        //ねじれ系（TwirlやVortex）マテリアルを適用して描きかえる
        protected virtual void SetDistortionMaterial(Material material, float angle, Vector2 center, Vector2 radius)
        {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, angle), Vector3.one);

            material.SetMatrix(ShaderPropertyID.RotationMatrix, rotationMatrix);
            material.SetVector(ShaderPropertyID.CenterRadius, new Vector4(center.x, center.y, radius.x, radius.y));
            material.SetFloat(ShaderPropertyID.Angle, angle*Mathf.Deg2Rad);
        }
        protected bool CheckActive(ScriptableRenderContext context, ref RenderingData renderingData, TVolume volume)
        {
            //ポストプロセス無効なら何もしない
            if (!renderingData.postProcessingEnabled) return false;
            //ボリュームコンポーネントがない場合
            if (volume == null) return false;
            //volumeコンポーネントの内容が無効な場合
            if (!volume.IsActive()) return false;
            return true;
        }

        // Volumeコンポーネントを取得
        protected TVolume GetVolumeComponent()
        {
            var volumeStack = VolumeManager.instance.stack;
            return volumeStack.GetComponent<TVolume>();
        }

        public override bool IsActiveVolume()
        {
            // Volumeコンポーネントを取得
            var volume = GetVolumeComponent();
            //ボリュームコンポーネントがない場合
            if (volume == null) return false;
            //volumeコンポーネントの内容が無効な場合
            if (!volume.IsActive()) return false;

            return true;
        }

    }

    //単純な1マテリアルだけのポストエフェクトで使うScriptableRenderPassのインターフェース
    public interface IScriptableRenderPassSimple
    {
        public Material Material { get; }
#if URP_17_OR_NEWER
        public TextureHandle CopiedColorTextureHandle { get; }
#endif
    }
    
    //単純な1マテリアルだけのポストエフェクトScriptableRenderPassの基底クラス
    public abstract class ScriptableRenderPassSimpleBase<TVolume> : ScriptableRenderPassBase<TVolume>, IScriptableRenderPassSimple
        where TVolume : VolumeComponent, IPostProcessComponent
    {
        protected ScriptableRenderPassSimpleBase(Shader shader, string passName) : base(passName)
        {
            Material = CoreUtils.CreateEngineMaterial(shader);
        }

        public Material Material { get; }

        public override bool EnablePass()
        {
            return Material != null;
        }
        
#if URP_17_OR_NEWER
        public TextureHandle CopiedColorTextureHandle { get; protected set; }
        
        //RenderGraphへの登録。
        //各派生クラスがoverrideするRecordRenderGraphメソッドから呼び出す
        protected virtual void RecordRenderGraphSub<TMainPass>(RenderGraph renderGraph, ContextContainer frameData)
            where TMainPass : MainPassDataBase, new()
        {
            Profiler.BeginSample("RecordRenderGraph");

            // コピーパス
            using (var builder =
                   renderGraph.AddRasterRenderPass<CopyPassData>(nameof(CopyPassData), out var passData,
                       profilingSampler))
            {
                //パスデータを記録
                passData.RecordPassData(this, builder, renderGraph, frameData);
                //コピーしたテクスチャハンドルを設定
                CopiedColorTextureHandle = passData.OutputTexture;
                //描画ロジックを設定（クロージャでAllocされないように、ラムダ式にstatic修飾子を付与）
                builder.SetRenderFunc(static (CopyPassData data, RasterGraphContext context) =>
                    data.RenderFunction(context));
            }

            // メインパス
            using (var builder =
                   renderGraph.AddRasterRenderPass<TMainPass>(this.passName, out var passData, profilingSampler))
            {
                //パスデータを記録
                passData.RecordPassData(this, builder, renderGraph, frameData);
                //描画ロジックを設定（クロージャでAllocされないように、ラムダ式にstatic修飾子を付与）
                builder.SetRenderFunc(static (TMainPass data, RasterGraphContext context) =>
                    data.RenderFunction(context));
            }

            Profiler.EndSample();
        }
#endif
    }
}
#endif
