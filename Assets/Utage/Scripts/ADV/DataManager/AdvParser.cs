// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// ADVデータ解析
	/// </summary>
	public class AdvParser
	{
		public static string Localize(AdvColumnName name)
		{
			//多言語化をしてみたけど、複雑になってかえって使いづらそうなのでやめた
			return name.QuickToString();
		}

		//指定の名前のセルを、型Tとして解析・取得（データがなかったらエラーメッセージを出す）
		public static T ParseCell<T>(StringGridRow row, AdvColumnName name)
		{
			return row.ParseCell<T>(Localize(name));
		}

		//指定の名前のセルを、型Tとして解析・取得（データがなかったらデフォルト値を返す）
		public static T ParseCellOptional<T>(StringGridRow row, AdvColumnName name, T defaultVal)
		{
			return row.ParseCellOptional<T>(Localize(name), defaultVal);
		}

		//指定の名前のセルを、型Tとして解析・取得（データがなかったらfalse）
		public static bool TryParseCell<T>(StringGridRow row, AdvColumnName name, out T val)
		{
			return row.TryParseCell<T>(Localize(name), out val);
		}

		//セルが空かどうか
		public static bool IsEmptyCell(StringGridRow row, AdvColumnName name)
		{
			return row.IsEmptyCell(Localize(name));
		}

		//ローカライズも含めてテキスト系コマンドデータが空かどうか
		public static bool IsEmptyTextCommand(StringGridRow row)
		{
			if (!IsEmptyCell(row, AdvColumnName.PageCtrl) || !IsEmptyCell(row, AdvColumnName.Text))
			{
				return false;
			}
			LanguageManagerBase languageManager = LanguageManagerBase.Instance;  
			if (languageManager == null) return true;
			return languageManager.IsEmptyTextCommand(row);
		}

		
		//現在の設定言語にローカライズされたテキストを取得
		public static string ParseCellLocalizedText(StringGridRow row, AdvColumnName defaultColumnName)
		{
			return ParseCellLocalizedText(row, defaultColumnName.QuickToString());
		}

		//現在の設定言語にローカライズされたテキストを取得
		public static string ParseCellLocalizedText(StringGridRow row, string defaultColumnName)
		{
			LanguageManagerBase languageManager = LanguageManagerBase.Instance;  
			if (languageManager == null) return row.ParseCellOptional<string>(defaultColumnName, "");

			return  languageManager.ParseCellLocalizedText(row, defaultColumnName);
		}

		//現在の設定言語にローカライズされたテキストを取得
		//指定の文字列を接頭辞にしたセル名からテキストを取得する
		public static string ParseCellSuffixedLanguageNameToLocalizedText(StringGridRow row, string cellName)
		{
			LanguageManagerBase languageManager = LanguageManagerBase.Instance;
			if (languageManager == null) return row.ParseCell<string>(cellName);
			
			return languageManager.ParseCellSuffixedLanguageNameToLocalizedText(row, cellName);
		}
		public static string ParseCellSuffixedLanguageNameToLocalizedTextOptional(StringGridRow row, string cellName, string defaultVal)
		{
			LanguageManagerBase languageManager = LanguageManagerBase.Instance;
			if (languageManager == null) return row.ParseCellOptional<string>(cellName, defaultVal);

			return languageManager.ParseCellSuffixedLanguageNameToLocalizedTextOptional(row, cellName, defaultVal);
		}
	}
}
