// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：スプライト表示
	/// </summary>
	public class AdvCommandSprite : AdvCommand
	{
		public AdvCommandSprite(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			this.spriteName = ParseCell<string>(AdvColumnName.Arg1);
			string fileName = ParseCellOptional<string>(AdvColumnName.Arg2, spriteName);

			if (!dataManager.TextureSetting.ContainsLabel(fileName))
			{
				Debug.LogError(ToErrorString(fileName + " is not contained in file setting"));
			}

			this.graphic = dataManager.TextureSetting.LabelToGraphic(fileName);
			if (this.graphic == null)
			{
				Debug.LogError(ToErrorString(fileName + " is not found in Texture sheet"));
			}
			else
			{
				AddLoadGraphic(graphic);
			}
			this.layerName = ParseCellOptional<string>(AdvColumnName.Arg3, "");
			if (string.IsNullOrEmpty(layerName))
			{
				layerName = dataManager.LayerSetting.FindDefaultLayer(AdvLayerSettingData.LayerType.Sprite).Name;
			}
			else if (!dataManager.LayerSetting.Contains(layerName))
			{
				Debug.LogError(ToErrorString( layerName + " is not contained in layer setting"));
			}

			this.fadeTime = ParseCellOptional<float>(AdvColumnName.Arg6, 0.2f);
		}

		public override void DoCommand(AdvEngine engine)
		{
			//グラフィック表示処理を作成
			AdvGraphicOperationArg graphicOperationArg = new AdvGraphicOperationArg(this, graphic.Main, fadeTime);
			//表示する
			engine.GraphicManager.DrawObject(layerName, spriteName, graphicOperationArg);

			//基本以外のコマンド引数の適用
			AdvGraphicObject obj = engine.GraphicManager.FindObject(spriteName);
			if (obj != null)
			{
				//位置の適用（Arg4とArg5）
				obj.SetCommandPostion(this);
				//その他の適用（モーション名など）
				obj.TargetObject.SetCommandArg(this);
			}
		}

		protected AdvGraphicInfoList graphic;
		protected string layerName;
		protected string spriteName;
		protected float fadeTime;
	}
}
