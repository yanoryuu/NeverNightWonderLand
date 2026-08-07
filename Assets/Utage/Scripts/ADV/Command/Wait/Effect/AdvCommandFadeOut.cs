// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：フェードアウト処理
	/// </summary>
	public class AdvCommandFadeOut : AdvCommandFadeBase
	{
		public AdvCommandFadeOut(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row, dataManager,false)
		{
		}
	}
}
