// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.VersionControl;
using UnityEngine.UI;
using QuickImportType = Utage.UtageEditorUserSettings.ImportSettings.QuickImportType;
namespace Utage
{

	//「Utage」のシナリオデータ用のインポーター
	public class AdvScenarioImporterInEditor 
	{
		public const string BookAssetExt = ".book.asset";
		public const string ChapterAssetExt = ".chapter.asset";
		public const string ScenarioAssetExt = ".asset";

		public AdvScenarioDataProject Project { get; }
		bool AllImport { get; set; }
		List<string> importedAssets = null;

		public List<AdvChapterData> OldChapters { get; } = new();
		//シナリオデータ
		public Dictionary<string, AdvScenarioData> ScenarioDataTbl { get; } = new();
		private AdvMacroManager MacroManager { get; set; }
		AdvImportScenarios ScenariosAsset { get; set; }
		AdvScenarioFileReaderManager ScenarioFileReader { get; set; }
		
		//テキストのバリデートを無効にするか
		public bool DisableTextValidate { get; set; }

		public AdvScenarioImporterInEditor(AdvScenarioDataProject project)
		{
			Project = project;
			ScenarioFileReader = Project.CreateScenarioReader();
		}

		public void ImportAll()
		{
			this.AllImport = true;
			ImportSub();
		}
		public void Import(List<string> targetFiles)
		{
			this.AllImport = false;
			this.importedAssets = targetFiles;
			ImportSub();
		}

		//ファイルの読み込み
		void ImportSub()
		{
			if (Project == null)
			{
				Debug.LogError($"Project is null");
				return;
			}

			if (Project.CustomProjectSetting == null)
			{
				Debug.LogError($"Project.CustomProjectSetting is null",Project);
				return;
			}
			if (Project.ChapterDataList.Count <= 0)
			{
				Debug.LogError("ChapterDataList is zeo",Project);
				return;
			}

			OnPreImport();

			UnityEngine.Profiling.Profiler.BeginSample("Import Scenarios");
			ImportScenarios();
			UnityEngine.Profiling.Profiler.EndSample();

			OnPostImport();
		}

		//インポートの前処理
		void OnPreImport()
		{
			CustomProjectSetting.Instance = Project.CustomProjectSetting;
			var languageManager = LanguageManagerBase.Instance;
			if (languageManager != null)
			{
				languageManager.ForceInit();
			}
			else
			{
				Debug.LogWarning("LanguageManagerBase is null");
			}

			Debug.Log( $"<b>Import {Project.ProjectName} Project</b> \n{AssetDatabase.GetAssetPath(Project)}", Project);

			AssetFileManager.IsEditorErrorCheck = true;
			AssetFileManager.ClearCheckErrorInEditor();
			AdvCommand.IsEditorErrorCheck = true;
			AdvCommand.IsEditorErrorCheckWaitType = UtageEditorProjectSettings.GetInstance().ImportSetting.CheckWaitType;
			this.ScenarioDataTbl.Clear();
			this.MacroManager = new AdvMacroManager();
			this.ScenariosAsset = Project.GetScenariosOrCreateIfMissing();
			OldChapters.Clear();
			OldChapters.AddRange(ScenariosAsset.Chapters);
			this.ScenariosAsset.ClearOnImport();
		}

		void ImportScenarios()
		{
			AdvEngine engine = WrapperFindObject.FindObjectOfTypeIncludeInactive<AdvEngine>();
			if (engine != null)
			{
				engine.BootInitCustomCommand();
			}
			//チャプターデータのインポート
			for (int i = 0; i < Project.ChapterDataList.Count; i++)
			{
				var chapter =Project.ChapterDataList[i];
				ImportChapter(chapter, i);
			}

			//ファイルが存在しているかチェック
			if (Project.ResourceDir != null)
			{
				string path = new MainAssetInfo(Project.ResourceDir).FullPath;
				AssetFileManager.CheckErrorInEditor(path, Project.CheckExt);
			}
		}
		
