// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimurausing UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：サブルーチンの終了
	/// </summary>
	public class AdvCommandEndSubroutine : AdvCommand
	{
		public AdvCommandEndSubroutine(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
		}

		public override void DoCommand(AdvEngine engine)
		{
			CurrentTread.JumpManager.EndSubroutine();
		}
	}
}
