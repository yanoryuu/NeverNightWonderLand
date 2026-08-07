// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;


namespace Utage
{

	/// <summary>
	/// コマンド：MessageWindow操作　初期化
	/// </summary>
	public class AdvCommandMessageWindowInit : AdvCommand
	{
		List<string> names = new List<string>();
		public AdvCommandMessageWindowInit(StringGridRow row)
			: base(row)
		{
			if (!IsEmptyCell(AdvColumnName.Arg1)) AddName(ParseCell<string>(AdvColumnName.Arg1));
			if (!IsEmptyCell(AdvColumnName.Arg2)) AddName(ParseCell<string>(AdvColumnName.Arg2));
			if (!IsEmptyCell(AdvColumnName.Arg3)) AddName(ParseCell<string>(AdvColumnName.Arg3));
			if (!IsEmptyCell(AdvColumnName.Arg4)) AddName(ParseCell<string>(AdvColumnName.Arg4));
			if (!IsEmptyCell(AdvColumnName.Arg5)) AddName(ParseCell<string>(AdvColumnName.Arg5));
			if (!IsEmptyCell(AdvColumnName.Arg6)) AddName(ParseCell<string>(AdvColumnName.Arg6));
			if (names.Count <= 0)
			{
				Debug.LogError(ToErrorString("Not set data in this command"));
			}
		}

		void AddName(string name)
		{
			if (names.Contains(name))
			{
				Debug.LogError(ToErrorString( name  +" is duplicated. You cannot use the same message windows name more than once."));
				return;
			}
			names.Add(name);
		}

		//ページ用のデータからコマンドに必要な情報を初期化
		public override void InitFromPageData(AdvScenarioPageData pageData)
		{
			if (names.Count > 0)
			{
				pageData.InitMessageWindowName(this, names[0]);
			}
		}

		public override void DoCommand(AdvEngine engine)
		{
			engine.MessageWindowManager.ChangeActiveWindows(names);
			engine.MessageWindowManager.ChangeCurrentWindow(engine.Page.CurrentData.MessageWindowName);
		}
	}
}