		//インポートの後処理
		void OnPostImport()
		{
			EditorUtility.SetDirty(this.ScenariosAsset);
			AssetDatabase.Refresh();
			AdvCommand.IsEditorErrorCheck = false;
			AdvCommand.IsEditorErrorCheckWaitType = false;
			AssetFileManager.IsEditorErrorCheck = false;
		}

		void ImportChapter(AdvScenarioDataProject.ChapterData chapterData, int index)
		{
			List<string> pathList = chapterData.FilePathList.Where(ScenarioFileReader.IsTargetTypeFile).ToList();
			if (pathList.Count <= 0) return;

			List<AdvImportBook> bookAssetList = new List<AdvImportBook>();

			bool reimport = false;
			//エクセルファイルのアセットを取得
			foreach (var path in pathList)
			{
				if (string.IsNullOrEmpty(path)) continue;

				AdvImportBook bookAsset;
				//再インポートが必要なアセットを取得
				//失敗する→再インポートが必要なし
				if (CheckReimport(path, out bookAsset))
				{
					Debug.Log("Reimport " + path);
					//対象のファイルを読み込み
					StringGridDictionary book = ReadFile(path);
					if (book ==null || book.List.Count <= 0)
					{
						//中身がない
						continue;
					}
					reimport = true;
					//末尾の空白文字をチェック
					if (UtageEditorProjectSettings.GetInstance().ImportSetting.CheckWhiteSpaceEndOfCell) CheckWhiteSpaceEndOfCell(book);
					bookAsset.Clear();
					bookAsset.AddSourceBook(book);
				}
				bookAssetList.Add(bookAsset);
			}
			//インポート処理をする
			if (IsImportTargetChapter(reimport, index))
			{
				ImportChapter(chapterData.chapterName, bookAssetList);
				//変更を反映
				foreach (var asset in bookAssetList)
				{
					Debug.Log(LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.Import, asset.name),asset);
					EditorUtility.SetDirty(asset);
				}
			}
		}
		
		//インポート対象のチャプターかチェック
		bool IsImportTargetChapter(bool reimport, int index)
		{
			switch (UtageEditorUserSettings.GetInstance().ImportSetting.QuickImport)
			{
				case QuickImportType.QuickChapter:
					return reimport;
				case QuickImportType.QuickChapterWithZeroChapter:
					if (index == 0)
					{
						return true;
					}
					return reimport;
				case QuickImportType.None:
				case QuickImportType.Quick:
				default:
					return true;
			}
		}

		//対象のファイルを読み込み
		StringGridDictionary ReadFile(string path)
		{
			
			if( !ScenarioFileReader.TryReadFile(path, out StringGridDictionary book))
			{
				return null;
			}

			var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
			book.SetSourceAsset(asset);
			var editorProjectSettings = UtageEditorProjectSettings.GetInstance().ImportSetting;
			foreach (var scenarioFileReadPostprocessor in editorProjectSettings.ScenarioFileReadPostprocessors)
			{
				scenarioFileReadPostprocessor.OnScenarioFileReadPostprocess(this,path,book);
			}


			//コメントアウト処理
			if (Project.EnableCommentOutOnImport)
			{
				book.EraseCommentOutStrings(@"//");
			}
			int checkCount = editorProjectSettings.CheckBlankRowCount; 
			int checkCellCount = editorProjectSettings.CheckCellCount; 
			foreach (var sheet in book.Values)
			{
				var grid = sheet.Grid;
				
				//末尾の空白行数多すぎないかチェック
				grid.ShapeUpRows(checkCount);
				
				//列数が多すぎないかチェック
				bool isOverCell = false;
				foreach (var row in grid.Rows)
				{
					if (row.Length >= checkCellCount)
					{
						isOverCell = true;
						break;
					}
				}
				if (isOverCell)
				{
					Debug.LogWarningFormat( "Column count is over {0}. {1}", checkCellCount, grid.Name  );
				}
				
			}
			return book;
		}


