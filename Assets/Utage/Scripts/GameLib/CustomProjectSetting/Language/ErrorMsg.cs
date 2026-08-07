// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
	public enum ErrorMsg
	{
		NotFound,
		UnknownType,
		UnknownVersion,
		ColorParseError,
		SpriteMimMap,
		UnknownFontData,
		StringGridRowPraseColumnName,
		StringGridRowPraseColumnIndex,
		StringGridRowPrase,
		StringGridParseHaeder,
		StringGridGetColumnIndex,
		SoundNotReadyToPlay,
		TweenWrite,
		FileWrite,
		FileRead,
		MemoryLeak,
		FileReferecedIsNull,
		FileIsNotReady,
		DisableChangeFileLoadFlag,
		DisableChangeFileVersion,
		SingletonError,
		NoChacheTypeFile,
		ExpUnknownParameter,
		ExpResultNotBool,
		ExpIllegal,
		PivotParse,
		TextTagParse,
		TextCallbackCalcExpression,
		TextFailedCalcExpression,
		ExpressionOperateSubstition,
		ExpressionOperator,
	};

	/// <summary>
	/// システムとして使うテキスト
	/// </summary>
	public static class LanguageErrorMsg
	{
		/// <summary>
		/// データ名
		/// </summary>
		const string LanguageDataName = "ErrorMsg";

		/// <summary>
		/// 指定のキーのテキストを、設定された言語に翻訳して取得
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string LocalizeText(ErrorMsg type)
		{
			LanguageManagerBase language = LanguageManagerBase.Instance;
			if (language == null)
			{
				Debug.LogWarning("LanguageManager is NULL");
				return type.ToString();
			}
			else
			{
				if (language.TryLocalizeText(type.ToString(), out string text))
				{
					return text;
				}
				else
				{
					return language.DefaultLanuguageText(type.ToString());
				}
			}
		}

		/// <summary>
		/// 指定のキーのテキストフォーマットを、設定された言語に翻訳して取得
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string LocalizeTextFormat(ErrorMsg type, params object[] args )
		{
			string format = LocalizeText(type);
			return string.Format(format, args);
		}

	}
}
