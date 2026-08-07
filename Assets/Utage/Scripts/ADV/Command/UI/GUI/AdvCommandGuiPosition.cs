// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;


namespace Utage
{

	/// <summary>
	/// コマンド：GUI操作　Position
	/// </summary>
	public class AdvCommandGuiPosition : AdvCommand
	{
		public AdvCommandGuiPosition(StringGridRow row)
			: base(row)
		{
			this.name = this.ParseCell<string>(AdvColumnName.Arg1);
			this.x = this.ParseCellOptionalNull<float>(AdvColumnName.Arg2);
			this.y = this.ParseCellOptionalNull<float>(AdvColumnName.Arg3);
		}

		public override void DoCommand(AdvEngine engine)
		{
			AdvGuiBase gui;
			if (!engine.UiManager.GuiManager.TryGet(this.name, out gui))
			{
				 Debug.LogError( this.ToErrorString(name + " is not found in GuiManager"));
			}
			else
			{
				gui.SetPosition(x,y);
			}
		}

		string name;
		float? x;
		float? y;
	}
}
