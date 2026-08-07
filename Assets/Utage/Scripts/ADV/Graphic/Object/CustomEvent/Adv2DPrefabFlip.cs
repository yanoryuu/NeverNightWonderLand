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

	[AddComponentMenu("Utage/ADV/Internal/GraphicObject/Adv2DPrefabFlip")]
	public class Adv2DPrefabFlip : MonoBehaviour
		, IAdvGraphicEventFlip
	{
		public void Flip(bool flipX, bool flipY)
		{
			var sprite = this.GetComponent<SpriteRenderer>();
			if (sprite != null)
			{
				sprite.flipX = flipX;
				sprite.flipY = flipY;
			}
		}
	}
}
