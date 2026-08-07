// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Utage;
using UtageExtensions;

namespace Utage
{

	// ノベルゲームのメニューボタンの処理
	// 後付けのため、UtageUguiMainGameと機能が重複している点に注意
	// UtageUguiMainGameはノベルゲーム以外での使用が難しかったので、
	// UtageUguiMenuButtonsはノベルゲーム以外でも使用できるように実装する
	[AddComponentMenu("Utage/TemplateUI/UtageUguiMenuButtons")]
	public class UtageUguiMenuButtons : MonoBehaviour
	{
		// ADVエンジン
		public virtual AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] protected AdvEngine engine;

		[SerializeField] protected GameObject rootButtons;

		//メニュー独自の背景
		//メッセージウィンドウの背景をメニューの背景として使うために、メニュー独自の背景のオンオフ切り替えを行うために設定
		//オンオフ切り替えが必要ない場合はNONEに設定すること
		[SerializeField] protected Image bg;
		
		// スキップボタン
		[SerializeField] protected Toggle checkSkip;

		// 自動で読み進むボタン
		[SerializeField] protected Toggle checkAuto;

		//ノベルゲームモードでのメイン画面
		[SerializeField] protected UtageUguiMainGame mainGame;
		//コンフィグ画面（ノベルゲーム以外で直接開く場合）
		[SerializeField] protected UtageUguiConfig config;
		
		//タイトル画面（ノベルゲームで使用しているばあい）
		[SerializeField] UtageUguiTitle title;
		//確認ダイアログの表示。設定してないときは表示しない
		public SystemUiDialog2Button dialog;

		public virtual void Open()
		{
			this.gameObject.SetActive(true);
		}

		
		public virtual void Close()
		{
			this.gameObject.SetActive(false);
		}

		protected virtual void LateUpdate()
		{
			//メニューボタンの表示・表示を切り替え
			bool activeMenuButtons = Engine.UiManager.IsShowingMenuButton && Engine.UiManager.Status == AdvUiManager.UiStatus.Default;

			//メニューボタンの表示・表示を切り替え
			if (rootButtons != null)
			{
				rootButtons.SetActive(activeMenuButtons);
			}

			//メニュー独自の背景の表示・表示を切り替え
			if (bg != null)
			{
				//メッセージウィンドウの背景をメニューの背景として使うために、以下の二つの状態を切り替えるための処理
				//メニュー独自の背景が非表示：「メッセージウィンドウの背景が表示されている」
				//メニュー独自の背景が表示：「メッセージウィンドウが表示されない」
				bg.enabled = activeMenuButtons && !Engine.UiManager.IsShowingMessageWindow;
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


		//スキップボタンが押された
		public virtual void OnTapSkip(bool isOn)
		{
			Engine.Config.IsSkip = isOn;
		}

		//自動読み進みボタンが押された
		public virtual void OnTapAuto(bool isOn)
		{
			Engine.Config.IsAutoBrPage = isOn;
		}

		//ウインドウ閉じるボタンが押された
		public virtual void OnTapCloseWindow()
		{
			Engine.UiManager.Status = AdvUiManager.UiStatus.HideMessageWindow;
		}

		//バックログボタンが押された
		public virtual void OnTapBackLog()
		{
			Engine.UiManager.Status = AdvUiManager.UiStatus.Backlog;
		}


		//コンフィグボタンが押された
		public virtual void OnTapConfig()
		{
			if(mainGame!=null)
			{
				mainGame.OnTapConfig();
			}
			else
			{
				config.Open();
			}
		}
		
		//タイトルに戻るボタンが押された
		public virtual void OnClickBackTitle()
		{
			void BackTitle()
			{
				Engine.EndScenario();
				mainGame.Close();
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
	}
}
