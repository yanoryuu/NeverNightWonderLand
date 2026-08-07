// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// 解析のための補助クラス
	/// </summary>
	public static class ParserUtil
	{
		/// <summary>
		/// Enum型を文字列から解析
		/// </summary>
		/// <typeparam name="T">enum型</typeparam>
		/// <param name="str">enum値の文字列</param>
		/// <param name="val">結果のenum値</param>
		/// <returns>成否</returns>
		public static bool TryParaseEnum<T>(string str, out T val)
		{
			try
			{
				val = (T)System.Enum.Parse(typeof(T), str);
				return true;
			}
			catch (System.Exception)
			{
				val = default(T);
				return false;
			}
		}

		/// <summary>
		/// タグ<　>を使ったテキストを解析して、タグを除いたテキストを返す
		/// </summary>
		public static string ParseTagTextToString(string text, Func<string, string, bool> callbackTagParse)
		{
			if (string.IsNullOrEmpty(text)) return text;

			//現在のテキストインデックス
			int index = 0;
			StringBuilder builder = new StringBuilder();
			while (index < text.Length)
			{
				int endIndex = ParseTag(text, index, callbackTagParse);
				if (endIndex == index)
				{
					builder.Append(text[index]);
					++index;
				}
				else
				{
					index = endIndex+1;
				}
			}
			return builder.ToString();
		}


		/// タグ<　>を使ったテキストを解析して、タグ末尾インデックスを返す。（タグ見つからない場合は、startと同じ値を返す）
		public static char[] DefaultTagSeparator { get; } = { '=', ' ' };
		public const char DefaultTagStartId = '<';
		public const char DefaultTagEndId = '>';
		public static int ParseTag(string text, int start, Func<string, string, bool> callbackParseTag)
		{
			return ParseTag(DefaultTagStartId, DefaultTagEndId, DefaultTagSeparator, text, start, callbackParseTag);
		}

		/// タグ記号を指定して、使ったテキストを解析して、タグ末尾インデックスを返す。（タグ見つからない場合は、startと同じ値を返す）
		public static int ParseTag(char startId, char endId, char[] separator, string text,  int start, Func<string, string, bool> callbackParseTag)
		{
			//タグ識別子ではないので、タグではない
			if (text[start] != startId) return start;

			int index = start + 1;
			int endIndex = text.IndexOf(endId, index);
			if (endIndex < 0) return start;

			string[] tagTexts = text.Substring(index, endIndex - index).Split(separator, 2,System.StringSplitOptions.RemoveEmptyEntries);
			if (tagTexts.Length < 1 || tagTexts.Length > 2)
			{
				return start;
			}

			string name = tagTexts[0];
			string arg = tagTexts.Length > 1 ? tagTexts[1] : "";
			bool ret = callbackParseTag(name, arg);
			if (ret)
			{
				return endIndex;
			}
			else
			{
				return start;
			}
		}

		/// <summary>
		/// 文字列からピボットを解析する
		/// </summary>
		/// <param name="text">テキスト</param>
		/// <param name="defaultValue">デフォルト値</param>
		/// <returns>解析したピボット値。文字列が設定されてなかったらデフォルト値を。解析できなかったら例外を投げる</returns>
		public static Vector2 ParsePivotOptional(string text, Vector2 defaultValue)
		{
			//何も設定がなければデフォルト値を
			if (string.IsNullOrEmpty(text))
			{
				return defaultValue;
			}

			Vector2 pivot = Vector2.one * 0.5f;

			Pivot pivotEnum;
			if (TryParaseEnum<Pivot>(text, out pivotEnum))
			{
				return PivotUtil.PivotEnumToVector2(pivotEnum);
			}

			if (TryParseVector2Optional(text, pivot, out pivot))
			{
				return pivot;
			}
			else
			{
				throw new System.Exception(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.PivotParse, text));
			}
		}

		/// <summary>
		/// 文字列で書かれた、スケール値を読みとる
		/// </summary>
		/// <param name="text">テキスト</param>
		/// <param name="defaultValue">デフォルト値</param>
		/// <returns>スケール値。文字列が設定されてなかったらデフォルト値を。解析できなかったら例外を投げる</returns>
		public static Vector2 ParseScale2DOptional(string text, Vector2 defaultValue)
		{
			//何も設定がなければデフォルト値を
			if (string.IsNullOrEmpty(text))
			{
				return defaultValue;
			}

			Vector2 scale = defaultValue;

			//数字だけが書かれていた場合はx,yを同じ値として扱う
			float s;
			if (WrapperUnityVersion.TryParseFloatGlobal(text, out s))
			{
				return Vector2.one * s;
			}

			if (ParserUtil.TryParseVector2Optional(text, scale, out scale))
			{
				return scale;
			}
			else
			{
				throw new System.Exception( "Parse Scale2D Error " + text);
			}
		}

		/// <summary>
		/// 文字列で書かれた、Vector2を読みとる
		/// </summary>
		public static bool TryParseVector2Optional(string text, Vector2 defaultValue, out Vector2 vec2)
		{
			vec2 = defaultValue;
			if (string.IsNullOrEmpty(text)) return false;

			bool ret = false;
			string[] strings;
			{
				char[] separator = { ' ' };
				strings = text.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);
			}
			foreach (string str in strings)
			{
				char[] separator = { '=' };
				string[] tags = str.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);
				if (tags.Length == 2)
				{
					switch (tags[0])
					{
						case "x":
							if (!WrapperUnityVersion.TryParseFloatGlobal(tags[1], out vec2.x))
							{
								return false;
							}
							ret = true;
							break;
						case "y":
							if (!WrapperUnityVersion.TryParseFloatGlobal(tags[1], out vec2.y))
							{
								return false;
							}
							ret = true;
							break;
						default:
							return false;
					}
				}
				else
				{
					return false;
				}
			}
			return ret;
		}


		/// <summary>
		/// 文字列で書かれた、スケール値を読みとる
		/// </summary>
		/// <param name="text">テキスト</param>
		/// <param name="defaultValue">デフォルト値</param>
		/// <returns>スケール値。文字列が設定されてなかったらデフォルト値を。解析できなかったら例外を投げる</returns>
		public static Vector3 ParseScale3DOptional(string text, Vector3 defaultValue)
		{
			//何も設定がなければデフォルト値を
			if (string.IsNullOrEmpty(text))
			{
				return defaultValue;
			}

			Vector3 scale = defaultValue;

			//数字だけが書かれていた場合はx,yを同じ値として扱う
			float s;
			if (WrapperUnityVersion.TryParseFloatGlobal(text, out s))
			{
				return Vector3.one * s;
			}

			if (ParserUtil.TryParseVector3Optional(text, scale, out scale))
			{
				return scale;
			}
			else
			{
				throw new System.Exception("Parse Scale3D Error " + text);
			}
		}

		/// <summary>
		/// 文字列で書かれた、Vector3を読みとる
		/// </summary>
		public static bool TryParseVector3Optional(string text, Vector3 defaultValue, out Vector3 vec3)
		{
			vec3 = defaultValue;
			if (string.IsNullOrEmpty(text)) return false;

			bool ret = false;
			string[] strings;
			{
				char[] separator = { ' ' };
				strings = text.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);
			}
			foreach (string str in strings)
			{
				char[] separator = { '=' };
				string[] tags = str.Split(separator, System.StringSplitOptions.RemoveEmptyEntries);
				if (tags.Length == 2)
				{
					switch (tags[0])
					{
						case "x":
							if (!WrapperUnityVersion.TryParseFloatGlobal(tags[1], out vec3.x))
							{
								return false;
							}
							ret = true;
							break;
						case "y":
							if (!WrapperUnityVersion.TryParseFloatGlobal(tags[1], out vec3.y))
							{
								return false;
							}
							ret = true;
							break;
						case "z":
							if (!WrapperUnityVersion.TryParseFloatGlobal(tags[1], out vec3.z))
							{
								return false;
							}
							ret = true;
							break;
						default:
							return false;
					}
				}
				else
				{
					return false;
				}
			}
			return ret;
		}

		//4文字の識別IDをintに変換
		public static int ToMagicID(char id0, char id1, char id2, char id3)
		{
			return (id3 << 24) + (id2 << 16) + (id1 << 8) + (id0);
		}
		

		// key:value 形式の要素を区切り文字で列挙したテキストから、
		// 各プロパティ名と値のペアを辞書として取得する。
		// ・JSON 構文解析は行わない
		// ・値の型変換やクォート解析は行わない
		// ・最初に見つかった一致のみを返す
		// ・構文エラーがあっても可能な限り走査を継続する
		/// 例1:
		/// text = "x=1,y=2,z=foo"
		/// pairSeparator = ','
		/// keyValueSeparator = '='
		///  - propertyName = "x" → "1"
		///  - propertyName = "y" → "2"
		///  - propertyName = "z" → "foo"
		// 例2: "x:1,y:2,z:\"foo\""
		// pairSeparator=','
		// keyValueSeparator=':'
		//  - propertyName = "x" → "1"
		//  - propertyName = "y" → "2"
		//  - propertyName = "z" → "\"foo\""

		public static bool TryParseKeyValueDictionary(
			string text,
			char pairSeparator,
			char keyValueSeparator,
			out Dictionary<string, string> dict)
		{
			
			dict = new Dictionary<string, string>();

			if (string.IsNullOrEmpty(text))
				return true;
			
			bool result = true;
			int i = 0;
			int length = text.Length;

			while (i < length)
			{
				SkipWhiteSpace();

				int nameStart = i;

				// key 読み取り
				while (i < length && (char.IsLetterOrDigit(text[i]) || text[i] == '_'))
					i++;

				if (i == nameStart)
				{
					i++;
					continue;
				}

				string name = text.Substring(nameStart, i - nameStart);

				SkipWhiteSpace();

				// keyValueSeparator がなければ次へ
				if (i >= length || text[i] != keyValueSeparator)
				{
					SkipToNextPair();
					continue;
				}

				i++; // keyValueSeparator

				SkipWhiteSpace();

				int valueStart = i;

				// value 読み取り
				while (i < length && text[i] != pairSeparator)
					i++;

				string value = text
					.Substring(valueStart, i - valueStart)
					.Trim();

				if (dict.TryGetValue(name, out var value1))
				{
					Debug.LogError(
						$"Duplicate property key detected: '{name}'. " +
						$"Previous value: '{value1}', New value: '{value}'");
					result = false;
				}

				dict[name] = value;

				if (i < length && text[i] == pairSeparator)
					i++;
			}

			return result;

			// ---- local functions ----

			void SkipWhiteSpace()
			{
				while (i < length && char.IsWhiteSpace(text[i]))
					i++;
			}

			void SkipToNextPair()
			{
				while (i < length && text[i] != pairSeparator)
					i++;

				if (i < length)
					i++;
			}
		}
	}
}
