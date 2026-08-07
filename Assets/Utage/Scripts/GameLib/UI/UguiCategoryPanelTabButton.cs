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
	//カテゴリタブの各ボタンのコンポーネント
	[AddComponentMenu("Utage/Lib/UI/UguiCategoryPanelTabButton")]
	public class UguiCategoryPanelTabButton : MonoBehaviour, IUguiIndex
	{
		public int Index { get; set;}
		public TextMeshProUGUI text;

		protected UguiCategoryPanel CategoryPanel => this.GetComponentCacheInParent(ref categoryPanel);
		UguiCategoryPanel categoryPanel;

		public virtual void SetIndex(int index, int length)
		{
			Index = index;
			//テキストを設定
			text.text = LanguageManager.Instance.LocalizeText( CategoryPanel.GetCategory(index));
		}
	}
}
