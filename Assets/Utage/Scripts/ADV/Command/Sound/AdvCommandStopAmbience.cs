// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

namespace Utage
{

	/// <summary>
	/// コマンド：環境音停止
	/// </summary>
	public class AdvCommandStopAmbience : AdvCommand
	{
		public AdvCommandStopAmbience(StringGridRow row)
			: base(row)
		{
			this.fadeTime = ParseCellOptional<float>(AdvColumnName.Arg6, 0.2f);
		}

		public override void DoCommand(AdvEngine engine)
		{
			engine.SoundManager.StopAmbience(fadeTime);
		}

		float fadeTime;
	}
}
