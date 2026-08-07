// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// コマンド：パーティクル表示
	/// </summary>
	public class AdvCommandParticleOff : AdvCommand
	{
		string name;
		AdvParticleStopType stopType;
		public AdvCommandParticleOff(StringGridRow row)
			: base(row)
		{
			this.name = ParseCellOptional<string>(AdvColumnName.Arg1, "");
			this.stopType = ParseCellOptional<AdvParticleStopType>(AdvColumnName.Arg2, AdvParticleStopType.Default);
		}

		public override void DoCommand(AdvEngine engine)
		{
			if (string.IsNullOrEmpty(name))
			{
				engine.GraphicManager.FadeOutAllParticle(stopType);
			}
			else
			{
				if (engine.GraphicManager.FindParticle(name)!=null)
				{
					engine.GraphicManager.FadeOutParticle(name,stopType);
				}
				else
				{
					var layer = engine.GraphicManager.FindLayer(name);
					if (layer != null)
					{
						//消す
						layer.FadeOutAllParticle(stopType);
					}
					else
					{
						//パーティクルの場合は自動で消えている可能性があるので
//						Debug.LogError("Not found " + name + " Please input particle name or layer name");
					}
				}
			}
		}
	}
}
