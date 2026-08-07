// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：パラメーターに数値代入
	/// </summary>
	public class AdvCommandParam : AdvCommand
		,IAdvCommandDelayInitialize
	{

		public AdvCommandParam(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
		}

		//遅延初期化処理
		public void DelayInitialize(AdvSettingDataManager dataManager)
		{
			//Expがまだ初期化されてなかったら初期化する
			if (exp != null) return;

			this.exp = dataManager.DefaultParam.CreateExpression(ParseCell<string>(AdvColumnName.Arg1));
			if (this.exp.ErrorMsg != null)
			{
				Debug.LogError(ToErrorString(this.exp.ErrorMsg));
			}
		}
		
		public override void DoCommand(AdvEngine engine)
		{
			DelayInitialize(engine.DataManager.SettingDataManager);
			engine.Param.CalcExpression(exp);
		}
		ExpressionParser exp;
	}
}
