using UnityEngine.Rendering;

namespace Utage.RenderPipeline
{
	//RenderPipelineのユーティリティ処理
	public static class RenderPipelineUtil
	{
		//RenderPipelineのタイプ
		public enum RenderPipelineType
		{
			Urp,
			Hdrp,
			BuiltIn,
		}
		
		//現在のRenderPipelineを判定
		public static RenderPipelineType GetCurrentRenderPipelineType()
		{
			RenderPipelineAsset current = GraphicsSettings.currentRenderPipeline;
			if (current == null) return RenderPipelineType.BuiltIn;
			
			//クラス名を名前判別
			if (current.GetType().Name.Contains("Universal"))
			{
				return RenderPipelineType.Urp;
			}
			else
			{
				return RenderPipelineType.Hdrp;
			}
		}

		public static bool IsUrp()
		{
			return GetCurrentRenderPipelineType() == RenderPipelineType.Urp;
		}

		public static bool IsHdrp()
		{
			return GetCurrentRenderPipelineType() == RenderPipelineType.Hdrp;
		}

		public static bool IsBuiltIn()
		{
			return GetCurrentRenderPipelineType() == RenderPipelineType.BuiltIn;
		}
	}
}
