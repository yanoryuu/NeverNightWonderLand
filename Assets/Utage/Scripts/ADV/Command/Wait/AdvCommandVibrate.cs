// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimurausing UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：バイブレーションを作動
	/// </summary>
	public class AdvCommandVibrate : AdvCommand
	{
		public AdvCommandVibrate(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
		}

		public override void DoCommand(AdvEngine engine)
		{
#if (UNITY_IPHONE || UNITY_ANDROID) && !UTAGE_IGNORE_VIBRATE
			Handheld.Vibrate();
#endif
		}
	}
}
