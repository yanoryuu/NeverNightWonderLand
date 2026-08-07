// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using Utage;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace Utage
{

	/// <summary>
	/// ロード待ち画面のサンプル
	/// </summary>
	[AddComponentMenu("Utage/TemplateUI/UtageUguiLoadWait")]
	public class UtageUguiLoadWait : UguiView
	{
		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] protected AdvEngine engine;

		/// <summary>スターター</summary>
		public AdvEngineStarter Starter
		{
			get { return this.GetComponentCacheFindIfMissing(ref starter); }
		}

		[SerializeField] protected AdvEngineStarter starter;

		public bool isAutoCacheFileLoad;

		public UtageUguiTitle title;

		public string bootSceneName;

		public GameObject buttonSkip;
		public GameObject buttonBack;
		public GameObject buttonDownload;
		public GameObject loadingBarRoot;
		public Image loadingBar;
		[HideIfTMP] public Text textMain;
		[HideIfLegacyText] public TextMeshProUGUI textMainTmp;
		[HideIfTMP] public Text textCount;
		[HideIfLegacyText] public TextMeshProUGUI textCountTmp;

		/// <summary>
		/// ダイアログ呼び出し
		/// </summary>
		public virtual OpenDialogEvent OnOpenDialog
		{
			set { this.onOpenDialog = value; }
			get
			{
				//ダイアログイベントに登録がないなら、SystemUiのダイアログを使う
				if (this.onOpenDialog.GetPersistentEventCount() == 0)
				{
					if (SystemUi.GetInstance() != null)
					{
						onOpenDialog.RemoveAllListeners();
						onOpenDialog.AddListener(SystemUi.GetInstance().OpenDialog);
					}
				}

				return onOpenDialog;
			}
		}

		[SerializeField] protected OpenDialogEvent onOpenDialog;

		protected enum State
		{
			Start,
			Downloding,
			DownlodFinished,
		};

		protected virtual State CurrentState { get; set; }

		protected enum Type
		{
			Default,
			Boot,
			ChapterDownload,
		};

		protected virtual Type DownloadType { get; set; }

		//すでにキャッシュファイルからロードしようとした
		//二回目からはダイアログで確認
		protected virtual bool AreadyTryReadCache { get; set; }


		//起動時に開く
		public virtual void OpenOnBoot()
		{
			DownloadType = Type.Boot;
			this.Open();
		}

		//章データのロードとして開く
		public virtual void OpenOnChapter()
		{
			DownloadType = Type.ChapterDownload;
			this.Open();
		}

		protected virtual void OnClose()
		{
			DownloadType = Type.Default;
		}

		protected virtual void OnOpen()
		{
			switch (DownloadType)
			{
				case Type.Boot:
					if (this.buttonBack) this.buttonBack.SetActive(false);
					if (this.buttonSkip) this.buttonSkip.SetActive(true);
					if (this.buttonDownload) this.buttonDownload.SetActive(true);
					break;
				case Type.Default:
					if (this.buttonBack) this.buttonBack.SetActive(true);
					if (this.buttonSkip) this.buttonSkip.SetActive(false);
					if (this.buttonDownload) this.buttonDownload.SetActive(true);
					break;
				case Type.ChapterDownload:
					if (this.buttonBack) this.buttonBack.SetActive(false);
					if (this.buttonSkip) this.buttonSkip.SetActive(false);
					if (this.buttonDownload) this.buttonDownload.SetActive(false);
					break;
			}

			if (!Starter.IsLoadStart)
			{
				ChangeState(State.Start);
			}
			else
			{
				ChangeState(State.Downloding);
			}
		}

		protected virtual void ChangeState(State state)
		{
			this.CurrentState = state;
			switch (state)
			{
				case State.Start:
					buttonDownload.SetActive(true);
					loadingBarRoot.SetActive(false);
					SetTextMain("");
					SetTextCount("");
					StartLoadEngine();
					break;
				case State.Downloding:
					buttonDownload.SetActive(false);
					StartCoroutine(CoUpdateLoading());
					break;
				case State.DownlodFinished:
					OnFinished();
					break;
			}
		}

		protected virtual void OnFinished()
		{
			switch (DownloadType)
			{
				case Type.Boot:
					this.Close();
					title.Open();
					break;
				case Type.Default:
					buttonDownload.SetActive(false);
					loadingBarRoot.SetActive(false);
					SetTextMain(LanguageSystemText.LocalizeText(SystemText.DownloadFinished));
					SetTextCount("");
					break;
				case Type.ChapterDownload:
					this.Close();
					break;
			}
		}

		//スキップボタン
		public virtual void OnTapSkip()
		{
			this.Close();
			title.Open();
		}

		//ｷｬｯｼｭｸﾘｱして最初のシーンを起動
		public virtual void OnTapReDownload()
		{
			AssetFileManager.GetInstance().AssetBundleInfoManager.DeleteAllCache();
			if (string.IsNullOrEmpty(bootSceneName))
			{
				WrapperUnityVersion.LoadScene(0);
			}
			else
			{
				WrapperUnityVersion.LoadScene(bootSceneName);
			}
		}

		//ローディング中の表示
		protected virtual IEnumerator CoUpdateLoading()
		{
			int maxCountDownLoad = 0;
			int countDownLoading = 0;
			loadingBarRoot.SetActive(true);
			loadingBar.fillAmount = 0.0f;
			SetTextMain(LanguageSystemText.LocalizeText(SystemText.Downloading));
			SetTextCount(string.Format(LanguageSystemText.LocalizeText(SystemText.DownloadCount), 0, 1));
			while (Engine.IsWaitBootLoading)
			{
				if (Starter.IsLoadErrorOnAwake)
				{
					Starter.IsLoadErrorOnAwake = false;
					OnFailedLoadEngine();
				}

				yield return null;
			}

			yield return null;

			while (!AssetFileManager.IsDownloadEnd())
			{
				yield return null;
				countDownLoading = AssetFileManager.CountDownloading();
				maxCountDownLoad = Mathf.Max(maxCountDownLoad, countDownLoading);
				int countDownLoaded = maxCountDownLoad - countDownLoading;
				SetTextCount(string.Format(LanguageSystemText.LocalizeText(SystemText.DownloadCount), countDownLoaded,
					maxCountDownLoad));
				if (maxCountDownLoad > 0)
				{
					loadingBar.fillAmount = (1.0f * (maxCountDownLoad - countDownLoading) / maxCountDownLoad);
				}
			}

			loadingBarRoot.gameObject.SetActive(false);
			ChangeState(State.DownlodFinished);
		}


		//ロード開始
		protected virtual void StartLoadEngine()
		{
			StartCoroutine(Starter.LoadEngineAsync(OnFailedLoadEngine));
			ChangeState(State.Downloding);
		}

		//ロード失敗
		protected virtual void OnFailedLoadEngine()
		{
			//キャッシュファイルから起動する
			if (isAutoCacheFileLoad && !AreadyTryReadCache)
			{
				AreadyTryReadCache = true;
				StartCoroutine(Starter.LoadEngineAsyncFromCacheManifest(OnFailedLoadEngine));
			}
			else
			{
				string text = LanguageSystemText.LocalizeText(SystemText.WarningNotOnline);
				List<ButtonEventInfo> buttons = new List<ButtonEventInfo>
				{
					new ButtonEventInfo(
						LanguageSystemText.LocalizeText(SystemText.Yes),
						() => { StartCoroutine(Starter.LoadEngineAsyncFromCacheManifest(OnFailedLoadEngine)); }
					),
					new ButtonEventInfo(
						LanguageSystemText.LocalizeText(SystemText.Retry),
						() => { StartCoroutine(Starter.LoadEngineAsync(OnFailedLoadEngine)); }
					),
				};
				OnOpenDialog.Invoke(text, buttons);
			}
		}

		//モバイルでのネットワークがオフラインになっているか
		protected bool IsMobileOffLine()
		{
			switch (Application.internetReachability)
			{
				//ネットにつながらないときに
				//キャッシュファイルがあるならそっちを使う
				case NetworkReachability.NotReachable:
					return true;
				case NetworkReachability.ReachableViaCarrierDataNetwork: //キャリア
				case NetworkReachability.ReachableViaLocalAreaNetwork: //Wifi
				default:
					return false;
			}
		}

		public virtual void SetTextMain(string text)
		{
			TextComponentWrapper.SetText(textMain, textMainTmp, text);
		}

		public virtual void SetTextCount(string text)
		{
			TextComponentWrapper.SetText(textCount, textCountTmp, text);
		}

	}
}
