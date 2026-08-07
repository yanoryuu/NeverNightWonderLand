// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimurausing UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{

	/// <summary>
	/// コマンド：シェイク表示
	/// </summary>
	public class AdvCommandShake : AdvCommandTween
	{
		public AdvCommandShake(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row, dataManager)
		{
		}
		
		//Tweenデータの初期化
		protected override void InitTweenData()
		{
			string defaultStr = " x=30 y=30";
			string arg = ParseCellOptional<string>(AdvColumnName.Arg3, defaultStr);
			if (!arg.Contains("x=") && !arg.Contains("y="))
			{
				arg += defaultStr;
			}
			string easeType = ParseCellOptional<string>(AdvColumnName.Arg4, "");
			string loopType = ParseCellOptional<string>(AdvColumnName.Arg5, "");
			this.tweenData = new iTweenData(iTweenType.ShakePosition.ToString(), arg, easeType, loopType);
		}
	}
}
