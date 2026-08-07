using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UtageExtensions;

namespace Utage
{
	//宴のタグ構文解析とTextMeshProのタグ構文解析をして、
	//TextMeshPro形式のタグ付き文字列を生成するためのクラス
	public class TextMeshProTextParser : CustomTextParserBase
	{
		//サロゲートペア文字の解析をするかどうか
		protected override bool EnableParseSurrogatePair => true;

		public TextMeshProTextParser(string text, bool isParseParamOnly = false)
			: base(text, isParseParamOnly)
		{
		}

		public void AutoIndent(TextMeshProAutoIndentSettings settings)
		{
			foreach (var indentData in settings.IndentDataList)
			{
				if(IsMatchIndentData(indentData, out int index ))
				{
					TagData tagData = new TagData(indentData.IndentTag, "indent", "");
					this.ParsedDataList.Insert(index+1,tagData);
					return;
				}
			}
			return;
			
			bool IsMatchIndentData(TextMeshProAutoIndentSettings.IndentData indentData, out int index)
			{
				index = 0;
				if (CharList.Count <= indentData.Prefix.Length) return false;

				int charIndex = 0;
				for (index = 0; index < ParsedDataList.Count; index++)
				{
					var parsedData = ParsedDataList[index];
					if (parsedData is CharData charData)
					{
						if(charData.Char != indentData.Prefix[charIndex]) return false;
						charIndex++;
						if(charIndex >= indentData.Prefix.Length) return true;
					}
				}
				return false;
			}
		}

		//TextMeshPro対応のタグ解析
		protected override bool TryParseCustomTag(string name, string arg)
		{
			//タグ処理が必要なものは処理をする
			if (TryTagOperation(name, arg))
			{
				return true;
			}

			//特に処理の必要のないTextMeshProの基本タグ
			if (IsDefaultTag(name))
			{
				return true;
			}
			
			return false;
		}

		//タグの解析を無視するタグが設定されている
		protected override bool IsNoParseTag(string name)
		{
			return (name == "/noparse");
		}

		//特に処理の必要のない基本タグか
		protected virtual bool IsDefaultTag(string name)
		{
			//特に処理の必要のないTextMeshProの基本タグ
			if (defaultTagTbl.Contains(name))
			{
				return true;
			}
			return false;
		}

		//特に処理の必要のないTextMeshProの基本タグ
		static readonly List<string> defaultTagTbl = new List<string>()
			{
				//テキストメッシュプロのタグ
				"align","/align",
				"alpha",//"/alpha",
				"color","/color",
				"b","/b",
				"i","/i",
				"cspace","/cspace",
				"font","/font",
				"indent","/indent",
				"line-height","/line-height",
				"line-indent","/line-indent",
				"link","/link",
				"lowercase","/lowercase",
				"uppercase","/uppercase",
				"smallcaps","/smallcaps",
				"margin","/margin",
				"margin-left",//"/margin-left",
				"margin-right", //"/margin-right",
				"mark","/mark",
				"mspace","/mspace",
				"nobr","/nobr",
				"page","/page",
				"pos",
				"size","/size",
				"space",
				"s","/s",
				"u","/u",
				"style","/style",
				"sub","/sub",
				"sup","/sup",
				"voffset","/voffset",
				"width","/width",
				"gradient","/gradient",
				"rotate","/rotate",
				"allcaps", "/allcaps",
				"font-weight", "/font-weight",
				
				//追加のタグ
				"url", "/url",
			};

		//処理の必要のあるタグなら、処理をする
		//タグ名そのものの命名規則対応や、解析時にタグではなくテキスト文字の追加する必要がある場合などはここに記述する
		protected virtual bool TryTagOperation(string name, string arg)
		{
			//カラーコードのみ指定のタグ
			if (name[0] == '#')
			{
				return true;
			}

			//特別な処理が必要なもの
			switch (name)
			{
				case "noparse":
					NoParse = true;
					return true;
				case "sprite":
					TryAddEmoji(arg);
					return true;
				case "dash":
					//二文字ぶん
					AddDash(arg);
					AddDash(arg);
					return true;
				case "br":
					//改行
					AddBr(arg);
					return true;
				default:
					break;
			}

			return false;
		}

