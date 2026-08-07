// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
	/// <summary>
	/// 表示言語切り替え用のクラス
	/// </summary>
	[CreateAssetMenu(menuName = "Utage/Language/" + nameof(LanguageManager))]
	public class LanguageManager : LanguageManagerBase
	{
		protected override void OnRefreshCurrentLanguage()
		{
			if (!IgnoreLocalizeUiText)
			{
				UguiLocalizeBase[] localizeTbl = WrapperFindObject.FindObjectsOfType<UguiLocalizeBase>();
				foreach (var item in localizeTbl)
				{
					item.OnLocalize();
				}
			}
		}
	}
}
