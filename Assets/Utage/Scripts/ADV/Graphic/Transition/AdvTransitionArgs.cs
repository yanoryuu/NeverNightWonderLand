// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// トランジションの実行データ
	/// </summary>
	public class AdvTransitionArgs
	{
		internal string TextureName { get; private set; }
		internal float Vague { get; private set; }
		float Time { get; set; }
		internal AdvAnimationData AnimationData{ get; set; }
		internal bool EnableAnimation { get{ return AnimationData!=null; }}
		
		internal AdvTransitionArgs( string textureName, float vague, float time, AdvAnimationData animationData )
		{
			this.TextureName = textureName;
			this.Vague = vague;
			this.Time = time;
			this.AnimationData = animationData;
		}

		internal float GetSkippedTime(AdvEngine engine)
		{
			return engine.Page.ToSkippedTime(Time);
		}
	}
}
