using System;
using System.Collections.Generic;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
	//ビルトインRenderPipelineを使ったポストエフェクト処理
	public class AdvPostEffectRenderPipelineUsingBuiltin
		: MonoBehaviour,
			IAdvPostEffectRenderPipelineBridge
	{
		AdvEngine Engine => AdvPostEffectManager.Engine;
		AdvPostEffectManager AdvPostEffectManager => this.GetComponentCache(ref postEffectManager);
		AdvPostEffectManager postEffectManager;
		
		public virtual (IPostEffectStrength effect, float start, float end) DoCommandColorFade(Camera targetCamera, IAdvCommandFade command)
		{
			bool ruleEnabled = targetCamera.gameObject.GetComponent<RuleFade>();
			if (ruleEnabled)
			{
				targetCamera.gameObject.SafeRemoveComponent<RuleFade>();
			}

			if (!ImageEffectUtil.TryGetComonentCreateIfMissing(ImageEffectType.ColorFade.ToString(),
				    out ImageEffectBase imageEffect, out bool alreadyEnabled, targetCamera.gameObject))
			{
				return (null, 0, 0);
			}
			float start, end;
			ColorFade colorFade = (ColorFade)imageEffect;
			if (command.Inverse)
			{
				//画面全体のフェードイン（つまりカメラのカラーフェードアウト）
				start = (ruleEnabled) ? 1 : colorFade.color.a;
				end = 0;
			}
			else
			{
				//画面全体のフェードアウト（つまりカメラのカラーフェードイン）
				//colorFade.Strengthで、すでにフェードされているのでそちらの値をつかう
				start = alreadyEnabled ? colorFade.Strength : 0;
				end = command.Color.a;
			}

			colorFade.enabled = true;
			colorFade.color = command.Color;
			return (colorFade, start, end);
		}

		public virtual (IPostEffectStrength effect, float start, float end) DoCommandRuleFade(Camera targetCamera, IAdvCommandFade command)
		{
			targetCamera.gameObject.SafeRemoveComponent<ColorFade>();
			ImageEffectUtil.TryGetComonentCreateIfMissing(ImageEffectType.RuleFade.ToString(), out ImageEffectBase imageEffect,
				out bool alreadyEnabled, targetCamera.gameObject);
			float start, end;
			RuleFade ruleFade = (RuleFade)imageEffect;
			ruleFade.ruleTexture = Engine.EffectManager.FindRuleTexture(command.RuleImage);
			ruleFade.vague = command.Vague;
			if (command.Inverse)
			{
				start = 1;
				end = 0;
			}
			else
			{
				start = alreadyEnabled ? ruleFade.Strength : 0;
				end = 1;
			}
			ruleFade.enabled = true;
			ruleFade.color = command.Color;
			return (ruleFade, start, end);
		}

		public (IPostEffect effect, Action onComplete) DoCommandImageEffect(Camera targetCamera, IAdvCommandImageEffect command,
			Action onComplete)
		{
//			alreadyEnabled;

			if (!ImageEffectUtil.TryGetComonentCreateIfMissing( command.ImageEffectType, out ImageEffectBase imageEffect, out bool alreadyEnabled, targetCamera.gameObject))
			{
				return (null,onComplete);
			}
			if (!command.Inverse) imageEffect.enabled = true;

			void Complete()
			{
				if (command.Inverse)
				{
					//                imageEffect.enabled = false;                
					UnityEngine.Object.DestroyImmediate(imageEffect);
				}
				onComplete();
			}

			return (imageEffect, Complete);
		}

		public void DoCommandImageEffectAllOff(Camera targetCamera, IAdvCommandImageEffect command, Action onComplete)
		{
			ImageEffectBase[] effects = targetCamera.gameObject.GetComponents<ImageEffectBase>();
			if (effects.Length<=0)
			{
				onComplete();
				return;
			}
			foreach (var effect in effects)
			{
				if(effect is ColorFade) continue;
				UnityEngine.Object.DestroyImmediate(effect);
			}
			onComplete();
		}

		public IPostEffect DoCommandPostEffect(Camera targetCamera, AdvCommandPostEffect command)
		{
			Debug.LogError("PostEffect Command is not supported in Builtin RenderPipeline", this);
			return null;
		}
		
		//指定の名前のポストエフェクト（ボリュームオブジェクト）を探す
		public IPostEffectVolumeObject FindVolume(Camera targetCamera, string volumeName)
		{
			Debug.LogError("PostEffect Volume is not supported in Builtin RenderPipeline", this);
			return null;
		}
		
		//指定のカメラに設定されている、エフェクト用のボリュームオブジェクトをすべて取得する
		//エフェクト用というのは、キャプチャとフェード用など専用のポストエフェクト以外のボリュームオブジェクト
		public IEnumerable<IPostEffectVolumeObject> GetAllActiveEffectVolumes(Camera targetCamera)
		{
			Debug.LogError("PostEffect Volume is not supported in Builtin RenderPipeline", this);
			yield break;
		}

	}
}
