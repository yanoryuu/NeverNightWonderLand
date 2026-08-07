// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UtageExtensions;

namespace Utage
{
	/// <summary>
	/// 宴のイベント処理
	/// </summary>
	[System.Serializable]
	public class AdvEvent : UnityEvent<AdvEngine> { }

	/// <summary>
	/// メインエンジン
	/// </summary>/
	[AddComponentMenu("Utage/ADV/AdvEngine")]
	public partial class AdvEngine : MonoBehaviour
	{
		/// <summary>
		/// 最初からはじめる場合のシナリオ名
		/// </summary>
		public string StartScenarioLabel
		{
			get {
				return startScenarioLabel;
			}
			set {
				startScenarioLabel = value;
			}
		}
		string startScenarioLabel = "Start";


		/// <summary>
		/// シナリオや設定等のデータ
		/// </summary>
		public AdvDataManager DataManager { get { return this.GetComponentCache( ref dataManager); } }
		AdvDataManager dataManager;

		/// <summary>
		/// シナリオの実行部分
		/// </summary>
		public AdvScenarioPlayer ScenarioPlayer { get { return this.GetComponentCache(ref scenarioPlayer); } }
		AdvScenarioPlayer scenarioPlayer;

		//シナリオ内のサウンド制御
		public AdvScenarioSound ScenarioSound { get { return this.GetComponentCacheCreateIfMissing(ref scenarioSound); } }
		AdvScenarioSound scenarioSound;

		/// <summary>
		/// ページ情報
		/// </summary>
		public AdvPage Page { get { return this.GetComponentCache(ref page); } }
		AdvPage page;


		/// <summary>
		/// 選択肢
		/// </summary>
		public AdvSelectionManager SelectionManager { get { return this.GetComponentCache(ref selectionManager); } }
		AdvSelectionManager selectionManager;

		/// <summary>
		/// メッセージウィンドウ
		/// </summary>
		public AdvMessageWindowManager MessageWindowManager { get { return this.GetComponentCacheCreateIfMissing( ref messageWindowManager); } }
		AdvMessageWindowManager messageWindowManager;

		/// <summary>
		/// バックログ
		/// </summary>
		public AdvBacklogManager BacklogManager { get { return this.GetComponentCache(ref backlogManager); } }
		AdvBacklogManager backlogManager;

		/// <summary>
		/// コンフィグデータ
		/// </summary>
		public AdvConfig Config { get { return this.GetComponentCache(ref config); } }
		AdvConfig config;

		/// <summary>
		/// システムセーブデータ
		/// </summary>
		public AdvSystemSaveData SystemSaveData { get { return this.GetComponentCache(ref systemSaveData); } }
		AdvSystemSaveData systemSaveData;

		/// <summary>
		/// 通常のセーブデータ
		/// </summary>
		public AdvSaveManager SaveManager { get { return this.GetComponentCache(ref saveManager); } }
		AdvSaveManager saveManager;

		/// <summary>
		/// グラフィック管理
		/// </summary>
		public AdvGraphicManager GraphicManager
		{
			get
			{
				if (this.graphicManager == null)
				{
					this.graphicManager = this.transform.GetComponentInChildrenCreateIfMissing<AdvGraphicManager>();
					this.graphicManager.transform.localPosition = new Vector3(0,0,20);
				}
				return this.graphicManager;
			}
		}
		[SerializeField]
		AdvGraphicManager graphicManager;

		/// <summary>
		/// エフェクト管理
		/// </summary>
		public AdvEffectManager EffectManager
		{
			get
			{
				if (effectManager == null)
				{
					effectManager = this.transform.GetComponentInChildrenCreateIfMissing<AdvEffectManager>();
				}
				return effectManager;
			}
		}
		[SerializeField]
		AdvEffectManager effectManager;

		//ポストエフェクト管理
		public AdvPostEffectManager AdvPostEffectManager
		{
			get
			{
				if (postEffectManager == null)
				{
					postEffectManager = this.transform.GetComponentInChildrenCreateIfMissing<AdvPostEffectManager>();
				}
				return postEffectManager;
			}
		}
		[SerializeField]
		AdvPostEffectManager postEffectManager;

		/// <summary>
		/// UI管理
		/// </summary>
		public AdvUiManager UiManager { get { return this.GetComponentCacheFindIfMissing(ref uiManager); } }
		[SerializeField]
		AdvUiManager uiManager;

