// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UtageExtensions;

namespace Utage
{
	public interface IAdvCommandImageEffect : IAdvCommandEffect
	{
		float Time { get; }
		bool Inverse { get; }
		string ImageEffectType { get; }
		Timer Timer { get; set; }
		AdvAnimationData AnimationData { get; }
		AdvAnimationPlayer AnimationPlayer { get; set; }
	}

	/// <summary>
	/// コマンド：イメージエフェクトコマンドの基底クラス
	/// </summary>
	public class AdvCommandImageEffectBase : AdvCommandEffectBase
		, IAdvCommandImageEffect
	{
		public float Time { get; }
		public bool Inverse { get; }
		public string ImageEffectType { get; }
		public Timer Timer { get; set; }
		public AdvAnimationData AnimationData { get; }
		public AdvAnimationPlayer AnimationPlayer { get; set; }

		public AdvCommandImageEffectBase(StringGridRow row, AdvSettingDataManager dataManager, bool inverse)
			: base(row,dataManager)
		{
			this.Inverse = inverse;
			this.targetType = AdvEffectManager.TargetType.Camera;
			this.ImageEffectType = RowData.ParseCell<string>(AdvColumnName.Arg2.ToString());
			var animationName = ParseCellOptional<string>(AdvColumnName.Arg3,"");
			//アニメーションの適用
			if (!string.IsNullOrEmpty(animationName))
			{
				AnimationData = dataManager.AnimationSetting.Find(animationName);
				if (AnimationData == null)
				{
					Debug.LogError(RowData.ToErrorString("Animation " + animationName + " is not found"));
				}
			}
			else
			{
				AnimationData = null;
			}
			this.Time = ParseCellOptional<float>(AdvColumnName.Arg6, 0);
			
			
		}

		//エフェクト開始時のコールバック
		protected override void OnStartEffect(GameObject target, AdvEngine engine, AdvScenarioThread thread)
		{
			Camera camera = target.GetComponentInChildren<Camera>(true);
			var commandExecutor = engine.AdvPostEffectManager.ImageEffect;
			if (ImageEffectType == "All")
			{
				commandExecutor.DoCommandAllOff(camera, this, () => OnComplete(thread));
//				OnStartAll(target,engine,thread);
				return;
			}

			commandExecutor.DoCommand(camera, this, () => OnComplete(thread));
		}

		public void OnEffectSkip()
		{
			if (Timer != null)
			{
				Timer.SkipToEnd();
			}

			if (AnimationPlayer != null)
			{
				AnimationPlayer.SkipToEnd();
			}
		}

		public void OnEffectFinalize()
		{
			Timer = null;
			AnimationPlayer = null;
		}
	}
}
