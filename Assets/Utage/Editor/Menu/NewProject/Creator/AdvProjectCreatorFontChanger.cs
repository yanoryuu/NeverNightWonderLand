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
using UtageExtensions;

namespace Utage
{
	//宴のシーンを作る処理
	public abstract class AdvProjectCreatorFontChanger
	{
		protected AdvProjectCreator Creator { get; }
		protected AdvProjectCreatorAssets AssetsCreator { get; }

		protected AdvProjectCreatorFontChanger(AdvProjectCreator creator, AdvProjectCreatorAssets assetsCreator)
		{
			Creator = creator;
			AssetsCreator = assetsCreator;
		}
		//フォントを変更
		public void ChangeFontAsset()
		{
			if (TryChangeFontAsset())
			{
				//アセットのセーブ
				AssetDatabase.SaveAssets();
				//いったんアセットをリフレッシュ
				AssetDatabase.Refresh();
			}
		}

		//フォントアセットを変更
		public abstract bool TryChangeFontAsset();


		//シーン内のフォントを変更
		public void ChangeFontInScene(Scene scene)
		{
			if (TryChangeFontInScene(scene))
			{
				if (!EditorSceneManager.SaveScene(scene))
				{
					//シーンのセーブに失敗
					Debug.LogError($"Failed save scene {scene.name}");
				}
			}
		}


		//シーン内のフォントを変更
		public abstract bool TryChangeFontInScene(Scene scene);
	}

	public abstract class AdvProjectCreatorFontChanger<T>
		: AdvProjectCreatorFontChanger
		where T : class, IAdvProjectCreatorFont
	{
		protected T ProjectSetting => Creator as T;

		protected AdvProjectCreatorFontChanger(AdvProjectCreator creator, AdvProjectCreatorAssets assetsCreator) 
			: base(creator, assetsCreator)
		{
		}
	}

	public class AdvProjectCreatorFontChangerLegacy
		: AdvProjectCreatorFontChanger<IAdvProjectCreatorFontLegacy>
	{
		public AdvProjectCreatorFontChangerLegacy(IAdvProjectCreatorFontLegacy creator, AdvProjectCreatorAssets assetsCreator)
			: base(creator as AdvProjectCreator, assetsCreator)
		{
		}

		//フォントアセットを変更
		//レガシーフォントの場合は、フォントアセットを指定のものに入れ替える
		public override bool TryChangeFontAsset()
		{
			if (!EnableFontChange()) return false;

			var fontChanger = CreateFontChanger();
			fontChanger.ChangeFontUnderDir(Creator.GetProjectRelativeDir());
			return true;
		}

		//シーン内のフォントを変更
		public override bool TryChangeFontInScene(Scene scene)
		{
			if (!EnableFontChange()) return false;

			var fontChanger = CreateFontChanger();
			fontChanger.ChangeFontInScene(scene);
			return true;
		}

		protected virtual bool EnableFontChange()
		{
			if (ProjectSetting.Font == null) return false;
			return ProjectSetting.Font != ProjectSetting.DefaultFont;
		}

		protected virtual FontChanger CreateFontChanger()
		{
			var fontAssetPare = new Dictionary<Object, Object>
			{
				{ ProjectSetting.DefaultFont, ProjectSetting.Font }
			};
			return new FontChanger<Font>(fontAssetPare);
		}
	}

