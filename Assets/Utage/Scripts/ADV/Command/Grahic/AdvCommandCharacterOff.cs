// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

namespace Utage
{

	/// <summary>
	/// コマンド：キャラクター＆台詞表示
	/// </summary>
	public class AdvCommandCharacterOff : AdvCommand
	{

		public AdvCommandCharacterOff(StringGridRow row)
			: base(row)
		{
			this.name = ParseCellOptional<string>(AdvColumnName.Arg1, "");
			//フェード時間
			this.time = ParseCellOptional<float>(AdvColumnName.Arg6, 0.2f);
		}

		public override void DoCommand(AdvEngine engine)
		{
			float fadeTime = engine.Page.ToSkippedTime(time);
			AdvGraphicGroup characterManager = engine.GraphicManager.CharacterManager;
			if (string.IsNullOrEmpty(name))
			{
				characterManager.FadeOutAll(fadeTime );
			}
			else
			{
				//オブジェクト名からレイヤーを探す
				AdvGraphicLayer layer = characterManager.FindLayerFromObjectName(name);
				if (layer != null)
				{
					//指定のオブジェクトを消す
					layer.FadeOut(name, fadeTime);
				}
				else
				{
					//レイヤー名として検索
					layer = characterManager.FindLayer(name);
					if (layer != null)
					{
						//レイヤー全てを消す
						layer.FadeOutAll(fadeTime);
					}
				}
			}
		}

		string name;
		float time;
	}

}
