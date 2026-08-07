// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;

namespace Utage
{

	/// <summary>
	/// コマンド：Tweenアニメーションをする
	/// </summary>
	public class AdvCommandTween : AdvCommandEffectBase
		, IAdvCommandEffect
		, IAdvCommandEffectLoop
	{
		protected iTweenData tweenData;
		private AdvITweenPlayer Player { get; set; }

		public AdvCommandTween(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row,dataManager)
		{
			//Tweenデータの初期化
			InitTweenData();

			//ストップの場合のみ、特殊
			if (this.tweenData.Type == iTweenType.Stop)
			{
				this.WaitType = AdvCommandWaitType.Add;
			}

			//エラーチェック
			if (!string.IsNullOrEmpty(tweenData.ErrorMsg))
			{
				Debug.LogError(ToErrorString(tweenData.ErrorMsg));
			}
		}


		//解析必要に応じてオーバーライド
		protected override void OnParse(AdvSettingDataManager dataManager)
		{
			ParseEffectTarget(AdvColumnName.Arg1);

			//ウェイトタイプ設定されているなら、それを優先
			if (!IsEmptyCell(AdvColumnName.WaitType))
			{
				ParseWait(AdvColumnName.WaitType);
			}
			else if (!IsEmptyCell(AdvColumnName.Arg6))
			{
				//ウェイトタイプがなく、Arg6がある
				if (AdvCommand.IsEditorErrorCheck && AdvCommand.IsEditorErrorCheckWaitType)
				{
					Debug.LogWarning( this.ToErrorString( "Please use 'WaitType' Column") );
				}
				ParseWait(AdvColumnName.Arg6);
			}
			else 
			{
				ParseWait(AdvColumnName.WaitType);
			}
		}

		//Tweenデータの初期化
		protected virtual void InitTweenData()
		{
			string type = ParseCell<string>(AdvColumnName.Arg2);
			string arg = ParseCellOptional<string>(AdvColumnName.Arg3, "");
			string easeType = ParseCellOptional<string>(AdvColumnName.Arg4, "");
			string loopType = ParseCellOptional<string>(AdvColumnName.Arg5, "");
			this.tweenData = new iTweenData(type, arg, easeType, loopType);
		}

		//エフェクト開始時のコールバック
		protected override void OnStartEffect(GameObject target, AdvEngine engine, AdvScenarioThread thread)
		{
			if (!string.IsNullOrEmpty(tweenData.ErrorMsg))
			{
				Debug.LogError(tweenData.ErrorMsg);
				OnComplete(thread);
				return;
			}
			Player = target.AddComponent<AdvITweenPlayer>();
			float skipSpeed = engine.Page.CheckSkip() ? engine.Config.SkipSpped : 0;

			Player.Init(tweenData, IsUnder2DSpace(target), engine.GraphicManager.PixelsToUnits, skipSpeed, engine.Time.Unscaled,
				(x) =>
				{
					Player = null;
					OnComplete(thread);
				});
			Player.Play();
			if (Player.IsEndlessLoop)
			{
//				waitType = EffectWaitType.Add;
			}
		}

		//2D座標以下にあるか
		bool IsUnder2DSpace(GameObject target)
		{
			switch ( this.targetType )
			{
				case AdvEffectManager.TargetType.MessageWindow:
					return true;
				case AdvEffectManager.TargetType.Default:
					return target.GetComponent<AdvGraphicObject>() != null;
				default:
					return false;
			}
		}

		public void OnEffectSkip()
		{
			if (Player == null)
			{
				Debug.LogError(" cant skip tween effect");
			}
			Player.SkipToEnd();
		}

		public void OnEffectFinalize()
		{
			Player = null;
		}
		
		//無限ループエフェクトか
		public bool IsLoopEffect()
		{
			if (Player != null)
			{
				return Player.IsEndlessLoop;
			}
			return false;
		}

	}
}
