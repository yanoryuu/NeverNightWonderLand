// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;


namespace Utage
{

	/// <summary>
	/// コマンド：MessageWindow操作　ChangeCurrent
	/// </summary>
	public class AdvCommandMessageWindowChangeCurrent : AdvCommand
	{
		public AdvCommandMessageWindowChangeCurrent(StringGridRow row)
			: base(row)
		{
			this.name = ParseCell<string>(AdvColumnName.Arg1);
		}

		//ページ用のデータからコマンドに必要な情報を初期化
		public override void InitFromPageData(AdvScenarioPageData pageData)
		{
			pageData.InitMessageWindowName(this, name);
		}

		public override void DoCommand(AdvEngine engine)
		{
			engine.MessageWindowManager.ChangeCurrentWindow(name);
		}

		string name;
	}
}
