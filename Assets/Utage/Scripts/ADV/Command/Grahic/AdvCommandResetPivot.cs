// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{
	// コマンド：ピボットのリセット
	public class AdvCommandResetPivot : AdvCommand
	{
		private readonly string targetName;
		public AdvCommandResetPivot(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			targetName = this.ParseCell<string>(AdvColumnName.Arg1);
		}

		public override void DoCommand(AdvEngine engine)
		{
			var target = engine.GraphicManager.FindObject(targetName);
			if (target == null)
			{
				Debug.LogError( targetName + " is not found");
				return;
			}
			target.ResetPivot();
		}
	}
}
