// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utage;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// サウンドルーム画面のサンプル
	/// </summary>
	[AddComponentMenu("Utage/TemplateUI/UtageUguiSoundRoom")]
	public class UtageUguiSoundRoom : UguiView
	{
		public UtageUguiGallery Gallery
		{
			get { return this.GetComponentCacheFindIfMissing(ref gallery); }
		}

		[SerializeField] protected UtageUguiGallery gallery;

		/// リストビュー(旧仕様)
		public UguiListView listView;

		/// 各サウンド再生ボタンのルートオブジェクト
		public RectTransform rootItems;
		public GameObject itemPrefab;

		/// <summary>
		/// リストビューアイテムのリスト
		/// </summary>
		protected List<AdvSoundSettingData> itemDataList = new List<AdvSoundSettingData>();

		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] protected AdvEngine engine;

		protected bool isInit = false;
		protected bool isChangedBgm = false;

		/// <summary>
		/// オープンしたときに呼ばれる
		/// </summary>
		protected virtual void OnOpen()
		{
			isInit = false;
			isChangedBgm = false;
			ClearItems();
			StartCoroutine(CoWaitOpen());
		}

		/// <summary>
		/// クローズしたときに呼ばれる
		/// </summary>
		protected virtual void OnClose()
		{
			isInit = false;
			ClearItems();
			if (isChangedBgm) Engine.SoundManager.StopAll(0.2f);
			isChangedBgm = false;
		}

		//起動待ちしてから開く
		protected virtual IEnumerator CoWaitOpen()
		{
			while (Engine.IsWaitBootLoading)
			{
				yield return null;
			}
			CreateItems();
			isInit = true;
		}
		
		//アイテム（ボタン）消去
		protected virtual void ClearItems()
		{
			if (this.listView != null)
			{
				this.listView.ClearItems();
			}
			else if (rootItems != null)
			{
				rootItems.DestroyChildren();
			}
		}

		//アイテム（ボタン）作成
		protected virtual void CreateItems()
		{
			itemDataList = Engine.DataManager.SettingDataManager.SoundSetting.GetSoundRoomList();
			if (this.listView != null)
			{
				listView.CreateItems(itemDataList.Count, CallBackCreateItem);
			}
			else if (rootItems != null)
			{
				rootItems.DestroyChildren();
				for (var i = 0; i < itemDataList.Count; i++)
				{
					var go = rootItems.AddChildPrefab(itemPrefab);
					CallBackCreateItem(go, i);
				}
			}
		}


		/// <summary>
		/// リストビューのアイテムが作成されるときに呼ばれるコールバック
		/// </summary>
		/// <param name="go">作成されたアイテムのGameObject</param>
		/// <param name="index">作成されたアイテムのインデックス</param>
		protected virtual void CallBackCreateItem(GameObject go, int index)
		{
			UtageUguiSoundRoomItem item = go.GetComponent<UtageUguiSoundRoomItem>();
			AdvSoundSettingData data = itemDataList[index];
			item.Init(data, OnTap, index);
		}

		protected virtual void Update()
		{
			//右クリックで戻る
			if (isInit && InputUtil.IsInputGuiClose())
			{
				Gallery.Back();
			}
		}

		/// <summary>
		/// 各アイテムが押された
		/// </summary>
		/// <param name="button">押されたアイテム</param>
		protected virtual void OnTap(UtageUguiSoundRoomItem item)
		{
			AdvSoundSettingData data = item.Data;
			string path = Engine.DataManager.SettingDataManager.SoundSetting.LabelToFilePath(data.Key, SoundType.Bgm);

			StartCoroutine(CoPlaySound(path));
		}

		//サウンドをロードして鳴らす
		protected virtual IEnumerator CoPlaySound(string path)
		{
			isChangedBgm = true;
			AssetFile file = AssetFileManager.Load(path, this);
			while (!file.IsLoadEnd) yield return null;
			Engine.SoundManager.PlayBgm(file);
			file.Unuse(this);
		}
	}
}
