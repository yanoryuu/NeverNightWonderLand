// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace Utage
{

	/// <summary>
	/// まばたき処理の基本クラス
	/// </summary>
	public abstract class EyeBlinkBase : MonoBehaviour
	{
		//瞬きと瞬きの間の時間
		//指定範囲の間の秒数でランダムで決まる
		public MinMaxFloat IntervalTime { get { return intervalTime; } set { intervalTime = value; } }
		[SerializeField, MinMax(0, 10)]
		MinMaxFloat intervalTime = new MinMaxFloat() { Min = 2, Max = 6 };

		//二連続瞬きする確率（0～1）
		public float RandomDoubleEyeBlink { get { return randomDoubleEyeBlink; } set { randomDoubleEyeBlink = value; } }
		[SerializeField,Range(0,1)]
		float randomDoubleEyeBlink = 0.2f;

		//二連続瞬きするときの間の時間
		[SerializeField, Range(0, 1)]
		float intervalDoubleEyeBlink = 0.01f;

		public bool UnscaledTime { get { return unscaledTime; } set { unscaledTime = value; } }
		[SerializeField]
		bool unscaledTime = false;

		//目のパターンタグ
		public string EyeTag { get { return eyeTag; } set { eyeTag = value; } }
		[SerializeField]
		string eyeTag = "eye";

		//アニメーションデータ
		public MiniAnimationData AnimationData { get { return animationData; } set { animationData = value; } }
		[SerializeField]
		MiniAnimationData animationData = new MiniAnimationData();


		void Start()
		{
			StartWaiting();
		}

		//待機開始
		void StartWaiting()
		{
			float duration = intervalTime.RandomRange();
			StartCoroutine(CoUpateWaiting(duration));
		}

		//待機して次の瞬きへ
		IEnumerator CoUpateWaiting(float waitTime)
		{
			yield return TimeUtil.WaitForSeconds(UnscaledTime,waitTime);
			StartCoroutine(CoEyeBlink(OnEndBlink));
		}

		protected abstract IEnumerator CoEyeBlink(Action onComplete);

		//瞬き終了
		void OnEndBlink()
		{
			if (randomDoubleEyeBlink > UnityEngine.Random.value)
			{
				//パチパチと二連続で瞬き
				StartCoroutine(CoDoubleEyeBlink());
				return;
			}
			else
			{
				StartWaiting();
			}
		}

		//パチパチと二連続で瞬き
		IEnumerator CoDoubleEyeBlink()
		{
			yield return TimeUtil.WaitForSeconds(UnscaledTime, intervalDoubleEyeBlink);
			StartCoroutine(CoEyeBlink(StartWaiting));
		}
	}
}

