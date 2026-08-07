// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using Utage;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;


namespace Utage
{

	/// <summary>
	/// コンフィグ画面のサンプル
	/// </summary>
	[AddComponentMenu("Utage/TemplateUI/UtageUguiConfig")]
	public class UtageUguiConfig : UguiView
	{
		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] protected AdvEngine engine;

		//コンフィグデータへのインターフェース
		protected virtual AdvConfig Config
		{
			get { return Engine.Config; }
		}
		

		/// <summary>タイトル画面</summary>
		[SerializeField] protected UtageUguiTitle title;

		/// <summary>「フルスクリーン表示」のチェックボックス</summary>
		[SerializeField] protected Toggle checkFullscreen;
		[SerializeField] protected GameObject checkFullscreenRoot;

		/// <summary>「マウスホイールでメッセージ送り」のチェックボックス</summary>
		[SerializeField] protected Toggle checkMouseWheel;
		[SerializeField] protected GameObject checkMouseWheelRoot;

		/// <summary>「未読スキップ」のチェックボックス</summary>
		[SerializeField] protected Toggle checkSkipUnread;

		/// <summary>「選択肢でスキップを解除」チェックボックス</summary>
		[SerializeField] protected Toggle checkStopSkipInSelection;

		/// <summary>「ボイス再生時にメッセージウィンドウを非表示に」チェックボックス</summary>
		[SerializeField] protected Toggle checkHideMessageWindowOnPlyaingVoice;

		/// <summary>「メッセージ速度」のスライダー</summary>
		[SerializeField] protected Slider sliderMessageSpeed;

		/// <summary>「メッセージ速度（既読）」のスライダー</summary>
		[SerializeField] protected Slider sliderMessageSpeedRead;

		/// <summary>「自動メッセージ速度」のスライダー</summary>
		[SerializeField] protected Slider sliderAutoBrPageSpeed;

		/// <summary>「ウインドウの透明度」のスライダー</summary>
		[SerializeField] protected Slider sliderMessageWindowTransparency;

		/// <summary>「サウンド」の音量スライダー</summary>
		[SerializeField] protected Slider sliderSoundMasterVolume;

		/// <summary>「BGM」の音量スライダー</summary>
		[SerializeField] protected Slider sliderBgmVolume;

		/// <summary>「SE」の音量スライダー</summary>
		[SerializeField] protected Slider sliderSeVolume;

		/// <summary>「環境音」の音量スライダー</summary>
		[SerializeField] protected Slider sliderAmbienceVolume;

		/// <summary>「ボイス」の音量スライダー</summary>
		[SerializeField] protected Slider sliderVoiceVolume;

		/// <summary>音声の再生タイプのラジオボタン</summary>
		[SerializeField] protected UguiToggleGroupIndexed radioButtonsVoiceStopType;

		[System.Serializable]
		protected class TagedMasterVolumSliders
		{
			public string tag = "";
			public Slider volumeSlider = null;
		}

		/// <summary>キャラ別のボリューム設定など</summary>
		[SerializeField] protected List<TagedMasterVolumSliders> tagedMasterVolumSliders;

		//確認ダイアログの表示。設定してないときは表示しない
		public SystemUiDialog2Button dialog;


		//開く時にタブをリセットする
		public bool ResetTabIndexOnOpen
		{
			get => resetTabIndexOnOpen;
			set => resetTabIndexOnOpen = value;
		}
		[SerializeField] bool resetTabIndexOnOpen;

		//開く時にタブをリセットする際のタブへの参照
		[SerializeField] UguiToggleGroupIndexed tabToggleGroup;


		//コンフィグの値全体をロードした際に呼ばれるイベント
		//表示開始時や、初期設定に戻したときに呼ばれ、各UIに値を反映された時に呼ばれるので
		//コンフィグUIを拡張して、値を反映する処理を追加する場合は、このイベントを使う
		public UnityEvent OnLoadValues => onLoadValues;
		[SerializeField] UnityEvent onLoadValues = new();
        

		//文字送り速度
		public virtual float MessageSpeed
		{
			set
			{
				if (!IsInit) return;
				Config.MessageSpeed = value;
			}
		}

		//文字送り速度(既読)
		public virtual float MessageSpeedRead
		{
			set
			{
				if (!IsInit) return;
				Config.MessageSpeedRead = value;
			}
		}

