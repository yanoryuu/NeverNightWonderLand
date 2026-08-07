using System.Text;
using UnityEngine;

namespace Utage
{
	//UTF8のBOM有無などのエンコーディング処理を吸収するユーティリティ
	public static class EncodingUtil
	{
		//UTF8 BOM付きエンコーディング
		public static readonly Encoding Utf8WithBom = new UTF8Encoding(true);

		//UTF8 BOMなしエンコーディング
		public static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);

		//UTF8エンコーディングを取得する（BOM付きかどうかを指定）
		public static Encoding GetUtf8Encoding(bool withBom)
		{
			return withBom ? Utf8WithBom : Utf8WithoutBom;
		}
		
		//エンコード名とBOMの有無からエンコーディングを取得する
		public static Encoding GetEncoding(string encode, bool withBom)
		{
			if (string.IsNullOrEmpty(encode))
			{
				return GetUtf8Encoding(withBom);
			}

			try
			{
				return Encoding.GetEncoding(encode);
			}
			catch
			{
				Debug.LogError($"Invalid encoding name: {encode}. Falling back to UTF-8 {(withBom ? "with BOM" : "without BOM")}.");
				return GetUtf8Encoding(withBom);
			}
		}

	}
}
