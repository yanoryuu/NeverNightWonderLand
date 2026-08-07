// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
	public enum AdvErrorMsg
	{
		Import,
		NotLinkedScenarioLabel,
		RedefinitionScenarioLabel,
		NotFoundScnarioLabel,
		NotSettingSheet,
		UnknownTag,
		SoundType,
		BgLayerIsNotDefined,
		ReadLayerSaveData,
		ElseIf,
		Else,
		EndIf,
		CommandParseNull,
		NotScenarioLabel,
		NotFoundTweenGameObject,
		
		UpdateSceneLabel,
		EndSceneGallery,
		ChacacterCountOnImport,
		Utage4VersionErrorSoundManager,
	};

	/// <summary>
	/// システムとして使うテキスト
	/// </summary>
	public static class LanguageAdvErrorMsg
	{
		/// <summary>
		/// データ名
		/// </summary>
		const string LanguageDataName = "AdvErrorMsg";

		/// <summary>
		/// 指定のキーのテキストを、設定された言語に翻訳して取得
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string LocalizeText(AdvErrorMsg type)
		{
			LanguageManagerBase language = LanguageManagerBase.Instance;
			if (language == null)
			{
				Debug.LogWarning("LanguageManager is NULL");
				return type.ToString();
			}
			else
			{
				string text;
				if (language.TryLocalizeText(type.ToString(), out text))
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
		public static string LocalizeTextFormat(AdvErrorMsg type, params object[] args)
		{
			string format = LocalizeText(type);
			return string.Format(format, args);
		}
	}
}
