// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using  UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：スプライト表示
	/// </summary>
	public class AdvCommandSpriteOff : AdvCommand
	{
		string name;
		float fadeTime = 0.2f;

		public AdvCommandSpriteOff(StringGridRow row)
			: base(row)
		{
			this.name = ParseCellOptional<string>(AdvColumnName.Arg1, AdvCommandKeyword.AllSpriteLayers);
			this.fadeTime = ParseCellOptional<float>(AdvColumnName.Arg6, fadeTime);
		}

		public override void DoCommand(AdvEngine engine)
		{
			float time = engine.Page.ToSkippedTime(this.fadeTime);
			switch (name)
			{
				case AdvCommandKeyword.AllSpriteLayers:
					engine.GraphicManager.SpriteManager.FadeOutAll(time);
					break;
				case AdvCommandKeyword.AllSpriteObjects:
					engine.GraphicManager.FadeOutAllObjects(AdvGraphicObjectType.Sprite,time);
					break;
				default:
					//オブジェクト名からレイヤーを探す
					AdvGraphicLayer layer = engine.GraphicManager.FindLayerByObjectName(name);
					if (layer != null)
					{
						//指定のオブジェクトを消す
						layer.FadeOut(name, time);
					}
					else
					{
						//レイヤー名として、レイヤー以下のスプライトを消す
						layer = engine.GraphicManager.FindLayer(name);
						if (layer != null)
						{
							//消す
							layer.FadeOutAllObjects(AdvGraphicObjectType.Sprite,time);
						}
						else
						{
							Debug.LogError("Not found " + name + " Please input sprite name or layer name");
						}
					}
					break;
			}
		}
	}
}
