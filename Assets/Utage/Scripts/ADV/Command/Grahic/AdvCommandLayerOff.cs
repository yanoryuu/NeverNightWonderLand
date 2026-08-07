// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：レイヤー操作　オブジェクトすべて消す
	/// </summary>
	public class AdvCommandLayerOff : AdvCommand
	{
		public AdvCommandLayerOff(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			this.name = ParseCell<string>(AdvColumnName.Arg1);
			if (!dataManager.LayerSetting.Contains(name))
			{
				Debug.LogError( row.ToErrorString( "Not found " + name + " Please input Layer name") );
			}
			//フェード時間
			this.fadeTime = ParseCellOptional<float>(AdvColumnName.Arg6, 0.2f);
		}

		public override void DoCommand(AdvEngine engine)
		{
			//オブジェクト名からレイヤーを探す
			AdvGraphicLayer layer = engine.GraphicManager.FindLayer(name);
			if (layer != null)
			{
				//消す
				layer.FadeOutAll(engine.Page.ToSkippedTime(this.fadeTime));
			}
			else
			{
				Debug.LogError("Not found " + name + " Please input Layer name");
			}
		}

		string name;
		float fadeTime;
	}
}
