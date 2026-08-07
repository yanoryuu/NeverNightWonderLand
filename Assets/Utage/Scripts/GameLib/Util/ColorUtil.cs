// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// カラー処理
	/// </summary>
	public static class ColorUtil
	{
		/// <summary>
		/// 色の定義。Unityのリッチテキストで使えるカラー名に対応している
		/// </summary>

		public static readonly Color Aqua = new Color32(0x00, 0xff, 0xff, 0xff);
		public static readonly Color Black = new Color32(0x00, 0x00, 0x00, 0xff);
		public static readonly Color Blue = new Color32(0x00, 0x00, 0xff, 0xff);
		public static readonly Color Brown = new Color32(0xa5, 0x2a, 0x2a, 0xff);
		public static readonly Color Cyan = new Color32(0x00, 0xff, 0xff, 0xff);
		public static readonly Color Darkblue = new Color32(0x0, 0x0, 0xa0, 0xff);
		public static readonly Color Fuchsia = new Color32(0xff, 0x00, 0xff, 0xff);
		public static readonly Color Green = new Color32(0x00, 0x80, 0x00, 0xff);
		public static readonly Color Grey = new Color32(0x80, 0x80, 0x80, 0xff);
		public static readonly Color Lightblue = new Color32(0xad, 0xd8, 0xe6, 0xff);
		public static readonly Color Lime = new Color32(0x00, 0xff, 0x00, 0xff);
		public static readonly Color Magenta = new Color32(0xff, 0x00, 0xff, 0xff);
		public static readonly Color Maroon = new Color32(0x80, 0x00, 0x00, 0xff);
		public static readonly Color Navy = new Color32(0x00, 0x00, 0x80, 0xff);
		public static readonly Color Olive = new Color32(0x80, 0x80, 0x00, 0xff);
		public static readonly Color Orange = new Color32(0xff, 0xa5, 0x00, 0xff);
		public static readonly Color Purple = new Color32(0x80, 0x00, 0x80, 0xff);
		public static readonly Color Red = new Color32(0xff, 0x00, 0x00, 0xff);
		public static readonly Color Silver = new Color32(0xc0, 0xc0, 0xc0, 0xff);
		public static readonly Color Teal = new Color32(0x00, 0x80, 0x80, 0xff);
		public static readonly Color White = new Color32(0xff, 0xff, 0xff, 0xff);
		public static readonly Color Yellow = new Color32(0xff, 0xff, 0x00, 0xff);

		/// <summary>
		/// 文字列からカラーデータを解析する
		/// </summary>
		/// <param name="text">テキスト</param>
		/// <returns>解析したカラー値。解析できなかったら例外を投げる</returns>
		public static Color ParseColor(string text)
		{
			Color color = Color.white;
			if (TryParseColor(text, ref color))
			{
				return color;
			}
			else
			{
				throw new System.Exception( LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.ColorParseError,text) );
			}
		}

		/// <summary>
		/// 文字列からカラーデータを解析する
		/// </summary>
		/// <param name="text">テキスト</param>
		/// <param name="color">カラー</param>
		/// <returns>解析に成功したらtrue。書式間違いなどで解析できなかったらfalse</returns>
		public static bool TryParseColor(string text, ref Color color)
		{
			if (string.IsNullOrEmpty(text)) return false;

			if (text[0] == '#')
			{
				//16進数値指定のカラー
				return TryParseColorDetail(text.Substring(1), ref color);
			}
			else
			{
				//対応するカラー名をチェック
				switch (text)
				{
					case "aqua": color = Cyan; break;
					case "black": color = Black; break;
					case "blue": color = Blue; break;
					case "brown": color = Brown; break;
					case "cyan": color = Cyan; break;
					case "darkblue": color = Darkblue; break;
					case "fuchsia": color = Magenta; break;
					case "green": color = Green; break;
					case "grey": color = Grey; break;
					case "lightblue": color = Lightblue; break;
					case "lime": color = Lime; break;
					case "magenta": color = Magenta; break;
					case "maroon": color = Maroon; break;
					case "navy": color = Navy; break;
					case "olive": color = Olive; break;
					case "orange": color = Orange; break;
					case "purple": color = Purple; break;
					case "red": color = Red; break;
					case "silver": color = Silver; break;
					case "teal": color = Teal; break;
					case "white": color = White; break;
					case "yellow": color = Yellow; break;
					default:
						return false;
				}
			}
			return true;
		}

		/// <summary>
		/// 16進数値の文字列からカラーを解析
		/// </summary>
		/// <param name="text">16進数値の文字列</param>
		/// <param name="color">カラー</param>
		/// <returns>解析に成功したらtrue。書式間違いなどで解析できなかったらfalse</returns>
		static bool TryParseColorDetail(string text, ref Color color)
		{
			try
			{
				if (text.Length == 6)
				{
					int num = System.Convert.ToInt32(text, 16);
					float r = ((num & 0xff0000) >> 16) / 255f;
					float g = ((num & 0xff00) >> 8) / 255f;
					float b = ((num & 0xff)) / 255f;
					color = new Color(r, g, b);
					return true;
				}
				else if (text.Length == 8)
				{
					int num = System.Convert.ToInt32(text, 16);
					float r = ((num & 0xff000000) >> 24) / 255f;
					float g = ((num & 0xff0000) >> 16) / 255f;
					float b = ((num & 0xff00) >> 8) / 255f;
					float a = ((num & 0xff)) / 255f;
					color = new Color(r, g, b, a);
					return true;
				}
				else
				{
					return false;
				}
			}
			catch (System.Exception )
			{
				return false;
			}
		}

		/// <summary>
		/// カラーを16進数値の文字列に変換
		/// </summary>
		/// <param name="color">カラー</param>
		/// <returns>変換後の16進数値の文字列</returns>
		public static string ToColorString(Color color)
		{
			int r = (int)(255f * color.r);
			int g = (int)(255f * color.g);
			int b = (int)(255f * color.b);
			int a = (int)(255f * color.a);
			int num = (r << 24) + (g << 16) + (b << 8) + a;
			return num.ToString("X8").ToLower();
		}

		/// <summary>
		/// カラーをNGUI用16進数値の文字列に変換
		/// </summary>
		/// <param name="color">カラー</param>
		/// <returns>変換後のNGUI用16進数値の文字列</returns>
		public static string ToNguiColorString(Color color)
		{
			int r = (int)(255f * color.r);
			int g = (int)(255f * color.g);
			int b = (int)(255f * color.b);
			int num = (r << 16) + (g << 8) + b;
			return num.ToString("X6").ToLower();
		}


		/// <summary>
		/// 文字列の前後にカラーのリッチテキストタグを追加
		/// </summary>
		/// <param name="str">文字列</param>
		/// <param name="colorKey">タグに設定するカラーの文字列</param>
		/// <returns></returns>
		public static string AddColorTag(string str, string colorKey)
		{
			string format = "<color={1}>{0}</color>";
			return string.Format(format, str, colorKey);
		}

		/// <summary>
		/// 文字列の前後にカラーのリッチテキストタグを追加
		/// </summary>
		/// <param name="str">文字列</param>
		/// <param name="colorKey">タグに設定するカラー</param>
		/// <returns></returns>
		public static string AddColorTag(string str, Color color)
		{
			return AddColorTag(str, "#" + ColorUtil.ToColorString(color));
		}
		
		//コンソールに表示するエラーメッセージに、エラーのカラータグなどを設定
		public static string ToColorTagErrorMsg(string errorMsg, bool enableEscapeNewlines = true)
		{
			if (enableEscapeNewlines)
			{
				//Unityのコンソールは、表示行数（ユーザー設定で可変）を超えた行はカットされてしまい、カラータグを閉じられなくなってしまうので
				//メッセージに改行文字が含まれる場合は、改行文字をエスケープして改行を無効化する。
				errorMsg = StringUtil.EscapeNewlines(errorMsg);
			}
			
			//エラーのカラータグを設定
			string str = AddColorTag(errorMsg, GetErrorColorInEditor());
			if (IsDarkThemeInEditor())
			{
				//ダークテーマの場合は見づらいので<b>タグを設定
				return $"<b>{str}</b>";
			}
			else
			{
				return str;
			}
		}
		
		//コンソールに表示するエラー文字の色
		public static Color GetErrorColorInEditor()
		{
			//ダークテーマの場合、明るめの赤にする
			return IsDarkThemeInEditor() ? new Color(1.0f, 0.5f, 0.5f) : Color.red;
		}
		
		//Unityエディタがダークテーマか？
		public static bool IsDarkThemeInEditor()
		{
#if  UNITY_EDITOR
			if (UnityEditor.EditorGUIUtility.isProSkin)
			{
				return true;
			}
#endif
			
			return false;
		}
		
		//エディター用のGUIボックスの背景色
		public static Color GetEditorGuiBoxBgColor()
		{
			return IsDarkThemeInEditor() ? new Color(0.1875f, 0.1875f, 0.1875f) : new Color(0.75f, 0.75f, 0.75f);
		}
	}
}
