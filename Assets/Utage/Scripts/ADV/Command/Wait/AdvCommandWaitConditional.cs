// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	// コマンド：待機処理(条件文)
	public class AdvCommandWaitConditional : AdvCommand
	{
		readonly float time;
		float waitEndTime;
		readonly ExpressionParser conditionalExp;				//条件式
		public AdvCommandWaitConditional(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			string expStr = ParseCell<string>(AdvColumnName.Arg1);
			this.conditionalExp = dataManager.DefaultParam.CreateExpressionBoolean(expStr);
			if (this.conditionalExp.ErrorMsg != null)
			{
				Debug.LogError(ToErrorString(this.conditionalExp.ErrorMsg));
			}
			this.time = this.ParseCellOptional<float>(AdvColumnName.Arg6,-1);
		}

		public override void DoCommand(AdvEngine engine)
		{
			waitEndTime = engine.Time.Time + (engine.Page.CheckSkip() ? time / engine.Config.SkipSpped : time);
		}

		public override bool Wait(AdvEngine engine)
		{
			return IsWaitingTime(engine) || engine.Param.CalcExpressionBoolean(conditionalExp);
		}

		protected virtual bool IsWaitingTime(AdvEngine engine)
		{
			if (this.time > 0)
			{
				return (engine.Time.Time < waitEndTime);
			}
			return false;
		}
	}
}
