using System;
using System.Linq;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
	//ポストエフェクトとして実際にポストエフェクトコマンドを実行するコンポーネント
	public class AdvPostEffectCommandExecutorPostEffect : AdvPostEffectCommandExecutorBase
	{
		public void DoCommand( Camera targetCamera, AdvCommandPostEffect command,  Action onComplete)
		{
			IPostEffect effect = RpBridge.DoCommandPostEffect(targetCamera, command);
			IPostEffectStrength effectStrength = effect as IPostEffectStrength;

			if (effectStrength==null)
			{
				onComplete();
				return;
			}

			float start = 0;
			float end = 1;
			command.Timer = SetTimer(effect, command.Time,
				(x) => effectStrength.Strength = x.GetCurve(start, end),
				(x) =>
				{
					onComplete();
				});
		}
		
		public void DoCommand( Camera targetCamera, AdvCommandPostEffectOff command,  Action onComplete)
		{
			if (command.VolumeName.IsNullOrEmpty())
			{
				//名前指定がない場合は、対象のカメラのエフェクト用のボリュームオブジェクトをすべて探してフェードアウトする
				var effects = RpBridge.GetAllActiveEffectVolumes(targetCamera).ToArray();
				if(effects.Length<=0)
				{
					onComplete();
					return;
				}

				for (var i = 0; i < effects.Length; i++)
				{
					if(i==effects.Length-1)
					{
						command.Timer = FadeOut(effects[i], command, onComplete);
					}
					else
					{
						FadeOut(effects[i], command, null);
					}
				}
			}
			else
			{
				//名前指定がある場合は、対象のカメラの指定の名前のボリュームオブジェクトを探してフェードアウトする
				IPostEffectVolumeObject effect = RpBridge.FindVolume(targetCamera, command.VolumeName);
				if(effect==null)
				{
					Debug.LogError($"Not found post effect volume. name={command.VolumeName}");
					onComplete();
					return;
				};
				command.Timer = FadeOut(effect, command, onComplete);
			}
		}

		Timer FadeOut(IPostEffectVolumeObject effect, AdvCommandPostEffectOff command, Action onComplete)
		{

			float start = effect.Strength;
			float end = 0;
			return SetTimer(effect, command.Time,
				(x) => effect.Strength = x.GetCurve(start, end),
				(x) =>
				{
					effect.OnClear();
					onComplete?.Invoke();
				});
		}
	}
}
