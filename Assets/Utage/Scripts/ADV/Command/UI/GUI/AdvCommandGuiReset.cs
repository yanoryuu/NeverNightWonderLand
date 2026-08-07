// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;


namespace Utage
{

	/// <summary>
	/// コマンド：GUI操作　Reset
	/// </summary>
	public class AdvCommandGuiReset : AdvCommand
	{
		public AdvCommandGuiReset(StringGridRow row)
			: base(row)
		{
			this.name = this.ParseCellOptional<string>(AdvColumnName.Arg1, "");
		}

		public override void DoCommand(AdvEngine engine)
		{
			if (string.IsNullOrEmpty(name))
			{
				foreach (var obj in engine.UiManager.GuiManager.Objects.Values)
				{
					obj.Reset();
				}
			}
			else
			{
				AdvGuiBase gui;
				if (!engine.UiManager.GuiManager.TryGet(this.name, out gui))
				{
					Debug.LogError(this.ToErrorString(name + " is not found in GuiManager"));
				}
				else
				{
					gui.Reset();
				}
			}
		}

		string name;
	}
}
