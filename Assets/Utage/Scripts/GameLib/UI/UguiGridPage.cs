// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// CGギャラリー画面のサンプル
	/// </summary>
	[AddComponentMenu("Utage/Lib/UI/UguiGridPage")]
	public class UguiGridPage : MonoBehaviour
	{
		/// <summary>
		/// グリッドグループ
		/// </summary>
		public GridLayoutGroup grid;

		/// <summary>
		/// アイテムプレハブ
		/// </summary>
		public GameObject itemPrefab;

		/// <summary>
		/// ページ切り替えボタンのグループ
		/// </summary>
		public UguiToggleGroupIndexed pageCarouselToggles;
		public UguiAlignGroup pageCarouselAlignGroup;

		/// <summary>
		/// 
		/// </summary>
		public GameObject pageCarouselPrefab;

		/// <summary>
		/// 1ページあたりの表示アイテム数
		/// </summary>
		public int MaxItemPerPage
		{
			get
			{
				if (maxItemPerPage < 0)
				{
					Rect rect = (grid.transform as RectTransform).rect;
					int countX = GetCellCount(grid.cellSize.x, rect.size.x, grid.spacing.x);
					int countY = GetCellCount(grid.cellSize.y, rect.size.y, grid.spacing.y);

					switch (grid.constraint)
					{
						case GridLayoutGroup.Constraint.FixedColumnCount:
						countX = Mathf.Min(countX, grid.constraintCount);
							break;
						case GridLayoutGroup.Constraint.FixedRowCount:
						countY = Mathf.Min(countY, grid.constraintCount);
							break;
						case GridLayoutGroup.Constraint.Flexible:
						default:
							break;
					}
					maxItemPerPage = countX * countY;
				}
				return maxItemPerPage;
			}
		}
		int maxItemPerPage = -1;

		int GetCellCount(float cellSize, float rectSize, float space)
		{
			int count = 0;
			float size = 0;
			while (true)
			{
				size += cellSize;
				if (size > rectSize)
				{
					break;
				}
				++count;
				size += space;
			}
			return count;
		}

		/// <summary>
		/// 表示アイテムの最大数
		/// </summary>
		int maxItemNum = 0;


		//現在のページ
		public int CurrentPage { get { return currentPage; } }
		int currentPage = 0;

		//最大ページ
		public int MaxPage { get { return (maxItemNum - 1) / MaxItemPerPage; } }

		//次のページ
		public int NextPage { get { return Mathf.Min(CurrentPage + 1, MaxPage); } }
		//前のページ
		public int PrevPage { get { return Mathf.Max(CurrentPage - 1, 0); } }

		//アイテムリスト
		public List<GameObject> Items { get { return items; } }
		List<GameObject> items = new List<GameObject>();

		System.Action<GameObject, int> CallbackCreateItem;  //アイテムが作成されたときのコールバック

		void Awake()
		{
			if (pageCarouselToggles)
			{
				pageCarouselToggles.OnValueChanged.AddListener(CreateItems);
			}
		}

		//
		public void Init(int maxItemNum, System.Action<GameObject, int> callbackCreateItem)
		{
			this.maxItemNum = maxItemNum;
			this.CallbackCreateItem = callbackCreateItem;
			if (pageCarouselToggles)
			{
				pageCarouselToggles.ClearToggles();
				pageCarouselAlignGroup.DestroyAllChildren();
				if (MaxPage > 0)
				{
					List<GameObject> children = 
						pageCarouselAlignGroup.AddChildrenFromPrefab( MaxPage + 1, pageCarouselPrefab, null );
					pageCarouselToggles.AddToggles(children.Select(go => go.GetComponent<Toggle>()));
					pageCarouselToggles.CurrentIndex = 0;
					pageCarouselToggles.SetActiveLRButtons(true);
				}
				else
				{
					pageCarouselToggles.SetActiveLRButtons(false);
				}
			}
		}

		//指定のページのアイテムを作成
		public void CreateItems(int page)
		{
			this.currentPage = page;
			this.pageCarouselToggles.CurrentIndex = page;
			///いったん削除
			ClearItems();

			int pageTopIndex = MaxItemPerPage * CurrentPage;
			for (int i = 0; i < MaxItemPerPage; ++i)
			{
				int index = pageTopIndex + i;
				if (index >= maxItemNum) break;

				GameObject go = grid.transform.AddChildPrefab(itemPrefab);
				items.Add(go);
				if (CallbackCreateItem != null) CallbackCreateItem(go, index);
			}
		}

		/// <summary>
		/// アイテムをクリア
		/// </summary>
		public void ClearItems()
		{
			///閉じる
			grid.transform.DestroyChildren();
		}

		/// <summary>
		/// 次ページボタンが押された
		/// </summary>
		public void OnClickNextPage()
		{
			int nextPage = NextPage;
			if (nextPage != CurrentPage)
			{
				CreateItems(nextPage);
			}
		}

		/// <summary>
		/// 前ページボタンが押された
		/// </summary>
		public void OnClickPrevPage()
		{
			int prevPage = PrevPage;
			if (prevPage != CurrentPage)
			{
				CreateItems(prevPage);
			}
		}
	}
}
