#if UTAGE_RENDER_PIPELINE
using UnityEngine;

namespace Utage.RenderPipeline
{
	public static class ShaderPropertyID
	{

		//作業用書き込み先テクスチャのNameID
		public static int TemporaryRT { get; } = Shader.PropertyToID("TemporaryRT");
		public static int TemporaryRT1 { get; } = Shader.PropertyToID("TemporaryRT1");
		public static int BlitTexture { get; } = Shader.PropertyToID("_BlitTexture");
		public static int BlitScaleBias { get; } = Shader.PropertyToID("_BlitScaleBias");

		public static int Color { get; } = Shader.PropertyToID("_Color");
		public static int Strength { get; } = Shader.PropertyToID("_Strength");
		public static int RuleTex { get; } = Shader.PropertyToID("_RuleTex");
		public static int Vague { get; } = Shader.PropertyToID("_Vague");
		public static int RotationMatrix { get; } = Shader.PropertyToID("_RotationMatrix");
		public static int Center { get; } = Shader.PropertyToID("_Center");
		public static int Radius { get; } = Shader.PropertyToID("_Radius");
		public static int CenterRadius { get; } = Shader.PropertyToID("_CenterRadius");
		public static int Angle { get; } = Shader.PropertyToID("_Angle");
		public static int Size { get; } = Shader.PropertyToID("_Size");
		public static int Parameter { get; } = Shader.PropertyToID("_Parameter");
		public static int UvOffset { get; } = Shader.PropertyToID("_UvOffset");
		public static int IntensityX { get; } = Shader.PropertyToID("_IntensityX");
		public static int IntensityY { get; } = Shader.PropertyToID("_IntensityY");
		public static int TextureSize { get; } = Shader.PropertyToID("_TextureSize");

	}
}
#endif
