#if UTAGE_URP
#if URP_17_OR_NEWER

using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Utage.RenderPipeline.Urp
{
	public abstract class MainPassDataBase
	{
		protected Material Material { get; set; }
		protected TextureHandle InputTexture { get; set; }
		protected TextureHandle OutputTexture { get; set; }

		public virtual void RecordPassData(IScriptableRenderPassSimple renderPass, IRasterRenderGraphBuilder builder, RenderGraph renderGraph, ContextContainer frameData)
		{
			UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();

			//マテリアルの設定
			Material = renderPass.Material;
			//入力テクスチャの設定
			InputTexture = renderPass.CopiedColorTextureHandle;
			builder.UseTexture(InputTexture);

			//出力テクスチャの設定
			OutputTexture = resourcesData.activeColorTexture;
			builder.SetRenderAttachment(OutputTexture, 0, AccessFlags.Write);
		}
		
		public virtual void RenderFunction(RasterGraphContext context)
		{
			Profiler.BeginSample("RenderFunction");

			RasterCommandBuffer cmd = context.cmd;
			var propertyBlock = RenderGraphUtil.GetSharedPropertyBlock();
			
			//テクスチャの設定
			ApplyTextureProperty(InputTexture, context, propertyBlock);
			//ボリュームコンポーネントの設定
			Profiler.BeginSample("ApplyVolumeComponent");
			ApplyVolumeComponent(context, propertyBlock);
			Profiler.EndSample();

			//描画
			cmd.DrawProcedural(Matrix4x4.identity, Material, 0, MeshTopology.Triangles, 3, 1, propertyBlock);
			
			Profiler.EndSample();
		}
		
		protected virtual void ApplyTextureProperty(TextureHandle sourceTexture, RasterGraphContext context, MaterialPropertyBlock propertyBlock)
		{
			propertyBlock.SetTexture(ShaderPropertyID.BlitTexture, sourceTexture);
			propertyBlock.SetVector(ShaderPropertyID.BlitScaleBias, new Vector4(1, 1, 0, 0));
		}

		protected abstract void ApplyVolumeComponent(RasterGraphContext context, MaterialPropertyBlock propertyBlock);
	}

	public abstract class MainPassDataBase<TVolume> : MainPassDataBase
		where TVolume : VolumeComponent
	{
		// Volumeコンポーネントを取得
		protected TVolume GetVolumeComponent()
		{
			var volumeStack = VolumeManager.instance.stack;
			return volumeStack.GetComponent<TVolume>();
		}
	}
}
#endif
#endif