		//再インポートが必要なアセットを取得
		bool CheckReimport(string path, out AdvImportBook bookAsset)
		{
			//シナリオデータ用のスクリプタブルオブジェクトを宣言
			string bookAssetPath = Path.ChangeExtension(path, BookAssetExt);
			bookAsset = AssetDatabase.LoadAssetAtPath<AdvImportBook>(bookAssetPath);
			if (bookAsset == null)
			{
				//まだないので作る
				bookAsset = ScriptableObject.CreateInstance<AdvImportBook>();
				AssetDatabase.CreateAsset(bookAsset, bookAssetPath);
				bookAsset.hideFlags = HideFlags.NotEditable;
				return true;
			}
			else
			{
				return CheckReimportFromPath(path);
			}
		}

		//再インポートが必要かパスからチェック
		bool CheckReimportFromPath(string path)
		{
			if (AllImport) return true;
			return importedAssets.Contains(path);
		}

		//末尾の空白文字をチェック
		private void CheckWhiteSpaceEndOfCell(StringGridDictionary book)
		{
			UtageEditorUserSettings setting = UtageEditorUserSettings.GetInstance();
			if (setting == null) return;
			if (!setting.ImportSetting.CheckWhiteSpace) return;

			List<string> ignoreHeader = new List<string>();
			ignoreHeader.Add("Text");
			if (LanguageManagerBase.Instance != null)
			{
				foreach (string language in LanguageManagerBase.Instance.Languages)
				{
					ignoreHeader.Add(language);
				}
			}

			foreach (var sheet in book.Values)
			{
				List<int> ignoreIndex = new List<int>();
				foreach (var item in ignoreHeader)
				{
					if (sheet.Grid.TryGetColumnIndex(item, out int index))
					{
						ignoreIndex.Add(index);
					}
				}
				foreach (var row in sheet.Grid.Rows)
				{
					if (row.RowIndex == 0) continue;

					for (int i = 0; i < row.Strings.Length; ++i)
					{
						string str = row.Strings[i];
						if (str.Length <= 0) continue;
						if (ignoreIndex.Contains(i)) continue;

						int endIndex = str.Length - 1;
						if (char.IsWhiteSpace(str[endIndex]))
						{
							Debug.LogWarning(row.ToErrorString("Last character is white space [" + ColorUtil.ToColorTagErrorMsg(str) + "]  \n"));
						}
					}
				}
			}
		}

		//マクロ処理したインポートデータを作成する
		// ReSharper disable Unity.PerformanceAnalysis
		void ImportChapter(string chapterName, List<AdvImportBook> books)
		{
			//チャプターデータを作成し、各シナリオを設定
			AdvChapterData chapter = LoadOrCreateChapterAsset(chapterName);
			chapter.SetChapterName(chapterName);
			this.ScenariosAsset.AddChapter(chapter);

			//初期化
			chapter.ImportBooks(books, this.MacroManager);

			//設定データの解析とインポート
			AdvSettingDataManager setting = new AdvSettingDataManager();
			setting.ImportedScenarios = this.ScenariosAsset;
			setting.BootInit("");
			chapter.MakeScenarioImportData(setting, this.MacroManager);
			EditorUtility.SetDirty(chapter);
			AdvGraphicInfo.CallbackExpression = setting.DefaultParam.CalcExpressionBoolean;
			TextParser.CallbackCalcExpression = setting.DefaultParam.CalcExpressionNotSetParam;
			iTweenData.CallbackGetValue = setting.DefaultParam.GetParameter;

			List<AdvScenarioData> scenarioList = new List<AdvScenarioData>();
			foreach (var book in books)
			{
				foreach (var grid in book.ImportGridList)
				{
					grid.InitLink();
					string sheetName = grid.SheetName;
					if (!AdvSheetParser.IsScenarioSheet(sheetName)) continue;
					if (ScenarioDataTbl.ContainsKey(sheetName))
					{
						Debug.LogError(sheetName + " is already contains in the sheets");
					}
					else
					{
						AdvScenarioData scenario = new AdvScenarioData(grid);
						ScenarioDataTbl.Add(sheetName, scenario);
						scenarioList.Add(scenario);
					}
				}
			}

			//シナリオデータとして解析、初期化
			foreach (AdvScenarioData data in scenarioList)
			{
				data.Init(setting);
				data.DelayInitializeAllCommand(setting);
			}


			AdvGraphicInfo.CallbackExpression = null;
			TextParser.CallbackCalcExpression = null;
			iTweenData.CallbackGetValue = null;

			//シナリオラベルのリンクチェック
			ErrorCheckScenarioLabel(scenarioList);

			if (EnableTextValidate())
			{
				var editorUserImportSettings = UtageEditorUserSettings.GetInstance().ImportSetting;
				//テキストの検証
				editorUserImportSettings.TextValidator.CreateValidator().Validate(scenarioList);
			}

			bool EnableTextValidate()
			{
				if( DisableTextValidate ) return false;

				var advEngineStarter = WrapperFindObject.FindObjectOfTypeIncludeInactive<AdvEngineStarter>();
				if (advEngineStarter == null) return false;
				if (advEngineStarter.ScenarioProject != this.Project) return false;
				var engine = WrapperFindObject.FindObjectOfTypeIncludeInactive<AdvEngine>();
				if (engine == null) return false;
				return true;
			}
		}


