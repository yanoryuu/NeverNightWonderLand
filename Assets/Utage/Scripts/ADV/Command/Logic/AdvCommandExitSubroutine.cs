// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimurausing UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：サブルーチンから抜ける。
	/// サブルーチンのコールスタックをクリアして、サブルーチン中という状態を解除する。
	/// サブルーチンの呼び出し元に戻れなくなる。
	/// </summary>
	public class AdvCommandExitSubroutine : AdvCommand
	{
		public AdvCommandExitSubroutine(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
		}

		public override void DoCommand(AdvEngine engine)
		{
			CurrentTread.JumpManager.ExitSubRoutine();
		}
	}
}
