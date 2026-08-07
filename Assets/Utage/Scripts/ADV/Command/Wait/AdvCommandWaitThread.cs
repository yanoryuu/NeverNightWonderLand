// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimurausing UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：スレッドの終了を待つ
	/// </summary>
	public class AdvCommandWaitThread : AdvCommand
	{
		public AdvCommandWaitThread(StringGridRow row)
			: base(row)
		{
			this.label = ParseScenarioLabel(AdvColumnName.Arg1);
			this.cancelInput = ParseCellOptional<bool>(AdvColumnName.Arg2,false);
		}


		public override void DoCommand(AdvEngine engine)
		{
			if (cancelInput)
			{
				engine.Page.IsWaitingInputCommand = true;
			}
		}

		public override bool Wait(AdvEngine engine)
		{
			bool wait = IsWaiting(engine);
			CurrentTread.SetWaitingSubTread(this.label, wait);
			if (wait)
			{
				return true;
			}
			else
			{
				if (cancelInput)
				{
					engine.Page.IsWaitingInputCommand = false;
				}
				return false;
			}
		}

		bool IsWaiting(AdvEngine engine)
		{
			if (cancelInput && engine.UiManager.IsInputTrig || engine.Page.CheckSkip())
			{
				CurrentTread.CancelSubThread(label);
				return false;
			}

			return CurrentTread.IsPlayingSubThread(label);
		}

		string label;
		bool cancelInput;
	}
}
