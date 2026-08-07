// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
	public enum SystemText
	{
		Yes,
		No,
		Ok,
		Cancel,
		Back,
		Clear,
		Retry,
		History,
		Save,
		Load,
		On,
		Off,
		None,
		Sound,
		Review,
		Restore,
		QuitGame,
		DebugInfo,
		DebugLog,
		DebugMenu,
		DeleteSaveAndQuit,
		DeleteAllSaveDataFilesTitle,
		DeleteAllSaveDataFilesMessage,
		DeleteAllCacheFilesTitle,
		DeleteAllCacheFilesMessage,
		DeleteAllOutputFilesTitle,
		DeleteAllOutputFilesMessage,
		Downloading,
		DownloadCount,
		DownloadFinished,
		WarningNotOnline,
		WarningNotWifi,
		ChangeCurrentProject,
		VersionUpScene,
		VersionUpScenario,
		Language,
		UtageGuideMessageQuickSave,
		UtageGuideMessageQuickLoad,
		UtageGuideMessageCgGalleryNotOpened,
		UtageGuideMessageSceneGalleryNotOpened,
		UtageGuideMessageLoadFailedNotSaved,
		UtageGuideMessageSaveFailedAutoSave,
		UtageDialogMessageSaveConfirm,
		UtageDialogMessageBackTitleConfirm,
		UtageDialogMessageResetConfigConfirm,
	};

	/// ゲーム実行中にUI表示するシステムテキスト
	public static class LanguageSystemText
	{
		/// <summary>
		/// データ名
		/// </summary>
		const string LanguageDataName = "SystemText";

		/// <summary>
		/// 指定のキーのテキストを、設定された言語に翻訳して取得
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string LocalizeText(SystemText type)
		{
			LanguageManagerBase language = LanguageManagerBase.Instance;
			if (language != null)
			{
				return language.LocalizeText(LanguageDataName, type.ToString());
			}
			else
			{
				Debug.LogWarning("LanguageManager is NULL");
				return type.ToString();
			}
		}

	}
}

