// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimurausing UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：選択肢追加終了
	/// </summary>
	public class AdvCommandSelectionEnd : AdvCommand
	{
		public AdvCommandSelectionEnd(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
		}

		public override void DoCommand(AdvEngine engine)
		{
			engine.Config.StopSkipInSelection();
			engine.SelectionManager.Show();
		}
	}
}
