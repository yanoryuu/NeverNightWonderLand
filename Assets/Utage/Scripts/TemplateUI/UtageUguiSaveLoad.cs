// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Utage;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// セーブロード画面のサンプル
	/// </summary>
	[AddComponentMenu("Utage/TemplateUI/UtageUguiSaveLoad")]
	public class UtageUguiSaveLoad : UguiView
	{
		[SerializeField] protected UguiGridPage gridPage;

		/// <summary>
		/// リストビューアイテムのリスト
		/// </summary>
		protected List<AdvSaveData> itemDataList;

		/// <summary>ADVエンジン</summary>
		public virtual AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] protected AdvEngine engine;

		/// <summary>メイン画面</summary>
		public UtageUguiMainGame mainGame;

		/// <summary>タイトル表記（セーブ画面かロード画面か）</summary>
		public GameObject saveRoot;

		/// <summary>タイトル表記（セーブ画面かロード画面か）</summary>
		public GameObject loadRoot;

		//ガイドメッセージの表示。設定してないときは表示しない
		public SystemUiGuideMessage guideMessage;
		//上書きセーブの確認ダイアログの表示。設定してないときは表示しない
		public SystemUiDialog2Button dialog;

		//ロード後、画面を閉じるまでの待機時間
		public float waitTimeOnLoad = 0;

		//セーブ画面か、ロード画面かの区別
		public bool IsSave => isSave;
		protected bool isSave;

		protected bool isInit = false;
		protected int lastPage;


		/// <summary>
		/// セーブ画面を開く
		/// </summary>
		/// <param name="prev">前の画面</param>
		public virtual void OpenSave(UguiView prev)
		{
			isSave = true;
			saveRoot.SetActive(true);
			loadRoot.SetActive(false);
			Open(prev);
		}

		/// <summary>
		/// ロード画面を開く
		/// </summary>
		/// <param name="prev">前の画面</param>
		public virtual void OpenLoad(UguiView prev)
		{
			isSave = false;
			saveRoot.SetActive(false);
			loadRoot.SetActive(true);
			Open(prev);
		}

		/// <summary>
		/// オープンしたときに呼ばれる
		/// </summary>
		protected virtual void OnOpen()
		{
			isInit = false;
			this.gridPage.ClearItems();
			StartCoroutine(CoWaitOpen());
		}

		/// <summary>
		/// クローズしたときに呼ばれる
		/// </summary>
		protected virtual void OnClose()
		{
			lastPage = gridPage.CurrentPage;
			this.gridPage.ClearItems();
		}

		//起動待ちしてから開く
		protected virtual IEnumerator CoWaitOpen()
		{
			while (Engine.IsWaitBootLoading)
			{
				yield return null;
			}

			AdvSaveManager saveManager = Engine.SaveManager;
			saveManager.ReadAllSaveData();
			List<AdvSaveData> list = new List<AdvSaveData>();
			if (saveManager.IsAutoSave) list.Add(saveManager.AutoSaveData);
			list.AddRange(saveManager.SaveDataList);
			this.itemDataList = list;
			gridPage.Init(itemDataList.Count, CallBackCreateItem);
			gridPage.CreateItems(lastPage);
			isInit = true;
		}


		/// <summary>
		/// リストビューのアイテムが作成されるときに呼ばれるコールバック
		/// </summary>
		/// <param name="go">作成されたアイテムのGameObject</param>
		/// <param name="index">作成されたアイテムのインデックス</param>
		protected virtual void CallBackCreateItem(GameObject go, int index)
		{
			UtageUguiSaveLoadItem item = go.GetComponent<UtageUguiSaveLoadItem>();
			AdvSaveData data = itemDataList[index];
			if (guideMessage != null)
			{
				//確認ダイアログやガイドメッセージを表示する場合はこっち
				//ボタンクリックのコールバックを設定しないので、ボタンプレハブのインスペクター上で設定しておくこと
				item.Init(data, index, isSave);
			}
			else
			{
				//確認ダイアログやガイドメッセージを表示しない場合はこっち
				item.Init(data, OnTap, index, isSave);
			}
		}

		protected virtual void Update()
		{
			//右クリックで戻る
			if (isInit && InputUtil.IsInputGuiClose())
			{
				Back();
			}
		}


		/// <summary>
		/// 各アイテムが押された
		/// </summary>
		/// <param name="button">押されたアイテム</param>
		public virtual void OnTap(UtageUguiSaveLoadItem item)
		{
			if (isSave)
			{
				//セーブ画面なら、セーブ処理
				Engine.WriteSaveData(item.Data);
				item.Refresh(true);
			}
			else
			{
				//ロード画面
				if (item.Data.IsSaved)
				{
					//セーブ済みのデータならこの画面は閉じてロードをする
					if (waitTimeOnLoad <= 0)
					{
						Close();
						mainGame.OpenLoadGame(item.Data);
					}
					else
					{
						mainGame.OpenLoadGame(item.Data);
						StartCoroutine(CoWaitOnLoad(item));
					}
				}
			}
		}

		
		protected virtual IEnumerator CoWaitOnLoad(UtageUguiSaveLoadItem item)
		{
			this.StoreAndChangeCanvasGroupInput(false);
			yield return new WaitForSeconds(waitTimeOnLoad);
			this.RestoreCanvasGroupInput();
			Close();
		}

		// 各アイテムが押された
		// 宴4以降 
		public virtual void OnClicked(UtageUguiSaveLoadItem item)
		{
			if (isSave)
			{
				//セーブ画面の処理

				if (item.Data.Type == AdvSaveData.SaveDataType.Auto)
				{
					//オートセーブならセーブでできない
					if (guideMessage != null)
					{
						guideMessage.Open(LanguageSystemText.LocalizeText(SystemText.UtageGuideMessageSaveFailedAutoSave));
					}
				}
				else
				{
					void WriteSaveData()
					{
						//セーブ画面なら、セーブ処理
						Engine.WriteSaveData(item.Data);
						item.Refresh(true);
					}

					if (item.Data.IsSaved)
					{
						//既にセーブされている
						if (dialog != null)
						{
							//上書き確認ダイアログを表示
							dialog.OpenYesNo(
								LanguageSystemText.LocalizeText(SystemText.UtageDialogMessageSaveConfirm), WriteSaveData,
								() => { });
						}
						else
						{
							WriteSaveData();
						}
					}
					else
					{
						WriteSaveData();
					}

				}
			}
			else
			{
				//ロード画面の処理
				if (item.Data.IsSaved)
				{
					//セーブ済みのデータならこの画面は閉じてロードをする
					if (waitTimeOnLoad <= 0)
					{
						Close();
						mainGame.OpenLoadGame(item.Data);
					}
					else
					{
						mainGame.OpenLoadGame(item.Data);
						StartCoroutine(CoWaitOnLoad(item));
					}
				}
				else
				{
					//セーブされていないデータなら、エラーメッセージを表示する
					if (guideMessage!=null)
					{
						guideMessage.Open(LanguageSystemText.LocalizeText(SystemText.UtageGuideMessageLoadFailedNotSaved));
					}
				}
			}
		}
	}
}
