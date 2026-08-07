// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Utage;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// 画面管理コンポーネントの基本クラス（各画面制御はこれを継承する）
	/// </summary>
	[RequireComponent(typeof(CanvasGroup))]
	public abstract class UguiView : MonoBehaviour
	{
		//前の画面
		[SerializeField]
		protected UguiView prevView;

		//BGM
		[SerializeField]
		protected AudioClip bgm;
		public AudioClip Bgm
		{
			get { return bgm; }
			set { bgm = value; }
		}

		//再生時のイントロループ時間
		[SerializeField]
		float introTime;
		public float IntroTime
		{
			get { return introTime; }
			set { introTime = value; }
		}

		//再生時のフェード時間
		[SerializeField]
		float fadeInTime = 0;
		public float FadeInTime
		{
			get => fadeInTime;
			set => fadeInTime = value;
		}

		//BGMが設定されていない場合に、鳴っている曲を止めるか
		[SerializeField]
		bool isStopBgmIfNoneBgm;
		public bool IsStopBgmIfNoneBgm
		{
			get { return isStopBgmIfNoneBgm; }
			set { isStopBgmIfNoneBgm = value; }
		}

		/// <summary>
		/// 画面閉じたときのイベント
		/// </summary>
		public UnityEvent onOpen;

		/// <summary>
		/// 画面閉じたときのイベント
		/// </summary>
		public UnityEvent onClose;


		/// <summary>CanvasGroup</summary>
		public CanvasGroup CanvasGroup { get { return this.GetComponentCache(ref canvasGroup); } }
		[NonSerialized]
		CanvasGroup canvasGroup = null;

		public enum Status
		{
			None,
			Opening,
			Opened,
			Closing,
			Closed,
		};
		protected Status status = Status.None;

		/// <summary>
		/// 画面を開く
		/// </summary>
		public virtual void Open()
		{
			Open(prevView);
		}

		/// <summary>
		/// 画面を開く
		/// </summary>
		/// <param name="prevView">前の画面</param>
		public virtual void Open(UguiView prevView)
		{
			if (this.status == Status.Closing)
			{
				CancelClosing();
			}
			this.status = Status.Opening;
			this.prevView = prevView;
			this.gameObject.SetActive(true);
			ChangeBgm();
			this.gameObject.SendMessage("OnOpen", SendMessageOptions.DontRequireReceiver);
			onOpen.Invoke();
			StartCoroutine(CoOpening());
		}

		protected virtual IEnumerator CoOpening()
		{
			ITransition[] transitions = this.gameObject.GetComponents<ITransition>();
			foreach (ITransition transition in transitions)
			{
				transition.Open();
			}

			while (!Array.TrueForAll(transitions,item => !item.IsPlaying))
			{
				yield return null;
			}
			RestoreCanvasGroupInput();
			this.status = Status.Opened;
			//開く処理終了を呼ぶ
			this.gameObject.SendMessage("OnEndOpen", SendMessageOptions.DontRequireReceiver);
		}

		/// <summary>
		/// 画面を閉じる
		/// </summary>
		public virtual void Close()
		{
			if (this.gameObject.activeSelf)
			{
				//閉じる処理開始処理を呼ぶ
				this.gameObject.SendMessage("OnBeginClose", SendMessageOptions.DontRequireReceiver);
				StartCoroutine(CoClosing());
			}
		}

		protected virtual IEnumerator CoClosing()
		{
			this.status = Status.Closing;

			StoreAndChangeCanvasGroupInput(true);
			ITransition[] transitions = this.gameObject.GetComponents<ITransition>();
			foreach (ITransition transition in transitions)
			{
				transition.Close();
			}

			while (!Array.TrueForAll(transitions,item => !item.IsPlaying))
			{
				yield return null;
			}
			RestoreCanvasGroupInput();
			EndClose();
		}

		//閉じる処理をキャンセル
		protected virtual void CancelClosing()
		{
			ITransition[] transitions = this.gameObject.GetComponents<ITransition>();
			foreach (ITransition transition in transitions)
			{
				transition.CancelClosing();
			}
			RestoreCanvasGroupInput();
			EndClose();
		}

		//閉じる処理終了
		protected virtual void EndClose()
		{
			//閉じる処理を呼ぶ
			this.gameObject.SendMessage("OnClose", SendMessageOptions.DontRequireReceiver);
			onClose.Invoke();
			this.gameObject.SetActive(false);
			this.status = Status.Closed;
		}

		//キャンバスグループの入力受けつけ状態を保存
		protected bool storedCanvasGroupInteractable;
		protected bool storedCanvasGroupBlocksRaycasts;
		protected bool isStoredCanvasGroupInfo;

		//キャンバスグループの入力受付状態を保存してから変える
		protected void StoreAndChangeCanvasGroupInput(bool isActive)
		{
			storedCanvasGroupInteractable = CanvasGroup.interactable;
			storedCanvasGroupBlocksRaycasts = CanvasGroup.blocksRaycasts;

			CanvasGroup.interactable = false;
			CanvasGroup.blocksRaycasts = false;
			isStoredCanvasGroupInfo = true;
		}

		//キャンバスグループの入力受付状態を保存して状態に戻す
		protected void RestoreCanvasGroupInput()
		{
			if (isStoredCanvasGroupInfo)
			{
				CanvasGroup.interactable = storedCanvasGroupInteractable;
				CanvasGroup.blocksRaycasts = storedCanvasGroupBlocksRaycasts;
				isStoredCanvasGroupInfo = false;
			}
		}

		/// <summary>
		/// 画面の開閉をする
		/// </summary>
		public virtual void ToggleOpen(bool isOpen)
		{
			if (isOpen)
			{
				Open();
			}
			else
			{
				Close();
			}
		}


		/// <summary>
		/// 前の画面に戻る
		/// </summary>
		public virtual void Back()
		{
			Close();
			if (null != prevView) prevView.Open(prevView.prevView);
		}

		/// <summary>
		/// 戻るボタンが押された
		/// </summary>
		/// <param name="button">押されたボタン</param>
		public virtual void OnTapBack()
		{
			Back();
		}

		/// <summary>
		/// BGMを変更
		/// </summary>
		protected virtual void ChangeBgm()
		{
			if (Bgm)
			{
				if (SoundManager.GetInstance())
				{
					if (IntroTime > 0)
					{
						SoundManager.GetInstance().PlayBgm(bgm, IntroTime, FadeInTime);
					}
					else
					{
						SoundManager.GetInstance().PlayBgm(bgm, true, FadeInTime);
					}
				}
			}
			else if (IsStopBgmIfNoneBgm)
			{
				if (SoundManager.GetInstance())
				{
					SoundManager.GetInstance().StopBgm();
				}
			}
		}
	}
}
