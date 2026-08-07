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

	[AddComponentMenu("Utage/ADV/Internal/GraphicObject/Adv2DPrefabSortingOrder")]
	public class Adv2DPrefabSortingOrder : MonoBehaviour
		, IAdvGraphicEventSortingOrder
	{
		public int offsetSortingOrder = 0;
		
		public void SetSortingOrder(int sortingOrder)
		{
			var sprite = this.GetComponent<SpriteRenderer>();
			if (sprite != null)
			{
				sprite.sortingOrder = sortingOrder + offsetSortingOrder;
			}
		}
	}
}
