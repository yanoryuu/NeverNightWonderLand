// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：パーティクル表示
	/// </summary>
	public class AdvCommandParticle : AdvCommand
	{
		protected string label;
		protected string layerName;
//		protected int sortingOrder;
		protected AdvGraphicInfo graphic;
		protected AdvGraphicOperationArg graphicOperationArg;
		public AdvCommandParticle(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row)
		{
			this.label = ParseCell<string>(AdvColumnName.Arg1);
			string fileName = ParseCellOptional<string>(AdvColumnName.Arg2, label);

			if (!dataManager.ParticleSetting.Dictionary.ContainsKey(fileName))
			{
				Debug.LogError(ToErrorString(fileName + " is not contained in file setting"));
			}

			this.graphic = dataManager.ParticleSetting.LabelToGraphic(fileName);
			if (this.graphic == null)
			{
				Debug.LogError(ToErrorString(fileName + " is not found in Particle sheet"));
			}
			else
			{
				AddLoadGraphic(graphic);
			}
			
			this.layerName = ParseCellOptional<string>(AdvColumnName.Arg3, "");
			if (!string.IsNullOrEmpty(layerName) && !dataManager.LayerSetting.Contains(layerName))
			{
				Debug.LogError(ToErrorString( layerName + " is not contained in layer setting"));
			}

			//グラフィック表示処理を作成
			this.graphicOperationArg =  new AdvGraphicOperationArg(this, graphic, 0 );

//			this.sortingOrder = ParseCellOptional<int>(AdvColumnName.Arg4,0);
		}

		public override void DoCommand(AdvEngine engine)
		{
			string layer = layerName;
			if (string.IsNullOrEmpty(layer))
			{
				//レイヤー名指定なしならスプライトのデフォルトレイヤー
				layer = engine.GraphicManager.SpriteManager.DefaultLayer.name;
			}
			//表示する
			engine.GraphicManager.DrawObject(layer, label, graphicOperationArg);
			//			AdvGraphicObjectParticle particle = obj.TargetObject as AdvGraphicObjectParticle;
			//			particle.AddSortingOrder(sortingOrder,"");

			//基本以外のコマンド引数の適用
			AdvGraphicObject obj = engine.GraphicManager.FindObject(label);
			if (obj != null)
			{
				//位置の適用（Arg4とArg5）
				obj.SetCommandPostion(this);
				//その他の適用（モーション名など）
				obj.TargetObject.SetCommandArg(this);
			}
		}
	}
}
