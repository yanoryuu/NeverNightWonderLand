// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// コマンド：ランダムにサブルーチンへジャンプ
	/// </summary>
	public class AdvCommandJumpSubroutineRandom : AdvCommand
		, IAdvInitOnCreateEntity
	{
		public AdvCommandJumpSubroutineRandom(StringGridRow row, AdvSettingDataManager dataManager)
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
			this.returnLabel = IsEmptyCell(AdvColumnName.Arg3) ? "" : ParseScenarioLabel(AdvColumnName.Arg3);

			string expRateStr = ParseCellOptional<string>(AdvColumnName.Arg4, "");
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

		//ページ用のデータからコマンドに必要な情報を初期化
		public override void InitFromPageData(AdvScenarioPageData pageData)
		{
			this.scenarioLabel = pageData.ScenarioLabelData.ScenarioLabel;
			this.subroutineCommandIndex = pageData.ScenarioLabelData.CountSubroutineCommandIndex(this);
		}
		//エンティティコマンドとして利用
		public void InitOnCreateEntity(AdvCommand original)
		{
			AdvCommandJumpSubroutineRandom originalCommand = original as AdvCommandJumpSubroutineRandom; 
			this.scenarioLabel = originalCommand.scenarioLabel;
			this.subroutineCommandIndex = originalCommand.subroutineCommandIndex;
		}

		public override string[] GetJumpLabels()
		{
			if (string.IsNullOrEmpty(returnLabel))
			{
				return new string[] { jumpLabel };
			}
			else
			{
				return new string[] { jumpLabel, returnLabel };
			}
		}

		public override void DoCommand(AdvEngine engine)
		{
			if (IsEnable(engine.Param))
			{
				CurrentTread.JumpManager.AddRandom(this, CalcRate(engine.Param));
			}
		}

		internal void DoRandomEnd(AdvScenarioThread thread, AdvEngine engine)
		{
			SubRoutineInfo info = new SubRoutineInfo(engine, this.returnLabel, this.scenarioLabel, this.subroutineCommandIndex);
			thread.JumpManager.RegistoreSubroutine(jumpLabel, info);
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

		// 選択肢終了などの特別なコマンドを自動生成する場合、そのIDを返す
		public override string[] GetExtraCommandIdArray(AdvCommand next)
		{
			if (next != null && (next is AdvCommandJumpSubroutineRandom))
			{
				return null;
			}
			else
			{
				return new string[] { AdvCommandParser.IdJumpSubroutineRandomEnd};
			}
		}

		string scenarioLabel;
		int subroutineCommandIndex;
		string jumpLabel;
		string returnLabel;
		ExpressionParser exp;	//ジャンプ条件式
		ExpressionParser expRate;	//確率割合計算式
	}
}
