// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;
using System;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// シェーダーの管理
	/// </summary>
	public static class ShaderManager
	{
		//ルール画像付きのフェード処理をする場合のシェーダー
		public static Shader RuleFade { get { return Shader.Find("Utage/UI/RuleFade"); } }

		//背景を透過しないクロスフェード処理をする場合のシェーダー
		public static Shader CrossFade { get { return Shader.Find("Utage/CrossFadeImage"); } }

		//透過画像を描きこむシェーダー
		public static Shader RenderTexture { get { return Shader.Find("Utage/RenderTexture"); } }

		//透過画像を描き込んだRenderTextureを描画するシェーダー
		public static Shader DrawByRenderTexture { get { return Shader.Find("Utage/DrawByRenderTexture"); } }

		//カラーフェード
		public static readonly string ColorFade = "Utage/ImageEffect/ColorFade";

		//ブルームシェーダー名
		public static readonly string BloomName = "Utage/ImageEffect/Bloom";

		//ブラー
		public static readonly string BlurName = "Utage/ImageEffect/Blur";

		//モザイク
		public static readonly string MosaicName = "Utage/ImageEffect/Mosaic";

		//カラーコレクション（ランプ画像）
		public static readonly string ColorCorrectionRampName = "Utage/ImageEffect/ColorCorrectionRamp";

		//グレースケール
		public static readonly string GrayScaleName = "Utage/ImageEffect/Grayscale";

		//モーションブラー
		public static readonly string MotionBlurName = "Utage/ImageEffect/MotionBlur";

		//ノイズ
		public static readonly string NoiseAndGrainName = "Utage/ImageEffect/NoiseAndGrain";

		//オーバーレイ
		public static readonly string BlendModesOverlayName = "Utage/ImageEffect/BlendModesOverlay";

		//セピア
		public static readonly string SepiatoneName = "Utage/ImageEffect/Sepiatone";

		//ネガポジ反転
		public static readonly string NegaPosiName = "Utage/ImageEffect/NegaPosi";

		//魚眼
		public static readonly string FisheyeName = "Utage/ImageEffect/Fisheye";

		//一点を中心に画像を歪ませる
		public static readonly string TwirlName = "Utage/ImageEffect/Twirl";

		//円で画像を歪ませる
		public static readonly string VortexName = "Utage/ImageEffect/Vortex";

		//ルール画像付きのフェード
		public static readonly string RuleFadeName = "Utage/ImageEffect/RuleFade";
	}
}
