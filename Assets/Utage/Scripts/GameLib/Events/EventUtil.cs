// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{
	public class ButtonEventInfo
	{
		public ButtonEventInfo(string text, UnityAction callBackClicked)
		{
			this.text = text;
			this.callBackClicked = callBackClicked;
		}
		public string text;
		public UnityAction callBackClicked;
	};

	/// ダイアログを開イベント
	[System.Serializable]
	public class OpenDialogEvent : UnityEvent<string, List<ButtonEventInfo>> { }

	/// 1ボタンダイアログを開イベント
	[System.Serializable]
	public class Open1ButtonDialogEvent : UnityEvent<string,ButtonEventInfo> { }

	/// 2ボタンダイアログを開イベント
	[System.Serializable]
	public class Open2ButtonDialogEvent : UnityEvent<string, ButtonEventInfo, ButtonEventInfo> { }

	/// 3ボタンダイアログを開イベント
	[System.Serializable]
	public class Open3ButtonDialogEvent : UnityEvent<string, ButtonEventInfo, ButtonEventInfo, ButtonEventInfo> { }

	/// floatイベント
	[System.Serializable]
	public class FloatEvent : UnityEvent<float> { }

	/// intイベント
	[System.Serializable]
	public class IntEvent : UnityEvent<int> { }

	/// stringイベント
	[System.Serializable]
	public class StringEvent : UnityEvent<string> { }

	/// boolイベント
	[System.Serializable]
	public class BoolEvent : UnityEvent<bool> { }
}