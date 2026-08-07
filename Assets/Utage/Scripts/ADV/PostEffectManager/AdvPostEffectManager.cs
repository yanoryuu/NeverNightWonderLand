using System;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
	//ポストエフェクト処理の全体制御
	public class AdvPostEffectManager : MonoBehaviour
	{
		public AdvEngine Engine => this.GetComponentCacheInParent(ref engine);
		AdvEngine engine;
		public AdvPostEffectCommandExecutorFade Fade => this.GetComponentCacheCreateIfMissing(ref fade);
		AdvPostEffectCommandExecutorFade fade;

		public AdvPostEffectCommandExecutorImageEffect ImageEffect => this.GetComponentCacheCreateIfMissing(ref imageEffect);
		AdvPostEffectCommandExecutorImageEffect imageEffect;

		public AdvPostEffectCommandExecutorPostEffect PostEffect => this.GetComponentCacheCreateIfMissing(ref postEffect);
		AdvPostEffectCommandExecutorPostEffect postEffect;

		//ポストエフェクトをRenderPipeLineの違いによって切り替えて実行するブリッジ
		public IAdvPostEffectRenderPipelineBridge RpBridge
		{
			get
			{
				if (rpBridge == null)
				{
					rpBridge = this.GetComponent<IAdvPostEffectRenderPipelineBridge>();
					if (rpBridge == null)
					{
						rpBridge = this.gameObject.AddComponent<AdvPostEffectRenderPipelineUsingBuiltin>();
					}
				}
				return this.GetComponentCache(ref rpBridge);
			}
		}
		IAdvPostEffectRenderPipelineBridge rpBridge;
	}
}
