// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Utage
{

	// テキストデータ（構文解析したテキストデータや、元の文字列などを管理）
	public class TextData
	{
		//テキスト解析処理カスタムする場合のコールバック
		//TextMeshPro対応などで使う
		[RuntimeInitializeStaticField] public static Func<string, TextParserBase> CreateCustomTextParser = null;

		//テキスト解析処理のインスタンスを作成
		static TextParserBase CreateTextParser(string text)
		{
			if (CreateCustomTextParser == null)
			{
				return new TextParser(text);
			}
			else
			{
				return CreateCustomTextParser(text);
			}
		}

		//ログテキストを作成する場合のコールバック
		//TextMeshPro対応などで使う
		[RuntimeInitializeStaticField] public static Func<string, string> MakeCustomLogText = null;

		//ログ用のテキストを作成
		public static string MakeLogText(string text)
		{
			if (MakeCustomLogText == null)
			{
				//数値タグだけテキストに変換し、その他のタグは残したままのテキストを生成
				return new TextParser(text, true).NoneMetaString;
			}
			
			else
			{
				return MakeCustomLogText(text);
			}
		}

		public TextParserBase ParsedText { get; private set; }

		//メタデータを含んだもともとのテキスト
		public string OriginalText { get { return ParsedText.OriginalText; } }


		/// <summary>
		/// メタ情報なしの文字列を取得
		/// </summary>
		/// <returns>メタ情報なしの文字列</returns>
		public string NoneMetaString { get { return ParsedText.NoneMetaString; } }

		/// <summary>
		/// 文字データリスト
		/// </summary>
		public List<CharData> CharList { get { return ParsedText.CharList; } }

		/// <summary>
		/// 表示文字数（メタデータを覗く）
		/// </summary>
		public int Length { get { return CharList.Count; } }

		/// <summary>
		/// 解析時のエラーメッセージ
		/// </summary>
		public string ErrorMsg { get { return ParsedText.ErrorMsg; } }

		//スピードタグがあるか
		public bool ContainsSpeedTag { get; protected set; }
		//スピードタグがすべてNoWait(スピード0)かどうか
		public bool IsNoWaitAll { get; protected set; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="text">メタデータを含むテキスト</param>
		public TextData(string text)
		{
			ParsedText = CreateTextParser(text);
			IsNoWaitAll = true;
			foreach (var data in ParsedText.CharList)
			{
				if (data.CustomInfo.IsSpeed)
				{
					ContainsSpeedTag = true;
				}

				if (data.CustomInfo.IsSpeed && data.CustomInfo.speed == 0)
				{
				}
				else
				{
					IsNoWaitAll = false;
				}
			}
		}

		public string MakeNoneMetaStringWithEmojiTMP()
		{
			StringBuilder builder = new StringBuilder();
			for (int i = 0; i < ParsedText.CharList.Count; ++i)
			{
				var c = ParsedText.CharList[i];
				var cInfo = c.CustomInfo;
				if(cInfo.IsEmoji)
				{
					string arg = c.CustomInfo.EmojiKey;
					if (!arg.Contains("index") && !arg.Contains("name"))
					{
						//旧式のemojiタグ
						arg = $" name=\"{arg}\"";
					}
					else
					{
						bool startsWithIndex = arg.StartsWith("index", StringComparison.OrdinalIgnoreCase);
						bool startsWithName = arg.StartsWith("name", StringComparison.OrdinalIgnoreCase);
						if (!startsWithIndex && !startsWithName)
						{
							//=で値にアセット名が指定されているタイプ
							arg = "=" + arg;
						}
						else
						{
							//アセット名省略で指定されているタイプ
							arg = " " + arg;
						}
					}
					builder.Append($"<sprite{arg}>");
				}
				else
				{
					c.AppendToStringBuilder(builder);
				}
			}
			return builder.ToString();
		}


		/// <summary>
		/// Unityのリッチテキストフォーマットのテキスト
		/// </summary>
		public string UnityRitchText
		{
			get
			{
				//未作成なら作成する
				InitUnityRitchText();
				return unityRitchText;
			}
		}
		string unityRitchText;
		const string BoldEndTag = "</b>";
		const string ItalicEndTag = "</i>";
		const string ColorEndTag = "</color>";
		const string SizeEndTag = "</size>";

		/// Unityのリッチテキストフォーマットのテキストを初期化する
		public void InitUnityRitchText()
		{
			//作成ずみならなにもしない
			if (!string.IsNullOrEmpty(unityRitchText)) return;

			unityRitchText = "";
			CharData.CustomCharaInfo curentCustomInfo = new CharData.CustomCharaInfo();

			//タグの前後関係を正しくするためにStackを使う
			Stack<string> endTagStack = new Stack<string>();

			for (int i = 0; i < CharList.Count; ++i)
			{
				CharData c = CharList[i];
				if (c.CustomInfo.IsEndBold(curentCustomInfo)) unityRitchText += endTagStack.Pop();
				if (c.CustomInfo.IsEndItalic(curentCustomInfo)) unityRitchText += endTagStack.Pop();
				if (c.CustomInfo.IsEndSize(curentCustomInfo)) unityRitchText += endTagStack.Pop();
				if (c.CustomInfo.IsEndColor(curentCustomInfo)) unityRitchText += endTagStack.Pop();

				if (c.CustomInfo.IsBeginColor(curentCustomInfo))
				{
//					unityRitchText += "<color=#" + ColorUtil.ToColorString(c.CustomInfo.color) + ">";
					unityRitchText += "<color=" + c.CustomInfo.colorStr + ">";
					endTagStack.Push(ColorEndTag);
				}
				if (c.CustomInfo.IsBeginSize(curentCustomInfo))
				{
					unityRitchText += "<size=" + c.CustomInfo.size + ">";
					endTagStack.Push(SizeEndTag);
				}
				if (c.CustomInfo.IsBeginItalic(curentCustomInfo))
				{
					unityRitchText += "<i>";
					endTagStack.Push(ItalicEndTag);
				}
				if (c.CustomInfo.IsBeginBold(curentCustomInfo))
				{
					unityRitchText += "<b>";
					endTagStack.Push(BoldEndTag);
				}

				c.UnityRitchTextIndex = unityRitchText.Length;
				unityRitchText += c.Char;
				if (c.CustomInfo.IsDoubleWord)
				{
					unityRitchText += " ";
				}
				curentCustomInfo = c.CustomInfo;
			}
			if (curentCustomInfo.IsBold) unityRitchText += endTagStack.Pop();
			if (curentCustomInfo.IsItalic) unityRitchText += endTagStack.Pop();
			if (curentCustomInfo.IsSize) unityRitchText += endTagStack.Pop();
			if (curentCustomInfo.IsColor) unityRitchText += endTagStack.Pop();
		}
	}
}
