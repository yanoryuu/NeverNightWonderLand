
// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using UtageExtensions;


namespace Utage
{
	/// <summary>
	/// ダイシングの簡易アニメ
	/// </summary>
	[RequireComponent(typeof(DicingImage))]
	[AddComponentMenu("Utage/Lib/UI/DicingAnimation")]
	public class DicingAnimation : MonoBehaviour
	{
		[SerializeField]
		bool playOnAwake = false;

		[SerializeField,LimitEnum("Default","Loop","PingPong")]
		MotionPlayType wrapMode = MotionPlayType.Default;

		[SerializeField]
		bool reverse = false;

		//秒間のコマ数
		[SerializeField]
		float frameRate = 15;

		public bool UnscaledTime { get { return unscaledTime; } set { unscaledTime = value; } }
		[SerializeField]
		bool unscaledTime = false;

		DicingImage Dicing { get { return this.gameObject.GetComponentCache<DicingImage>(ref dicing); } }
		DicingImage dicing;

		void Awake()
		{
			if (playOnAwake)
			{
				Play(null);
			}
		}

		public void Play(Action onComplete)
		{
			StartCoroutine(CoPlay(onComplete));
		}

		IEnumerator CoPlay(Action onComplete)
		{
			List<string> list = Dicing.DicingData.GetPattenNameList();
			if (reverse) list.Reverse();

			if (list.Count > 0)
			{
				bool isEnd = false;
				while (!isEnd)
				{
					yield return CoPlayOnce(list);
					switch (wrapMode)
					{
						case MotionPlayType.Default:
							isEnd = true;
							break;
						case MotionPlayType.Loop:
							break;
						case MotionPlayType.PingPong:
							list.Reverse();
							break;
						default:
							Debug.LogError("NotSupport");
							isEnd = true;
							break;
					}
				}
			}
			if (onComplete != null) onComplete();
			yield break;
		}

		IEnumerator CoPlayOnce(List<string> patternList)
		{
			foreach (var pattern in patternList)
			{
				Dicing.ChangePattern( pattern );
				yield return TimeUtil.WaitForSeconds(UnscaledTime, 1.0f / frameRate);
			}
		}
	}
}

