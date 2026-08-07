// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{
	public class SampleCustomCommandWaitInputOrVoice : AdvCustomCommandManager
	{
		public override void OnBootInit()
		{
			Utage.AdvCommandParser.OnCreateCustomCommandFromID += CreateCustomCommand;
		}
		void OnDestroy()
		{
			Utage.AdvCommandParser.OnCreateCustomCommandFromID -= CreateCustomCommand;
		}

		public override void OnClear()
		{
		}
 		
		public void CreateCustomCommand(string id, StringGridRow row, AdvSettingDataManager dataManager, ref AdvCommand command )
		{
			switch (id)
			{
				case "WaitInputOrAutoVoice":
					command = new AdvCommandWaitInputOrAutoVoice(row);
					break;
			}
		}
	}

	public class AdvCommandWaitInputOrAutoVoice : AdvCommand
	{

		public AdvCommandWaitInputOrAutoVoice(StringGridRow row)
			: base(row)
		{
		}

		public override void DoCommand(AdvEngine engine)
		{
			if (CurrentTread.IsMainThread)
			{
				engine.Page.IsWaitingInputCommand = true;
			}
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

		bool IsWaitng(AdvEngine engine)
		{
			if ((engine.Config.IsAutoBrPage))
			{
				//オート中はボイス再生が終わるまでは待機
				return engine.SoundManager.IsPlayingVoice();
			}
			else
			{
				//それ以外は入力があったら待機解除
				if (engine.Page.CheckSkip())
				{
					return false;
				}
				if (engine.UiManager.IsInputTrig)
				{
					return false;
				}

				return true;
			}
		}
	}
}
