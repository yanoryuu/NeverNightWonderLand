// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{
	/// <summary>
	/// システム系のUI処理
	/// </summary>
	[AddComponentMenu("Utage/Lib/System UI/SystemUi")]
	public class SystemUi : MonoBehaviour
	{
		/// <summary>
		/// シングルトンなインスタンスの取得
		/// </summary>
		/// <returns></returns>
		public static SystemUi GetInstance()
		{
			return instance;
		}
		[StaticField] static SystemUi instance;


		void Awake()
		{
			if (null == instance)
			{
				instance = this;
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.SingletonError));
				Destroy(this);
			}
		}

		[SerializeField]
		protected SystemUiDialog2Button dialogGameExit = null;

		[SerializeField]
		protected SystemUiDialog1Button dialog1Button = null;

		[SerializeField]
		protected SystemUiDialog2Button dialog2Button = null;

		[SerializeField]
		protected SystemUiDialog3Button dialog3Button = null;

		[SerializeField]
		protected IndicatorIcon indicator = null;
		
		/// <summary>
		/// Escapeボタンの入力有効か
		/// </summary>
		public bool IsEnableInputEscape
		{
			get { return isEnableInputEscape; }
			set { isEnableInputEscape = value; }
		}
		[SerializeField]
		bool isEnableInputEscape = true;

		/// 1ボタンのダイアログを開く
		public void OpenDialog(string text, List<ButtonEventInfo> buttons)
		{
			switch(buttons.Count)
			{
				case 1:
					OpenDialog1Button(text, buttons[0]);
					break;
				case 2:
					OpenDialog2Button(text, buttons[0], buttons[1]);
					break;
				case 3:
					OpenDialog3Button(text, buttons[0], buttons[1], buttons[2]);
					break;
				default:
					Debug.LogError(" Dilog Button Count over = " + buttons.Count );
					break;
			}
		}

		/// 1ボタンのダイアログを開く
		public void OpenDialog1Button(string text, ButtonEventInfo button1)
		{
			OpenDialog1Button(text, button1.text, button1.callBackClicked);
		}
		/// 2ボタンのダイアログを開く
		public void OpenDialog2Button(string text, ButtonEventInfo button1, ButtonEventInfo button2)
		{
			OpenDialog2Button(text, button1.text, button2.text, button1.callBackClicked, button2.callBackClicked);
		}
		/// 3ボタンのダイアログを開く
		public void OpenDialog3Button(string text, ButtonEventInfo button1, ButtonEventInfo button2, ButtonEventInfo button3)
		{
			OpenDialog3Button(text, button1.text, button2.text, button3.text, button1.callBackClicked, button2.callBackClicked, button3.callBackClicked);
		}

		/// <summary>
		/// 1ボタンのダイアログを開く
		/// </summary>
		/// <param name="text">表示テキスト</param>
		/// <param name="buttonText1">ボタン1のテキスト</param>
		/// <param name="target">ボタンを押したときの呼ばれるコールバック</param>
		public void OpenDialog1Button(string text, string buttonText1, UnityAction callbackOnClickButton1)
		{
			dialog1Button.Open(text, buttonText1, callbackOnClickButton1);
		}

		/// <summary>
		///  2ボタンのダイアログを開く
		/// </summary>
		/// <param name="text">表示テキスト</param>
		/// <param name="buttonText1">ボタン1用のテキスト</param>
		/// <param name="buttonText2">ボタン2用のテキスト</param>
		/// <param name="callbackOnClickButton1">ボタン1を押したときの呼ばれるコールバック</param>
		/// <param name="callbackOnClickButton2">ボタン2を押したときの呼ばれるコールバック</param>
		public void OpenDialog2Button(string text, string buttonText1, string buttonText2, UnityAction callbackOnClickButton1, UnityAction callbackOnClickButton2)
		{
			dialog2Button.Open(text, buttonText1, buttonText2, callbackOnClickButton1, callbackOnClickButton2);
		}
		
		/// <summary>
		/// 3ボタンのダイアログを開く
		/// </summary>
		/// <param name="text">表示テキスト</param>
		/// <param name="buttonText1">ボタン1用のテキスト</param>
		/// <param name="buttonText2">ボタン2用のテキスト</param>
		/// <param name="buttonText3">ボタン3用のテキスト</param>
		/// <param name="callbackOnClickButton1">ボタン1を押したときの呼ばれるコールバック</param>
		/// <param name="callbackOnClickButton2">ボタン2を押したときの呼ばれるコールバック</param>
		/// <param name="callbackOnClickButton3">ボタン3を押したときの呼ばれるコールバック</param>
		public void OpenDialog3Button(string text, string buttonText1, string buttonText2, string buttonText3, UnityAction callbackOnClickButton1, UnityAction callbackOnClickButton2, UnityAction callbackOnClickButton3 )
		{
			dialog3Button.Open(text, buttonText1, buttonText2, buttonText3, callbackOnClickButton1, callbackOnClickButton2, callbackOnClickButton3 );
		}

		/// <summary>
		/// はい、いいえダイアログを開く
		/// </summary>
		/// <param name="text">表示テキスト</param>
		/// <param name="target">ボタンを押したときのメッセージの送り先</param>
		/// <param name="callbackOnClickYes">Yesボタンを押したときの呼ばれるコールバック</param>
		/// <param name="callbackOnClickNo">Noボタンを押したときの呼ばれるコールバック</param>
		public void OpenDialogYesNo(string text, UnityAction callbackOnClickYes, UnityAction callbackOnClickNo)
		{
			OpenDialog2Button(text, LanguageSystemText.LocalizeText(SystemText.Yes), LanguageSystemText.LocalizeText(SystemText.No), callbackOnClickYes, callbackOnClickNo);
		}

		/// <summary>
		/// インジケーターの表示開始
		/// 表示要求しているオブジェクトはあちこちから設定できる。
		/// 全ての要求が終了しない限りは表示を続ける
		/// </summary>
		/// <param name="obj">表示を要求してるオブジェクト</param>
		public void StartIndicator(Object obj)
		{
			if (indicator) indicator.StartIndicator(obj);
		}

		/// <summary>
		/// インジケーターの表示終了
		/// 表示要求しているオブジェクトはあちこちから設定できる。
		/// 全ての要求が終了しない限りは表示を続ける
		/// </summary>
		/// <param name="obj">表示を要求していたオブジェクト</param>
		public void StopIndicator(Object obj)
		{
			if (indicator) indicator.StopIndicator(obj);
		}

		void Update()
		{
			//Android版・バックキーでアプリ終了確認
			if (IsEnableInputEscape)
			{
				if (InputUtil.IsInputExit())
				{
					OnOpenDialogExitGame();
				}
			}
		}

		public virtual void OnOpenDialogExitGame()
		{
			InputUtil.EnableInput = false;
			dialogGameExit.Open(
				LanguageSystemText.LocalizeText(SystemText.QuitGame),
				LanguageSystemText.LocalizeText(SystemText.Yes),
				LanguageSystemText.LocalizeText(SystemText.No),
				OnDialogExitGameYes, OnDialogExitGameNo
				);
		}

		//ゲーム終了確認「はい」
		protected virtual void OnDialogExitGameYes()
		{
			InputUtil.EnableInput = true;
			StartCoroutine(CoGameExit());
		}
		//ゲーム終了確認「いいえ」
		protected virtual void OnDialogExitGameNo()
		{
			InputUtil.EnableInput = true;
		}

		//ゲーム終了
		protected virtual IEnumerator CoGameExit()
		{
			Application.Quit();
			yield break;
		}
	}
}