		//オート文字送り速度
		public virtual float AutoBrPageSpeed
		{
			set
			{
				if (!IsInit) return;
				Config.AutoBrPageSpeed = value;
			}
		}

		//メッセージウィンドウの透過色（バー）
		public virtual float MessageWindowTransparency
		{
			set
			{
				if (!IsInit) return;
				Config.MessageWindowTransparency = value;
			}
		}

		//音量設定 サウンド全体
		public virtual float SoundMasterVolume
		{
			set
			{
				if (!IsInit) return;
				Config.SoundMasterVolume = value;
			}
		}

		//音量設定 BGM
		public virtual float BgmVolume
		{
			set
			{
				if (!IsInit) return;
				Config.BgmVolume = value;
			}
		}

		//音量設定 SE
		public virtual float SeVolume
		{
			set
			{
				if (!IsInit) return;
				Config.SeVolume = value;
			}
		}

		//音量設定 環境音
		public virtual float AmbienceVolume
		{
			set
			{
				if (!IsInit) return;
				Config.AmbienceVolume = value;
			}
		}

		//音量設定 ボイス
		public virtual float VoiceVolume
		{
			set
			{
				if (!IsInit) return;
				Config.VoiceVolume = value;
			}
		}

		//フルスクリーン切り替え
		public virtual bool IsFullScreen
		{
			set
			{
				if (!IsInit) return;
				Engine.ScreenResolution.IsFullScreen = value;
			}
		}

		//マウスホイールでメッセージ送り切り替え
		public virtual bool IsMouseWheel
		{
			set
			{
				if (!IsInit) return;
				Config.IsMouseWheelSendMessage = value;
			}
		}

		//エフェクトON・OFF切り替え
		public virtual bool IsEffect
		{
			set
			{
				if (!IsInit) return;
				Config.IsEffect = value;
			}
		}

		//未読スキップON・OFF切り替え
		public virtual bool IsSkipUnread
		{
			set
			{
				if (!IsInit) return;
				Config.IsSkipUnread = value;
			}
		}

		//選択肢でスキップ解除ON・OFF切り替え
		public virtual bool IsStopSkipInSelection
		{
			set
			{
				if (!IsInit) return;
				Config.IsStopSkipInSelection = value;
			}
		}

		//ボイス再生時にメッセージウィンドウを非表示にON・OFF切り替え
		public virtual bool HideMessageWindowOnPlyaingVoice
		{
			set
			{
				if (!IsInit) return;
				Config.HideMessageWindowOnPlayingVoice = value;
			}
		}

		public virtual bool IsInit
		{
			get { return isInit; }
			set { isInit = value; }
		}

		protected bool isInit = false;

		/// <summary>
		/// オープンしたときに呼ばれる
		/// </summary>
		protected virtual void OnOpen()
		{
			isInit = false;
			//スクショをクリア
			if (Engine.SaveManager.Type != AdvSaveManager.SaveType.SavePoint)
			{
				Engine.SaveManager.ClearCaptureTexture();
			}

			if (ResetTabIndexOnOpen)
			{
				ResetTabIndex();
			}

			StartCoroutine(CoWaitOpen());
		}

		public void ResetTabIndex()
		{
			if (tabToggleGroup == null)
			{
				Debug.LogWarning("tabToggleGroup is null", this);
				return;
			}
			
			//トグル変更のアニメーションをいったん無効化
			var toggles = tabToggleGroup.TogglesToArray;
			List<Toggle.ToggleTransition> toggleTransitions = new List<Toggle.ToggleTransition>();  
			foreach (var toggle in toggles)
			{
				toggleTransitions.Add(toggle.toggleTransition);
				toggle.toggleTransition = Toggle.ToggleTransition.None;
			}
			//タブインデックスを0にリセット
			tabToggleGroup.CurrentIndex = 0;
			//トグル変更のアニメーションを戻しておく
			for (var i = 0; i < toggles.Length; i++)
			{
				toggles[i].toggleTransition = toggleTransitions[i];
			}
		}


		//起動待ちしてから開く
		protected virtual IEnumerator CoWaitOpen()
		{
			while (Engine.IsWaitBootLoading) yield break;
			LoadValues();
		}

		/// <summary>
		/// 画面を閉じる処理
		/// </summary>
		public override void Close()
		{
			Engine.WriteSystemData();
			base.Close();
		}

		protected virtual void Update()
		{
			//右クリックで戻る
			if (isInit && InputUtil.IsInputGuiClose())
			{
				Back();
			}
		}