		//タグデータを作成
		//宴の基本タグをTextMeshProのタグ形式に変換したり、
		//独自定義タグをTextMeshProのタグ形式のタグに変換したりする
		protected override TagData MakeTag(string fullString, string name, string arg)
		{
			TagData tagData = MakeCustomTag(fullString, name, arg);
			if (tagData != null) return tagData;

			tagData = MakeUtageTag(fullString, name, arg);
			if (tagData != null) return tagData;

			switch (name)
			{
				//TextMeshPro用の特殊な操作が必要なタグ
				case "br":
					//改行文字はすでに文字として組み込まれているので、タグにしない
					return new TagData(fullString, name, arg, true);
				case "sprite":
					//文字数を1カウント
					return new TagData(fullString, name, arg) { CountCharacter = 1 };

				//通常のタグ
				default:
					return new TagData(fullString, name, arg);
			}
		}
		
		//さらにタグ処理をカスタムする場合はさらにここを追加
		protected virtual TagData MakeCustomTag(string fullString, string name, string arg)
		{
			return null;
		}

		//宴の基本タグ用のタグデータを作成する
		protected virtual TagData MakeUtageTag(string fullString, string name, string arg)
		{
			switch (name)
			{
				//宴独自タグをTextMeshProのタグに変換
				case "group":
					return new TagData("<nobr>", "nobr", arg);
				case "/group":
					return new TagData("</nobr>", "nobr", arg);
				case "strike":
					return new TagData("<s>", "s", arg);
				case "/strike":
					return new TagData("</s>", "/s", arg);
				case "emoji":
					string emojiName = "name=\"" + arg + "\"";
					return new TagData("<sprite "+ emojiName +">", "sprite", emojiName) { CountCharacter = 1 };

				case "dash":
					//return new TagData("<nobr><mspace=0>--</mspace></nobr>", name, arg);
					int space = int.Parse(arg);
					return new TagData(
							"<nobr><space=0.25em><s> <space=" + (space - 1) + "em> </s><space=-0.25em></nobr>", name,
							arg)
						{ CountCharacter = 1 };
				case "color":
					//旧宴のカラーコードとの互換のために
					string colorString = ToTextMeshProColorString(arg);
					return new TagData("<color=" + colorString + ">", name, colorString);

				case "tips":
				{
					string linkId = $"tips,{StringUtil.EscapeQuotes(arg.Trim())}";
					return new TagData($"<u><link={linkId}>", "tips", linkId);
				}
				case "/tips":
					return new TagData("</u></link>", "/link", arg);

				case "url":
				{
					string linkId = $"url,{StringUtil.EscapeQuotes(arg.Trim())}";
					return new TagData($"<u><link={linkId}>", "url", linkId);
				}
				case "/url":
					return new TagData("</u></link>", "/url", arg);

				//宴の独自タグなので、TextMeshProのタグとして表記せずに無視する
				case "ruby":
				case "/ruby":
				case "em":
				case "/em":
				case "sound":
				case "/sound":
				case "speed":
				case "/speed":
				case "interval":
				case "param":
				case "format":
				case "skip_page":
				case "skip_text":
					return new TagData(fullString, name, arg, true);
				default:
					return null;
			}
		}

		//TextMeshPro形式のカラー文字列に変換する
		protected virtual string ToTextMeshProColorString(string color)
		{
			switch (color)
			{
				//TextMeshProがサポートしていないカラー名の場合はカラーコードに変換
				case "aqua": return "#00ffff";
				case "brown": return "#a52a2a";
				case "cyan": return "#00ffff";
				case "darkblue": return "#0000a0";
				case "fuchsia": return "#ff00ff";
				case "grey": return "#808080";
				case "lightblue": return "#add8e6";
				case "lime": return "#00ff00";
				case "magenta": return "#ff00ff";
				case "maroon": return "#800000";
				case "navy": return "#000080";
				case "olive": return "#808000";
				case "silver": return "#c0c0c0";
				case "teal": return "#008080";

				//以下はTextMeshProでもサポートしているカラー名ならそのままで
				case "black":
				case "blue":
				case "green":
				case "orange":
				case "purple":
				case "red":
				case "white":
				case "yellow":
					return color;
				//カラーコードなので変換無し
				default:
					return color;
			}
		}
	}
}
