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
	//Imageに設定するSpriteを、インデックスによって左、中央、右の3種類に切り替える
	//動的に作成するタブボタンなどに使用する
	[AddComponentMenu("Utage/Lib/UI/UguiIndexImageLeftCenterRight")]
	public class UguiIndexImageLeftCenterRight : MonoBehaviour, IUguiIndex
	{
		public int Index { get; set;}
		public Image Image => this.GetComponentCache(ref image);
		Image image;

		public Sprite left;
		public Sprite center;
		public Sprite right;

		public virtual void SetIndex(int index, int length)
		{
			Index = index;

			//右、左、その他でSpriteを切り替える
			if (index <= 0)
			{
				//Left
				Image.sprite = left;
			}
			else if (index >= length - 1)
			{
				//Right
				Image.sprite = right;
			}
			else
			{
				//Center
				Image.sprite = center;
			}
		}
	}
}
