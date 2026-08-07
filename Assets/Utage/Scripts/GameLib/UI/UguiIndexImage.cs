// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{
	//Imageに設定するSpriteを、インデックスによって変化させる
	[AddComponentMenu("Utage/Lib/UI/UguiIndexImage")]
	public class UguiIndexImage : MonoBehaviour, IUguiIndex
	{
		public int Index { get; set;}
		public Image Image => this.GetComponentCache(ref image);
		Image image;

		public List<Sprite> Sprites => sprites;
		[SerializeField] List<Sprite> sprites = new List<Sprite>();

		public virtual void SetIndex(int index, int length)
		{
			Index = index;
			Image.sprite = Sprites[index];
		}
	}
}
