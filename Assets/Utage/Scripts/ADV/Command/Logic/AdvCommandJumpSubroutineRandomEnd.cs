// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimurausing UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：ランダムにサブルーチンへジャンプ終了
	/// </summary>
	public class AdvCommandJumpSubroutineRandomEnd : AdvCommand
	{
		public AdvCommandJumpSubroutineRandomEnd(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
		}
		//ページ区切り系のコマンドか
		public override bool IsTypePage() { return true; }
		//ページ終端のコマンドか
		public override bool IsTypePageEnd() { return true; }

		public override void DoCommand(AdvEngine engine)
		{
			AdvCommandJumpSubroutineRandom command = CurrentTread.JumpManager.GetRandomJumpCommand() as AdvCommandJumpSubroutineRandom;
			if (command != null)
			{
				command.DoRandomEnd(CurrentTread,engine);
			}
		}
	}
}
