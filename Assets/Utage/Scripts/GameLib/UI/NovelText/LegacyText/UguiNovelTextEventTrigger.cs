// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{

	[System.Serializable]
	public class OnClickLinkEvent : UnityEvent<UguiNovelTextHitArea> { }


	
	/// <summary>
	/// ノベルテキスト用の入力イベントトリガー
	/// </summary>
	[AddComponentMenu("Utage/Lib/UI/UguiNovelTextEventTrigger")]
	[RequireComponent(typeof(UguiNovelText))]
	public class UguiNovelTextEventTrigger : MonoBehaviour, ICanvasRaycastFilter, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerClickHandler
	{
		public UguiNovelTextGenerator Generator { get { return this.GetComponentCache(ref generator); } }
		UguiNovelTextGenerator generator;
		
		public UguiNovelText NovelText { get { return this.GetComponentCache(ref novelText); } }
		UguiNovelText novelText;

		RectTransform cachedRectTransform;
		public RectTransform CachedRectTransform { get { if (this.cachedRectTransform == null) cachedRectTransform = GetComponent<RectTransform>(); return cachedRectTransform; } }

		public OnClickLinkEvent OnClick = new OnClickLinkEvent();

		public Color hoverColor = ColorUtil.Red;
		UguiNovelTextHitArea currentTarget;

		bool isEntered;

		//当たり判定のチェック
		public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
		{
			UguiNovelTextHitArea target = HitTest(sp, eventCamera);
			if (isEntered) ChangeCurrentTarget(target);
			return target!=null;
		}

		//クリック
		public void OnPointerClick(PointerEventData eventData)
		{
			UguiNovelTextHitArea group = HitTest(eventData);
			if (group != null)
			{
				OnClick.Invoke(group);
			}
		}

		public void OnPointerDown(PointerEventData eventData) { }

		//当たり判定に入ったとき
		public void OnPointerEnter(PointerEventData eventData)
		{
			isEntered = true;
			ChangeCurrentTarget(HitTest(eventData));
		}

		//当たり判定から出た
		public void OnPointerExit(PointerEventData eventData)
		{
			isEntered = false;
			ChangeCurrentTarget(null);
		}

		UguiNovelTextHitArea HitTest(PointerEventData eventData)
		{
			return HitTest(eventData.position, eventData.pressEventCamera);
		}

		UguiNovelTextHitArea HitTest(Vector2 screenPoint, Camera cam)
		{
			Vector2 localPosition;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(this.CachedRectTransform, screenPoint, cam, out localPosition);
			foreach (UguiNovelTextHitArea group in Generator.HitGroupLists)
			{
				if (group.HitTest(localPosition)) return group;
			}
			return null;
		}

		void ChangeCurrentTarget(UguiNovelTextHitArea target)
		{
			if (currentTarget != target)
			{
				if (currentTarget != null) currentTarget.ResetEffectColor();

				currentTarget = target;
				if (currentTarget!=null) currentTarget.ChangeEffectColor(hoverColor);
			}
		}

		void OnDrawGizmos()
		{
			foreach (UguiNovelTextHitArea group in Generator.HitGroupLists)
			{
				foreach (Rect rect in group.HitAreaList)
				{
					Gizmos.color = Color.yellow;

					Vector3 pos = rect.center;
					Vector3 size = rect.size;
					pos = CachedRectTransform.TransformPoint(pos);
					size = CachedRectTransform.TransformVector(size);
					Gizmos.DrawWireCube(pos, size);

				}
			}
		}
	}
}
