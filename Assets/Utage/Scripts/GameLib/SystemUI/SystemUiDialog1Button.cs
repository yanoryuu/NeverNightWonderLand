// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using TMPro;

namespace Utage
{

	/// <summary>
	/// ボタン一つのダイアログ
	/// </summary>
	[AddComponentMenu("Utage/Lib/System UI/SystemUiDialog1Button")]
	public class SystemUiDialog1Button : MonoBehaviour
	{
		public virtual bool DisableInputUtilOnOpen { get { return disableInputUtilOnOpen; } set{ disableInputUtilOnOpen = value;} }
		[SerializeField]
		protected bool disableInputUtilOnOpen = false;

		/// <summary>
		/// 本文表示用のテキスト
		/// </summary>
		[SerializeField, HideIfTMP] protected Text titleText;
		[SerializeField, HideIfLegacyText] protected TextMeshProUGUI titleTextTmp;

		/// <summary>
		/// ボタン1用のテキスト
		/// </summary>
		[SerializeField, HideIfTMP] protected Text button1Text;
		[SerializeField, HideIfLegacyText] protected TextMeshProUGUI button1TextTmp;

		/// <summary>
		/// ボタン1を押したときのイベント
		/// </summary>
		[SerializeField]
		protected UnityEvent OnClickButton1;

		/// <summary>
		/// ダイアログを開く
		/// </summary>
		/// <param name="text">表示テキスト</param>
		/// <param name="buttonText1">ボタン1のテキスト</param>
		/// <param name="target">ボタンを押したときの呼ばれるコールバック</param>
		public virtual void Open(string text, string buttonText1, UnityAction callbackOnClickButton1)
		{
			SetTitle(text);
			SetTextButton1(buttonText1);
			this.OnClickButton1.RemoveAllListeners();
			this.OnClickButton1.AddListener(callbackOnClickButton1);
			Open();
		}
		
		public virtual void SetTitle(string text)
		{
			TextComponentWrapper.SetText(titleText, titleTextTmp,text);
		}

		public virtual void SetTextButton1(string text)
		{
			TextComponentWrapper.SetText(button1Text, button1TextTmp, text);
		}

		/// <summary>
		/// ボタン1が押された時の処理
		/// </summary>
		public virtual void OnClickButton1Sub()
		{
			if (disableInputUtilOnOpen) InputUtil.EnableInput = true;
			OnClickButton1.Invoke();
			Close();
		}

		/// <summary>
		/// オープン
		/// </summary>
		public virtual void Open()
		{
			if (disableInputUtilOnOpen)
			{
				InputUtil.EnableInput = false;
			}
			this.gameObject.SetActive(true);
		}

		/// <summary>
		/// クローズ
		/// </summary>
		public virtual void Close()
		{
			this.gameObject.SetActive(false);
		}
	}
}
