// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using TMPro;

namespace Utage
{
	/// <summary>
	/// ボタン3つのダイアログ
	/// </summary>
	[AddComponentMenu("Utage/Lib/System UI/SystemUiDialog3Button")]
	public class SystemUiDialog3Button : SystemUiDialog2Button
	{

		[SerializeField, HideIfTMP] protected Text button3Text;
		[SerializeField, HideIfLegacyText] protected TextMeshProUGUI button3TextTmp;


		/// <summary>
		/// ボタン3を押したときのイベント
		/// </summary>
		[SerializeField]
		protected UnityEvent OnClickButton3;

		/// <summary>
		/// 3ボタンダイアログをダイアログを起動
		/// </summary>
		/// <param name="text">表示テキスト</param>
		/// <param name="buttonText1">ボタン1用のテキスト</param>
		/// <param name="buttonText2">ボタン2用のテキスト</param>
		/// <param name="buttonText3">ボタン3用のテキスト</param>
		/// <param name="callbackOnClickButton1">ボタン1を押したときの呼ばれるコールバック</param>
		/// <param name="callbackOnClickButton2">ボタン2を押したときの呼ばれるコールバック</param>
		/// <param name="callbackOnClickButton3">ボタン3を押したときの呼ばれるコールバック</param>
		public virtual void Open(string text, string buttonText1, string buttonText2, string buttonText3, UnityAction callbackOnClickButton1, UnityAction callbackOnClickButton2, UnityAction callbackOnClickButton3 )
		{
			SetTextButton3(buttonText3);
			this.OnClickButton3.RemoveAllListeners();
			this.OnClickButton3.AddListener(callbackOnClickButton3);
			base.Open(text, buttonText1, buttonText2, callbackOnClickButton1, callbackOnClickButton2);
		}

		public virtual void SetTextButton3(string text)
		{
			TextComponentWrapper.SetText(button3Text, button3TextTmp, text);
		}

		/// <summary>
		/// ボタン3が押された
		/// </summary>
		public virtual void OnClickButton3Sub()
		{
			if (disableInputUtilOnOpen) InputUtil.EnableInput = true;
			this.OnClickButton3.Invoke();
			Close();
		}
	}

}
