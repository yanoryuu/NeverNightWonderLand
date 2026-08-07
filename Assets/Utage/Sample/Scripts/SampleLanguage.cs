// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{
	public class SampleLanguage : MonoBehaviour
	{
		//指定の言語に切り替え
		public void ChangeLanguage(string language)
		{
			LanguageManagerBase langManager = LanguageManagerBase.Instance;
			if (langManager == null) return;
			if (langManager.Languages.Count < 1) return;

			langManager.CurrentLanguage = language;
		}
	}
}

