// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Utage;

namespace UtageExtensions
{
	//拡張メソッド（stringに対して）
	public static class UtageExtensionsString
	{
		//カンマやスラッシュなど、区切り文字の前後で文字列を分割する（区切り文字が複数ある場合は最初か最後で分割）
		public static void Separate(this string str, char separator, bool isFirst, out string str1, out string str2)
		{
			int index = isFirst ? str.IndexOf(separator) : str.LastIndexOf(separator);
			str1 = str.Substring(0, index);
			str2 = str.Substring(index + 1);
		}

		public static bool IsNullOrEmpty(this string str)
		{
			return string.IsNullOrEmpty(str);
		}

		//サロゲートペアを考慮したstringのLengthを取得
		public static int LengthWithSurrogatePairs(this string str)
		{
			return FontUtil.LengthWithSurrogatePairs(str);
		}

	}
}
