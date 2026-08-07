// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	// コマンド：背景表示・切り替え
	public class AdvCommandBg : AdvCommandBgBase
	{
		public AdvCommandBg(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row,dataManager)
		{
		}

		public override void DoCommand(AdvEngine engine)
		{
			engine.GraphicManager.IsEventMode = false;
			DoCommandBgSub(engine);
		}
	}
}
