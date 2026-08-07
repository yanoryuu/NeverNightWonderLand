// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using Utage;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{
	/// コンフィグ画面をタブボタンで表示パネルを切り替える処理
	[AddComponentMenu("Utage/TemplateUI/UtageUguiConfig")]
	public class UtageUguiConfigTabPanels : MonoBehaviour
	{
		public List<GameObject> tabPanels = new List<GameObject>();
		public void OnChangedTabIndex(int index)
		{
			if (index < 0 || index >= tabPanels.Count)
			{
				//タブインデックスが範囲外です
				Debug.LogError($"Tab index {index}is out of range");
				return;
			}

			for (int i = 0; i < tabPanels.Count; i++)
			{
				var panel = tabPanels[i];
				if (i == index)
				{
					panel.SetActive(true);
				}
				else
				{
					panel.SetActive(false);
				}
			}
		}
	}
}
