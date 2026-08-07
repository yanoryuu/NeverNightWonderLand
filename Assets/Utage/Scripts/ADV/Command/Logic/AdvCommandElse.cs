// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

namespace Utage
{

	/// <summary>
	/// コマンド：ELSE処理
	/// </summary>
	public class AdvCommandElse : AdvCommand, IAdvCommandElse
	{

		public AdvCommandElse(StringGridRow row)
			: base(row)
		{
		}

		public override void DoCommand(AdvEngine engine)
		{
			CurrentTread.IfManager.Else();
		}

		//IF文タイプのコマンドか
		public override bool IsIfCommand { get { return true; } }
	}
}
