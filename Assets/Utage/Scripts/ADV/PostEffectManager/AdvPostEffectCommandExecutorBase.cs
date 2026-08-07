using System;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
	//ポストエフェクトとしてコマンドを実行するコンポーネントの抽象クラス
	public abstract class AdvPostEffectCommandExecutorBase : MonoBehaviour
	{
		public AdvEngine Engine => this.GetComponentCacheInParent(ref engine);
		AdvEngine engine;
		public IAdvPostEffectRenderPipelineBridge RpBridge => PostEffectManager.RpBridge;
		public AdvPostEffectManager PostEffectManager => this.GetComponentCacheInParent(ref postEffectManager);
		AdvPostEffectManager postEffectManager;
		
		
		protected Timer SetTimer(IPostEffect postEffect, float time, Action<Timer> onUpdate , Action<Timer> onComplete)
		{
			var timer = postEffect.gameObject.AddComponent<Timer>();
			timer.AutoDestroy = true;
			timer.StartTimer(
				Engine.Page.ToSkippedTime(time),
				Engine.Time.Unscaled,
				onUpdate , onComplete);
			return timer;
		}

		
		protected virtual AdvAnimationPlayer SetAnimation(IPostEffect postEffect, AdvAnimationData animationData, Action onComplete )
		{
			//アニメーションを再生
			var animationPlayer = postEffect.gameObject.AddComponent<AdvAnimationPlayer>();
			animationPlayer.AutoDestory = true;
			animationPlayer.Play(animationData.Clip, Engine.Page.SkippedSpeed, onComplete);
			return animationPlayer;
		}
	}
}
