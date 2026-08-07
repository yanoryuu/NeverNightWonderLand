// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
	/// <summary>
	/// 画面管理コンポーネントの基本クラス（各画面制御はこれを継承する）
	/// </summary>
	[AddComponentMenu("Utage/Lib/UI/UguiViewTransitionCrossFade")]
	[RequireComponent(typeof(UguiView))]
	public class UguiViewTransitionCrossFade : MonoBehaviour, ITransition
	{
		/// <summary>CanvasGroup</summary>
		public UguiView UguiView { get { return this.GetComponentCache(ref uguiView); } }
		UguiView uguiView;

		public bool IsPlaying { get { return isPlaying; } }
		bool isPlaying;

		public float time = 1.0f;
		public bool unscaledTime = false;


		//画面を開く処理
		public void Open()
		{
			StopCoroutine(CoClose());
			StartCoroutine(CoOpen());
		}

		//画面を閉じる処理
		public void Close()
		{
			StopCoroutine(CoOpen());
			StartCoroutine(CoClose());
		}

		//画面を閉じる処理をキャンセル
		public void CancelClosing()
		{
			StopCoroutine(CoClose());
			EndClose();
			isPlaying = false;
		}

		//画面を開く処理
		IEnumerator CoOpen()
		{
			isPlaying = true;
			CanvasGroup canvasGroup = UguiView.CanvasGroup;
			canvasGroup.interactable = false;
			canvasGroup.blocksRaycasts = false;
			float currentTime = 0;
			while (currentTime < time)
			{
				canvasGroup.alpha = currentTime / time;
				currentTime += TimeUtil.GetDeltaTime(unscaledTime);
				yield return null;
			}
			canvasGroup.alpha = 1.0f;
			canvasGroup.interactable = true;
			canvasGroup.blocksRaycasts = true;
			isPlaying = false;
			yield break;
		}

		//画面を閉じる処理
		IEnumerator CoClose()
		{
			isPlaying = true;
			CanvasGroup canvasGroup = UguiView.CanvasGroup;
			canvasGroup.interactable = false;
			canvasGroup.blocksRaycasts = false;
			float currentTime = 0;
			while (currentTime < time)
			{
				canvasGroup.alpha = (1.0f - currentTime / time);
				currentTime += TimeUtil.GetDeltaTime(unscaledTime);
				yield return null;
			}
			canvasGroup.interactable = true;
			canvasGroup.blocksRaycasts = true;
			EndClose();
			yield break;
		}

		void EndClose()
		{
			CanvasGroup canvasGroup = UguiView.CanvasGroup;
			canvasGroup.alpha = 0.0f;
			isPlaying = false;
		}
	}
}
