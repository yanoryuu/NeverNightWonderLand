// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utage;
using UtageExtensions;

namespace Utage
{
	/// <summary>
	/// CGギャラリー画面のサンプル
	/// </summary>
	[AddComponentMenu("Utage/TemplateUI/UtageUguiCgGallery")]
	public class UtageUguiCgGallery : UguiView
	{
		public UtageUguiGallery Gallery
		{
			get { return this.GetComponentCacheFindIfMissing(ref gallery); }
		}

		[SerializeField] UtageUguiGallery gallery;

		/// <summary>
		/// CG表示画面
		/// </summary>
		public UtageUguiCgGalleryViewer CgView;

		/// カテゴリつきのグリッドビュー(ページ切り替え機能付き)
		/// 宴3までの古いやり方
		[UnityEngine.Serialization.FormerlySerializedAs("categoryGirdPage")]
		public UguiCategoryGridPage categoryGridPage;

		/// カテゴリつきのグリッドビュー(ページ切り替え機能なし)
		/// 宴4以降の新しいやり方
		public UguiCategoryPanel categoryPanel;

		/// <summary>アイテムのリスト</summary>
		List<AdvCgGalleryData> itemDataList = new List<AdvCgGalleryData>();

		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] AdvEngine engine;

		//ガイドメッセージの表示。設定してないときは表示しない
		public SystemUiGuideMessage guideMessage;


		protected bool isInit = false;

		/*
			void OnEnable()
			{
				OnClose();
				OnOpen();
			}
		*/
		/// <summary>
		/// オープンしたときに呼ばれる
		/// </summary>
		protected virtual void OnOpen()
		{
			StartCoroutine(CoWaitOpen());
		}

		/// <summary>
		/// クローズしたときに呼ばれる
		/// </summary>
		protected virtual void OnClose()
		{
			if (categoryGridPage != null)
			{
				categoryGridPage.Clear();
			}
			else if(categoryPanel!=null)
			{
				categoryPanel.Clear();
			}
		}

		//ロード待ちしてから開く
		protected virtual IEnumerator CoWaitOpen()
		{
			isInit = false;
			while (Engine.IsWaitBootLoading)
			{
				yield return null;
			}
			
			
			if (categoryGridPage != null)
			{
				categoryGridPage.Init(
					Engine.DataManager.SettingDataManager.TextureSetting.CreateCgGalleryCategoryList().ToArray(),
					OpenCurrentCategory);
			}
			else if (categoryPanel != null)
			{
				// 宴4以降の新しいやり方
				categoryPanel.Init(Engine.DataManager.SettingDataManager.TextureSetting.CreateCgGalleryCategoryList()
					.ToArray(), OpenCurrentCategory);
			}
			isInit = true;
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
		/// 現在のカテゴリのページを開く（宴3までの古いやり方）
		/// </summary>
		protected virtual void OpenCurrentCategory(UguiCategoryGridPage gridPage)
		{
			itemDataList =
				Engine.DataManager.SettingDataManager.TextureSetting.CreateCgGalleryList(
					Engine.SystemSaveData.GalleryData, gridPage.CurrentCategory);
			gridPage.OpenCurrentCategory(itemDataList.Count, CreateItem);
		}

		/// <summary>
		/// リストビューのアイテムが作成されるときに呼ばれるコールバック（宴3までの古いやり方）
		/// </summary>
		/// <param name="go">作成されたアイテムのGameObject</param>
		/// <param name="index">作成されたアイテムのインデックス</param>
		protected virtual void CreateItem(GameObject go, int index)
		{
			AdvCgGalleryData data = itemDataList[index];
			UtageUguiCgGalleryItem item = go.GetComponent<UtageUguiCgGalleryItem>();
			item.Init(data, OnTap);
		}

		/// <summary>
		/// 各アイテムが押された（宴3までの古いやり方）
		/// </summary>
		/// <param name="button">押されたアイテム</param>
		protected virtual void OnTap(UtageUguiCgGalleryItem item)
		{
			CgView.Open(item.Data);
		}
		
		// 現在のカテゴリのページを開く
		// 宴4以降の新しいやり方
		protected virtual void OpenCurrentCategory(UguiCategoryPanel panel)
		{
			itemDataList =
				Engine.DataManager.SettingDataManager.TextureSetting.CreateCgGalleryList(
					Engine.SystemSaveData.GalleryData, panel.CurrentCategory);
			panel.OpenCurrentCategory(itemDataList.Count, 
				(GameObject go, int index)=>
				{
					AdvCgGalleryData data = itemDataList[index];
					UtageUguiCgGalleryItem item = go.GetComponent<UtageUguiCgGalleryItem>();
					item.Init(data);
				});
		}

		//UtageUguiCgGalleryItemがクリックされたときに、プログラムから呼ばれる
		//宴4以降の新しいやり方
		public virtual void OnClickedButton(UtageUguiCgGalleryItem item)
		{
			if (item.Data.IsOpened)
			{
				//正常に開く
				CgView.Open(item.Data);
			}
			else
			{
				//開けない
				//ガイドメッセージの表示
				if (guideMessage != null)
				{
					guideMessage.Open(LanguageSystemText.LocalizeText(SystemText.UtageGuideMessageCgGalleryNotOpened));
				}
			}
		}
	}
}
