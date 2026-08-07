// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

namespace Utage
{

	/// <summary>
	/// コマンド：END_IF処理
	/// </summary>
	public class AdvCommandEndIf : AdvCommand, IAdvCommandEndIf
	{

		public AdvCommandEndIf(StringGridRow row)
			: base(row)
		{
		}

		public override void DoCommand(AdvEngine engine)
		{
			CurrentTread.IfManager.EndIf();
		}

		//IF文タイプのコマンドか
		public override bool IsIfCommand { get { return true; } }
	}
}
