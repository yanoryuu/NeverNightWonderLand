// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Utage
{

	// テキストの解析の基底クラス
	public abstract class TextParserBase
	{
		public static string AddTag(string text, string tag, string arg)
		{
			return string.Format("<{1}={2}>{0}</{1}>", text, tag, arg);
		}

		/// <summary>
		/// 文字データリスト
		/// </summary>
		internal virtual List<CharData> CharList { get { return this.charList; } }
		protected List<CharData> charList = new List<CharData>();

		/// 構文解析したデータのリスト
		public virtual List<IParsedTextData> ParsedDataList { get { return this.parsedDataList; } }
		protected List<IParsedTextData> parsedDataList = new List<IParsedTextData>();

		/// 構文解析注のデータのリスト
		List<IParsedTextData> PoolList { get { return this.poolList; } }
		List<IParsedTextData> poolList = new List<IParsedTextData>();

		//テキストスキップタグが存在している
		public bool SkipText { get; protected set; } 
		//テキストスキップをする際にページコントロールを有効にするか
		public bool EnablePageCtrlOnSkipText { get; protected set; }

		//サロゲートペア文字の解析をするかどうか
		protected virtual bool EnableParseSurrogatePair => false;
		
		/// <summary>
		/// エラーメッセージ
		/// </summary>
		public virtual string ErrorMsg { get { return this.errorMsg; } }
		protected string errorMsg = null;
		protected virtual void AddErrorMsg(string msg)
		{
			if (string.IsNullOrEmpty(errorMsg)) errorMsg = "";
			else errorMsg += "\n";

			errorMsg += msg;
		}

		/// <summary>
		/// 表示文字数（メタデータを覗く）
		/// </summary>
		public virtual int Length { get { return CharList.Count; } }

		//もとのテキスト
		public virtual string OriginalText
		{
			get { return originalText; }
		}
		protected string originalText;

		/// <summary>
		/// メタ情報なしの文字列を取得
		/// </summary>
		/// <returns>メタ情報なしの文字列</returns>
		public virtual string NoneMetaString
		{
			get
			{
				//未作成なら作成する
				InitNoneMetaText();
				return noneMetaString;
			}
		}
		protected string noneMetaString;

		//メタ情報なしのテキストを初期化する
		protected virtual void InitNoneMetaText()
		{
			//作成ずみならなにもしない
			if (!string.IsNullOrEmpty(noneMetaString)) return;

			StringBuilder builder = new StringBuilder();
			for (int i = 0; i < CharList.Count; ++i)
			{
				CharList[i].AppendToStringBuilder(builder);
			}
			noneMetaString = builder.ToString();
		}

		//解析中の文字情報
		protected CharData.CustomCharaInfo parsingInfo = new CharData.CustomCharaInfo();

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="text">メタデータを含むテキスト</param>
		public TextParserBase(string text)
		{
			originalText = text;
		}

		/// <summary>
		/// メタデータを含むテキストデータを解析
		/// </summary>
		/// <param name="text">解析するテキスト</param>
		protected virtual void Parse()
		{
			try
			{
				//テキストを先頭から1文字づつ解析
				int max = OriginalText.Length;
				int index = 0;
				while (index < max)
				{
					if (ParseEscapeSequence(index))
					{
						//エスケープシーケンスの処理
						index += 2;
					}
					else
					{
						if (!TryParseTag(index, out int endIndex, out string tagName, out string tagArg))
						{
							//タグがなかった
							//通常パターンのテキストを1文字追加
							AddChar(OriginalText,ref index);
							++index;
						}
						else
						{
							//タグデータを挿入
							string tagString = OriginalText.Substring(index,endIndex- index+1);
							PoolList.Insert( 0, MakeTag(tagString, tagName, tagArg));
							index = endIndex+1;
						}
					}
					ParsedDataList.AddRange(PoolList);
					PoolList.Clear();
				}
				PoolList.Clear();
			}
			catch ( System.Exception e )
			{
				AddErrorMsg(e.Message + e.StackTrace );
			}
		}

		//タグを作成（特殊な処理が必要な場合はここをoverride）
		protected virtual TagData MakeTag(string fullString, string name, string arg)
		{
			return new TagData(fullString, name, arg);
		}

		//文字を追加
		protected virtual void AddCharData(CharData data)
		{
			CharList.Add(data);
			PoolList.Add(data);
			parsingInfo.ClearOnNextChar();
		}

		//文字を追加
		protected virtual int AddChar(string srcString, ref int index)
		{
			if(index >= srcString.Length)
			{
				return index;
			}

			//サロゲートペアを考慮した解析
			if (EnableParseSurrogatePair && char.IsSurrogatePair(srcString, index))
			{
				AddCharData(new CharData(srcString[index], srcString[index+1], parsingInfo));
				++index;
				return index;
			}
			else
			{
				AddCharData(new CharData(srcString[index], parsingInfo));
				return index;
			}
		}

		//文字列を追加
		protected virtual void AddString(string text)
		{
			for(int i=0; i<text.Length; ++i )
			{
				AddChar(text,ref i);
			}
		}

		//エスケープシーケンス解析
		protected virtual bool ParseEscapeSequence(int index)
		{
			//二文字目がない場合は何もしない
			if (index + 1 >= OriginalText.Length)
			{
				return false;
			}

			char c0 = OriginalText[index];
			char c1 = OriginalText[index + 1];

			//改行コードの処理だけはする
			if (c0 == '\\' && c1 == 'n')
			{   //文字列としての改行コード　\n
				//通常パターンのテキストを1文字追加
				AddDoubleLineBreak();
				return true;
			}
			else if (c0 == '\r' && c1 == '\n')
			{   //改行コード \r\nを1文字で扱う
				AddDoubleLineBreak();
				return true;
			}
			return false;
		}


		//本来二文字ぶんの改行文字を追加
		protected virtual void AddDoubleLineBreak()
		{
			CharData data = new CharData('\n', parsingInfo);
			data.CustomInfo.IsDoubleWord = true;
			AddCharData(data);
		}

		//タグの解析。解析方法によってここをoverrideして処理
		protected virtual bool TryParseTag(int index, out int endIndex, out string tagName, out string tagArg)
		{
			string name0 ="";
			string arg0 = "";

			endIndex = ParserUtil.ParseTag(OriginalText, index, 
				(name,arg)=>
				{
					bool ret = ParseTag(name, arg);
					if (ret)
					{
						name0 = name;
						arg0 = arg;
					}
					return ret;
				});
			tagName =name0;
			tagArg = arg0;
			return endIndex != index;
		}

		//タグの解析、タグの内容によってここをoverrideして処理
		protected abstract bool ParseTag(string name, string arg);
	}
}
