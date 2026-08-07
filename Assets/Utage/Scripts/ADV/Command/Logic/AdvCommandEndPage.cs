// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimurausing UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：ページ終了コマンド
	/// </summary>
	public class AdvCommandEndPage : AdvCommand
	{
		public AdvCommandEndPage(StringGridRow row)
			: base(row)
		{
		}

		public override void DoCommand(AdvEngine engine)
		{
		}
	}
}
