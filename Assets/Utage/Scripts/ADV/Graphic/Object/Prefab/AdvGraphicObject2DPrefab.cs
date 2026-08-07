// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// フェード切り替え機能つきのスプライト表示
	/// </summary>
	[AddComponentMenu("Utage/ADV/Internal/GraphicObject/AdvGraphicObject2DPrefab")]
	public class AdvGraphicObject2DPrefab : AdvGraphicObjectPrefabBase
	{
		protected SpriteRenderer sprite;

		//********描画時のリソース変更********//
		protected override void ChangeResourceOnDrawSub(AdvGraphicInfo graphic)
		{
			if (this.Layer != null && !this.Layer.Manager.IgnoreOverride2DPrefabSortingOrder)
			{
				OverrideSortingOrder();
			}
		}

		//SortingOrderの上書き
		protected void OverrideSortingOrder()
		{
			this.sprite = currentObject.GetComponent<SpriteRenderer>();
			var canvas = this.Layer.Canvas;
			if(canvas==null)
			{
				return;
			}
			//SortingOrderの上書きのイベントがある場合
			var eventSortingOrder = currentObject.GetComponent<IAdvGraphicEventSortingOrder>();
			if ( eventSortingOrder!= null)
			{
				eventSortingOrder.SetSortingOrder(canvas.sortingOrder);
				return;
			}
			
			if (sprite != null)
			{
				sprite.sortingOrder = canvas.sortingOrder;
			}
		}


		//エフェクト用の色が変化したとき
		public override void OnEffectColorsChange(AdvEffectColor color)
		{
			if (sprite == null) return;
			sprite.color = color.MulColor;
		}
	}
}
