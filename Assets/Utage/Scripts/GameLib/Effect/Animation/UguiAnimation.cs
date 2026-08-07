// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{
	[System.Flags]
	public enum UiEventMask
	{
		BeginDrag = 0x1 << 0,
		Cancel = 0x1 << 1,
		Deselect = 0x1 << 2,
		Drag = 0x1 << 3,
		Drop = 0x1 << 4,
		EndDrag = 0x1 << 5,
		InitializePotentialDrag = 0x1 << 6,
		Move = 0x1 << 7,
		PointerClick = 0x1 << 8,
		PointerDown = 0x1 << 9,
		PointerEnter = 0x1 << 10,
		PointerExit = 0x1 << 11,
		PointerUp = 0x1 << 12,
		Scroll = 0x1 << 13,
		Select = 0x1 << 14,
		Submit = 0x1 << 15,
		UpdateSelected = 0x1 << 16,
	}

	/// <summary>
	/// UI用の簡易アニメーション処理
	/// </summary>
	public abstract class UguiAnimation : CurveAnimation,
		IBeginDragHandler, ICancelHandler, IDeselectHandler, IDragHandler, IDropHandler, IEndDragHandler, IInitializePotentialDragHandler, IMoveHandler, IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IScrollHandler, ISelectHandler, ISubmitHandler, IUpdateSelectedHandler
	{
		public UiEventMask EventMask { get { return eventMask; } set { eventMask = value; } }
		[SerializeField,EnumFlagsAttribute]
		UiEventMask eventMask = UiEventMask.PointerClick;

		public enum AnimationType
		{
			To,			//現在の値から、指定の値までのアニメ
			From,		//指定の値から、現在の値までのアニメ
			FromTo,		//指定の値から、指定の値までのアニメ
			By,			//現在の値に加算するアニメ
		};

		public AnimationType Type { get { return animationType; } set { animationType = value; } }
		[SerializeField]
		AnimationType animationType;

		public Graphic TargetGraphic { get { return targetGraphic; } set { targetGraphic = value; } }
		[SerializeField]
		Graphic targetGraphic;

		protected void Reset()
		{
			targetGraphic = GetComponent<Graphic>();
		}

		public void Play()
		{
			Play(null);
		}
		public void Play(Action onComplete)
		{
			StartAnimation();
			PlayAnimation(UpdateAnimation, onComplete);
		}

		protected abstract void StartAnimation();
		protected abstract void UpdateAnimation(float value);

		protected virtual bool CheckEventMask(UiEventMask mask)
		{
			return (EventMask & mask) == mask;
		}
		protected virtual void PlayOnEvent(UiEventMask mask)
		{
			if (CheckEventMask(mask))
				Play ();
		}

		public virtual void OnBeginDrag (PointerEventData eventData)
		{
			PlayOnEvent(UiEventMask.BeginDrag);
		}
		public virtual void OnCancel (BaseEventData eventData)
		{
			PlayOnEvent(UiEventMask.Cancel);
		}
		public virtual void OnDeselect (BaseEventData eventData)
		{
			PlayOnEvent(UiEventMask.Deselect);
		}
		public virtual void OnDrag (PointerEventData eventData)
		{
			PlayOnEvent(UiEventMask.Drag);
		}
		public virtual void OnDrop (PointerEventData eventData)
		{
			PlayOnEvent(UiEventMask.Drop);
		}
		public virtual void OnEndDrag (PointerEventData eventData)
		{
			PlayOnEvent(UiEventMask.EndDrag);
		}
		public virtual void OnInitializePotentialDrag (PointerEventData eventData)
		{
			PlayOnEvent(UiEventMask.InitializePotentialDrag);
		}
		public virtual void OnMove (AxisEventData eventData)
		{
			PlayOnEvent(UiEventMask.Move);
		}
		public virtual void OnPointerClick (PointerEventData eventData)
		{
			PlayOnEvent(UiEventMask.PointerClick);
		}
		public virtual void OnPointerDown (PointerEventData eventData)
		{
			PlayOnEvent(UiEventMask.PointerDown);
		}
		public virtual void OnPointerEnter (PointerEventData eventData)
		{
			PlayOnEvent(UiEventMask.PointerEnter);
		}
		public virtual void OnPointerExit (PointerEventData eventData)
		{
			PlayOnEvent(UiEventMask.PointerExit);
		}
		public virtual void OnPointerUp (PointerEventData eventData)
		{
			PlayOnEvent(UiEventMask.PointerUp);
		}
		public virtual void OnScroll (PointerEventData eventData)
		{
			PlayOnEvent(UiEventMask.Scroll);
		}
		public virtual void OnSelect (BaseEventData eventData)
		{
			PlayOnEvent(UiEventMask.Select);
		}
		public virtual void OnSubmit (BaseEventData eventData)
		{
			PlayOnEvent(UiEventMask.Submit);
		}
		public virtual void OnUpdateSelected (BaseEventData eventData)
		{
			PlayOnEvent(UiEventMask.UpdateSelected);
		}
	}
}