// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

namespace Utage
{

	/// <summary>
	/// コマンド：シーン回想終了
	/// </summary>
	public class AdvCommandEndSceneGallery : AdvCommand
	{
		public AdvCommandEndSceneGallery(StringGridRow row)
			: base(row)
		{
		}

		public override void DoCommand(AdvEngine engine)
		{
			engine.ScenarioPlayer.EndSceneGallery(engine);
			if (engine.IsSceneGallery)
			{
				engine.ScenarioPlayer.IsReservedEndScenario = true;
			}
		}
	}
}
