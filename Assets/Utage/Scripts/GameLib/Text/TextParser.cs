// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Utage
{

	// テキストの解析クラス（宴の基本のノベル用の構文解析）
	public class TextParser : TextParserBase
	{
		public const string TagSound = "sound";
		public const string TagSpeed = "speed";
		public const string TagUnderLine = "u";
		
		/// <summary>
		/// 文字列から数式を計算するコールバック
		/// </summary> 
		[RuntimeInitializeStaticField] public static Func<string, object> CallbackCalcExpression;

		[Obsolete("Use TextData.MakeLogText")]
		public static string MakeLogText(string text)
		{
			//数値タグだけテキストに変換し、その他のタグは残したままのテキストを生成
			return new TextParser(text, true).NoneMetaString;
		}

		//パラメーターのみパースする
		protected bool isParseParamOnly;
	

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="text">メタデータを含むテキスト</param>
		public TextParser(string text, bool isParseParamOnly = false)
			: base(text)
		{
			this.isParseParamOnly = isParseParamOnly;
			Parse();
		}

		protected override bool ParseTag(string name, string arg)
		{
			if (isParseParamOnly)
			{
				return ParseTagParamOnly(name, arg);
			}
			else
			{
				return ParseNovelTag(name, arg);
			}
		}

		//ノベル用のタグ解析
		protected virtual bool ParseNovelTag(string name, string arg)
		{
			switch (name)
			{
				//太字
				case "b":
					return parsingInfo.TryParseBold(arg);
				case "/b":
					parsingInfo.ResetBold();
					return true;
				//イタリック
				case "i":
					return parsingInfo.TryParseItalic(arg);
				case "/i":
					parsingInfo.ResetItalic();
					return true;
				//カラー
				case "color":
					return parsingInfo.TryParseColor(arg);
				case "/color":
					parsingInfo.ResetColor();
					return true;
				//サイズ
				case "size":
					return parsingInfo.TryParseSize(arg);
				case "/size":
					parsingInfo.ResetSize();
					return true;
				//ルビ
				case "ruby":
					return parsingInfo.TryParseRuby(arg);
				case "/ruby":
					parsingInfo.ResetRuby();
					return true;
				//傍点
				case "em":
					return parsingInfo.TryParseEmphasisMark(arg);
				case "/em":
					parsingInfo.ResetEmphasisMark();
					return true;
				//上付き文字
				case "sup":
					return parsingInfo.TryParseSuperScript(arg);
				case "/sup":
					parsingInfo.ResetSuperScript();
					return true;
				//下付き文字
				case "sub":
					return parsingInfo.TryParseSubScript(arg);
				case "/sub":
					parsingInfo.ResetSubScript();
					return true;
				//下線
				case TagUnderLine:
					return parsingInfo.TryParseUnderLine(arg);
				case "/" + TagUnderLine:
					parsingInfo.ResetUnderLine();
					return true;
				//取り消し線
				case "strike":
					return parsingInfo.TryParseStrike(arg);
				case "/strike":
					parsingInfo.ResetStrike();
					return true;
				//グループ文字
				case "group":
					return parsingInfo.TryParseGroup(arg);
				case "/group":
					parsingInfo.ResetGroup();
					return true;
				//絵文字
				case "emoji":
					return TryAddEmoji(arg);
				//ダッシュ（ハイフン・横線）
				case "dash":
					AddDash(arg);
					return true;
				//スペース
				case "space":
					return TryAddSpace(arg);
				//リンク
				case "link":
					return parsingInfo.TryParseLink(arg);
				case "/link":
					parsingInfo.ResetLink();
					return true;
				//TIPS
				case "tips":
					return parsingInfo.TryParseTips(arg);
				case "/tips":
					parsingInfo.ResetTips();
					return true;
				//サウンド
				case TagSound:
					return parsingInfo.TryParseSound(arg);
				case "/"+TagSound:
					parsingInfo.ResetSound();
					return true;
				//スピード
				case TagSpeed:
					return parsingInfo.TryParseSpeed(arg);
				case "/"+TagSpeed:
					parsingInfo.ResetSpeed();
					return true;
				//インターバル
				case "interval":
					return TryAddInterval(arg);
				//変数の文字表示
				case "param":
					return ParseParam(arg);
				//フォーマットつき変数表示
				case "format":
					return ParseParamFormat(arg);
				//テキストのスキップ
				case "skip_text":
					ParseSkipText(arg);
					return true;
				default:
					return false;
			};
		}

		//パラメーター数値のみの解析
		protected virtual bool ParseTagParamOnly(string name, string arg)
		{
			switch (name)
			{
				//変数の文字表示
				case "param":
					return ParseParam(arg);
				//フォーマットつき変数表示
				case "format":
					return ParseParamFormat(arg);
				default:
					return false;
			};
		}

		//変数の文字表示
		protected virtual bool ParseParam(string arg)
		{
			string str = ExpressionToString(arg);
			AddString(str);
			return true;
		}

		//フォーマットつき変数表示
		protected virtual bool ParseParamFormat(string arg)
		{
			char[] separator = { ':' };
			string[] args = arg.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);
			int num = args.Length - 1;
			string[] paramKeys = new string[num];
			Array.Copy(args, 1, paramKeys, 0, num);
			string str = FormatExpressionToString(args[0], paramKeys);
			AddString(str);
			return true;
		}

		//棒線（ダッシュ、ダーシ）を追加
		protected virtual void AddDash(string arg)
		{
			int size;
			if (!int.TryParse(arg, out size))
			{
				size = 1;
			}
			CharData data = new CharData(CharData.Dash, parsingInfo);
			data.CustomInfo.IsDash = true;
			data.CustomInfo.DashSize = size;
			AddCharData(data);
		}

		//絵文字を追加
		protected virtual bool TryAddEmoji(string arg)
		{
			if (string.IsNullOrEmpty(arg))
			{
				return false;
			}

			CharData data = new CharData('□', parsingInfo);
			data.CustomInfo.IsEmoji = true;
			data.CustomInfo.EmojiKey = arg;
			AddCharData(data);
			return true;
		}

		//サイズ指定のスペースの追加
		protected virtual bool TryAddSpace(string arg)
		{
			CharData data = new CharData(' ', parsingInfo);
			data.CustomInfo.IsSpace = true;
			AddCharData(data);

			int size;
			if (int.TryParse(arg, out size))
			{
				data.CustomInfo.SpaceSize = size;
				return true;
			}
			else
			{
				return false;
			}
		}

		//改行文字を追加
		protected virtual void AddBr(string arg)
		{
			CharData data = new CharData('\n', parsingInfo);
			AddCharData(data);
		}

		//インターバルの追加
		protected virtual bool TryAddInterval(string arg)
		{
			if (CharList.Count <= 0) return false;
			return CharList[charList.Count - 1].TryParseInterval(arg);
		}


		/// <summary>
		/// 数式の結果を文字列にする
		/// </summary>
		/// <param name="exp">数式の文字列</param>
		/// <returns>結果の値の文字列</returns>
		protected virtual string ExpressionToString(string exp)
		{
			if (null == CallbackCalcExpression)
			{
				AddErrorMsg(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.TextCallbackCalcExpression));
				return "";
			}
			else
			{
				object obj = CallbackCalcExpression(exp);
				if (obj == null)
				{
					AddErrorMsg(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.TextFailedCalcExpression));
					return "";
				}
				else
				{
					return obj.ToString();
				}
			}
		}



		/// <summary>
		/// フォーマットつき数式の結果を文字列にする
		/// </summary>
		/// <param name="format">出力フォーマット</param>
		/// <param name="exps">数式の文字列のテーブル</param>
		/// <returns>結果の値の文字列</returns>
		protected virtual string FormatExpressionToString(string format, string[] exps)
		{
			if (null == CallbackCalcExpression)
			{
				AddErrorMsg(LanguageErrorMsg.LocalizeTextFormat(Utage.ErrorMsg.TextCallbackCalcExpression));
				return "";
			}
			else
			{
				List<object> args = new List<object>();
				foreach (string exp in exps)
				{
					args.Add(CallbackCalcExpression(exp));
				}
				return string.Format(format, args.ToArray());
			}
		}

		//テキストスキップタグを解析
		protected virtual void ParseSkipText(string arg)
		{
			if (bool.TryParse(arg, out bool flag))
			{
				EnablePageCtrlOnSkipText = flag;
			}
			this.SkipText = true;
		}

	}
}