		//各UIに値を反映
		protected virtual void LoadValues()
		{
			isInit = false;
			if (checkFullscreen) checkFullscreen.isOn = Engine.ScreenResolution.IsFullScreen;
			if (checkMouseWheel) checkMouseWheel.isOn = Config.IsMouseWheelSendMessage;
			if (checkSkipUnread) checkSkipUnread.isOn = Config.IsSkipUnread;
			if (checkStopSkipInSelection) checkStopSkipInSelection.isOn = Config.IsStopSkipInSelection;
			if (checkHideMessageWindowOnPlyaingVoice)
				checkHideMessageWindowOnPlyaingVoice.isOn = Config.HideMessageWindowOnPlayingVoice;

			if (sliderMessageSpeed) sliderMessageSpeed.value = Config.MessageSpeed;
			if (sliderMessageSpeedRead) sliderMessageSpeedRead.value = Config.MessageSpeedRead;

			if (sliderAutoBrPageSpeed) sliderAutoBrPageSpeed.value = Config.AutoBrPageSpeed;
			if (sliderMessageWindowTransparency)
				sliderMessageWindowTransparency.value = Config.MessageWindowTransparency;
			if (sliderSoundMasterVolume) sliderSoundMasterVolume.value = Config.SoundMasterVolume;
			if (sliderBgmVolume) sliderBgmVolume.value = Config.BgmVolume;
			if (sliderSeVolume) sliderSeVolume.value = Config.SeVolume;
			if (sliderAmbienceVolume) sliderAmbienceVolume.value = Config.AmbienceVolume;
			if (sliderVoiceVolume) sliderVoiceVolume.value = Config.VoiceVolume;

			if (radioButtonsVoiceStopType) radioButtonsVoiceStopType.CurrentIndex = (int)Config.VoiceStopType;

			//サブマスターボリュームの設定
			foreach (var item in tagedMasterVolumSliders)
			{
				if (string.IsNullOrEmpty(item.tag) || item.volumeSlider == null)
				{
					continue;
				}

				float volume;
				if (Config.TryGetTaggedMasterVolume(item.tag, out volume))
				{
					item.volumeSlider.value = volume;
				}
			}

			//フルスクリーンはPC版のみの操作
			if (!UtageToolKit.IsPlatformStandAloneOrEditor())
			{
				if (checkFullscreen)
				{
					checkFullscreen.gameObject.SetActive(false);
				}
				if(checkFullscreenRoot)
				{
					checkFullscreenRoot.gameObject.SetActive(false);
				}
				//マウスホイールはPC版とWebGL以外では無効
				if (Application.platform != RuntimePlatform.WebGLPlayer)
				{
					if (checkMouseWheel)
					{
						checkMouseWheel.gameObject.SetActive(false);
					}

					if (checkMouseWheelRoot)
					{
						checkMouseWheelRoot.gameObject.SetActive(false);
					}
				}
			}
			
			OnLoadValues.Invoke();
			isInit = true;
		}

		//タイトルに戻る
		public virtual void OnTapBackTitle()
		{
			void BackTitle()
			{
				Engine.EndScenario();
				this.Close();
				title.Open();
			}
			if (dialog!=null)
			{
				dialog.OpenYesNo(LanguageSystemText.LocalizeText(SystemText.UtageDialogMessageBackTitleConfirm),
					BackTitle, () => { });
			}
			else
			{
				BackTitle();
			}
		}

		//全てデフォルト値で初期化
		public virtual void OnTapInitDefaultAll()
		{
			if (!IsInit) return;

			void InitDefaultAll()
			{
				Config.InitDefaultAll();
				LoadValues();
			}

			if (dialog != null)
			{
				dialog.OpenYesNo(LanguageSystemText.LocalizeText(SystemText.UtageDialogMessageResetConfigConfirm),
					InitDefaultAll, () => { });
			}
			else
			{
				InitDefaultAll();
			}
		}

		//音声設定（クリックで停止、次の音声まで再生を続ける）
		public virtual void OnTapRadioButtonVoiceStopType(int index)
		{
			if (!IsInit) return;
			Config.VoiceStopType = (VoiceStopType)index;
		}

		//タグつきボリュームの設定
		public virtual void OnValugeChangedTaggedMasterVolume(string tag, float value)
		{
			if (!IsInit) return;
			Config.SetTaggedMasterVolume(tag, value);
		}
	}
}
