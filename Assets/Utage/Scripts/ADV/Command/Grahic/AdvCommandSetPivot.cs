// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{
	// コマンド：ピボットの設定
	public class AdvCommandSetPivot : AdvCommand
	{
		private readonly string targetName;
		private readonly float pivotX;
		private readonly float pivotY;
		private readonly float x;
		private readonly float y;
		private readonly AdvGraphicObjectPivotType pivotType;

		public AdvCommandSetPivot(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			targetName = this.ParseCell<string>(AdvColumnName.Arg1);

			string strPivotX = ParseCell<string>(AdvColumnName.Arg2);
			switch (strPivotX)
			{
				case AdvCommandKeyword.Left:
					pivotX = 0.0f;
					break;
				case AdvCommandKeyword.Center:
					pivotX = 0.5f;
					break;
				case AdvCommandKeyword.Right:
					pivotX = 1.0f;
					break;
				default:
					pivotX = this.ParseCell<float>(AdvColumnName.Arg2);
					break;
			}

			string strPivotY = ParseCell<string>(AdvColumnName.Arg3);
			switch (strPivotY)
			{
				case AdvCommandKeyword.Bottom:
					pivotY = 0.0f;
					break;
				case AdvCommandKeyword.Center:
					pivotY = 0.5f;
					break;
				case AdvCommandKeyword.Top:
					pivotY = 1.0f;
					break;
				default:
					pivotY = this.ParseCell<float>(AdvColumnName.Arg3);
					break;
			}
			
			x = this.ParseCellOptional<float>(AdvColumnName.Arg4,0.0f);
			y = this.ParseCellOptional<float>(AdvColumnName.Arg5,0.0f);
			pivotType = this.ParseCellOptional(AdvColumnName.Arg6,AdvGraphicObjectPivotType.SpritePos);
		}

		public override void DoCommand(AdvEngine engine)
		{
			var target = engine.GraphicManager.FindObject(targetName);
			if (target == null)
			{
				Debug.LogError( targetName + " is not found");
				return;
			}
			target.SetPivot(pivotX,pivotY, x,y,pivotType);
		}
	}
}