	public class AdvProjectCreatorFontChangerTMP
		: AdvProjectCreatorFontChanger<IAdvProjectCreatorFontTMP>
	{
		AdvProjectTemplateFontSettingsTMP ReplacedFontSettings { get; }
		public AdvProjectCreatorFontChangerTMP(IAdvProjectCreatorFontTMP creator, AdvProjectCreatorAssets assetsCreator) 
			: base(creator as AdvProjectCreator, assetsCreator)
		{
			var fontSettings = ProjectSetting.FontSettings;
			
			if (!assetsCreator.CloneAssetPair.TryGetValue(fontSettings.LanguageManager, out var languageManager))
			{
				//差し替えられたフォント設定が見当たらない
				Debug.LogError($"Replaced {nameof(LanguageManager)} is not found", fontSettings.LanguageManager);
			}

			if (!assetsCreator.CloneAssetPair.TryGetValue(fontSettings.Font, out var font))
			{
				//差し替えられたフォント設定が見当たらない
				Debug.LogError($"Replaced {nameof(FontSettings)} is not found", fontSettings.Font);
			}

			if (!assetsCreator.CloneAssetPair.TryGetValue(fontSettings.FontFallbackEditor, out var fontFallback))
			{
				//差し替えられたフォント設定が見当たらない
				Debug.LogError($"Replaced {nameof(FontFallbackSettingsInEditor)} is not found", fontSettings.FontFallbackEditor);
			}

			if (!assetsCreator.CloneAssetPair.TryGetValue(fontSettings.FontFallbackRuntime, out var fontFallbackRuntime))
			{
				//差し替えられたフォント設定が見当たらない
				Debug.LogError($"Replaced {nameof(FontFallbackSettingsInEditor)} is not found", fontSettings.FontFallbackEditor);
			}
			ReplacedFontSettings = new AdvProjectTemplateFontSettingsTMP(
				(LanguageManagerBase)languageManager,
				(FontSettings)font,
				(FontFallbackSettingsInEditor)fontFallback,
				(FontFallbackSettingsRuntime)fontFallbackRuntime);
		}

		//フォントアセットを変更
		//TextMeshProの場合は、言語フォールバックの変更と、シーン内のUIテキスト言語の入れ替え
		public override bool TryChangeFontAsset()
		{
			foreach ( var keyValuePair in AssetsCreator.CloneAssetPair)
			{
				if(keyValuePair.Value is not TMP_FontAsset fontAsset) continue;
				TMP_FontAsset oldFontAsset = (TMP_FontAsset)keyValuePair.Key;
				string dst = oldFontAsset.name.Replace(Creator.TemplateFolderName, Creator.ProjectName);
				FontAssetEditorUtil.RenameFontSubAssets(fontAsset);
				UnityEditor.EditorUtility.SetDirty(fontAsset);
			}

//			AssetDatabase.Refresh();
//			AssetDatabase.SaveAssets();

//			Debug.Log($"Complete RenameFontAssets {Stopwatch.Elapsed}");

			//ランタイムのフォント変更アセットの、設定されているフォント名を変更
			var languageManager = ReplacedFontSettings.LanguageManager;
			var fontFallbackRuntime = ReplacedFontSettings.FontFallbackRuntime;
			var fontFallback = ReplacedFontSettings.FontFallbackEditor;
			var fontSettings = ReplacedFontSettings.Font;

			//ランタイムフォールバックで設定しているフォント名を入れ替え
			fontFallbackRuntime.ReplaceFontNames(Creator.TemplateFolderName, Creator.ProjectName);
			UnityEditor.EditorUtility.SetDirty(fontFallbackRuntime);

			string language = ProjectSetting.FontLanguage;

			//ユーザーの起動言語と開発言語を指定フォントの言語に固定
			languageManager.Language = language;
			languageManager.DataLanguage = language;
			UnityEditor.EditorUtility.SetDirty(languageManager);

			if (ProjectSetting.FontSettings.Font.DefaultFontLanguage == language)
			{
				//言語による変更なし
				return true;
			}
			

			if (fontFallback.TryChangeLanguage(fontSettings.DefaultFontLanguage, language))
			{
				fontSettings.DefaultFontLanguage = language;
				UnityEditor.EditorUtility.SetDirty(fontSettings);
			}

			//言語が変更された場合（つまり、日本語じゃなかった場合）
			//プレハブ内のテキストをフォント言語に合わせて共通言語（英語）に入れ替える
			var commonLanguage = SystemLanguage.English.ToString();
			foreach (var localize in AssetDataBaseEx.GetAllComponentInPrefabAssets<UguiLocalize>(
				         Creator.GetProjectRelativeDir()))
			{
				ChangeText(localize, commonLanguage);
			}

			return true;
		}

		//シーン内のフォントを変更
		//TextMeshProの場合は、言語フォールバックの変更と、シーン内のUIテキスト言語の入れ替え
		public override bool TryChangeFontInScene(Scene scene)
		{
			string language = ProjectSetting.FontLanguage;
			if (ProjectSetting.FontSettings.Font.DefaultFontLanguage == language)
			{
				//言語による変更なし
				return false;
			}

			//言語が変更された場合
			//シーン内のテキストをフォント言語に合わせてに入れ替える
			foreach (var localize in scene.GetComponentsInScene<UguiLocalize>(true))
			{
				ChangeText(localize, language);
			}

			return true;
		}



		void ChangeText(UguiLocalize localize, string language)
		{
			if (!localize.gameObject.TryGetComponent(out TextMeshProUGUI textMeshProUGUI))
			{
				//UguiLocalizeがTextMeshProUGUIを持っていないのでエラー
				Debug.LogError(
					$"{nameof(UguiLocalize)} has not {nameof(TextMeshProUGUI)} {localize.gameObject.GetHierarchyPath()}",
					localize);
				return;
			}

			ChangeText(textMeshProUGUI, localize, language);
			UnityEditor.EditorUtility.SetDirty(textMeshProUGUI);
		}

		void ChangeText(TextMeshProUGUI textMesh, UguiLocalize localize, string language)
		{
			//指定言語がなかったらデフォルト言語が設定される
			if (!LanguageManagerBase.Instance.TryLocalizeText(localize.Key, language, out string text))
			{
				Debug.LogError(
					$"{nameof(UguiLocalize)} key {localize.Key} has not localizeText {localize.gameObject.GetHierarchyPath()}",
					localize);
				return;
			}
			textMesh.text = text;
		}
	}

}
