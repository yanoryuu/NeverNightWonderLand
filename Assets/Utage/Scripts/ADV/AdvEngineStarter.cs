// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

//waring:CS0414を無視
#pragma warning disable 0414

using UnityEngine;
using UtageExtensions;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{
	/// <summary>
	/// ゲーム起動処理のサンプル
	/// </summary>
	[AddComponentMenu("Utage/ADV/AdvEngineStarter")]
	public class AdvEngineStarter : MonoBehaviour
	{
		public enum StrageType
		{
			Local,								//ローカルに組み込んだものをロード
			StreamingAssets,					//StreamingAssetsに置いたものをロード
			Server,								//サーバーに置いたものをロード
			StreamingAssetsAndLocalScenario,    //StreamingAssetsに置いたものをロード。シナリオだけはローカルから
			ServerAndLocalScenario,				//サーバーに置いたものをロード。シナリオだけはローカルから
			LocalAndServerScenario,				//ローカルに組み込んだものをロード。シナリオだけはサーバーから
		};

		//Awake時にロードする
		[SerializeField]
		bool isLoadOnAwake = true;

		/// <summary>開始フレームで自動でADVエンジンを起動する</summary>
		[SerializeField]
		bool isAutomaticPlay = false;

		/// <summary>開始フレームで自動でADVエンジンを起動する</summary>
		[SerializeField]
		string startScenario = "";

		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] AdvEngine engine;

		/// <summary>データの置き場所</summary>
		public StrageType Strage
		{
			get { return strageType; }
			set { strageType = value; }
		}
		[SerializeField]
		StrageType strageType;

		/// <summary>
		/// シナリオ
		/// </summary>
		public AdvImportScenarios Scenarios { get { return scenarios; } set { scenarios = value; } }
		[SerializeField]
		AdvImportScenarios scenarios;

		public string RootResourceDir { get { return rootResourceDir; } set { rootResourceDir = value; } }
		/// <summary>リソースディレクトリのルートパス</summary>
		[SerializeField]
		string rootResourceDir;

		/// <summary>サーバーからDLする場合のパス</summary>
		public string ServerUrl
		{
			get { return serverUrl; }
			set { serverUrl = value; }
		}
		[SerializeField]
		string serverUrl;

		/// <summary>
		/// シナリオ名(空欄の場合は、RootResourceDirと同名のscenarios.assetを使う)
		/// </summary>
		public string ScenariosName { get { return scenariosName; } set { scenariosName = value; } }
		[SerializeField]
		string scenariosName;

		/// <summary>
		/// チャプター別のロード機能を使うか
		/// </summary>
		public bool UseChapter { get { return useChapter; } set { useChapter = value; } }
		[SerializeField]
		bool useChapter;

		/// <summary>
		/// チャプター名の指定
		/// </summary>
		public List<string> ChapterNames { get { return chapterNames; } }
		[SerializeField]
		List<string> chapterNames = new List<string>();

		//もうロードが始まっているか
		public bool IsLoadStart { get; set; }

		//起動時のロードエラーチェック
		public bool IsLoadErrorOnAwake { get; set; }

		void Awake()
		{
			CheckProjectSetting();
			if (isLoadOnAwake)
			{
				StartCoroutine(LoadEngineAsync(
				//onFailed
				() =>
				{
					IsLoadErrorOnAwake = true;
				}
				));
			}
		}


		//シナリオをロード
		public IEnumerator LoadEngineAsync(Action onFailed)
		{
			yield return LoadEngineAsyncSub(false, onFailed);
		}

		//アセットバンドルマニフェストをキャッシュファイルからロードして起動
		public IEnumerator LoadEngineAsyncFromCacheManifest(Action onFailed)
		{
			yield return LoadEngineAsyncSub(true,onFailed);
		}

		IEnumerator LoadEngineAsyncSub(bool loadManifestFromCache, Action onFailed)
		{
			this.IsLoadStart = true;
			bool isFailed = false;

			//ローカルなら必要ない
			if (Strage != StrageType.Local)
			{ 
				//シナリオやリソースのロードのまえに
				//アセットバンドルマニフェストのロードをする
				yield return LoadAssetBundleManifestAsync(
					loadManifestFromCache,
					() =>
					{
						isFailed = true;
					}
					);
			}
			if (isFailed)
			{
				onFailed();
			}
			else
			{
				yield return LoadEngineAsyncSub();
			}
		}

		//エンジンをロード
		IEnumerator LoadEngineAsyncSub()
		{
			//開始ラベルを登録しておく
			if (!string.IsNullOrEmpty(startScenario))
			{
				Engine.StartScenarioLabel = startScenario;
			}

			//ストレージごとに、ロードタイプを設定しておく
			switch (Strage)
			{
				case StrageType.Local:
				case StrageType.LocalAndServerScenario:
					AssetFileManager.InitLoadTypeSetting(AssetFileManagerSettings.LoadType.Local);
					break;
				case StrageType.StreamingAssets:
				case StrageType.StreamingAssetsAndLocalScenario:
					AssetFileManager.InitLoadTypeSetting(AssetFileManagerSettings.LoadType.StreamingAssets);
					break;
				case StrageType.Server:
				case StrageType.ServerAndLocalScenario:
					AssetFileManager.InitLoadTypeSetting(AssetFileManagerSettings.LoadType.Server);
					break;
				default:
					Debug.LogError("Unkonw Strage" + Strage.ToString());
					break;
			}

			//シナリオのロードが必要なものはロードする
			bool needsToLoadScenario = false;
			switch (Strage)
			{
				case StrageType.Local:
				case StrageType.ServerAndLocalScenario:
				case StrageType.StreamingAssetsAndLocalScenario:
					break;
				default:
					needsToLoadScenario = true;
					break;
			}
			if (needsToLoadScenario)
			{
				if (UseChapter)
				{
					//追加シナリオがDLがされないうちにオートセーブされると、未DLの部分のシステムセーブがない状態で上書きされしまうので
					//デフォルトの「AdvEnigne SystemSaveData IsAutoSaveOnQuit」はオフにしてある必要がある。
					if (this.Engine.SystemSaveData.IsAutoSaveOnQuit)
					{
						Debug.LogError(
							"Check Off AdvEnigne SystemSaveData IsAutoSaveOnQuit\n"
							+ "「AdvEnigne SystemSaveData IsAutoSaveOnQuit」のチェックをオフにして起動してください\n"
							+ "チャプター機能を使う場合、追加シナリオをDLする前にシステムセーブデータを上書きされないように、アプリ終了・スリープでのオートセーブを無効にする必要があります");
						//DL中はオートセーブを解除する
						this.Engine.SystemSaveData.IsAutoSaveOnQuit = false;
					}
					yield return LoadChaptersAsync(GetDynamicStrageRoot());
				}
				else
				{
					yield return LoadScenariosAsync(GetDynamicStrageRoot());
				}
			}
			if (this.Scenarios == null)
			{
				Debug.LogError("Scenarios is Blank. Please set .scenarios Asset", this);
				yield break;
			}

			//シナリオとルートパスを指定して、エンジン起動
			//カスタムしてスクリプトを書くときは、最終的にここにくればよい
			switch (Strage)
			{
				case StrageType.Local:
				case StrageType.LocalAndServerScenario:
					Engine.BootFromExportData(this.Scenarios, this.RootResourceDir);
					break;
				default:
					Engine.BootFromExportData(this.Scenarios, GetDynamicStrageRoot() );
					break;
			}

			if (isAutomaticPlay)
			{
				//自動再生
				StartEngine();
			}
		}

		//ルートパスを取得
		string GetDynamicStrageRoot()
		{
			switch (Strage)
			{
				case StrageType.Server:
				case StrageType.ServerAndLocalScenario:
				case StrageType.LocalAndServerScenario:
					return FilePathUtil.Combine(this.ServerUrl, AssetBundleHelper.RuntimeAssetBundleTarget().ToString());
				case StrageType.StreamingAssets:
				case StrageType.StreamingAssetsAndLocalScenario:
					string root = FilePathUtil.Combine(this.RootResourceDir, AssetBundleHelper.RuntimeAssetBundleTarget().ToString());
					return FilePathUtil.ToStreamingAssetsPath(root);
				default:
					Debug.LogError("UnDefine");
					return "";
			}
		}

		//アセットバンドルマニフェストのロード
		IEnumerator LoadAssetBundleManifestAsync(bool fromCache, Action onFailed)
		{
			if (Strage == StrageType.Local)
			{
				//ローカルなら必要ない
				yield break;
			}

			//　マニフェストファイルをロードする
			//　マニフェストファイルは
			//　「ルートパス/プラッフォーム名にある
			if (fromCache)
			{
				yield return AssetFileManager.GetInstance().AssetBundleInfoManager.LoadCacheManifestAsync(
					GetDynamicStrageRoot(),
					AssetBundleHelper.RuntimeAssetBundleTarget().ToString(),
					//onComplete
					() =>
					{
					},
					//onFailed
					() =>
					{
						onFailed();
					}
					);
			}
			else
			{
				yield return AssetFileManager.GetInstance().AssetBundleInfoManager.DownloadManifestAsync(
					GetDynamicStrageRoot(),
					AssetBundleHelper.RuntimeAssetBundleTarget().ToString(),
					//onComplete
					() =>
					{
					},
					//onFailed
					() =>
					{
						onFailed();
					}
					);
			}
		}

		//シナリオをロードする
		IEnumerator LoadScenariosAsync(string rootDir)
		{
			string url = ToScenariosFilePath(rootDir);
			AssetFile file = AssetFileManager.Load(url, this);
			while (!file.IsLoadEnd) yield return null;

			AdvImportScenarios scenarios = file.UnityObject as AdvImportScenarios;
			if (scenarios == null)
			{
				Debug.LogError(url + " is  not scenario file");
				yield break;
			}
			this.Scenarios = scenarios;
		}

		string ToScenariosFilePath(string rootDir)
		{
			string fileName = ScenariosName;
			if (string.IsNullOrEmpty(fileName))
			{
				fileName = RootResourceDir + ".scenarios.asset";
			}
			return FilePathUtil.Combine(rootDir, fileName);
		}

		//章別に分かれたシナリオをロードする
		IEnumerator LoadChaptersAsync(string rootDir)
		{
			//動的に作成
			AdvImportScenarios scenarios = ScriptableObject.CreateInstance<AdvImportScenarios>();
			foreach (string chapterName in ChapterNames)
			{
				string url = FilePathUtil.Combine(rootDir, chapterName) + ".chapter.asset";
				AssetFile file = AssetFileManager.Load(url, this);
				while (!file.IsLoadEnd) yield return null;

				AdvChapterData chapter = file.UnityObject as AdvChapterData;
				if (scenarios == null)
				{
					Debug.LogError(url + " is  not scenario file");
					yield break;
				}
				scenarios.AddChapter(chapter);
			}
			this.Scenarios = scenarios;
		}


		//シナリオ開始
		public void StartEngine()
		{
			StartCoroutine(CoPlayEngine());
		}

		IEnumerator CoPlayEngine()
		{
			//初期化（シナリオのDLなど）を待つ
			while (Engine.IsWaitBootLoading) yield return null;

			if (string.IsNullOrEmpty(startScenario))
			{
				Engine.StartGame();
			}
			else
			{
				Engine.StartGame(startScenario);
			}
		}
#if UNITY_EDITOR
		const int Version = 1;
		[SerializeField, HideInInspector]
		int version = 0;

		/// シナリオデータのプロジェクト設定
		public AdvScenarioDataProject ScenarioProject
		{
			get { return scenarioProject; }
			set { scenarioProject = value; }
		}
		[SerializeField] AdvScenarioDataProject scenarioProject;

		/// シナリオデータのプロジェクト設定（旧設定）
		[SerializeField,Hide()]
		UnityEngine.Object scenarioDataProject;

		//スクリプトから初期化
		public void InitOnCreate(AdvEngine engine, AdvImportScenarios scenarios, string rootResourceDir)
		{
			this.engine = engine;
			this.scenarios = scenarios;
			this.rootResourceDir = rootResourceDir;
			EditorVersionUp();
		}

		//最新バージョンにバージョンアップ
		public void EditorVersionUp()
		{
			version = Version;
		}

		//プロジェクト設定をチェック
		void CheckProjectSetting()
		{
			if (ScenarioProject ==null)
			{
				Debug.LogWarning($" {nameof(ScenarioProject)} is null.",this);
				return;
			}


			var bootSetting = this.gameObject.scene.GetComponentInScene<BootCustomProjectSetting>(true);
			if (bootSetting != null)
			{
				if (ScenarioProject.CustomProjectSetting != bootSetting.CustomProjectSetting)
				{
					//BootCustomProjectSettingコンポーネントに設定されている「CustomProjectSetting」と
					//AdvScenarioDataProject に設定されている「CustomProjectSetting」が違う場合警告
					Debug.LogWarning(
						$"{nameof(BootCustomProjectSetting)} and {nameof(AdvScenarioDataProject)} have different CustomProjectSetting.",
						bootSetting);
				}
			}

		}

		public void OnChangeEditorScene()
		{
			//宴3から宴4への以降で必要な処理
			if (this.ScenarioProject == null && this.scenarioDataProject != null)
			{
				this.ScenarioProject = this.scenarioDataProject as AdvScenarioDataProject;
				this.scenarioDataProject = null;
				UnityEditor.EditorUtility.SetDirty(this);
				Debug.LogWarning("Update ScenarioProject", this);
			}
		}

#else
		void CheckProjectSetting()
		{
		}
#endif
	}
}
