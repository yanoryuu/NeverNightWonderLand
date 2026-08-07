// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimurausing UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：ランダムジャンプ終了
	/// </summary>
	public class AdvCommandJumpRandomEnd : AdvCommand
	{
		public AdvCommandJumpRandomEnd(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
		}

		public override void DoCommand(AdvEngine engine)
		{
			AdvCommandJumpRandom command = CurrentTread.JumpManager.GetRandomJumpCommand() as AdvCommandJumpRandom;
			if(command!=null )
			{
				command.DoRandomEnd(engine, CurrentTread);
			}
		}
	}
}
