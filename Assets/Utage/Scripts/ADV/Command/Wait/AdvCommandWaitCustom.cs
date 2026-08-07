// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：待機処理(カスタム入力用)
	/// </summary>
	public class AdvCommandWaitCustom : AdvCommand
	{
		public AdvCommandWaitCustom(StringGridRow row)
			: base(row)
		{
		}

		public override void DoCommand(AdvEngine engine)
		{
		}

		public override bool Wait(AdvEngine engine)
		{
			return !engine.UiManager.IsInputTrigCustom;
		}
	}
}
