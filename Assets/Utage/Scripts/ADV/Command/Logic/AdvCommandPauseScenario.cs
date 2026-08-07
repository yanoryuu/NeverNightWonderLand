// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

namespace Utage
{

	/// <summary>
	/// コマンド：シナリオ中断
	/// </summary>
	public class AdvCommandPauseScenario : AdvCommand
	{
		public AdvCommandPauseScenario(StringGridRow row)
			: base(row)
		{
		}

		public override void DoCommand(AdvEngine engine)
		{
			engine.ScenarioPlayer.Pause();
		}

		//コマンド終了待ち
		public override bool Wait(AdvEngine engine)
		{
			return engine.ScenarioPlayer.IsPausing;
		}
		//ページ区切り系のコマンドか
		public override bool IsTypePage() { return true; }
		//ページ終端のコマンドか
		public override bool IsTypePageEnd() { return true; }
	}
}
