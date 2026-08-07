// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimurausing UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：別スレッド作成
	/// </summary>
	public class AdvCommandThread : AdvCommand
	{
		public AdvCommandThread(StringGridRow row)
			: base(row)
		{
			this.label = ParseScenarioLabel(AdvColumnName.Arg1);
//			this.name = ParseCellOptional<string>(AdvColumnName.Arg2, this.label);
		}


		public override void DoCommand(AdvEngine engine)
		{
			CurrentTread.StartSubThread(label);
		}

		string label;
//		string name;
	}
}
