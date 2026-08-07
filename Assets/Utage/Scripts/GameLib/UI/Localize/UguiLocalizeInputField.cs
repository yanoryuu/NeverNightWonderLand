// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{
	// InputFieldの表示言語切り替え用のクラス
	// 初期値として設定されているテキストをローカライズするためのもの
	//　ユーザーが入力後に言語が変更されると、入力内容が消えてローカライズされたテキストに上書きされるので注意
	[ExecuteInEditMode]
	[AddComponentMenu("Utage/Lib/UI/UguiLocalizeInputField")]
	public class UguiLocalizeInputField : UguiLocalizeBase
	{
		public string Key
		{
			set { key = value; ForceRefresh(); }
			get { return key; }
		}
		[SerializeField]
		protected string key;

		[NonSerialized]
		protected string defaultText;

		protected InputField InputField => this.GetComponentCache(ref inputField);
		InputField inputField;
		protected TMP_InputField InputFieldTmp => this.GetComponentCache(ref inputFieldTmp);
		TMP_InputField inputFieldTmp;
		
		protected virtual void SetText(string text)
		{
			if(InputField != null)
			{
				InputField.text = text;
			}
			else if(InputFieldTmp != null)
			{
				InputFieldTmp.text = text;
			}
		}

		protected virtual string GetText()
		{
			if(InputField != null)
			{
				return InputField.text;
			}
			else if(InputFieldTmp != null)
			{
				return InputFieldTmp.text;
			}
			return "";
		}
		
		protected override void RefreshSub()
		{
			if (!LanguageManagerBase.Instance.IgnoreLocalizeUiText )
			{
				if (LanguageManagerBase.Instance.TryLocalizeText(key, out string str))
				{
					SetText(str);
				}
				else
				{
					Debug.LogError(key + " is not found in localize key" , this);
				}
			}
		}

		protected override void InitDefault()
		{
			defaultText = GetText();
		}
		public override void ResetDefault()
		{
			SetText(defaultText);
		}
	}
}