		/// <summary>
		/// サウンドマネージャー
		/// </summary>
		public SoundManager SoundManager { get { return this.GetComponentCacheFindIfMissing(ref soundManager ); } }
		[SerializeField,UnityEngine.Serialization.FormerlySerializedAs("soundManger")]
		SoundManager soundManager;

		/// <summary>
		/// カメラマネージャー
		/// </summary>
		public CameraManager CameraManager { get { return this.GetComponentCacheFindIfMissing(ref cameraManager ); } }
		[SerializeField]
		CameraManager cameraManager;


		//画面解像度管理
		public virtual ScreenResolution ScreenResolution
		{
			get
			{
				this.GetComponentCacheFindIfMissing(ref screenResolution);
				if (screenResolution == null)
				{
					Debug.LogWarning("Not found ScreenResolution Component");
					screenResolution = this.gameObject.AddComponent<ScreenResolution>();
				}
				return screenResolution;
			}
		}
		[SerializeField] protected ScreenResolution screenResolution;

		/// <summary>
		/// 時間管理
		/// </summary>
		public AdvTime Time { get { return this.GetComponentCacheCreateIfMissing(ref time); } }
		[SerializeField]
		AdvTime time;

		/// <summary>
		/// パラメータ管理
		/// </summary>
		public AdvParamManager Param { get { return this.param; } }
		AdvParamManager param = new AdvParamManager();

		//パラメーター変更イベント
		public AdvParameterEventTrigger ParameterEventTrigger => this.GetComponentCacheCreateIfMissing(ref parameterEventTrigger);
		AdvParameterEventTrigger parameterEventTrigger;

		//起動時に非同期で
		[SerializeField]
		bool bootAsync = false;
		
		//開始時にサウンドを止めるか
		public bool IsStopSoundOnStart
		{
			get => isStopSoundOnStart;
			set => isStopSoundOnStart = value;
		} 
		[SerializeField]
		bool isStopSoundOnStart = true;

		//終了時にサウンドを止めるか
		public bool IsStopSoundOnEnd
		{
			get => isStopSoundOnEnd;
			set => isStopSoundOnEnd = value;
		} 
		[SerializeField]
		bool isStopSoundOnEnd = true;

		//ロード時にサウンドを止めるか
		public bool IsStopSoundOnLoad
		{
			get => isStopSoundOnLoad;
			set => isStopSoundOnLoad = value;
		} 
		[SerializeField]
		bool isStopSoundOnLoad = true;

		[SerializeField]
		bool isStopVoiceOnSoundStop = true;

		[SerializeField]
		bool isStopSeOnSoundStop = false;

		//パラメーターに言語設定があればそれに合わせる
		public string LanguageKeyOfParam
		{
			get => languageKeyOfParam;
			set => languageKeyOfParam = value;
		}
		[SerializeField]
		string languageKeyOfParam = "";

		//パラメーターにボイス言語設定があればそれに合わせる
		public string VoiceLanguageKeyOfParam
		{
			get => voiceLanguageKeyOfParam;
			set => voiceLanguageKeyOfParam = value;
		}
		[SerializeField]
		string voiceLanguageKeyOfParam = "";
		
		/// <summary>
		/// カスタムコマンド用のコンポーネントリスト
		/// </summary>
		public List<AdvCustomCommandManager> CustomCommandManagerList
		{
			get
			{
				if(customCommandManagerList==null)
				{
					customCommandManagerList = new List<AdvCustomCommandManager>();
					this.GetComponentsInChildren<AdvCustomCommandManager>(true,customCommandManagerList);
				}
				return this.customCommandManagerList;
			}
		}
		List<AdvCustomCommandManager> customCommandManagerList;

		/// <summary>
		/// 初期化の際に呼ばれるコールバック
		/// </summary>
		public UnityEvent onPreInit;

		// 起動処理後に呼ばれるコールバック
		public UnityEvent OnPostInit{get{return onPostInit;}}
		[SerializeField]
		UnityEvent onPostInit = new UnityEvent();

