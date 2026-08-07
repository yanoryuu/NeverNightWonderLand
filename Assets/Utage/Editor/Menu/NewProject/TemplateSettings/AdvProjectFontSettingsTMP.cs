// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UtageExtensions;

namespace Utage
{
	//宴のテンプレートのプロジェクト設定のうち、TextMeshProのフォント設定
	[System.Serializable]
	public class AdvProjectTemplateFontSettingsTMP
	{
		public LanguageManagerBase LanguageManager => languageManager;
		[SerializeField] private LanguageManagerBase languageManager;

		public FontSettings Font => font;
		[SerializeField]private FontSettings font;

		public FontFallbackSettingsInEditor FontFallbackEditor => fontFallbackEditor;
		[SerializeField] private FontFallbackSettingsInEditor fontFallbackEditor;

		public FontFallbackSettingsRuntime FontFallbackRuntime => fontFallbackRuntime;
		[SerializeField] FontFallbackSettingsRuntime fontFallbackRuntime;

		public AdvProjectTemplateFontSettingsTMP(
			LanguageManagerBase languageManager,
			FontSettings fontSettings,
			FontFallbackSettingsInEditor fontFallbackEditorSettingsInEditor,
			FontFallbackSettingsRuntime fontFallbackSettingsRuntime)
		{
			this.languageManager = languageManager;
			font = fontSettings;
			fontFallbackEditor = fontFallbackEditorSettingsInEditor;
			fontFallbackRuntime = fontFallbackSettingsRuntime;
		}
	}
}
