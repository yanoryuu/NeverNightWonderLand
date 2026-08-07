// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// テキストの表示言語切り替え用のクラス
	/// </summary>
	[ExecuteInEditMode]
	[AddComponentMenu("Utage/Lib/UI/UguiLocalize")]
	public class UguiLocalize : UguiLocalizeBase
		, IUsingTextMeshProAndLegacyText
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

		protected Text CachedText => this.GetComponentCache(ref cachedText);
		Text cachedText;

		protected TextMeshProUGUI CachedTmp => this.GetComponentCache(ref cachedTmp);
		TextMeshProUGUI cachedTmp;

		//TextComponentを取得
		protected Component GetTextComponent()
		{
			return TextComponentWrapper.GetTextComponentCache(this, ref cachedText, ref cachedTmp);
		}

		protected virtual void SetText(string text)
		{
			TextComponentWrapper.SetText(GetTextComponent(), text);
		}

		protected virtual string GetText()
		{
			return TextComponentWrapper.GetText(GetTextComponent());
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

