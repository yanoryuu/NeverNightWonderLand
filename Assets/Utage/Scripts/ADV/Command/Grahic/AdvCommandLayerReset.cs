// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：レイヤー操作　Reset（位置なども初期状態に戻す）
	/// </summary>
	public class AdvCommandLayerReset : AdvCommand
	{
		string name;
		public AdvCommandLayerReset(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			this.name = ParseCell<string>(AdvColumnName.Arg1);
			if ( name != AdvCommandKeyword.All  && !dataManager.LayerSetting.Contains(name))
			{
				Debug.LogError(row.ToErrorString("Not found " + name + " Please input Layer name"));
			}
		}

		public override void DoCommand(AdvEngine engine)
		{
			if (name == AdvCommandKeyword.All)
			{
				engine.GraphicManager.ResetAllLayerRectTransform();
				return;
			}

			//オブジェクト名からレイヤーを探す
			AdvGraphicLayer layer = engine.GraphicManager.FindLayer(name);
			if (layer != null)
			{
				//リセットして初期状態に
				layer.ResetCanvasRectTransform();
			}
			else
			{
				Debug.LogError("Not found " + name + " Please input Layer name");
			}
		}
	}
}
