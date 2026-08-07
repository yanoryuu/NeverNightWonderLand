// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// コマンド：アニメーションクリップの再生をする
	/// </summary>
	public class AdvCommandPlayAnimatin : AdvCommandEffectBase
		, IAdvCommandEffect
		, IAdvCommandEffectLoop
	{
		string animationName;
		AdvAnimationPlayer AnimationPlayer { get; set; }
		bool EnableSave { get; set; }

		public AdvCommandPlayAnimatin(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row,dataManager)
		{
			this.animationName = ParseCell<string>(AdvColumnName.Arg2);
			EnableSave = ParseCellOptional(AdvColumnName.Arg3,true); 
		}

		//エフェクト開始時のコールバック
		protected override void OnStartEffect(GameObject target, AdvEngine engine, AdvScenarioThread thread)
		{
			AdvAnimationData animationData = engine.DataManager.SettingDataManager.AnimationSetting.Find(animationName);
			if (animationData == null)
			{
				Debug.LogError(RowData.ToErrorString("Animation " + animationName + " is not found"));
				OnComplete(thread);
				return;
			}

			//ループアニメーションを上書きするときなどは古いアニメを消す
			var old = target.GetComponent<AdvAnimationPlayer>();
			if (old!=null)
			{
				old.DestroyComponentImmediate();
				Object.Destroy(old);
			}
			AnimationPlayer = target.AddComponent<AdvAnimationPlayer>();
			AnimationPlayer.AutoDestory = true;
			AnimationPlayer.EnableSave = EnableSave;
			AnimationPlayer.Play(animationData.Clip, engine.Page.SkippedSpeed,
				() =>
				{
					OnComplete(thread);
				});
		}
		
		public void OnEffectSkip()
		{
			if (AnimationPlayer != null)
			{
				AnimationPlayer.SkipToEnd();
			}
		}

		public void OnEffectFinalize()
		{
			AnimationPlayer = null;
		}
		
		//無限ループエフェクトか
		public bool IsLoopEffect()
		{
			if (AnimationPlayer != null)
			{
				return AnimationPlayer.IsLoop;
			}
			return false;
		}
	}
}
