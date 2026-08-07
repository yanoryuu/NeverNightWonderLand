// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{
	public class SampleCustomCommandNoFade : AdvCustomCommandManager
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
				case AdvCommandParser.IdCharacter:
					command = new AdvCommandCharacterNoFade(row, dataManager);
					break;
			}
		}
	}

	public class AdvCommandCharacterNoFade : AdvCommandCharacter
	{
		public AdvCommandCharacterNoFade(StringGridRow row, AdvSettingDataManager dataManager)
			:base(row, dataManager)
		{
			this.fadeTime = ParseCellOptional<float>(AdvColumnName.Arg6, 0.0f);
		}
	}
}
