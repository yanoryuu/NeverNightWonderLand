// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{
	[System.Serializable]
	public class CurveAnimationEvent : UnityEvent<CurveAnimation> { }

	/// <summary>
	/// 簡易的なセルアニメーション
	/// </summary>
	[AddComponentMenu("Utage/Lib/Effect/CurveAnimation")]
	public class CurveAnimation : MonoBehaviour
	{
		//アニメーションカーブ
		public AnimationCurve Curve { get { return curve; } }
		[SerializeField]
		AnimationCurve curve = AnimationCurve.Linear(0,0,1,1);

		//アニメーションするまでの時間
		public float Delay { get { return delay; } set { delay = value; } }
		[SerializeField]
		float delay = 0;

		//アニメーションする時間
		public float Duration { get { return duration; } set { duration = value; } }
		[SerializeField]
		float duration = 1.0f;

		//時間のスケール設定を有効にするか
		public bool UnscaledTime { get { return unscaledTime; } set { unscaledTime = value; } }
		[SerializeField]
		bool unscaledTime = true;

		//現在の値
		public float Value { get; set; }

		//現在の値で補完した値を取得
		public float LerpValue(float from, float to)
		{
			return Mathf.Lerp(from, to, Value);
		}

		//アニメーション中か
		public bool IsPlaying { get; protected set; }

		//アニメーションの開始時に呼ばれるイベント
		public CurveAnimationEvent OnStart { get { return onStart; } }
		[SerializeField]
		CurveAnimationEvent onStart = new CurveAnimationEvent();

		//アニメーションの更新時に呼ばれるイベント
		public CurveAnimationEvent OnUpdate { get { return onUpdate; } }
		[SerializeField]
		CurveAnimationEvent onUpdate = new CurveAnimationEvent();

		//アニメーションの終了時に呼ばれるイベント
		public CurveAnimationEvent OnComplete { get { return onComplete; } }
		[SerializeField]
		CurveAnimationEvent onComplete = new CurveAnimationEvent();

		//現在の時間
		protected float Time { get { return TimeUtil.GetTime(UnscaledTime); } }
		//前フレームとの時間差分
		protected float DeltaTime { get { return TimeUtil.GetDeltaTime(UnscaledTime); } }

		//現在のアンメーションした時間
		protected float CurrentAnimationTime { get; set; }

		Coroutine currentCoroutine;
		//アニメーション開始
		public void PlayAnimation()
		{
			PlayAnimation(null, null);
		}
		
		//アニメーション開始
		public void PlayAnimation(Action<float> onUpdate = null, Action onComplete = null)
		{
			if(IsPlaying)
			{
//				Debug.LogError("Already playing animation ");
				StopCoroutine(currentCoroutine);
			}
			currentCoroutine = StartCoroutine(CoAnimation(onUpdate, onComplete));
		}		

		IEnumerator CoAnimation(Action<float> onUpdate, Action onComplete)
		{
			IsPlaying = true;

			if (Delay >= 0)
			{
				float delayStartTime = Time;
				while (Time-delayStartTime < Delay) yield return null;
			}

			float endTime = Curve.keys[ Curve.length-1 ].time;
			Value = Curve.Evaluate(0);
			OnStart.Invoke(this);

			float startTime = Time;
			CurrentAnimationTime = 0;
			while (CurrentAnimationTime < Duration)
			{
				Value = Curve.Evaluate(endTime * CurrentAnimationTime / Duration);
				if(onUpdate!=null) onUpdate(Value);
				this.OnUpdate.Invoke(this);

				yield return null;
				CurrentAnimationTime = Time - startTime;
			}
			Value = Curve.Evaluate(endTime);
			if (onUpdate != null) onUpdate(Value);
			OnUpdate.Invoke(this);

			if (onComplete != null) onComplete();
			OnComplete.Invoke(this);
			IsPlaying = false;
			currentCoroutine = null;
		}
	}
}