		/// <summary>
		/// ダイアログ呼び出し
		/// </summary>
		public OpenDialogEvent OnOpenDialog
		{
			set { this.onOpenDialog = value; }
			get
			{
				//ダイアログイベントに登録がないなら、SystemUiのダイアログを使う
				if (this.onOpenDialog.GetPersistentEventCount() == 0)
				{
					if (SystemUi.GetInstance() != null)
					{
						onOpenDialog.AddListener(SystemUi.GetInstance().OpenDialog);
					}
				}
				return onOpenDialog;
			}
		}
		[SerializeField]
		OpenDialogEvent onOpenDialog;

		/// <summary>
		/// ページ内のテキストが変更されたら呼ばれる
		/// </summary>
		public AdvEvent OnPageTextChange { get { return this.onPageTextChange; } }
		[SerializeField]
		AdvEvent onPageTextChange = new AdvEvent();

		/// <summary>
		///　起動時、終了時、ロード時などに呼ばれるクリア処理
		/// </summary>
		public AdvEvent OnClear;

		/// <summary>
		///　言語切り替え時に呼ばれる
		/// </summary>
		public AdvEvent OnChangeLanguage { get { return this.onChangeLanguage; } }
		[SerializeField]
		public AdvEvent onChangeLanguage = new AdvEvent ();

		/// <summary>
		/// 起動時ロード待ちか判定
		/// </summary>
		public bool IsWaitBootLoading { get { return isWaitBootLoading; } }
		bool isWaitBootLoading = true;

		/// <summary>
		/// 起動したか
		/// </summary>
		public bool IsStarted { get { return isStarted; } }
		bool isStarted = false;

		/// <summary>
		/// シーン回想を再生中か
		/// </summary>
		public bool IsSceneGallery => GalleryController.IsPlayingSceneGallery;

		//シーン回想・CGギャラリーの制御
		public AdvGalleryController GalleryController => this.GetComponentCacheCreateIfMissing(ref galleryController);
		private AdvGalleryController galleryController;

		/// <summary>
		/// ロード待ちか判定
		/// </summary>
		public bool IsLoading
		{
			get
			{
				if (IsWaitBootLoading) return true;
				if (GraphicManager.IsLoading) return true;

				return ScenarioPlayer.IsLoading;
			}
		}

		/// <summary>
		/// シナリオが終了したか判定
		/// </summary>
		public bool IsEndScenario
		{
			get
			{
				if (ScenarioPlayer == null ) return false;
				if (IsLoading) return false;

				return ScenarioPlayer.IsEndScenario;
			}
		}

		/// <summary>
		/// シナリオが終了、またはポーズしたかの判定
		/// </summary>
		public bool IsPausingScenario
		{
			get
			{
				return ScenarioPlayer.IsPausing;
			}
		}

		/// <summary>
		/// シナリオが終了、またはポーズしたかの判定
		/// </summary>
		public bool IsEndOrPauseScenario
		{
			get
			{
				return IsEndScenario || IsPausingScenario;
			}
		}

		bool InitCallback { get; set; }

		void OnDestroy()
		{
			if (InitCallback)
			{
				AdvGraphicInfo.CallbackExpression = null;
				TextParser.CallbackCalcExpression -= Param.CalcExpressionNotSetParam;
				iTweenData.CallbackGetValue -= Param.GetParameter;
			}
		}

		/// <summary>
		/// 設定されたエクスポートデータからゲームを開始
		/// </summary>
		/// <param name="rootDirResource">リソースディレクトリ</param>
		public void BootFromExportData(AdvImportScenarios scenarios, string resourceDir)
		{
			this.gameObject.SetActive(true);
			StopAllCoroutines();
			StartCoroutine(CoBootFromExportData(scenarios, resourceDir));
		}

		/// <summary>
		/// 設定されたエクスポートデータからゲームを開始
		/// </summary>
		/// <param name="rootDirResource">リソースディレクトリ</param>
		IEnumerator CoBootFromExportData(AdvImportScenarios scenarios, string resourceDir)
		{
			ClearSub(false);
			isStarted = true;
			isWaitBootLoading = true;
			onPreInit.Invoke();

			while (!AssetFileManager.IsInitialized()) yield return null;

			//プロファイラ―が最初の1フレームはちゃんんと記録してくれないので遅らせる
			yield return null;
			DataManager.SettingDataManager.ImportedScenarios = scenarios;
			yield return CoBootInit(resourceDir);
			isWaitBootLoading = false;
			OnPostInit.Invoke();
		}


