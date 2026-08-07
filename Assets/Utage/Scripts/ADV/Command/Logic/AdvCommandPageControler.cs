// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimurausing UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：ページ制御
	/// </summary>
	public class AdvCommandPageControler : AdvCommand
	{
		public AdvCommandPageControler(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			if (row == null)
			{
				this.pageCtrlType = AdvPageControllerType.InputBrPage;
			}
			else
			{
				this.pageCtrlType = ParseCellOptional<AdvPageControllerType>(AdvColumnName.PageCtrl, AdvPageControllerType.InputBrPage);
			}
		}
		
		public override void DoCommand(AdvEngine engine)
		{
			engine.Page.UpdatePageTextData (pageCtrlType);
		}
		
		public override bool Wait(AdvEngine engine)
		{
			return engine.Page.IsWaitTextCommand;
		}

		//ページ区切り系のコマンドか
		public override bool IsTypePage() { return true; }
		//ページ終端のコマンドか
		public override bool IsTypePageEnd() { return AdvPageController.IsPageEndType(pageCtrlType); }
		AdvPageControllerType pageCtrlType;
	}
}
