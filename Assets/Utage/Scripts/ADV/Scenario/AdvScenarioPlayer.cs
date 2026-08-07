// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UtageExtensions;

namespace Utage
{
	[System.Serializable]
	public class AdvScenarioPlayerEvent : UnityEvent<AdvScenarioPlayer> { }
	[System.Serializable]
	public class AdvCommandEvent : UnityEvent<AdvCommand> { }

	/// <summary>
	/// シナリオを進めていくプレイヤー
	/// </summary>
	[AddComponentMenu("Utage/ADV/Internal/AdvScenarioPlayer")]
	public class AdvScenarioPlayer : MonoBehaviour, IBinaryIO
	{
		/// <summary>
		/// 「SendMessage」コマンドが実行されたときにSendMessageを受け取るGameObject
		/// </summary>
		public GameObject SendMessageTarget
		{
			get => sendMessageTarget;
			set => sendMessageTarget = value;
		}
		[SerializeField]
		GameObject sendMessageTarget=null;

		//デバッグログを出力するか
		[System.Flags]
		enum DebugOutPut
		{
			Log = 0x01,
			Waiting = 0x02,
			CommandEnd = 0x04,
		};
		[SerializeField]
		[EnumFlags]
		DebugOutPut debugOutPut = 0;

		internal bool DebugOutputLog { get { return (debugOutPut & DebugOutPut.Log) == DebugOutPut.Log; } }
		internal bool DebugOutputWaiting { get { return (debugOutPut & DebugOutPut.Waiting) == DebugOutPut.Waiting; } }
		internal bool DebugOutputCommandEnd { get { return (debugOutPut & DebugOutPut.CommandEnd) == DebugOutPut.CommandEnd; } }

		///事前にロードするファイルの最大数
		internal int MaxFilePreload { get { return maxFilePreload; } }
		[SerializeField]
		int maxFilePreload = 20;

		///ジャンプ先に潜って深くプリロードするか
		internal int PreloadDeep { get { return preloadDeep; } }
		[SerializeField]
		int preloadDeep = 5;

		//Jumpコマンドなどの条件付きジャンプ先も含めて深くプリロードするか
	public bool PreloadDeepJumpIf => preloadDeepJumpIf;
		[SerializeField]
		bool preloadDeepJumpIf = true;
		

		/// <summary>
		///　シナリオ開始時に呼ばれる
		/// </summary>
		public AdvScenarioPlayerEvent OnBeginScenario { get { return this.onBeginScenario; } }
		[SerializeField] public AdvScenarioPlayerEvent onBeginScenario = new AdvScenarioPlayerEvent();

		//　シナリオの開始時のうち、パラメータの初期化が終わった後に呼ばれる
		public AdvScenarioPlayerEvent OnBeginScenarioAfterParametersInitialized => onBeginScenarioAfterParametersInitialized;
		[SerializeField] AdvScenarioPlayerEvent onBeginScenarioAfterParametersInitialized = new AdvScenarioPlayerEvent();

		/// <summary>
		///　シナリオ終了時に呼ばれる
		/// </summary>
		public AdvScenarioPlayerEvent OnEndScenario { get { return this.onEndScenario; } }
		[SerializeField]
		public AdvScenarioPlayerEvent onEndScenario = new AdvScenarioPlayerEvent();

		/// <summary>
		///　シナリオポーズ時に呼ばれる
		/// </summary>
		public AdvScenarioPlayerEvent OnPauseScenario { get { return this.onPauseScenario; } }
		[SerializeField]
		public AdvScenarioPlayerEvent onPauseScenario = new AdvScenarioPlayerEvent();

		/// <summary>
		///　シナリオ終了かポーズ時に呼ばれる
		/// </summary>
		public AdvScenarioPlayerEvent OnEndOrPauseScenario { get { return this.onEndOrPauseScenario; } }
		[SerializeField]
		public AdvScenarioPlayerEvent onEndOrPauseScenario = new AdvScenarioPlayerEvent();

		/// <summary>
		///　コマンド開始時に呼ばれる
		/// </summary>
		public AdvCommandEvent OnBeginCommand { get { return this.onBeginCommand; } }
		[SerializeField]
		public AdvCommandEvent onBeginCommand = new AdvCommandEvent();

		/// <summary>
		///　コマンド待機中の前に呼ばれる
		/// </summary>
		public AdvCommandEvent OnUpdatePreWaitingCommand { get { return this.onUpdatePreWaitingCommand; } }
		[SerializeField]
		public AdvCommandEvent onUpdatePreWaitingCommand = new AdvCommandEvent();		

