// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Utage
{
	//宴のトップメニュー　Tools>Utage以下の処理は全てここから呼び出す
	public class MenuTool : ScriptableObject
	{
		public const string MenuToolRoot = "Tools/Utage/";

		const int PriorityAdv = 0;

		/// <summary>
		/// シナリオデータビルダーを開く
		/// </summary>
		[MenuItem(MenuToolRoot + "Scenario Data Builder", priority = PriorityAdv + 1)]
		public static void AdvExcelEditorWindow()
		{
			EditorWindow.GetWindow(typeof(AdvScenarioDataBuilderWindow), false, "Scenario Data");
		}
	
		/// <summary>
		/// リソースコンバーターを開く
		/// </summary>
		[MenuItem(MenuToolRoot + "Resource Converter", priority = PriorityAdv + 2)]
		public static void AdvResourceConverterWindow()
		{
			EditorWindow.GetWindow(typeof(AdvResourcesConverter), false, "Resource Converter");
		}

		/// <summary>
		/// ダイシングコンバーターを開く
		/// </summary>
		[MenuItem(MenuToolRoot + "Dicing Converter", priority = PriorityAdv + 3)]
		public static void AdvDicingConverter()
		{
			EditorWindow.GetWindow(typeof(DicingConverter), false, "Dicing Converter");
		}


		/// <summary>
		/// シナリオビュワーを開く
		/// </summary>
		[MenuItem(MenuToolRoot + "Viewer/Scenario Viewer", priority = PriorityAdv + 10)]
		static void OpenAdvScenarioViewer()
		{
			EditorWindow.GetWindow(typeof(AdvScenarioViewer), false, "Scenario");
		}

		/// <summary>
		/// パラメータービュワーを開く
		/// </summary>
		[MenuItem(MenuToolRoot + "Viewer/Parameter Viewer", priority = PriorityAdv + 11)]
		static void OpenAdvParamViewer()
		{
			EditorWindow.GetWindow(typeof(AdvParamViewer), false, "Parameter");
		}

		/// <summary>
		/// ファイルマネージャービュワーを開く
		/// </summary>
		[MenuItem(MenuToolRoot + "Viewer/File Manager Viewer", priority = PriorityAdv + 12)]
		static void OpenFileViewer()
		{
			EditorWindow.GetWindow(typeof(AdvFileManagerViewer), false, "File Manager");
		}


		//************************出力ファイル************************//

		const int PriorityOutPut = 100;
		/// <summary>
		/// セーブデータフォルダを開く
		/// </summary>
		[MenuItem(MenuToolRoot + "Open Output Folder/SaveData", priority = PriorityOutPut + 0)]
		static void OpenSaveDataFolder()
		{
			OpenFilePanelCreateIfMissing("Open utage save data folder", FileIOManager.SdkPersistentDataPath);
		}

		/// <summary>
		/// キャッシュデータフォルダを開く
		/// </summary>
		[MenuItem(MenuToolRoot + "Open Output Folder/Cache", priority = PriorityOutPut + 1)]
		static void OpenCacheFolder()
		{
			OpenFilePanelCreateIfMissing("Open utage cache folder", FileIOManager.SdkTemporaryCachePath);
		}

		/// <summary>
		/// セーブデータを全て削除
		/// </summary>
		[MenuItem(MenuToolRoot + "Delete Output Files/SaveData", priority = PriorityOutPut+2)]
		static void DeleteSaveDataFiles()
		{
			if (EditorUtility.DisplayDialog(
				LanguageSystemText.LocalizeText(SystemText.DeleteAllSaveDataFilesTitle),
				LanguageSystemText.LocalizeText(SystemText.DeleteAllSaveDataFilesMessage),
				LanguageSystemText.LocalizeText(SystemText.Ok),
				LanguageSystemText.LocalizeText(SystemText.Cancel)
				))
			{
				DeleteFolder(FileIOManager.SdkPersistentDataPath);
			}
		}

		/// <summary>
		/// AssetBundleのキャッシュファイルを削除
		/// </summary>
		[MenuItem(MenuToolRoot + "Delete Output Files/Cache and AssetBundles", priority = PriorityOutPut + 3)]
		static void DeleteCacheFilesAndAssetBundles()
		{
			if (EditorUtility.DisplayDialog(
				LanguageSystemText.LocalizeText(SystemText.DeleteAllCacheFilesTitle),
				LanguageSystemText.LocalizeText(SystemText.DeleteAllCacheFilesMessage),
				LanguageSystemText.LocalizeText(SystemText.Ok),
				LanguageSystemText.LocalizeText(SystemText.Cancel)
				))
			{
				DeleteFolder(FileIOManager.SdkTemporaryCachePath);
				WrapperUnityVersion.CleanCache();
			}
		}

		/// <summary>
		/// 全ファイルを全て削除
		/// </summary>
		[MenuItem(MenuToolRoot + "Delete Output Files/All Files", priority = PriorityOutPut+4)]
		static void DeleteAllFiles()
		{
			if (EditorUtility.DisplayDialog(
				LanguageSystemText.LocalizeText(SystemText.DeleteAllOutputFilesTitle),
				LanguageSystemText.LocalizeText(SystemText.DeleteAllOutputFilesMessage),
				LanguageSystemText.LocalizeText(SystemText.Ok),
				LanguageSystemText.LocalizeText(SystemText.Cancel)
				))
			{
				DeleteSaveDataFiles();
			}
		}



		static void DeleteFolder(string path)
		{
			if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
				Debug.Log("Delete " + path);
			}
			else
			{
				Debug.Log("Not found " + path);
			}
		}

		static void OpenFilePanelCreateIfMissing(string title, string path)
		{
			if (!Directory.Exists(path))
			{
				Directory.CreateDirectory(path);
			}
			EditorUtility.OpenFilePanel(title, path, "");
		}

		//************************ツール等************************//

		const int PriorityTools = 500;
		//プロジェクトのパッケージを全て出力する
		[MenuItem(MenuToolRoot + "Tools/Export Project Package", priority = PriorityTools+1)]
		static void OpenExportProjectPackage()
		{
			string path = EditorUtility.SaveFilePanel("Export Project Package...", "../", "", "unitypackage");
			if (!string.IsNullOrEmpty(path))
			{
				WrapperUnityVersion.ExportPackage("Assets", path,
				ExportPackageOptions.Recurse | ExportPackageOptions.Interactive | ExportPackageOptions.IncludeLibraryAssets);
			}
		}


		//プロジェクトをzip圧縮する
		[MenuItem(MenuToolRoot + "Tools/Zip Project", priority = PriorityTools + 1)]
		public static void OpenZipProject()
		{
			string path = EditorUtility.SaveFilePanel("Zip Project...", "../", Application.productName, "zip");
			if (!string.IsNullOrEmpty(path))
			{
				new ProjectZipper().ZipProject(path);
			}
		}


		/// フォントを変更
		[MenuItem(MenuToolRoot + "Tools/FontChanger(Legacy)", priority = PriorityTools + 2)]
		static void OpenFontChanger()
		{
			EditorWindow.GetWindow(typeof(FontChangerWindowLegacy), false, "Font Changer (Legacy)");
		}

		/// フォントを変更(TextMeshPro版
		[MenuItem(MenuToolRoot + "Tools/FontChanger(TextMeshPro)", priority = PriorityTools + 2)]
		static void OpenFontChangerTMP()
		{
			EditorWindow.GetWindow(typeof(FontChangerWindowTMP), false, "Font Changer (TextMeshPro)");
		}

		/// フォントを名を変更(TextMeshPro版
		[MenuItem(MenuToolRoot + "Tools/FontAsset Rename(TextMeshPro)", priority = PriorityTools + 4)]
		static void OpenTmpFontAssetRename()
		{
			EditorWindow.GetWindow(typeof(TmpFontAssetRenameWindow), false, "FontAsset Rename(TextMeshPro)");
		}

		/// シナリオの使用文字を検証
		[MenuItem(MenuToolRoot + "Tools/Scenario Character Validator", priority = PriorityTools + 4)]
		static void OpenAdvScenarioCharacterValidatorWindow()
		{
			EditorWindow.GetWindow(typeof(AdvScenarioCharacterValidatorWindow), false, "Scenario Character Validator");
		}

		//************************Package************************//

		public const int PriorityPackage = 800;

		/// 拡張パッケージのインポートやアップデート
		[MenuItem(MenuToolRoot + "Extension Package Manager", priority = PriorityPackage + 0)]
		static void OpenExtensionPackageImporter()
		{
			EditorWindow.GetWindow(typeof(ExtensionPackageManagerWindow), false, "Extension Package Manager");
		}

		//************************About************************//

		const int PriorityAbout = 900;

		/// <summary>
		/// 宴の情報を開く
		/// </summary>
		[MenuItem(MenuToolRoot + "About Utage...", priority = PriorityAbout+0)]
		static void OpenAboutUtage()
		{
			EditorWindow.GetWindow(typeof(AboutUtage), false, "About Utage");
		}
	}
}
