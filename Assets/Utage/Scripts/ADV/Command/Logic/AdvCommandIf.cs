// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{
	/// <summary>
	/// コマンド：IF処理
	/// </summary>
	public class AdvCommandIf : AdvCommand
		, IAdvCommandIf
		, IAdvCommandDelayInitialize
	{

		public AdvCommandIf(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
		}

		//遅延初期化処理
		public void DelayInitialize(AdvSettingDataManager dataManager)
		{
			//Expがまだ初期化されてなかったら初期化する
			if (exp != null) return;

			this.exp = dataManager.DefaultParam.CreateExpressionBoolean(ParseCell<string>(AdvColumnName.Arg1));
			if (this.exp.ErrorMsg != null)
			{
				Debug.LogError(ToErrorString(this.exp.ErrorMsg));
			}
		}

		public override void DoCommand(AdvEngine engine)
		{
			DelayInitialize(engine.DataManager.SettingDataManager);
			CurrentTread.IfManager.BeginIf(engine.Param, exp);
		}

		//IF文タイプのコマンドか
		public override bool IsIfCommand { get { return true; } }

		ExpressionParser exp;
	}
}