		//チャプターデータのアセット取得
		AdvChapterData LoadOrCreateChapterAsset( string chapterName )
		{
			AdvChapterData asset = this.OldChapters.Find(x=>x.name == chapterName);
			if (asset != null) return asset;
			string path = AssetDatabase.GetAssetPath(this.ScenariosAsset);
			path = FilePathUtil.Combine(FilePathUtil.GetDirectoryPath(path), chapterName);

			string assetPath = Path.ChangeExtension(path, ChapterAssetExt);
			asset = AssetDatabase.LoadAssetAtPath<AdvChapterData>(assetPath);
			if (asset == null)
			{
				//まだないので作る
				asset = ScriptableObject.CreateInstance<AdvChapterData>();
				AssetDatabase.CreateAsset(asset, assetPath);
				asset.hideFlags = HideFlags.NotEditable;
			}
			return asset;
		}

		/// <summary>
		/// シナリオラベルのリンクチェック
		/// </summary>
		/// <param name="label">シナリオラベル</param>
		/// <returns>あればtrue。なければfalse</returns>
		void ErrorCheckScenarioLabel(List<AdvScenarioData> scenarioList)
		{
			//リンク先のシナリオラベルがあるかチェック
			foreach (AdvScenarioData data in scenarioList)
			{
				foreach (AdvScenarioJumpData jumpData in data.JumpDataList)
				{
					if (!IsExistScenarioLabel(jumpData.ToLabel))
					{
						Debug.LogError( 
							jumpData.FromRow.ToErrorString( 
							LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.NotLinkedScenarioLabel, jumpData.ToLabel, "")
							));
					}
				}
			}

			//シナリオラベルが重複しているかチェック
			foreach (AdvScenarioData data in scenarioList)
			{
				foreach (var keyValue in data.ScenarioLabels)
				{
					AdvScenarioLabelData labelData = keyValue.Value;
					if (IsExistScenarioLabel(labelData.ScenarioLabel, data))
					{
						string error = labelData.ToErrorString(LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.RedefinitionScenarioLabel, labelData.ScenarioLabel,""), data.DataGridName );
						Debug.LogError(error);
					}
				}
			}
		}


		/// <summary>
		/// シナリオラベルがあるかチェック
		/// </summary>
		/// <param name="label">シナリオラベル</param>
		/// <param name="egnoreData">チェックを無視するデータ</param>
		/// <returns>あればtrue。なければfalse</returns>
		bool IsExistScenarioLabel(string label, AdvScenarioData egnoreData = null )
		{
			foreach (AdvScenarioData data in ScenarioDataTbl.Values)
			{
				if (data == egnoreData) continue;
				if (data.IsContainsScenarioLabel(label))
				{
					return true;
				}
			}
			return false;
		}
	}

	[Obsolete("Use AdvScenarioImporterInEditor or AdvScenarioDataProject.ImportAll")]
	public class AdvExcelImporter
	{
	}
}
#endif
