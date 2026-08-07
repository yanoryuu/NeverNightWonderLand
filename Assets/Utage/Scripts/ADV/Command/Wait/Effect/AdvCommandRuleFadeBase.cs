// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UtageExtensions;

namespace Utage
{

	public abstract class AdvCommandRuleFadeBase : AdvCommandEffectBase
		, IAdvCommandEffect
	{
		protected IAdvFadeSkippable Fade { get; set; }
		protected AdvTransitionArgs TransitionArgs { get; set; }
		protected AdvAnimationPlayer AnimationPlayer { get; set; }
		protected IAnimationRuleFade AnimationRuleFade { get; set; }

		protected AdvCommandRuleFadeBase(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row,dataManager)
		{
			string textureName = ParseCell<string>(AdvColumnName.Arg2);
			float vague = ParseCellOptional<float>(AdvColumnName.Arg3, 0.2f);
			string arg6 = ParseCellOptional<string>(AdvColumnName.Arg6,"");
			AdvAnimationData animationData = null;
			float time = 0.2f;
			if (!arg6.IsNullOrEmpty())
			{
				float f;
				if (WrapperUnityVersion.TryParseFloatGlobal(arg6, out f))
				{
					time = f;
				}
				else
				{
					animationData = dataManager.AnimationSetting.Find(arg6);
					if (animationData == null)
					{
						Debug.LogError(RowData.ToErrorString("Animation " + arg6 + " is not found"));
					}
				}
			}
			this.TransitionArgs = new AdvTransitionArgs(textureName, vague, time, animationData);
		}

		//エフェクト開始時のコールバック
		protected override void OnStartEffect( GameObject target, AdvEngine engine, AdvScenarioThread thread)
		{
			this.Fade = target.GetComponentInChildren<IAdvFadeSkippable>(true);
			if (Fade == null)
			{
				Debug.LogError("Can't find [ " + this.TargetName +" ]");
				OnComplete(thread);
			}
			else if (!TransitionArgs.EnableAnimation)
			{
				OnStartFade(target,engine,thread);
			}
			else
			{
				//アニメーションに対応していない
				IAdvFadeAnimation fadeAnimation = Fade as IAdvFadeAnimation;
				if (fadeAnimation == null)
				{
					Debug.LogError(RowData.ToErrorString(Fade.GetType() + " is not support Animation"));
					OnComplete(thread);
					return;
				}

				//ルール画像等の設定
				AnimationRuleFade = fadeAnimation.BeginRuleFade(engine, TransitionArgs);
				if (AnimationRuleFade == null)
				{
					Debug.LogError(RowData.ToErrorString(Fade.GetType() + " is not support Animation"));
					OnComplete(thread);
				}

				//アニメーションを再生
				AnimationPlayer = AnimationRuleFade.gameObject.AddComponent<AdvAnimationPlayer>();
				AnimationPlayer.AutoDestory = true;
				AnimationPlayer.Play(TransitionArgs.AnimationData.Clip, engine.Page.SkippedSpeed,
					() => { OnComplete(thread); });
			}
		}

		//フェード開始時のコールバック
		protected abstract void OnStartFade(GameObject target, AdvEngine engine, AdvScenarioThread thread);

		//エフェクト開始時のコールバック
		public void OnEffectSkip()
		{
			if (Fade == null)
			{
				return;
			}
			OnSkipFade();
		}

		//フェードスキップ時
		protected virtual void OnSkipFade()
		{
			Fade.SkipRuleFade();
			if (AnimationPlayer != null)
			{
				AnimationPlayer.SkipToEnd();
			}
		}

		//エフェクト終了時
		public virtual void OnEffectFinalize()
		{
			Fade = null;
			AnimationPlayer = null;
			if (AnimationRuleFade != null)
			{
				AnimationRuleFade.EndRuleFade();
				(AnimationRuleFade as Component).RemoveComponentMySelf();
			}
			AnimationRuleFade = null;
		}
	}
	
}