		/// <summary>
		///　コマンド待機中に呼ばれる
		/// </summary>
		public AdvCommandEvent OnUpdateWaitingCommand { get { return this.onUpdateWaitingCommand; } }
		[SerializeField]
		public AdvCommandEvent onUpdateWaitingCommand = new AdvCommandEvent();

		/// <summary>
		///　コマンド終了時に呼ばれる
		/// </summary>
		public AdvCommandEvent OnEndCommand { get { return this.onEndCommand; } }
		[SerializeField]
		public AdvCommandEvent onEndCommand = new AdvCommandEvent();

		//　セーブデータ読み込み前に呼ばれる
		public AdvScenarioPlayerEvent OnBeginReadSaveData { get { return this.onBeginLoadSaveData; } }
		[SerializeField]
		public AdvScenarioPlayerEvent onBeginLoadSaveData = new AdvScenarioPlayerEvent();

		//　セーブデータ読み込み直後に呼ばれる
		public AdvScenarioPlayerEvent OnEndReadSaveData { get { return this.onEndLoadSaveData; } }
		[SerializeField]
		public AdvScenarioPlayerEvent onEndLoadSaveData = new AdvScenarioPlayerEvent();

		//セーブデータのロード処理が始まる前に呼ばれる（準備処理などの前）
		public AdvScenarioPlayerEvent OnBeforeLoadSaveData => onBeforeLoadSaveData;
		[SerializeField] AdvScenarioPlayerEvent onBeforeLoadSaveData = new ();

		//セーブデータのロード処理が終わった後に呼ばれる（全て終わった後）
		public AdvScenarioPlayerEvent OnAfterLoadSaveData => onAfterLoadSaveData;
		[SerializeField] AdvScenarioPlayerEvent onAfterLoadSaveData = new ();


		//ADVエンジン本体への参照
		public AdvEngine Engine { get { return this.GetComponentCache( ref engine); } }
		AdvEngine engine;

		//メインのシナリオ実行スレッド（なければ自動生成）
		public AdvScenarioThread MainThread
		{
			get
			{
				if (mainThread == null)
				{
					mainThread = this.gameObject.GetComponentCreateIfMissing<AdvScenarioThread>();
					mainThread.Init(this, "MainThread", null);
				}
				return mainThread;
			}
		}
		AdvScenarioThread mainThread;

		/// <summary>
		/// シナリオ終了したか
		/// </summary>
		public bool IsEndScenario { get; set; }


		//シナリオ終了が予約されているか
		public bool IsReservedEndScenario { get; set; }

		//ポーズ中か
		public bool IsPausing { get; set; }


		/// <summary>
		/// 現在の、シーン回想用のシーンラベル
		/// </summary>
		public string CurrentGallerySceneLabel { get; set; }

		//シナリオファイルを読み込み中か
		public bool IsLoading
		{
			get
			{
				return MainThread.IsLoadingDeep;
			}
		}

		/// <summary>
		/// シナリオの実行開始
		/// </summary>
		/// <param name="scenarioLabel">ジャンプ先のシナリオラベル</param>
		/// <param name="page">シナリオラベルからのページ数</param>
		public virtual void StartScenario(string label, int page)
		{
			this.IsPausing = false;
			this.IsEndScenario = false;
			this.IsReservedEndScenario = false;

			//現在のシーン回想登録用のラベルをクリア
			this.CurrentGallerySceneLabel = "";
			MainThread.Clear();
			OnBeginScenario.Invoke(this);
			//パラメータの初期化が終わった後に呼ばれる、シナリオの開始時イベント
			OnBeginScenarioAfterParametersInitialized.Invoke(this);
			MainThread.StartScenario(label, page, false);
		}

		//セーブデータを使ってシナリオを開始
		internal IEnumerator CoStartSaveData(AdvSaveData saveData)
		{
			OnBeforeLoadSaveData.Invoke(this);
			this.IsPausing = false;
			this.IsEndScenario = false;
			this.IsReservedEndScenario = false;

			MainThread.Clear();
			OnBeginScenario.Invoke(this);
			//各オブジェクトにセーブデータの値を読み込ませる
			saveData.LoadGameData(
				Engine,
				Engine.SaveManager.CustomSaveDataIOList,
				Engine.SaveManager.GetSaveIoListCreateIfMissing(Engine)
				);
			yield return null;
			//パラメータの初期化が終わった後に呼ばれる、シナリオの開始時イベント
			OnBeginScenarioAfterParametersInitialized.Invoke(this);
			//シナリオを読み込み
			saveData.Buffer.Overrirde(this);
			OnAfterLoadSaveData.Invoke(this);
		}

