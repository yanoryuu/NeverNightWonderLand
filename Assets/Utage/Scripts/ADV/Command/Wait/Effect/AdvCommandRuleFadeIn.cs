// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UtageExtensions;

namespace Utage
{

	// コマンド：ルール画像付きのフェードイン
	public class AdvCommandRuleFadeIn : AdvCommandRuleFadeBase
	{
		public AdvCommandRuleFadeIn(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row, dataManager)
		{
		}

		//フェード開始
		protected override void OnStartFade(GameObject target, AdvEngine engine, AdvScenarioThread thread)
		{
			Fade.RuleFadeIn(engine, TransitionArgs, ()=>OnComplete(thread) );
		}
	}
}
