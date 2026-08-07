// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimurausing UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：スレッドの終了
	/// </summary>
	public class AdvCommandEndThread : AdvCommand
	{
		public AdvCommandEndThread(StringGridRow row)
			: base(row)
		{
		}

		public override void DoCommand(AdvEngine engine)
		{
			CurrentTread.IsPlaying = false;
		}
	}
}
