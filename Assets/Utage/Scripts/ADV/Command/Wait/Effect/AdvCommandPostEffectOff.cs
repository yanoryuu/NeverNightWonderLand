// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// コマンド：ポストエフェクトの終了コマンド
	/// </summary>
	public class AdvCommandPostEffectOff : AdvCommandEffectBase
	{
		public float Time { get; }
		public string VolumeName { get; }
		public Timer Timer { get; set; }

		public AdvCommandPostEffectOff(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row, dataManager)
		{
			this.targetType = AdvEffectManager.TargetType.Camera;
			this.VolumeName = ParseCellOptional<string>(AdvColumnName.Arg2,string.Empty);
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
