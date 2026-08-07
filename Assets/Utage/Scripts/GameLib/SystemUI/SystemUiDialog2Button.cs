// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using TMPro;

namespace Utage
{
	/// <summary>
	/// ボタン二つのダイアログ
	/// </summary>
	[AddComponentMenu("Utage/Lib/System UI/SystemUiDialog2Button")]
	public class SystemUiDialog2Button : SystemUiDialog1Button
	{

		/// <summary>
		/// ボタン2用のテキスト
		/// </summary>
		[SerializeField, HideIfTMP] protected Text button2Text;
		[SerializeField, HideIfLegacyText] protected TextMeshProUGUI button2TextTmp;

		/// <summary>
		/// ボタン2を押したときのイベント
		/// </summary>
		[SerializeField]
		protected UnityEvent OnClickButton2;

		/// <summary>
		/// 二ボタンダイアログをダイアログを起動
		/// </summary>
		/// <param name="text">表示テキスト</param>
		/// <param name="buttonText1">ボタン1用のテキスト</param>
		/// <param name="buttonText2">ボタン2用のテキスト</param>
		/// <param name="callbackOnClickButton1">ボタン1を押したときの呼ばれるコールバック</param>
		/// <param name="callbackOnClickButton2">ボタン2を押したときの呼ばれるコールバック</param>
		public virtual void Open(string text, string buttonText1, string buttonText2, UnityAction callbackOnClickButton1, UnityAction callbackOnClickButton2 )
		{
			SetTextButton2(buttonText2);
			this.OnClickButton2.RemoveAllListeners();
			this.OnClickButton2.AddListener(callbackOnClickButton2);
			base.Open(text, buttonText1, callbackOnClickButton1 );
		}

		public virtual void OpenYesNo(string text, UnityAction callbackOnClickYes, UnityAction callbackOnClickNo)
		{
			Open(text, LanguageSystemText.LocalizeText(SystemText.Yes),
				LanguageSystemText.LocalizeText(SystemText.No), callbackOnClickYes, callbackOnClickNo);
		}

		public virtual void SetTextButton2(string text)
		{
			TextComponentWrapper.SetText(button2Text, button2TextTmp, text);
		}

		/// <summary>
		/// ボタン2が押された
		/// </summary>
		public virtual void OnClickButton2Sub()
		{
			if (disableInputUtilOnOpen) InputUtil.EnableInput = true;
			OnClickButton2.Invoke();
			Close();
		}
	}

}
