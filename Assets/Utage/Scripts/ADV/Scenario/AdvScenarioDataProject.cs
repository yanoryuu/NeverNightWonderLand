// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#pragma warning disable 414
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Utage
{

	//「Utage」のシナリオデータ用のプロジェクトデータ
	[CreateAssetMenu(menuName = "Utage/Scenario/ScenarioDataProject")]
	public class AdvScenarioDataProject
		: ScriptableObject
			, IScenarioImportSettings
			, IAdvScenarioDataProject
	{

		//章別に分けたエクセルのリスト
		[System.Serializable]
		public class ChapterData
		{
			public string chapterName = "";

			[FormerlySerializedAs("excelDir"), Folder]
			public Object scenarioDir;

			public List<Object> excelList = new List<Object>();

			public List<string> FilePathList
			{
				get
				{
					List<string> list = new();
					if (excelList.Count > 0)
					{
//						Debug.LogWarning("ExcelList is obsolete. Use ScenarioDir instead.");
						list.AddRange(UtageEditorToolKit.AssetsToPathList(excelList));
					}

					//指定ディレクトリ以下のアセットを全て取得
					if (scenarioDir != null)
					{
						MainAssetInfo inputDirAsset = new MainAssetInfo(scenarioDir);
						foreach (MainAssetInfo asset in inputDirAsset.GetAllChildren())
						{
							if (asset.IsDirectory) continue;
							string path = asset.AssetPath;
							if (list.Contains(path)) continue;
							list.Add(path);
						}
					}
					
					//重複を削除して返す
					return list.Distinct().ToList();
				}
			}
		}

		[SerializeField] List<ChapterData> chapterDataList = new List<ChapterData>();

		//章分けしたデータを取得
		public List<ChapterData> ChapterDataList
		{
			get { return chapterDataList; }
		}

		//インポート対象のシナリオアセット
		public AdvImportScenarios Scenarios
		{
			get { return scenarios; }
		}

		[SerializeField] AdvImportScenarios scenarios;


		/// データ作成の段階でコメントアウトを有効にするか
		public bool EnableCommentOutOnImport => enableCommentOutOnImport;

		[SerializeField] bool enableCommentOutOnImport = true;

		//シナリオファイルのリーダー
		List<ScenarioFileReaderSettings> ScenarioFileReaderSettings => scenarioFileReaderSettings;
		[SerializeField] List<ScenarioFileReaderSettings> scenarioFileReaderSettings = new();

		
		// リソースのあるルートディレクトリ（インポート時のエラーチェックに使用）
		public Object ResourceDir
		{
			get { return resourceDir; }
			internal set { resourceDir = value; }
		}

		[FormerlySerializedAs("recourceDir")] [SerializeField, Folder]
		Object resourceDir = null;

		public bool CheckExt
		{
			get { return checkExt; }
			internal set { checkExt = value; }
		}

		[SerializeField] bool checkExt = false;

		public CustomProjectSetting CustomProjectSetting
		{
			get { return customProjectSetting; }
			set { customProjectSetting = value; }
		}

		[SerializeField] CustomProjectSetting customProjectSetting = null;


		/// <summary>
		/// 宴用のカスタムインポート設定を強制するSpriteフォルダassetのリスト
		/// </summary>
		[FormerlySerializedAs("customInportSpriteFolders")] [SerializeField]
		List<Object> customImportSpriteFolders = new List<Object>();

		public List<Object> CustomImportSpriteFolders
		{
			get { return customImportSpriteFolders; }
		}

		/// <summary>
		/// 宴用のカスタムインポート設定を強制するAudioフォルダassetのリスト
		/// </summary>
		[FormerlySerializedAs("customInportAudioFolders")] [SerializeField]
		List<Object> customImportAudioFolders = new List<Object>();

		public List<Object> CustomImportAudioFolders
		{
			get { return customImportAudioFolders; }
		}


		public string ProjectName
		{
			get { return FilePathUtil.GetFileNameWithoutDoubleExtension(this.name); }
		}


		public void ImportAll()
		{
			AdvScenarioImporterInEditor importer = new(this);
			importer.ImportAll();
		}

		public AdvImportScenarios GetScenariosOrCreateIfMissing()
		{
			CreateScenariosIfMissing();
			return Scenarios;
		}

		public void CreateScenariosIfMissing()
		{
			if (this.scenarios != null) return;

			string path = AssetDatabase.GetAssetPath(this);
			path = FilePathUtil.Combine(FilePathUtil.GetDirectoryPath(path), ProjectName + ".scenarios.asset");
			//設定データのアセットをロードまたは作成
			this.scenarios = UtageEditorToolKit.GetImportedAssetCreateIfMissing<AdvImportScenarios>(path);
			this.scenarios.hideFlags = HideFlags.NotEditable;
			EditorUtility.SetDirty(this);
		}

		public bool IsEnableImport => this.GetAllScenarioFiles().Any(path => null != path);

		/// <summary>
		/// 宴用のカスタムインポート設定を強制するAudioアセットかチェック
		/// </summary>
		public bool IsCustomImportAudio(AssetImporter importer)
		{
			string assetPath = importer.assetPath;
			foreach (Object folderAsset in CustomImportAudioFolders)
			{
				if (assetPath.StartsWith(AssetDatabase.GetAssetPath(folderAsset)))
				{
					return true;
				}
			}

			return false;
		}

		public enum TextureType
		{
			Unknown,
			Character,
			Sprite,
			BG,
			Event,
			Thumbnail,
		};

		/// <summary>
		/// 宴用のカスタムインポート設定を強制するSpriteアセットかチェック
		/// </summary>
		public TextureType ParseCustomImportTextureType(AssetImporter importer)
		{
			string assetPath = importer.assetPath;
			foreach (Object folderAsset in CustomImportSpriteFolders)
			{
				string folderPath = AssetDatabase.GetAssetPath(folderAsset);
				if (assetPath.StartsWith(folderPath))
				{
					string fileName = System.IO.Path.GetFileName(folderPath);
					TextureType type;
					if (ParserUtil.TryParaseEnum<TextureType>(fileName, out type))
					{
						return type;
					}

					return TextureType.Unknown;
				}
			}

			return TextureType.Unknown;
		}

		/// <summary>
		/// 管理対象のリソースを再インポート
		/// </summary>
		public void ReImportResources()
		{
			ImportAssetOptions options = ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive;
			foreach (Object folder in CustomImportSpriteFolders)
			{
				AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(folder), options);
			}

			foreach (Object folder in CustomImportAudioFolders)
			{
				AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(folder), options);
			}

			AssetDatabase.Refresh();
		}

		public IEnumerable<string> GetAllScenarioFiles()
		{
			foreach (var chapter in this.ChapterDataList)
			{
				foreach (var filePath in chapter.FilePathList)
				{
					yield return filePath;
				}
			}
		}

		public AdvScenarioFileReaderManager CreateScenarioReader()
		{
			return new AdvScenarioFileReaderManager(this, this.GetAllScenarioFiles().ToList());
		}

		public IEnumerable<IAdvScenarioFileReader> CreateScenarioFileReaders(AdvScenarioFileReaderManager manager)
		{
			bool containsExcel = false;
			bool containsCsv = false;
			foreach (var settings in ScenarioFileReaderSettings)
			{
				if (settings == null) continue;
				switch (settings)
				{
					case IScenarioFileReaderSettingsExcel excel:
						containsExcel = true;
						break;
					case AdvScenarioFileReaderSettingsCsv csv:
						containsCsv = true;
						break;
				}

				yield return settings.CreateReader();
			}

			//互換性のために、ExcelとCsvの設定がない場合は、ExcelとCsvの設定を自動で追加する
			if (!containsExcel)
			{
				var template =
					AssetDataBaseEx.LoadAssetByGuid<ScenarioFileReaderSettings>("066d8603f7b3eb34eb31ed418685f707");
				var excelReader = ScriptableObject.Instantiate(template) as IScenarioFileReaderSettingsExcel;
				if (excelReader == null)
				{
					Debug.LogError("Failed to create excel reader");
				}
				else
				{
					yield return excelReader.CreateReader();
				}
			}

			if (!containsCsv)
			{
				var csvReader = CreateInstance<AdvScenarioFileReaderSettingsCsv>();
				yield return csvReader.CreateReader();
			}
		}

		public bool TryGetImportTarget(string[] importedAssets, out List<string> targetFiles)
		{
			var reader = CreateScenarioReader();
			targetFiles = reader.ToTargetFiles(importedAssets);
			if (targetFiles.Count <= 0)
			{
				//現在のプロジェクトのアセットがないのでインポートしない
				return false;
			}

			return true;
		}


		//以下、レガシーな設定。昔の設定を確認するときに、デバッグ表示で再表示するために残している

		#region Legacy

		enum AutoImportType
		{
			Always,
			OnUtageScene,
			None,
		};

		enum QuickImportType
		{
			None,
			Quick,
			QuickChapter,
			QuickChapterWithZeroChapter,
		}

		[SerializeField, Hide] QuickImportType quickImportType = QuickImportType.None;
		[SerializeField, Hide] bool parseFormula = false;
		[SerializeField, Hide] bool parseNumreic = false;
		[SerializeField, Hide] bool checkWhiteSpace = true;
		[SerializeField, Hide] bool checkWaitType = false;
		[SerializeField, Hide] bool checkWhiteSpaceEndOfCell = true;
		[SerializeField, Hide] bool checkTextCount = true;
		[SerializeField, Hide] bool checkTextCountAllLanguage = false;
		[SerializeField, Hide] AutoImportType autoImportType = AutoImportType.OnUtageScene;
		[SerializeField, Hide] int checkBlankRowCountOnImport = 1000;
		[SerializeField, Hide] int checkColumnCountOnImport = 500;

		#endregion


		[CustomEditor(typeof(AdvScenarioDataProject))]
		public sealed class ScriptableObjectInspector : Editor
		{
			SerializedProperty PropertyCustomProjectSetting { get; set; }
			SerializedProperty PropertyScenarios { get; set; }

			SerializedProperty PropertyChapters { get; set; }
			SerializedProperty PropertyScenarioFileReaderSettings { get; set; }
			SerializedProperty PropertyEnableCommentOutOnImport { get; set; }
			SerializedProperty PropertyResourceDir { get; set; }
			SerializedProperty PropertyCheckExt { get; set; }
			SerializedProperty PropertyCustomImportSpriteFolders { get; set; }
			SerializedProperty PropertyCustomImportAudioFolders { get; set; }
			

			private void OnEnable()
			{
				PropertyScenarios = serializedObject.FindProperty(nameof(scenarios));
				PropertyCustomProjectSetting = serializedObject.FindProperty(nameof(customProjectSetting));

				PropertyChapters = serializedObject.FindProperty(nameof(chapterDataList));
				PropertyScenarioFileReaderSettings = serializedObject.FindProperty(nameof(scenarioFileReaderSettings));
				PropertyEnableCommentOutOnImport = serializedObject.FindProperty(nameof(enableCommentOutOnImport));
				PropertyResourceDir = serializedObject.FindProperty(nameof(resourceDir));
				PropertyCheckExt = serializedObject.FindProperty(nameof(checkExt));
				PropertyCustomImportSpriteFolders = serializedObject.FindProperty(nameof(customImportSpriteFolders));
				PropertyCustomImportAudioFolders = serializedObject.FindProperty(nameof(customImportAudioFolders));
			}

			public override void OnInspectorGUI()
			{
				var project = (target as AdvScenarioDataProject);

				serializedObject.Update();
				EditorGUILayout.PropertyField(PropertyScenarios);
				EditorGUILayout.PropertyField(PropertyCustomProjectSetting);
				
				

				/*********************************************************************/
				using (new EditorGuiLayoutGroupScope("Import Scenario Files"))
				{
					EditorGUILayout.PropertyField(PropertyChapters, new GUIContent("Chapters"));

					GUILayout.Space(8f);

					EditorGUI.BeginDisabledGroup(!project.IsEnableImport);
					EditorGUILayout.PropertyField(PropertyScenarioFileReaderSettings);
					GUILayout.Space(8f);


					using (new EditorGuiLayoutGroupScope("Import Settings"))
					{
						EditorGUILayout.PropertyField(PropertyEnableCommentOutOnImport, new GUIContent("Comment Out On Import"));
					}

					GUILayout.Space(8f);


					using (new EditorGuiLayoutGroupScope("File Path Check On Import"))
					{
						EditorGUILayout.PropertyField(PropertyResourceDir);
						EditorGUILayout.PropertyField(PropertyCheckExt);
					}

					GUILayout.Space(8f);

					using (new EditorGuiLayoutGroupScope("Editor Import Settings"))
					{
						if (GUILayout.Button("Editor Project Settings", GUILayout.Width(200)))
						{
							UtageEditorProjectSettingsWindow.ShowWindow();
						}

						if (GUILayout.Button("Editor User Settings", GUILayout.Width(200)))
						{
							UtageEditorUserSettingsWindow.ShowWindow();
						}
					}

					GUILayout.Space(8f);

					GUILayout.Space(8f);
					if (GUILayout.Button("Import", GUILayout.Width(180)))
					{
						project.ImportAll();
					}

					EditorGUI.EndDisabledGroup();
				}


				/*********************************************************************/
				using (new EditorGuiLayoutGroupScope("Custom Import Folders"))
				{
					EditorGUILayout.PropertyField(PropertyCustomImportSpriteFolders,
						new GUIContent("Sprite Folder List"));
					EditorGUILayout.PropertyField(PropertyCustomImportAudioFolders,
						new GUIContent("Audio Folder List"));

					bool isEnableResources = project.CustomImportAudioFolders.Count <= 0 &&
					                         project.CustomImportSpriteFolders.Count <= 0;

					EditorGUI.BeginDisabledGroup(isEnableResources);
					if (GUILayout.Button("ReimportResources", GUILayout.Width(180)))
					{
						project.ReImportResources();
					}

					EditorGUI.EndDisabledGroup();
				}
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
}
#endif
