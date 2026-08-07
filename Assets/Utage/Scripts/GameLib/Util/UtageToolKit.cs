// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UtageExtensions;
using System;

namespace Utage
{

	/// <summary>
	/// 便利クラス 
	/// </summary>
	public class UtageToolKit
	{
		public static bool IsHankaku(char c)
		{
			if ((c <= '\u007e') || // 英数字
				(c == '\u00a5') || // \記号
				(c == '\u203e') || // ~記号
				(c >= '\uff61' && c <= '\uff9f') // 半角カナ
			)
				return true;
			else
				return false;
		}

		public static bool IsPlatformStandAloneOrEditor()
		{
			return Application.isEditor || IsPlatformStandAlone();
		}

		public static bool IsPlatformStandAlone()
		{
			switch (Application.platform)
			{
				case RuntimePlatform.WindowsPlayer:
				case RuntimePlatform.OSXPlayer:
				case RuntimePlatform.LinuxPlayer:
					return true;
				default:
					return false;
			}
		}

		/// <summary>
		/// キャプチャ用のテクスチャを作る(yield return new WaitForEndOfFrame()の後に呼ぶこと)
		/// </summary>
		/// <returns>キャプチャ画像</returns>
		public static Texture2D CaptureScreen()
		{
			return CaptureScreen(new Rect(0, 0, Screen.width, Screen.height));
		}

		/// <summary>
		/// キャプチャ用のテクスチャを作る(yield return new WaitForEndOfFrame()の後に呼ぶこと)
		/// </summary>
		/// <returns>キャプチャ画像</returns>
		public static Texture2D CaptureScreen(Rect rect)
		{
			return CaptureScreen(TextureFormat.RGB24, rect);
		}

		/// <summary>
		/// キャプチャ用のテクスチャを作る(yield return new WaitForEndOfFrame()の後に呼ぶこと)
		/// </summary>
		/// <param name="format">テクスチャフォーマット</param>
		/// <returns>キャプチャ画像</returns>
		public static Texture2D CaptureScreen(TextureFormat format, Rect rect)
		{
			Texture2D tex = new Texture2D((int)rect.width, (int)rect.height, format, false);
			try
			{
				tex.ReadPixels(rect, 0, 0);
				tex.Apply();
			}
			catch
			{
			}
			return tex;
		}

		/// <summary>
		/// 日付を日本式表記のテキストで取得
		/// </summary>
		/// <param name="date">日付</param>
		/// <returns>日付の日本式表記テキスト</returns>
		static public string DateToStringJp(System.DateTime date)
		{
			return date.ToString(cultureInfJp);
		}
		static readonly System.Globalization.CultureInfo cultureInfJp = new System.Globalization.CultureInfo("ja-JP");


		/// <summary>
		/// サイズ変更したテクスチャを作成する
		/// </summary>
		/// <param name="tex">リサイズするテクスチャ</param>
		/// <param name="captureW">リサイズ後のテクスチャの横幅(pix)</param>
		/// <param name="captureH">リサイズ後のテクスチャの縦幅(pix)</param>
		/// <returns>キャプチャ画像のテクスチャバイナリ</returns>
		public static Texture2D CreateResizeTexture(Texture2D tex, int width, int height)
		{
			if (tex == null) return null;
			return CreateResizeTexture(tex, width, height, tex.format, tex.mipmapCount > 1);
		}

		/// <summary>
		/// サイズ変更したテクスチャを作成する
		/// </summary>
		/// <param name="tex">リサイズするテクスチャ</param>
		/// <param name="width">リサイズ後のテクスチャの横幅(pix)</param>
		/// <param name="height">リサイズ後のテクスチャの縦幅(pix)</param>
		/// <param name="format">リサイズ後のテクスチャフォーマット</param>
		/// <param name="isMipmap">ミップマップを有効にするか</param>
		/// <returns>リサイズして作成したテクスチャ</returns>
		public static Texture2D CreateResizeTexture(Texture2D tex, int width, int height, TextureFormat format, bool isMipmap)
		{
			if (tex == null) return null;

			TextureWrapMode wrap = tex.wrapMode;
			tex.wrapMode = TextureWrapMode.Clamp;
			Color[] colors = new Color[width * height];
			int index = 0;
			for (int y = 0; y < height; y++)
			{
				float v = 1.0f * y / (height - 1);
				for (int x = 0; x < width; x++)
				{
					float u = 1.0f * x / (width - 1);
					colors[index] = tex.GetPixelBilinear(u, v);
					++index;
				}
			}
			tex.wrapMode = wrap;

			Texture2D resizedTex = new Texture2D(width, height, format, isMipmap);
			resizedTex.SetPixels(colors);
			resizedTex.Apply();
			return resizedTex;
		}
		public static Texture2D CreateResizeTexture(Texture2D tex, int width, int height, TextureFormat format)
		{
			return CreateResizeTexture(tex, width, height, format, false);
		}

		/// <summary>
		/// テクスチャから基本的なスプライト作成
		/// </summary>
		/// <param name="tex">テクスチャ</param>
		/// <param name="pixelsToUnits">スプライトを作成する際の、座標1.0単位辺りのピクセル数</param>
		/// <returns></returns>
		public static Sprite CreateSprite(Texture2D tex, float pixelsToUnits)
		{
			return CreateSprite(tex, pixelsToUnits, new Vector2(0.5f, 0.5f));
		}
		/// <summary>
		/// テクスチャから基本的なスプライト作成
		/// </summary>
		/// <param name="tex">テクスチャ</param>
		/// <param name="pixelsToUnits">スプライトを作成する際の、座標1.0単位辺りのピクセル数</param>
		/// <returns></returns>
		public static Sprite CreateSprite(Texture2D tex, float pixelsToUnits, Vector2 pivot)
		{
			if (tex == null)
			{
				Debug.LogError("texture is null");
				tex = Texture2D.whiteTexture;
			}
			if (tex.mipmapCount > 1) Debug.LogWarning(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.SpriteMimMap, tex.name));
			Rect rect = new Rect(0, 0, tex.width, tex.height);
			return Sprite.Create(tex, rect, pivot, pixelsToUnits);
		}
	}
}
