// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Utage
{
	internal enum ImageEffectType
	{
		Custom,             //独自カスタム
		ColorFade,          //カラーフェード
		Bloom,              //ブルーム
		Blur,               //ボカシ
		Mosaic,             //モザイク
		GrayScale,          //グレースケール
		MotionBlur,         //モーションブラー
		ScreenOverlay,      //スクリーンオーバーレイ
		Sepia,              //セピア
		NegaPosi,           //ネガポジ反転
		FishEye,            //魚眼
		Twirl,              //一点を中心に画像を歪ませる
		Vortex,             //円で画像を歪ませる
		RuleFade,           //ルール画像付きのフェードイン
	}

	//イメージエフェクト関連のちょっとした処理
	public static class ImageEffectUtil
    {
		class ImageEffectPattern
		{
			internal ImageEffectPattern(string type, Type componentType, Shader[] shaders)
			{
				this.type = type;
				this.componentType = componentType;
				this.shaders = shaders;
			}
			public string type;
			public Type componentType;
			public Shader[] shaders;
		}

		static readonly List<ImageEffectPattern> patterns = new List<ImageEffectPattern>()
		{
			new ImageEffectPattern(ImageEffectType.ColorFade.ToString(), typeof(ColorFade), new Shader[] { Shader.Find(ShaderManager.ColorFade) }),
			new ImageEffectPattern(ImageEffectType.Bloom.ToString(), typeof(Bloom), new Shader[] { Shader.Find(ShaderManager.BloomName) }),
			new ImageEffectPattern(ImageEffectType.Blur.ToString(), typeof(Blur), new Shader[] { Shader.Find(ShaderManager.BlurName) }),
			new ImageEffectPattern(ImageEffectType.Mosaic.ToString(), typeof(Mosaic), new Shader[] { Shader.Find(ShaderManager.MosaicName) }),
			new ImageEffectPattern(ImageEffectType.GrayScale.ToString(), typeof(Grayscale), new Shader[] { Shader.Find(ShaderManager.GrayScaleName) }),
			new ImageEffectPattern(ImageEffectType.MotionBlur.ToString(), typeof(MotionBlur), new Shader[] { Shader.Find(ShaderManager.MotionBlurName) }),
			new ImageEffectPattern(ImageEffectType.ScreenOverlay.ToString(), typeof(ScreenOverlay), new Shader[] { Shader.Find(ShaderManager.BlendModesOverlayName) }),
			new ImageEffectPattern(ImageEffectType.Sepia.ToString(), typeof(SepiaTone), new Shader[] { Shader.Find(ShaderManager.SepiatoneName) }),
			new ImageEffectPattern(ImageEffectType.NegaPosi.ToString(), typeof(NegaPosi), new Shader[] { Shader.Find(ShaderManager.NegaPosiName) }),
			new ImageEffectPattern(ImageEffectType.FishEye.ToString(), typeof(FishEye), new Shader[] { Shader.Find(ShaderManager.FisheyeName) }),
			new ImageEffectPattern(ImageEffectType.Twirl.ToString(), typeof(Twirl), new Shader[] { Shader.Find(ShaderManager.TwirlName) }),
			new ImageEffectPattern(ImageEffectType.Vortex.ToString(), typeof(Vortex), new Shader[] { Shader.Find(ShaderManager.VortexName) }),
			new ImageEffectPattern(ImageEffectType.RuleFade.ToString(), typeof(RuleFade), new Shader[] { Shader.Find(ShaderManager.RuleFadeName) }),
		};

		static internal bool TryParse(string type, out Type ComponentType, out Shader[] Shaders)
		{
			ImageEffectPattern pattern = patterns.Find(x => x.type == type);
			if (pattern == null)
			{
				ComponentType = null;
				Shaders = null;
				return false;
			}

			ComponentType = pattern.componentType;
			Shaders = pattern.shaders;
			return true;
		}

		static internal string ToImageEffectType(Type ComponentType)
		{
			ImageEffectPattern pattern = patterns.Find(x => x.componentType == ComponentType);
			if (pattern == null)
			{
				return "";
			}

			return pattern.type;
		}

		static internal bool TryGetComonentCreateIfMissing(string type, out ImageEffectBase component, out bool alreadyEnabled, GameObject target)
		{
			Type componentType;
			Shader[] shaders;
			alreadyEnabled = false;
			if (!TryParse(type, out componentType, out shaders))
			{
				Debug.LogError(type + " is not find in Image effect patterns");
				component = null;
				return false;
			}

			component = target.GetComponent(componentType) as ImageEffectBase;
			if (component == null)
			{
				component = target.gameObject.AddComponent(componentType) as ImageEffectBase;
				component.SetShaders(shaders);
			}
			else
			{
				alreadyEnabled = component.enabled;
			}
			return true;
		}

		//TwirlとVortexに共通のねじれ描画処理
		public static void RenderDistortion(Material material, RenderTexture source, RenderTexture destination, float angle, Vector2 center, Vector2 radius)
        {
            bool invertY = source.texelSize.y < 0.0f;
            if (invertY)
            {
                center.y = 1.0f - center.y;
                angle = -angle;
            }

            Matrix4x4 rotationMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, angle), Vector3.one);

            material.SetMatrix("_RotationMatrix", rotationMatrix);
            material.SetVector("_CenterRadius", new Vector4(center.x, center.y, radius.x, radius.y));
            material.SetFloat("_Angle", angle*Mathf.Deg2Rad);

            Graphics.Blit(source, destination, material);
        }

		//イメージエフェクトがサポートされているかどうか
		//グラフィックカードが画像のポストプロセッシングエフェクトをサポートしている場合、 /True/を返します。
		public static bool SupportsImageEffects
		{
#if UNITY_2019_1_OR_NEWER
			get { return true; }
#else
			get { return SystemInfo.supportsImageEffects; }
#endif
		}

		//テクスチャ書き込みがサポートされているかどうか
		public static bool SupportsRenderTextures
		{
#if UNITY_5_5_OR_NEWER
			get { return true; }
#else
			get { return SystemInfo.supportsRenderTextures; }
#endif
		}

		//テクスチャ書き込みでDepth書き込みがサポートされているかどうか
		public static bool SupportsDepth
		{
			get { return SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth); }
		}

		//HDRテクスチャサポートのチェック
		public static bool SupportsHDRTextures
		{
			get { return SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf); }
		}

		//DX11テクスチャサポートのチェック
		public static bool SupportDX11
		{
			get { return SystemInfo.graphicsShaderLevel >= 50 && SystemInfo.supportsComputeShaders; }
		}

	}
}
