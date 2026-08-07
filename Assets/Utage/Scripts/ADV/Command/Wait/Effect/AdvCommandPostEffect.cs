// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEngine;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// コマンド：ポストエフェクト開始
	/// </summary>
	public class AdvCommandPostEffect : AdvCommandEffectBase
	{
		public float Time { get; }
		public string VolumeName { get; }
		public string[] EffectNames { get; }
		public Timer Timer { get; set; }

		public AdvCommandPostEffect(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row,dataManager)
		{
			this.targetType = AdvEffectManager.TargetType.Camera;
			this.VolumeName = ParseCell<string>(AdvColumnName.Arg2);
			this.EffectNames = ParseCellOptionalArray(AdvColumnName.Arg3, Array.Empty<string>());
			this.Time = ParseCellOptional<float>(AdvColumnName.Arg6, 0);
		}
		
		//エフェクト開始時のコールバック
		protected override void OnStartEffect(GameObject target, AdvEngine engine, AdvScenarioThread thread)
		{
			Camera camera = target.GetComponentInChildren<Camera>(true);
			var commandExecutor = engine.AdvPostEffectManager.PostEffect;
			commandExecutor.DoCommand(camera, this, () => OnComplete(thread));
		}

		
		public void OnEffectSkip()
		{
			if (Timer != null)
			{
				Timer.SkipToEnd();
			}
		}

		public void OnEffectFinalize()
		{
			Timer = null;
		}

	}
}
