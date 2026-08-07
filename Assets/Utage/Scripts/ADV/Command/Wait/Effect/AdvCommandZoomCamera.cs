// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// コマンド：カメラをズームする
	/// </summary>
	public class AdvCommandZoomCamera : AdvCommandEffectBase
		, IAdvCommandEffect
	{
		bool isEmptyZoom;
		float zoom;
		bool isEmptyZoomCenter;
		Vector2 zoomCenter;
		float time;
		Timer Timer { get; set; }
		
		public AdvCommandZoomCamera(StringGridRow row, AdvSettingDataManager dataManager)
			: base(row,dataManager)
		{
			this.targetType = AdvEffectManager.TargetType.Camera;
			this.isEmptyZoom = IsEmptyCell(AdvColumnName.Arg2);
			this.zoom = ParseCellOptional<float>(AdvColumnName.Arg2, 1);
			this.isEmptyZoomCenter = IsEmptyCell(AdvColumnName.Arg3) && IsEmptyCell(AdvColumnName.Arg4);
			this.zoomCenter.x = ParseCellOptional<float>(AdvColumnName.Arg3, 0);
			this.zoomCenter.y = ParseCellOptional<float>(AdvColumnName.Arg4, 0);
			this.time = ParseCellOptional<float>(AdvColumnName.Arg6, 0.2f);
		}


		//エフェクト開始時のコールバック
		protected override void OnStartEffect(GameObject target, AdvEngine engine, AdvScenarioThread thread)
		{
			if (target != null)
			{
				LetterBoxCamera camera = target.GetComponentInChildren<LetterBoxCamera>();

				//現在の倍率
				float zoom0 = camera.Zoom2D;
				//目標の倍率
				float zoomTo = isEmptyZoom ? zoom0 : zoom;

				//現在の中心点、今の倍率が1の場合は目標の中心点と同じで扱う（無駄な補間を入れないため）
				Vector2 center0 = (zoom0 == 1) ? zoomCenter : camera.Zoom2DCenter;
				//目標の中心点
				Vector2 centerTo = isEmptyZoomCenter ? center0 : zoomCenter;
				Timer = target.AddComponent<Timer>();
				Timer.AutoDestroy = true;
				Timer.StartTimer(
					engine.Page.ToSkippedTime(this.time),
					engine.Time.Unscaled,
					(x)=>
					{
						if(Timer != null)
                        {
	                        float zoom1 = Timer.GetCurve(zoom0, zoomTo);
	                        Vector2 center1 = Timer.GetCurve(center0, centerTo);
	                        camera.SetZoom2D(zoom1, center1);
                        }
						else
						{
							Debug.LogWarning( "Timer is null",target);
						}
					},
					(x) =>
					{
						//倍率1倍なら一応中心点を戻しておく
						if (zoomTo == 1)
						{
							camera.Zoom2DCenter = Vector2.zero;
						}
						OnComplete(thread);
					}
					);
			}
			else
			{
				//記述ミス、タゲーットが見つからない
				Debug.LogError(LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.NotFoundTweenGameObject, "SpriteCamera"));
				OnComplete(thread);
			}
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
			if (Timer != null && Timer.IsPlaying)
			{
				//エンジンの停止などでエフェクトが途中で終わるときは、コルーチンを止めておく
				Timer.StopAllCoroutines();
			}
			Timer = null;
		}
	}
}
