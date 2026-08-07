// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using UnityEngine;
using Utage;
using System.Collections.Generic;

namespace Utage
{

	// パラメーターの「font」に設定された名前のフォントを、自動的にメッセージウィンドウに設定するサンプル
	public class SampleChangeFont : MonoBehaviour
	{
		public AdvEngine engine = null;
		public string paramNameFont = "font";
		public List<Font> fonts = new List<Font>();
		public AdvUguiMessageWindow messageWindow = null;

		string CurrentFontName { get; set; }

		//指定の名前のフォントに変更
		void ChangeFont(string fontName)
		{
			var font = fonts.Find(x => x.name == fontName);
			if (font == null)
			{
				Debug.LogError($"{fontName} は設定されていないフォント名です");
				return;
			}

			messageWindow.Text.font = font;
			CurrentFontName = fontName;
		}

		void Update()
		{
			if (!engine.IsStarted) return;
			if (!engine.Param.IsInit) return;

			var fontName = engine.Param.GetParameterString(paramNameFont);
			if (CurrentFontName != fontName)
			{
				ChangeFont(fontName);
			}
		}
	}
}