		//データのキー
		public string SaveKey { get { return "ScenarioPlayer"; } }

		const int Version0 = 0;
		const int Version1 = 1;
		const int Version2 = 2;
		//書き込み
		public void OnWrite(System.IO.BinaryWriter writer)
		{
			writer.Write(Version2);
			this.MainThread.IfManager.Write(writer);
			this.MainThread.JumpManager.Write(writer);
			this.MainThread.Write(writer);
			writer.Write(Engine.Page.ScenarioLabel);
			writer.Write(Engine.Page.PageNo);
			writer.Write(CurrentGallerySceneLabel);
			writer.Write(MainThread.SkipPageHeaerOnSave);
		}

		//読み込み
		public void OnRead(System.IO.BinaryReader reader)
		{
			OnBeginReadSaveData.Invoke(this);
			int version = reader.ReadInt32();
			if (0<= version && version <= Version2)
			{
				if (version >= Version1)
				{
					this.MainThread.IfManager.Read(reader);
				}
				else
				{
					this.MainThread.IfManager.ReadOld();
				}
				this.MainThread.JumpManager.Read(this.Engine, reader);

				if (version>=Version2)
				{
					this.MainThread.Read(this.Engine, reader);
				}
				string scenarioLabel = reader.ReadString();
				int pageNo = reader.ReadInt32();
				string gallerySceneLabel = reader.ReadString();
				bool skipPageHeaer = reader.ReadBoolean();


				//現在のシーン回想登録用のラベルを記録
				MainThread.ScenarioPlayer.CurrentGallerySceneLabel = gallerySceneLabel;

				//シナリオを開始
				MainThread.StartScenario(scenarioLabel, pageNo, skipPageHeaer);
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
			OnEndReadSaveData.Invoke(this);
		}

		/// <summary>
		/// シナリオ終了
		/// </summary>
		public virtual void EndScenario()
		{
			this.OnEndScenario.Invoke(this);
			this.OnEndOrPauseScenario.Invoke(this);
			Engine.ClearOnEnd();
			MainThread.Clear();
			IsEndScenario = true;
		}

		//シナリオ実行を一時停止する
		public void Pause()
		{
			IsPausing = true;
			this.OnPauseScenario.Invoke(this);
			this.OnEndOrPauseScenario.Invoke(this);
		}
		//シナリオ実行を再開する
		public void Resume()
		{
			IsPausing = false;
		}


		/// <summary>
		/// クリア処理
		/// </summary>
		public void Clear()
		{
			MainThread.Clear();
			CurrentGallerySceneLabel = "";
		}

		/// <summary>
		/// シーン回想のためにシーンラベルを更新
		/// </summary>
		/// <param name="label">シーンラベル</param>
		/// <param name="engine">ADVエンジン</param>
		internal void UpdateSceneGallery(string label, AdvEngine engine)
		{
			AdvSceneGallerySetting galleryData = engine.DataManager.SettingDataManager.SceneGallerySetting;
			if (galleryData.Contains(label))
			{
				if (CurrentGallerySceneLabel != label)
				{
					if (!string.IsNullOrEmpty(CurrentGallerySceneLabel))
					{
						//別のシーンが終わってないのに、新しいシーンに移っている
						Debug.LogError(LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.UpdateSceneLabel, CurrentGallerySceneLabel, label));
					}
					CurrentGallerySceneLabel = label;
				}
			}
		}

		/// <summary>
		/// シーン回想のためのシーンの終了処理
		/// </summary>
		/// <param name="engine">ADVエンジン</param>
		public void EndSceneGallery(AdvEngine engine)
		{
			if (string.IsNullOrEmpty(CurrentGallerySceneLabel))
			{
				//シーン回想に登録されていないのに、シーン回想終了がされています
				Debug.LogError(LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.EndSceneGallery));
			}
			else
			{
				engine.SystemSaveData.GalleryData.AddSceneLabel(CurrentGallerySceneLabel);
				CurrentGallerySceneLabel = "";
			}
		}
		
		//今のページのシナリオ進行を強制的に中断
		public void ForceBreakCurrentPage()
		{
			Engine.Page.Clear();
			MainThread.Clear();
		}
	}
}
