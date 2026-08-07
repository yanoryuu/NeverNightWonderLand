// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Utage
{

	/// <summary>
	/// カテゴリタブつきのグリッドページ
	/// </summary>
	[AddComponentMenu("Utage/Lib/UI/UguiCategoryGridPage")]
	public class UguiCategoryGridPage : MonoBehaviour
	{
		/// <summary>
		/// グリッドビュー
		/// </summary>
		public UguiGridPage gridPage;

		/// <summary>
		/// タブグループ
		/// </summary>
		public UguiToggleGroupIndexed categoryToggleGroup;
		public UguiAlignGroup categoryAlignGroup;
		public GameObject categoryPrefab;

		/// <summary>
		/// ボタンのSpriteリスト
		/// </summary>
		public List<Sprite> buttonSpriteList;

		/// <summary>カテゴリのリスト</summary>
		string[] categoryList;

		//現在のカテゴリ
		public string CurrentCategory
		{
			get
			{
				if (categoryList == null) return "";
				else if (categoryToggleGroup.CurrentIndex >= categoryList.Length) return "";
				else return categoryList[categoryToggleGroup.CurrentIndex];
			}
		}

		public virtual void Clear()
		{
			categoryToggleGroup.ClearToggles();
			categoryAlignGroup.DestroyAllChildren();
			gridPage.ClearItems();
		}

		public virtual void Init(string[] categoryList, System.Action<UguiCategoryGridPage> OpenCurrentCategory)
		{
			this.categoryList = categoryList;
			categoryToggleGroup.ClearToggles();
			categoryAlignGroup.DestroyAllChildren();
			if (categoryList.Length > 1)
			{
				List<GameObject> children = categoryAlignGroup.AddChildrenFromPrefab( categoryList.Length, categoryPrefab, CreateTabButton );
				categoryToggleGroup.AddToggles(children.Select(go => go.GetComponent<Toggle>()));
				categoryToggleGroup.CurrentIndex = 0;
			}

			categoryToggleGroup.OnValueChanged.AddListener((int index) => OpenCurrentCategory(this) );
			OpenCurrentCategory(this);
		}

		/// リストビューのアイテムが作成されるときに呼ばれるコールバック
		/// <param name="go">作成されたアイテムのGameObject</param>
		/// <param name="index">作成されたアイテムのインデックス</param>
		protected virtual void CreateTabButton(GameObject go, int index)
		{
			if (index < categoryList.Length)
			{
				SetTabText(go, LanguageManager.Instance.LocalizeText(categoryList[index]));
			}

			Image image = go.GetComponentInChildren<Image>();
			if (image && index < buttonSpriteList.Count) image.sprite = buttonSpriteList[index];
		}

		protected virtual void SetTabText(GameObject go, string text)
		{
			TextComponentWrapper.SetTextInChildren(go,text);
		}

		public virtual void OpenCurrentCategory(int itemCount, System.Action<GameObject, int> CreateItem)
		{
			gridPage.Init(itemCount, CreateItem);
			gridPage.CreateItems(0);
		}
	}
}
