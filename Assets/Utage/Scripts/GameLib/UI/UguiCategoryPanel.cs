// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{

	/// 動的に作成するカテゴリタブつきのパネル制御（ページ切り替え機能なし）
	[AddComponentMenu("Utage/Lib/UI/UguiCategoryGrid")]
	public class UguiCategoryPanel : MonoBehaviour
	{
		public UguiToggleGroupIndexed categoryToggleGroup;
		public RectTransform rootCategory;
		public GameObject categoryTabPrefab;

		public RectTransform rootItems;
		public GameObject gridItemPrefab;
		
		/// カテゴリのリスト
		public List<string> Categories { get; protected set; }

		public int CategoryCount => Categories.Count;

		//開くときの前回のインデックスを維持する（falseなら0にリセット）
		public bool keepCurrentIndexOnOpen = false;

		//現在のカテゴリ
		public string CurrentCategory
		{
			get
			{
				return GetCategory(categoryToggleGroup.CurrentIndex); 
			}
		}

		//指定インデックスのカテゴリ取得
		public string GetCategory(int index)
		{
			if (index < 0 || index >= Categories.Count) return "";
			return Categories[index];
		}

		public virtual void Clear()
		{
			categoryToggleGroup.ClearToggles();
			categoryToggleGroup.OnValueChanged.RemoveAllListeners();
			rootCategory.DestroyChildren();
			rootItems.DestroyChildren();
		}

		public virtual void Init(string[] categoryList, System.Action<UguiCategoryPanel> onOpenCurrentCategory)
		{
			this.Categories = new List<string>(categoryList);
			categoryToggleGroup.ClearToggles();
			rootCategory.DestroyChildren();
			if (categoryList.Length > 1)
			{
				IEnumerable<Toggle> CreateToggles()
				{
					for (int i = 0; i < categoryList.Length; i++)
					{
						var go = rootCategory.AddChildPrefab(categoryTabPrefab);
						yield return go.GetComponent<Toggle>();
					}
				}
				categoryToggleGroup.AddToggles(CreateToggles());

				int index = 0;
				if (keepCurrentIndexOnOpen && categoryToggleGroup.CurrentIndex >= 0 && categoryToggleGroup.CurrentIndex < categoryList.Length)
				{
					index = categoryToggleGroup.CurrentIndex;
				}
				categoryToggleGroup.CurrentIndex = index;
			}

			categoryToggleGroup.OnValueChanged.AddListener((int index) => onOpenCurrentCategory(this) );
			onOpenCurrentCategory(this);
		}
		
		//現在のカテゴリを開く
		public virtual void OpenCurrentCategory(int itemCount, System.Action<GameObject, int> CreateItem)
		{
			//表示アイテムを再作成
			rootItems.DestroyChildren();
			for (int i = 0; i < itemCount; i++)
			{
				var go = this.rootItems.AddChildPrefab(gridItemPrefab);
				CreateItem(go, i);
			}
		}
	}
}