		/// <summary>
		/// 既にその章データを設定済みか
		/// </summary>
		/// <param name="url">パス</param>
		public bool ExitsChapter(string url)
		{
			string chapterAssetName = FilePathUtil.GetFileNameWithoutExtension(url);
			return DataManager.SettingDataManager.ImportedScenarios.Chapters.Exists(x => x.name == chapterAssetName);
		}

		/// <summary>
		/// 起動用TSVをロード
		/// </summary>
		/// <param name="url">CSVのパス</param>
		/// <param name="version">シナリオバージョン（-1以下で必ずサーバーからデータを読み直す）</param>
		/// <returns></returns>
		public IEnumerator LoadChapterAsync(string url)
		{
			AssetFile file = AssetFileManager.Load(url, this);
			while (!file.IsLoadEnd) yield return null;

			AdvChapterData chapter = file.UnityObject as AdvChapterData;
			if (chapter == null)
			{
				Debug.LogError(url + " is  not scenario file");
				yield break;
			}

			if (this.DataManager.SettingDataManager.ImportedScenarios == null)
			{
				this.DataManager.SettingDataManager.ImportedScenarios = new AdvImportScenarios();
			}
			if (this.DataManager.SettingDataManager.ImportedScenarios.TryAddChapter(chapter))
			{
				//シナリオデータの初期化
				DataManager.BootInitChapter(chapter);
			}
		}

		//起動時にシステムパラメーターに言語設定があれば、それの言語にする
		void AutoChangeLanguageOnBoot()
		{
			string language = string.IsNullOrEmpty(LanguageKeyOfParam) ? "" : Param.GetParameterString(LanguageKeyOfParam);
			string voiceLanguage = string.IsNullOrEmpty(VoiceLanguageKeyOfParam) ? "" : Param.GetParameterString(VoiceLanguageKeyOfParam);
			
			if (!string.IsNullOrEmpty(language))
			{
				//セーブされた言語名に変更
				LanguageManagerBase.Instance.CurrentLanguage = language;
			}
			else
			{
				//初回起動時は現在の言語名（デバイスの環境言語が設定されているはず）をパラメーターとして保存
				if (!string.IsNullOrEmpty(LanguageKeyOfParam))
				{
					Param.SetParameterString(LanguageKeyOfParam, LanguageManagerBase.Instance.CurrentLanguage);
				}
			}
			if (!string.IsNullOrEmpty(voiceLanguage))
			{
				//セーブされた言語名に変更
				LanguageManagerBase.Instance.VoiceLanguage = voiceLanguage;
			}
		}


		//　言語切り替え時に呼ばれる
		void ChangeLanguage()
		{
			if (!string.IsNullOrEmpty(LanguageKeyOfParam))
			{
				Param.SetParameterString(LanguageKeyOfParam, LanguageManagerBase.Instance.CurrentLanguage);
			}
			if (!string.IsNullOrEmpty(VoiceLanguageKeyOfParam))
			{
				Param.SetParameterString(VoiceLanguageKeyOfParam, LanguageManagerBase.Instance.VoiceLanguage);
			}

			this.Page.OnChangeLanguage();
			OnChangeLanguage.Invoke(this);
			//ローカライズ時に呼びだす（今のところボイスファイルの変更が必要な時のみ）
			ForEachCommand( (x)=> x.OnChangeLanguage(this));
		}

		void ForEachCommand(Action<AdvCommand> action)
		{
			foreach (var scenarioData in DataManager.ScenarioDataTbl)
			{
				foreach (var scenarioLabel in scenarioData.Value.ScenarioLabels)
				{
					foreach (var page in scenarioLabel.Value.PageDataList)
					{
						foreach (var command in page.CommandList)
						{
							action(command);
						}
					}
				}
			}
		}



		//シナリオ開始時のクリア処理
		public void ClearOnStart()
		{
			ClearSub(isStopSoundOnStart);
		}

		//シナリオ終了時のクリア処理
		public void ClearOnEnd()
		{
			ClearSub(isStopSoundOnEnd);
		}

		void ClearOnLaod()
		{
			ClearSub(isStopSoundOnLoad);
		}


