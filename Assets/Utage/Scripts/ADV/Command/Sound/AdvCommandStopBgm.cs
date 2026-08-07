// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

namespace Utage
{

	/// <summary>
	/// コマンド：BGM停止
	/// </summary>
	public class AdvCommandStopBgm : AdvCommand
	{
		public AdvCommandStopBgm(StringGridRow row)
			: base(row)
		{
			this.fadeTime = ParseCellOptional<float>(AdvColumnName.Arg6, 0.2f);
		}

		public override void DoCommand(AdvEngine engine)
		{
			engine.SoundManager.StopBgm(fadeTime);
		}

		float fadeTime;
	}
}
