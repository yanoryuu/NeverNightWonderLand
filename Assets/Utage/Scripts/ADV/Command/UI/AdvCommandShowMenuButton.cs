// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

namespace Utage
{

	/// <summary>
	/// コマンド：メニューボタンを表示
	/// </summary>
	public class AdvCommandShowMenuButton : AdvCommand
	{
		public AdvCommandShowMenuButton(StringGridRow row)
			: base(row)
		{
		}

		public override void DoCommand(AdvEngine engine)
		{
			engine.UiManager.ShowMenuButton();
		}
	}
}
