// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Utage;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// メインゲーム画面のサンプル
	/// 入力処理に起点になるため、スクリプトの実行順を通常よりも少しはやくすること
	/// http://docs-jp.unity3d.com/Documentation/Components/class-ScriptExecution.html
	/// </summary>
	[AddComponentMenu("Utage/TemplateUI/UtageUguiMainGame")]
	public class UtageUguiMainGame : UguiView
	{
		/// <summary>ADVエンジン</summary>
		public virtual AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] protected AdvEngine engine;

		/// <summary>キャプチャ用のカメラ</summary>
		public virtual LetterBoxCamera LetterBoxCamera
		{
			get { return this.GetComponentCacheFindIfMissing(ref letterBoxCamera); }
		}

		[SerializeField] protected LetterBoxCamera letterBoxCamera;


		/// <summary>タイトル画面</summary>
		public UtageUguiTitle title;

		/// <summary>コンフィグ画面</summary>
		public UtageUguiConfig config;

		/// <summary>セーブロード画面</summary>
		public UtageUguiSaveLoad saveLoad;

		/// <summary>ギャラリー画面</summary>
		public UtageUguiGallery gallery;

		/// <summary>ボタン</summary>
		public GameObject buttons;

		/// <summary>スキップボタン</summary>
		public Toggle checkSkip;

		/// <summary>自動で読み進むボタン</summary>
		public Toggle checkAuto;

		//ガイドメッセージの表示。設定してないときは表示しない
		public SystemUiGuideMessage guideMessage;

		//起動タイプ
		protected enum BootType
		{
			Default,
			Start,
			Load,
			SceneGallery,
			StartLabel,
		};

		protected BootType bootType;

		//ロードするセーブデータ
		protected AdvSaveData loadData;

		protected bool isInit = false;

		/// <summary>起動するシナリオラベル</summary>
		protected string scenarioLabel;

		protected virtual void Awake()
		{
			Engine.Page.OnEndText.AddListener((page) => CaptureScreenOnSavePoint(page));
		}

		/// <summary>
		/// 画面を閉じる
		/// </summary>
		public override void Close()
		{
			base.Close();
			Engine.UiManager.Close();
			Engine.Config.IsSkip = false;
		}

		//起動データをクリア
		protected virtual void ClearBootData()
		{
			bootType = BootType.Default;
			isInit = false;
			loadData = null;
		}

		/// <summary>
		/// ゲームをはじめから開始
		/// </summary>
		public virtual void OpenStartGame()
		{
			ClearBootData();
			bootType = BootType.Start;
			Open();
		}

		/// <summary>
		/// 指定ラベルからゲーム開始
		/// </summary>
		public virtual void OpenStartLabel(string label)
		{
			ClearBootData();
			bootType = BootType.StartLabel;
			this.scenarioLabel = label;
			Open();
		}

		/// <summary>
		/// セーブデータをロードしてゲーム再開
		/// </summary>
		/// <param name="loadData">ロードするセーブデータ</param>
		public virtual void OpenLoadGame(AdvSaveData loadData)
		{
			ClearBootData();
			bootType = BootType.Load;
			this.loadData = loadData;
			Open();
		}

		/// <summary>
		/// シーン回想としてシーンを開始
		/// </summary>
		/// <param name="scenarioLabel">シーンラベル</param>
		public virtual void OpenSceneGallery(string scenarioLabel)
		{
			ClearBootData();
			bootType = BootType.SceneGallery;
			this.scenarioLabel = scenarioLabel;
			Open();
		}

		/// <summary>
		/// オープンしたときに呼ばれる
		/// </summary>
		protected virtual void OnOpen()
		{
			//スクショをクリア
			if (Engine.SaveManager.Type != AdvSaveManager.SaveType.SavePoint)
			{
				Engine.SaveManager.ClearCaptureTexture();
			}

			StartCoroutine(CoWaitOpen());
		}


		//起動待ちしてから開く
		protected virtual IEnumerator CoWaitOpen()
		{
			while (Engine.IsWaitBootLoading) yield return null;

			switch (bootType)
			{
				case BootType.Default:
					Engine.UiManager.Open();
					break;
				case BootType.Start:
					Engine.StartGame();
					break;
				case BootType.Load:
					Engine.OpenLoadGame(loadData);
					break;
				case BootType.SceneGallery:
					Engine.StartSceneGallery(scenarioLabel);
					break;
				case BootType.StartLabel:
					Engine.StartGame(scenarioLabel);
					break;
			}

			ClearBootData();
			loadData = null;
			Engine.Config.IsSkip = false;
			isInit = true;
		}

		//更新中
		protected virtual void Update()
		{
			if (!isInit) return;

			//ローディングアイコンを表示
			if (SystemUi.GetInstance())
			{
				if (Engine.IsLoading)
				{
					SystemUi.GetInstance().StartIndicator(this);
				}
				else
				{
					SystemUi.GetInstance().StopIndicator(this);
				}
			}


			if (Engine.IsEndScenario)
			{
				Close();
				if (Engine.IsSceneGallery)
				{
					//回想シーン終了したのでギャラリーに
					gallery.Open();
				}
				else
				{
					//シナリオ終了したのでタイトルへ
					title.Open(this);
				}
			}
		}
		
		//表示の更新
		//UtageUguiMenuButtonsと機能が重複しているので、
		//UtageUguiMenuButtonsを使う場合は、buttonsやcheckSkipをnullにすること
		protected virtual void LateUpdate()
		{
			//メニューボタンの表示・表示を切り替え
			if (buttons != null)
			{
				buttons.SetActive(Engine.UiManager.IsShowingMenuButton &&
				                  Engine.UiManager.Status == AdvUiManager.UiStatus.Default);
			}

			//スキップフラグを反映
			if (checkSkip)
			{
				if (checkSkip.isOn != Engine.Config.IsSkip)
				{
					checkSkip.isOn = Engine.Config.IsSkip;
				}
			}

			//オートフラグを反映
			if (checkAuto)
			{
				if (checkAuto.isOn != Engine.Config.IsAutoBrPage)
				{
					checkAuto.isOn = Engine.Config.IsAutoBrPage;
				}
			}
		}

		protected virtual void CaptureScreenOnSavePoint(AdvPage page)
		{
			if (Engine.SaveManager.Type == AdvSaveManager.SaveType.SavePoint)
			{
				if (page.IsSavePoint)
				{
//					Debug.Log("Capture");
					StartCoroutine(CoCaptureScreen());
				}
			}
		}

		protected virtual IEnumerator CoCaptureScreen()
		{
			yield return new WaitForEndOfFrame();
			//セーブ用のスクショを撮る
			Engine.SaveManager.CaptureTexture = CaptureScreen();
		}

		//スキップボタンが押された
		//UtageUguiMenuButtonsと機能が重複しているので注意
		public virtual void OnTapSkip(bool isOn)
		{
			Engine.Config.IsSkip = isOn;
		}

		//自動読み進みボタンが押された
		//UtageUguiMenuButtonsと機能が重複しているので注意
		public virtual void OnTapAuto(bool isOn)
		{
			Engine.Config.IsAutoBrPage = isOn;
		}

		//コンフィグボタンが押された
		public virtual void OnTapConfig()
		{
			Close();
			config.Open(this);
		}

		//セーブボタンが押された
		public virtual void OnTapSave()
		{
			if (Engine.IsSceneGallery) return;

			StartCoroutine(CoSave());
		}

		protected virtual IEnumerator CoSave()
		{
			if (Engine.SaveManager.Type != AdvSaveManager.SaveType.SavePoint)
			{
				yield return new WaitForEndOfFrame();
				//セーブ用のスクショを撮る
				Engine.SaveManager.CaptureTexture = CaptureScreen();
			}

			//セーブ画面開く
			Close();
			saveLoad.OpenSave(this);
		}

		//ロードボタンが押された
		public virtual void OnTapLoad()
		{
			if (Engine.IsSceneGallery) return;

			Close();
			saveLoad.OpenLoad(this);
		}

		//クイックセーブボタンが押された
		public virtual void OnTapQSave()
		{
			if (Engine.IsSceneGallery) return;

			Engine.Config.IsSkip = false;
			StartCoroutine(CoQSave());
		}

		protected virtual IEnumerator CoQSave()
		{
			if (Engine.SaveManager.Type != AdvSaveManager.SaveType.SavePoint)
			{
				yield return new WaitForEndOfFrame();
				//セーブ用のスクショを撮る
				Engine.SaveManager.CaptureTexture = CaptureScreen();
			}

			//クイックセーブ
			Engine.QuickSave();
			//スクショをクリア
			if (Engine.SaveManager.Type != AdvSaveManager.SaveType.SavePoint)
			{
				Engine.SaveManager.ClearCaptureTexture();
			}

			//ガイドメッセージの表示
			if (guideMessage != null)
			{
				guideMessage.Open(LanguageSystemText.LocalizeText(SystemText.UtageGuideMessageQuickSave));
			}
		}

		//クイックロードボタンが押された
		public virtual void OnTapQLoad()
		{
			if (Engine.IsSceneGallery) return;

			Engine.Config.IsSkip = false;
			Engine.QuickLoad();
			
			//ガイドメッセージの表示
			if (guideMessage!=null)
			{
				guideMessage.Open(LanguageSystemText.LocalizeText(SystemText.UtageGuideMessageQuickLoad));
			}
		}


		//セーブ用のスクショを撮る
		protected virtual Texture2D CaptureScreen()
		{
			if (!Engine.SaveManager.EnableCapture(Engine.Param))
			{
				return null;
			}
			Rect rect = LetterBoxCamera.CachedCamera.rect;
			int x = Mathf.CeilToInt(rect.x * Screen.width);
			int y = Mathf.CeilToInt(rect.y * Screen.height);
			int width = Mathf.FloorToInt(rect.width * Screen.width);
			int height = Mathf.FloorToInt(rect.height * Screen.height);
			return UtageToolKit.CaptureScreen(new Rect(x, y, width, height));
		}
	}
}
