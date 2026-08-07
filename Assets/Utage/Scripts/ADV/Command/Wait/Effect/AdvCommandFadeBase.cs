// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UtageExtensions;

namespace Utage
{
	public interface IAdvCommandFade
	{
		public float Time { get; }
		public bool Inverse { get; }
		public Color Color { get; }
		public string RuleImage { get; }
		public float Vague { get; }
		public Timer Timer { get; set; }
		public AdvAnimationData AnimationData { get; }
		public AdvAnimationPlayer AnimationPlayer { get; set; }
		
	}

	/// <summary>
	/// コマンド：フェードイン処理
	/// </summary>
	public abstract class AdvCommandFadeBase : AdvCommandEffectBase
		, IAdvCommandEffect
		, IAdvCommandFade
	{
		public float Time => time;
		float time;

		public bool Inverse { get; }

		public Color Color => color;
		Color color;

		public string RuleImage => ruleImage;
		string ruleImage;

		public float Vague => vague;
		float vague;
		
		public Timer Timer { get; set; }
		public AdvAnimationData AnimationData => animationData;
		AdvAnimationData animationData;
		
		public AdvAnimationPlayer AnimationPlayer { get; set; }
		
		protected AdvCommandFadeBase(StringGridRow row, AdvSettingDataManager dataManager, bool inverse)
			: base(row, dataManager)
		{
			this.Inverse = inverse;
		}

		protected override void OnParse(AdvSettingDataManager dataManager)
		{
			this.color = ParseCellOptional<Color>(AdvColumnName.Arg1, Color.white);
			if (IsEmptyCell(AdvColumnName.Arg2))
			{
				this.targetName = "SpriteCamera";
			}
			else
			{
				//第2引数はターゲットの設定
				this.targetName = ParseCell<string>(AdvColumnName.Arg2);
			}

			this.ruleImage = ParseCellOptional(AdvColumnName.Arg3, "");
			this.vague = ParseCellOptional(AdvColumnName.Arg4, 0.2f);
			this.targetType = AdvEffectManager.TargetType.Camera;
			string arg6 = ParseCellOptional<string>(AdvColumnName.Arg6,"");
			this.animationData = null;
			this.time = 0.2f;
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

			ParseWait(AdvColumnName.WaitType);
		}

		protected override void OnStartEffect(GameObject target, AdvEngine engine, AdvScenarioThread thread)
		{
			Camera camera = target.GetComponentInChildren<Camera>(true);
			engine.AdvPostEffectManager.Fade.DoCommand(camera, this, ()=>OnComplete(thread));
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