		//ゲームの開始、終了、ロード時などのクリア処理
		void ClearSub( bool isStopSound )
		{
			Page.Clear();
			SelectionManager.Clear();
			BacklogManager.Clear();
			GraphicManager.Clear();
			GraphicManager.gameObject.SetActive(true);
			if (UiManager != null) UiManager.Close();

			ClearCustomCommand();
			ScenarioPlayer.Clear();
			if (isStopSound && SoundManager !=null)
			{
				SoundManager.StopBgm();
				SoundManager.StopAmbience();
				SoundManager.StopAllLoop();
				if (isStopVoiceOnSoundStop)
				{
					SoundManager.StopVoice();
				}
				if (isStopSeOnSoundStop)
				{
					SoundManager.StopSeAll(SoundManager.DefaultFadeTime);
				}
			}
			ScenarioSound.Clear();

			if(MessageWindowManager==null)
			{
				Debug.LogError("MessageWindowManager is Missing");
			}
            CameraManager.OnClear();
			SaveManager.GetSaveIoListCreateIfMissing(this).ForEach( x => ((IAdvSaveData)x).OnClear());
			SaveManager.CustomSaveDataIOList.ForEach(x => ((IAdvSaveData)x).OnClear());
			OnClear.Invoke(this);			
		}

		/// <summary>
		/// シナリオ終了
		/// </summary>
		public void EndScenario()
		{
			ScenarioPlayer.EndScenario();
		}

		/// <summary>
		/// 起動時の初期化
		/// </summary>
		/// <param name="rootDirResource">ルートディレクトリのリソース</param>
		IEnumerator CoBootInit(string rootDirResource )
		{
			//カスタムコマンドの初期化
			BootInitCustomCommand();

			DataManager.BootInit(rootDirResource);
			//設定データを反映
			GraphicManager.BootInit(this, DataManager.SettingDataManager.LayerSetting);
			//パラメーターをデフォルト値でリセット
			Param.InitDefaultAll(DataManager.SettingDataManager.DefaultParam);
			Param.SetAdvEngine(this);
			//パラメーターを反映
			InitCallback = true;
			AdvGraphicInfo.CallbackExpression = Param.CalcExpressionBoolean;
			TextParser.CallbackCalcExpression += Param.CalcExpressionNotSetParam;
			iTweenData.CallbackGetValue += Param.GetParameter;
			LanguageManagerBase.Instance.OnChangeLanguage = ChangeLanguage;

			//カスタム初期化処理を呼ぶ
			foreach (var item in this.GetComponentsInChildren<IAdvEngineCustomEventBootInit>(true))
			{
				item.OnBootInit();
			}

			//システムセーブデータの初期化＆ロード
			SystemSaveData.Init(this);
			//通常セーブデータの初期化
			SaveManager.Init();

			//ロードしたセーブデータに言語設定がにあれば、それに言語変更
			AutoChangeLanguageOnBoot();

			//シナリオデータの初期化
			if (bootAsync)
			{
				//非同期初期化
				yield return StartCoroutine(DataManager.CoBootInitScenariodData());
			}
			else
			{
				//シナリオデータの初期化
				DataManager.BootInitScenariodData();
				//リソースファイル(画像やサウンド)のダウンロードをバックグラウンドで進めておく
				DataManager.StartBackGroundDownloadResource();
			}
		}

		//カスタムコマンドの初期化
		public void BootInitCustomCommand()
		{
			AdvCommandParser.OnCreateCustomCommandFromID = null;
#if UNITY_EDITOR
			if(Application.isEditor)
			{
				this.customCommandManagerList = null;
			}
#endif
			foreach (var item in CustomCommandManagerList)
			{
				item.OnBootInit();
			}
		}

		//カスタムコマンドの関係のクリア処理
		public void ClearCustomCommand()
		{
			foreach (var item in CustomCommandManagerList)
			{
				item.OnClear();
			}
		}


		/// <summary>
		/// システムセーブデータを書き込み
		/// </summary>
		public void WriteSystemData()
		{
			systemSaveData.Write();
		}

		/// <summary>
		/// セーブデータを書き込み
		/// </summary>
		/// <param name="saveData">書き込むセーブデータ</param>
		public void WriteSaveData(AdvSaveData saveData)
		{
			SaveManager.WriteSaveData(this, saveData);
		}

