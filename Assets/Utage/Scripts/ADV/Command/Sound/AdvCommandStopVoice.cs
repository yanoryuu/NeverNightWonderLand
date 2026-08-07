// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

namespace Utage
{

	/// <summary>
	/// コマンド：ボイス停止
	/// </summary>
	public class AdvCommandStopVoice : AdvCommand
	{
		public AdvCommandStopVoice(StringGridRow row)
			: base(row)
		{
			this.fadeTime = ParseCellOptional<float>(AdvColumnName.Arg6, 0.2f);
		}

		public override void DoCommand(AdvEngine engine)
		{
			engine.SoundManager.StopVoice(fadeTime);
		}

		float fadeTime;
	}
}
