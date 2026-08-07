// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：
	/// </summary>
	public class AdvCommandWaitInput : AdvCommand
	{

		public AdvCommandWaitInput(StringGridRow row)
			: base(row)
		{
			this.time = this.ParseCellOptional<float>(AdvColumnName.Arg6,-1);
		}

		public override void DoCommand(AdvEngine engine)
		{
			if (CurrentTread.IsMainThread)
			{
				engine.Page.IsWaitingInputCommand = true;
			}
			waitEndTime = engine.Time.Time + (engine.Page.CheckSkip() ? time / engine.Config.SkipSpped : time);
		}

		public override bool Wait(AdvEngine engine)
		{
			bool waiting = IsWaitng(engine);
			if (waiting)
			{
				return true;
			}
			else
			{
				//ボイスを止める
				if (engine.Config.VoiceStopType == VoiceStopType.OnClick)
				{
					//ループじゃないボイスを止める
					engine.SoundManager.StopVoiceIgnoreLoop();
				}
				engine.UiManager.ClearPointerDown();
				if (CurrentTread.IsMainThread)
				{
					engine.Page.IsWaitingInputCommand = false;
				}
				return false;
			}
		}

		protected virtual bool IsWaitng(AdvEngine engine)
		{
			if (engine.Page.CheckSkip())
			{
				return false;
			}
			if (engine.UiManager.IsInputTrig)
			{
				return false;
			}
			if (this.time > 0)
			{
				return (engine.Time.Time < waitEndTime);
			}

			return true;
		}

		protected float time;
		protected float waitEndTime;
	}
}
