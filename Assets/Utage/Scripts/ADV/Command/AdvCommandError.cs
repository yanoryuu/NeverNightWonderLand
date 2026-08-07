// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

namespace Utage
{

	/// <summary>
	/// コマンド：Errorコマンド
	/// </summary>
	public class AdvCommandError : AdvCommand
	{

		public AdvCommandError(StringGridRow row)
			: base(row)
		{
		}

		public override void DoCommand(AdvEngine engine)
		{
		}
	}
}
