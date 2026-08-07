// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UtageExtensions;

namespace Utage
{

	// コマンド：ルール画像付きのフェードアウト
	public class AdvCommandRuleFadeOut : AdvCommandRuleFadeBase
	{
		public AdvCommandRuleFadeOut(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row, dataManager)
		{
		}

		//フェード開始
		protected override void OnStartFade(GameObject target, AdvEngine engine, AdvScenarioThread thread)
		{
			Fade.RuleFadeOut(engine, this.TransitionArgs, ()=>OnComplete(thread));
		}
		
		//エフェクト終了時
		public override void OnEffectFinalize()
		{
			if (this.TransitionArgs.EnableAnimation && Fade!=null)
			{
				IAdvFadeAnimation fadeAnimation = Fade as IAdvFadeAnimation;
				if (fadeAnimation!=null)
				{
					fadeAnimation.Clear();
				}
			}
			base.OnEffectFinalize();
		}

	}
}