		/// <summary>
		/// セーブデータのロード
		/// </summary>
		/// <param name="saveData">ロードするセーブデータ</param>
		void LoadSaveData(AdvSaveData saveData)
		{
			ClearOnLaod();
			StartCoroutine( CoStartSaveData(saveData) );
		}

		/// <summary>
		/// クイックセーブ
		/// </summary>
		public void QuickSave()
		{
			WriteSaveData(SaveManager.QuickSaveData);
		}

		/// <summary>
		/// クイックロード
		/// </summary>
		/// <returns>成否</returns>
		public bool QuickLoad()
		{
			if (SaveManager.ReadQuickSaveData())
			{
				LoadSaveData(SaveManager.QuickSaveData);
				return true;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// シナリオを一番最初から開始
		/// </summary>
		public void StartGame()
		{
			StartGame(StartScenarioLabel);
		}

		/// <summary>
		/// シナリオを指定のシーンから開始
		/// </summary>
		public void StartGame(string scenarioLabel)
		{
			this.GalleryController.StartGame(false);
			StartGameSub(scenarioLabel);
		}

		void StartGameSub(string scenarioLabel)
		{
			StartCoroutine(CoStartGameSub(scenarioLabel));
		}

		IEnumerator CoStartGameSub(string scenarioLabel)
		{
			while (IsWaitBootLoading) yield return null;

			//基本的なパラメーターをデフォルト値でリセット（システムデータ以外）
			Param.InitDefaultNormal(DataManager.SettingDataManager.DefaultParam);
			ClearOnStart();
			StartScenario(scenarioLabel, 0);
		}

		/// <summary>
		/// セーブデータをロードして開始
		/// </summary>
		/// <param name="saveData">ロードするセーブデータ</param>
		public void OpenLoadGame(AdvSaveData saveData)
		{
			this.GalleryController.StartGame(false);
			LoadSaveData(saveData);
		}

		/// <summary>
		/// シーン回想を開始
		/// </summary>
		/// <param name="label">シーンラベル</param>
		public void StartSceneGallery(string label)
		{
			this.GalleryController.StartGame(true);
			StartGameSub(label);
		}

		/// <summary>
		/// シナリオを再開
		/// </summary>
		public bool ResumeScenario()
		{
			if (!ScenarioPlayer.IsPausing)
			{
				return false;
			}
			else
			{
				ScenarioPlayer.Resume();
				return true;
			}
		}

		
		/// <summary>
		/// 指定のラベルにシナリオジャンプ
		/// </summary>
		/// <param name="label">ジャンプ先のラベル</param>
		public void JumpScenario(string label)
		{
			if (ScenarioPlayer.MainThread.IsPlaying)
			{
				if (ScenarioPlayer.IsPausing)
				{
					ScenarioPlayer.Resume();
				}
				ScenarioPlayer.MainThread.JumpManager.RegistoreLabel(label);
			}
			else
			{
				StartScenario(label, 0);
			}
		}

		//指定のラベル・ページからシナリオを開始する
		public void StartScenario(string label, int page)
		{
			StartCoroutine( CoStartScenario(label, page));
		}

		IEnumerator CoStartScenario(string label, int page)
		{
			while (IsWaitBootLoading) yield return null;
			while (GraphicManager.IsLoading) yield return null;
			while (SoundManager.IsLoading) yield return null;

			//UIを開く（このタイミングで開かないとUIオブジェクト以下のAwakeが呼ばれてない可能性があって、不具合が起きる可能性がある）
			if (UiManager != null) UiManager.Open();
			if (label.Length > 1 && label[0] == '*')
			{
				label = label.Substring(1);
			}
			ScenarioPlayer.StartScenario( label, page);
		}
	
		IEnumerator CoStartSaveData(AdvSaveData saveData)
		{
			while (IsWaitBootLoading) yield return null;
			while (GraphicManager.IsLoading) yield return null;
			while (SoundManager.IsLoading) yield return null;

			//UIを開く（このタイミングで開かないとUIオブジェクト以下のAwakeが呼ばれてない可能性があって、不具合が起きる可能性がある）
			if (UiManager != null) UiManager.Open();
			yield return ScenarioPlayer.CoStartSaveData(saveData);
		}

		//全てのファイルを取得
		public HashSet<AssetFile> GetAllFileSet()
		{
			var fileSet = this.DataManager.GetAllFileSet();
			return fileSet;
		}
	}
}
