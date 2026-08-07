// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// コマンド：サブルーチンにジャンプ
	/// </summary>
	public class AdvCommandJumpSubroutine : AdvCommand
	, IAdvInitOnCreateEntity
	{
		public AdvCommandJumpSubroutine(StringGridRow row, AdvSettingDataManager dataManager)
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
			this.returnLabel = IsEmptyCell(AdvColumnName.Arg3) ? ""  : ParseScenarioLabel(AdvColumnName.Arg3);
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
			AdvCommandJumpSubroutine originalCommand = original as AdvCommandJumpSubroutine; 
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
				SubRoutineInfo info = new SubRoutineInfo( engine, this.returnLabel, this.scenarioLabel, this.subroutineCommandIndex);
				CurrentTread.JumpManager.RegistoreSubroutine(jumpLabel, info);
			}
		}

		//ページ区切り系のコマンドか
		public override bool IsTypePage() { return true; }
		//ページ終端のコマンドか
		public override bool IsTypePageEnd() { return true; }


		bool IsEnable( AdvParamManager param )
		{
			return (exp == null || param.CalcExpressionBoolean( exp ) );
		}

		string scenarioLabel;
		int subroutineCommandIndex;
		string jumpLabel;
		string returnLabel;
		ExpressionParser exp;	//ジャンプ条件式
	}
}
