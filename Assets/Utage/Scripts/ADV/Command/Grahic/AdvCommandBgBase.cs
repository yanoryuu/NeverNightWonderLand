// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	// コマンド：背景表示・切り替えの基底クラス
	public abstract class AdvCommandBgBase : AdvCommand
	{
		protected string label;
		protected AdvGraphicInfoList graphic;
		protected string layerName;
		protected float fadeTime;

		protected AdvCommandBgBase(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			this.label = ParseCell<string>(AdvColumnName.Arg1);
			if (!dataManager.TextureSetting.ContainsLabel(label))
			{
				Debug.LogError(ToErrorString(label + " is not contained in file setting"));
			}

			this.graphic = dataManager.TextureSetting.LabelToGraphic(label);
			if (this.graphic == null)
			{
				Debug.LogError(ToErrorString(label + " is not found in Texture sheet"));
			}
			else
			{
				AddLoadGraphic(graphic);
			}

			this.layerName = ParseCellOptional<string>(AdvColumnName.Arg3, "");
			if (!string.IsNullOrEmpty(layerName) && !dataManager.LayerSetting.Contains(layerName, AdvLayerSettingData.LayerType.Bg))
			{
				Debug.LogError(ToErrorString(layerName + " is not contained in layer setting"));
			}
			this.fadeTime = ParseCellOptional<float>(AdvColumnName.Arg6, 0.2f);
		}

		protected virtual AdvGraphicOperationArg DoCommandBgSub(AdvEngine engine)
		{
			AdvGraphicOperationArg graphicOperationArg = new AdvGraphicOperationArg(this, graphic.Main, fadeTime);
			//表示する
			if (string.IsNullOrEmpty(layerName))
			{
				engine.GraphicManager.BgManager.DrawToDefault(engine.GraphicManager.BgSpriteName, graphicOperationArg);
			}
			else
			{
				engine.GraphicManager.BgManager.Draw(layerName, engine.GraphicManager.BgSpriteName, graphicOperationArg);
			}

			//基本以外のコマンド引数の適用
			AdvGraphicObject obj = engine.GraphicManager.BgManager.FindObject(engine.GraphicManager.BgSpriteName);
			if (obj != null)
			{
				//位置の適用（Arg4とArg5）
				obj.SetCommandPostion(this);
				//その他の適用（モーション名など）
				obj.TargetObject.SetCommandArg(this);
			}

			return graphicOperationArg;
		}
	}
}
