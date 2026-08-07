// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using UnityEngine;
using Utage.RenderPipeline;

namespace Utage
{
    //URPかどうかのフラグを設定するサンプル
	public class SampleUrpFlag : MonoBehaviour
	{
		public AdvEngine advEngine;
		public string urpFlagName = "urpFlag";

		private void Awake()
		{
			if (advEngine == null) return;
			advEngine.ScenarioPlayer.OnBeginScenarioAfterParametersInitialized.AddListener(OnBeginScenarioAfterParametersInitialized);
		}

		void OnDestroy()
		{
			if(advEngine==null) return;
			advEngine.ScenarioPlayer.OnBeginScenarioAfterParametersInitialized.RemoveListener(OnBeginScenarioAfterParametersInitialized);
		}

		void OnBeginScenarioAfterParametersInitialized(AdvScenarioPlayer scenarioPlayer)
		{
			advEngine.Param.SetParameterBoolean(urpFlagName, RenderPipelineUtil.IsUrp() );
		}
	}
}
