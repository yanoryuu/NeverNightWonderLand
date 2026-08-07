// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

namespace Utage
{
	/// <summary>
	/// ローカライズの設定
	/// </summary>
	public class AdvLocalizeSetting : AdvSettingBase
	{
		protected override void OnParseGrid(StringGrid grid)
		{
			LanguageManagerBase languageManager = LanguageManagerBase.Instance;
			if(languageManager!=null)
			{
				languageManager.OverwriteData(grid);
			}
		}
	}
}
