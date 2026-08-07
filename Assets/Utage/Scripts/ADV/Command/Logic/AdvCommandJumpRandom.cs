// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// コマンド：ランダムジャンプ
	/// </summary>
	public class AdvCommandJumpRandom : AdvCommand
	{
		public AdvCommandJumpRandom(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			this.jumpLabel = ParseScenarioLabel(AdvColumnName.Arg1);
			string expStr = ParseCellOptional<string>(AdvColumnName.Arg2, "");
			if (string.IsNullOrEmpty(expStr))
			{
				this.exp = null;
			}
			else
			{
				this.exp = dataManager.DefaultParam.CreateExpressionBoolean(expStr);
				if (this.exp.ErrorMsg != null)
				{
					Debug.LogError(ToErrorString(this.exp.ErrorMsg));
				}
			}
			string expRateStr = ParseCellOptional<string>(AdvColumnName.Arg3, "");
			if (string.IsNullOrEmpty(expRateStr))
			{
				this.expRate = null;
			}
			else
			{
				this.expRate = dataManager.DefaultParam.CreateExpression(expRateStr);
				if (this.expRate.ErrorMsg != null)
				{
					Debug.LogError(ToErrorString(this.expRate.ErrorMsg));
				}
			}
		}


		public override void DoCommand(AdvEngine engine)
		{
			if (IsEnable(engine.Param))
			{
				CurrentTread.JumpManager.AddRandom(this, CalcRate(engine.Param));
			}
		}

		internal void DoRandomEnd(AdvEngine engine, AdvScenarioThread thread)
		{
			if (!string.IsNullOrEmpty(jumpLabel))
			{
				thread.JumpManager.ClearOnJump();
				thread.JumpManager.RegistoreLabel(jumpLabel);
			}
		}

		bool IsEnable( AdvParamManager param )
		{
			return (exp == null || param.CalcExpressionBoolean( exp ) );
		}

		float CalcRate( AdvParamManager param )
		{
			if (expRate == null)
			{
				return 1;
			}
			else
			{
				return param.CalcExpressionFloat(expRate);
			}
		}

		public override string[] GetJumpLabels() { return new string[]{jumpLabel}; }

		// 選択肢終了などの特別なコマンドを自動生成する場合、そのIDを返す
		public override string[] GetExtraCommandIdArray(AdvCommand next)
		{
			if (next != null && (next is AdvCommandJumpRandom))
			{
				return null;
			}
			else
			{
				return new string[] { AdvCommandParser.IdJumpRandomEnd };
			}
		}
		
		string jumpLabel;
		ExpressionParser exp;	//ジャンプ条件式
		ExpressionParser expRate;	//確率割合計算式
	}
}
