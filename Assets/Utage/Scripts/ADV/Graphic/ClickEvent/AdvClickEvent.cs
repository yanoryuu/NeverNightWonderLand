// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimurausing System;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{
	/// <summary>
	/// 基本オブジェクトのクリックイベント管理
	/// </summary>
	[AddComponentMenu("Utage/ADV/Internal/AdvClickEvent")]
	internal class AdvClickEvent : MonoBehaviour, IPointerClickHandler, IAdvClickEvent
	{
		AdvGraphicBase AdvGraphic { get { return this.GetComponentCache<AdvGraphicBase>(ref advGraphic); } }
		AdvGraphicBase advGraphic;

		StringGridRow Row { get; set; }
		UnityAction<BaseEventData> action { get; set; }
		

		public void OnPointerClick(PointerEventData eventData)
		{
			if (action != null)
			{
				action.Invoke(eventData);
			}
		}

		/// <summary>
		/// クリックイベントを設定
		/// </summary>
		public virtual void AddClickEvent(bool isPolygon, StringGridRow row, UnityAction<BaseEventData> action)
		{
			this.Row = row;
			this.action = action;
			SetEnableCanvasRaycaster(true);
			foreach (var advClickEventCustom in this.GetComponentsInChildren<IAdvClickEventCustom>(true))
			{
				advClickEventCustom.OnAddClickEvent();
			}
		}

		/// <summary>
		/// クリックイベントを削除
		/// </summary>
		public virtual void RemoveClickEvent()
		{
			this.Row = null;
			this.action = null;
			SetEnableCanvasRaycaster(false);
			foreach (var advClickEventCustom in this.GetComponentsInChildren<IAdvClickEventCustom>(true))
			{
				advClickEventCustom.OnRemoveClickEvent();
			}
		}

		void SetEnableCanvasRaycaster(bool enable)
		{
			Canvas canvas = this.GetComponentInParent<Canvas>();
			if (canvas == null) return;

			GraphicRaycaster graphicRaycaster = canvas.GetComponentCreateIfMissing<GraphicRaycaster>();
			graphicRaycaster.enabled = enable;
		}
	}
}
