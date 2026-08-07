// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// コマンド：選択肢表示
	/// </summary>
	public class AdvCommandSelection 
		: AdvCommand
			, IAdvCommandTexts
	{
		string jumpLabel;
		ExpressionParser exp; //選択肢表示条件式
		ExpressionParser selectedExp; //選択後に実行する演算式
		string prefabName;
		float? x;
		float? y;

		public AdvCommandSelection(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			this.jumpLabel = ParseScenarioLabel(AdvColumnName.Arg1);
			string expStr = ParseCellOptional<string>(AdvColumnName.Arg2, "");
			if( string.IsNullOrEmpty(expStr) )
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

			string selectedExpStr = ParseCellOptional<string>(AdvColumnName.Arg3, "");
			if (string.IsNullOrEmpty(selectedExpStr))
			{
				this.selectedExp = null;
			}
			else
			{
				this.selectedExp = dataManager.DefaultParam.CreateExpression(selectedExpStr);
				if (this.selectedExp.ErrorMsg != null)
				{
					Debug.LogError(ToErrorString(this.selectedExp.ErrorMsg));
				}
			}
			this.prefabName = ParseCellOptional<string>(AdvColumnName.Arg4, "");
			this.x = ParseCellOptionalNull<float>(AdvColumnName.Arg5);
			this.y = ParseCellOptionalNull<float>(AdvColumnName.Arg6);

			//エディター用のチェック
			if (AdvCommand.IsEditorErrorCheck)
			{
				TextData textData = new TextData(ParseCellLocalizedText());
				if (!string.IsNullOrEmpty(textData.ErrorMsg))
				{
					Debug.LogError(ToErrorString(textData.ErrorMsg));
				}
			}
		}

		public override void DoCommand(AdvEngine engine)
		{
			if (exp == null || engine.Param.CalcExpressionBoolean(exp))
			{
				engine.SelectionManager.AddSelection(jumpLabel, ParseCellLocalizedText(), selectedExp, prefabName, x, y, RowData);
			}
		}

		public override string[] GetJumpLabels() { return new string[] { jumpLabel }; }
		// 選択肢終了などの特別なコマンドを自動生成する場合、そのIDを返す
		public override string[] GetExtraCommandIdArray(AdvCommand next)
		{
			if (next != null && ( (next is AdvCommandSelection) || (next is AdvCommandSelectionClick)) )
			{
				return null;
			}
			else
			{
				if (AdvPageController.IsPageEndType(ParseCellOptional<AdvPageControllerType>(AdvColumnName.PageCtrl, AdvPageControllerType.InputBrPage)))
				{
					return new string[] { AdvCommandParser.IdSelectionEnd, AdvCommandParser.IdPageControler };
				}
				else
				{
					return new string[] { AdvCommandParser.IdSelectionEnd };
				}
			}
		}

		//ページ区切り系のコマンドか
		public override bool IsTypePage()
		{
			return true;
		}

		public IEnumerable<string> GetTextStrings()
		{
			yield return ParseCellLocalizedText();
		}
	}
}
