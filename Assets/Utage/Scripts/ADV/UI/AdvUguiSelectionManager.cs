// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using Utage;
using System.Collections.Generic;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// 選択肢表示のサンプル
	/// </summary>
	[AddComponentMenu("Utage/ADV/AdvUguiSelectionManager")]
	public class AdvUguiSelectionManager : MonoBehaviour
		, IAdvEngineGetter
	{
		//ADVエンジン
		public AdvEngine Engine
		{
			get => engine;
			protected set => engine = value;
		}
		[SerializeField]
		protected AdvEngine engine;
		public AdvEngine AdvEngineGetter => Engine;


		/// <summary>選択済みのテキスト色</summary>
		/// <summary>選択済みのテキスト色を変えるか</summary>
		protected enum SelectedColorMode
		{
			None,		//選択済みでも変えない
			Change,		//選択済みで色を変える
		}
		[SerializeField]
		protected SelectedColorMode selectedColorMode = SelectedColorMode.None;
		
		/// <summary>選択済みのテキスト色</summary>
		[SerializeField]
		protected Color selectedColor = new Color(0.8f,0.8f,0.8f);


		//表示する選択肢プレハブのリスト
		public List<GameObject> PrefabList { get { return prefabList; } }
		[SerializeField]
		protected List<GameObject> prefabList;

		protected AdvSelectionManager SelectionManager { get { return engine.SelectionManager; } }

		/// <summary>選択肢のリストビュー</summary>
		public UguiListView ListView
		{
			get { return this.GetComponentCache( ref listView); }
		}
		UguiListView listView;


		CanvasGroup CanvasGroup
		{
			get
			{
				return this.gameObject.GetComponentCacheCreateIfMissing<CanvasGroup>(ref canvasGroup);
			}
		}
		CanvasGroup canvasGroup;

		public List<GameObject> Items { get { return items; } }
		List<GameObject> items = new List<GameObject>();
		
		//初期化済みかのフラグ
		private bool IsInitialized { get; set; }

		/// <summary>開く</summary>
		public virtual void Open()
		{
			this.gameObject.SetActive(true);
		}

		/// <summary>閉じる</summary>
		public virtual void Close()
		{
			this.gameObject.SetActive(false);
		}

		protected virtual void Awake()
		{
			InitSub();
		}
		
		//外部からAdvEngineを設定する
		public void InitEngine(AdvEngine advEngine)
		{
			Engine = advEngine;
			InitSub();
		}

		//外部からAdvEngineを設定する
		public void ReleaseEngine()
		{
			ReleaseEvents();
			Engine = null;
		}

		protected virtual void InitSub()
		{
			if( IsInitialized) return;
			if (Engine == null || SelectionManager == null) return;

			SelectionManager.OnClear.AddListener(OnClear);
			SelectionManager.OnBeginShow.AddListener(OnBeginShow);
			SelectionManager.OnBeginWaitInput.AddListener(OnBeginWaitInput);
			ClearAll();
			IsInitialized = true;
		}
		

		protected virtual void ReleaseEvents()
		{
			if(!IsInitialized) return;
			if (Engine != null && SelectionManager != null)
			{
				SelectionManager.OnClear.RemoveListener(OnClear);
				SelectionManager.OnBeginShow.RemoveListener(OnBeginShow);
				SelectionManager.OnBeginWaitInput.RemoveListener(OnBeginWaitInput);
			}
			IsInitialized = false;
		}

		//全てクリア
		protected virtual void ClearAll()
		{
			ListView.ClearItems();
			foreach (var item in Items)
			{
				GameObject.Destroy(item);
			}
			Items.Clear();
		}

		//選択肢を作成
		protected virtual void CreateItems()
		{
			ClearAll();

			List<GameObject> listViewItems = new List<GameObject>();
			foreach( var data in SelectionManager.Selections )
			{
				GameObject go = GameObject.Instantiate(GetPrefab(data)) as GameObject;
				AdvUguiSelection selection = go.GetComponentInChildren<AdvUguiSelection>();
				if(selection) selection.Init(data, OnTap);

				switch (selectedColorMode)
				{
					case SelectedColorMode.Change:
						if (Engine.SystemSaveData.SelectionData.Check(data))
						{
							selection.OnInitSelected(selectedColor);
//							go.SendMessage("OnInitSelected", selectedColor);
						}
						break;
					case SelectedColorMode.None:
					default:
						break;
				}


				Items.Add(go);
				//X,Y座標の指定がないならリストビューに追加
				if (data.X == null || data.Y == null)
				{
					listViewItems.Add(go);
				}
				else
				{
					this.transform.AddChild(go, new Vector3(data.X.Value, data.Y.Value, 0));
				}
			}
			ListView.AddItems(listViewItems);
			ListView.Reposition();
		}

		//選択肢表示に使うプレハブを取得
		protected virtual GameObject GetPrefab( AdvSelection selectionData )
		{
			GameObject prefab = null;
			if (!string.IsNullOrEmpty(selectionData.PrefabName))
			{
				//プレハブ名と一致するものを探す
				prefab = PrefabList.Find(x => x.name == selectionData.PrefabName);
				if (prefab != null)
				{
					return prefab;
				}
				else
				{
					//見つからないとき
					Debug.LogError("Not found Selection Prefab : " + selectionData.PrefabName);
				}
			}

			//プレハブ名の指定がなかったら、リストの一番目を
			//旧バージョンとの互換性のためListViewから選ぶ
			prefab = PrefabList.Count > 0 ? PrefabList[0] : ListView.ItemPrefab;
			return prefab;
		}


		//選択肢が押された
		protected virtual void OnTap(AdvUguiSelection item)
		{
			SelectionManager.Select(item.Data );
			ClearAll();
		}

		//クリア
		public virtual void OnClear(AdvSelectionManager manager)
		{
			ClearAll();
		}
		
		//表示開始
		public virtual void OnBeginShow( AdvSelectionManager manager )
		{
			CreateItems();
			CanvasGroup.interactable = false;
		}

		//入力待ち開始
		public virtual void OnBeginWaitInput(AdvSelectionManager manager)
		{
			CanvasGroup.interactable = true;
		}
	}
}
