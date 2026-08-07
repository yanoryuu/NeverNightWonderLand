#if UTAGE_URP
#if URP_17_OR_NEWER
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Utage.RenderPipeline.Urp
{
	// アクティブカラーをコピーするだけのパスデータ
	public class CopyPassData
	{
		TextureHandle InputTexture { get; set; }
		public TextureHandle OutputTexture { get; set; }

		public virtual void RecordPassData(ScriptableRenderPassBase renderPass, IRasterRenderGraphBuilder builder,
			RenderGraph renderGraph, ContextContainer frameData)
		{
			UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
			InputTexture = resourcesData.activeColorTexture;
			builder.UseTexture(InputTexture, AccessFlags.Read);
			OutputTexture = RenderGraphUtil.CreateRenderGraphTextureFromActiveColor(renderGraph, frameData);
			builder.SetRenderAttachment(OutputTexture, 0, AccessFlags.Write);
		}
		public void RenderFunction(RasterGraphContext context)
		{
			Blitter.BlitTexture(context.cmd, InputTexture, new Vector4(1, 1, 0, 0), 0.0f, false);
		}
	}
}
#endif
#endif
