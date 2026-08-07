// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

namespace Utage
{

	/// <summary>
	/// コマンド：シナリオ終了
	/// </summary>
	public class AdvCommandEndScenario : AdvCommand
	{
		public AdvCommandEndScenario(StringGridRow row)
			: base(row)
		{
		}

		public override void DoCommand(AdvEngine engine)
		{
			engine.ScenarioPlayer.IsReservedEndScenario = true;
		}
	}
}
