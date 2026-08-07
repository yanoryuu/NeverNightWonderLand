// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;


namespace Utage
{

	// TIPSのマスターディテールタイプの表示画面
	//項目リストと詳細表示が1画面に表示される
	public class SampleUiTipsMasterDetail : UguiView
	{
		//ADVエンジン
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] AdvEngine engine;

		//TIPS管理
		protected TipsManager TipsManager => Engine.GetComponentCacheInChildren(ref tipsManager);
		[SerializeField] TipsManager tipsManager;

		//カテゴリのトグルグループ
		[SerializeField] UguiToggleGroupIndexed categoryToggleGroup;

		//TIPS名のトグルグループ
		[SerializeField] UguiToggleGroupIndexed tipsToggleGroup;
		//TIPS名ボタンのプレハブ
		[SerializeField] SampleUiTipsMasterDetailButton prefabTipsButton;

		//選択されたTIPSの詳細情報
		[SerializeField] TextMeshProUGUI tipsTitle;
		[SerializeField] AdvUguiLoadGraphicFile tipsImage;
		[SerializeField] TextMeshProNovelText tipsText;
		
		//詳細が開放されていない場合の表示
		[SerializeField] GameObject detailIfNotOpen;

		public UnityEvent OnInit => onInit;
		[SerializeField] UnityEvent onInit = new();

		protected bool IsInit { get; set; }

		public TipsInfo TipsInfo { get; protected set; }
		
		//TIPSカテゴリの情報
		public class TipsCategoryInfo
		{
			public string Id { get; set; }
			public List<TipsInfo> TipsList = new ();
			
			//Newアイコンの表示
			public bool IsNew()
			{
				//解放済みの未読TIPSがあるか
				return TipsList.Exists(tipsInfo => !tipsInfo.HasRead && tipsInfo.IsOpened);
			}

		}

		public Dictionary<string,TipsCategoryInfo> TipsCategories { get; } = new();


		void Awake()
		{
			//カテゴリボタンの初期化
			categoryToggleGroup.AddToggles(categoryToggleGroup.GetComponentsInChildren<Toggle>(true));
			//カテゴリ選択ボタンの処理を登録
			categoryToggleGroup.OnValueChanged.AddListener(OnChangedCategory );
			//TIPS選択ボタンの処理を登録
			tipsToggleGroup.OnValueChanged.AddListener(OnChangedTips );
			
			var scrollRect = tipsToggleGroup.GetComponentInParent<ScrollRect>();
			scrollRect.onValueChanged.AddListener(x=>Debug.Log(x));

		}


		//TIPS情報を指定して開く
		public void Open(TipsInfo tipsInfo, UguiView prevView)
		{
			TipsInfo = tipsInfo;
			base.Open(prevView);
		}

		//開く処理
		protected virtual void OnOpen()
		{
			IsInit = false;
			StartCoroutine(CoWaitOpen());
		}
		
		//閉じる処理
		protected virtual void OnClose()
		{
			TipsInfo = null;
		}

		//起動待ちしてから開く
		protected virtual IEnumerator CoWaitOpen()
		{
			while (Engine.IsWaitBootLoading) yield break;
			Init();
		}

		//ボタンを初期化
		protected virtual void Init()
		{
			InitCategories();
			InitCategoryButtons();
			InitSelected();
			IsInit = true;
			OnInit.Invoke();
		}
		
		//カテゴリ情報を初期化
		void InitCategories()
		{
			//作成済み
			if(TipsCategories.Count != 0) return;
			
			foreach (var keyValuePair in TipsManager.TipsMap)
			{
				var tipsInfo = keyValuePair.Value;
				if (tipsInfo == null) continue;

				var category = tipsInfo.Data.RowData.ParseCellOptional<string>("Category", "");

				if (!TipsCategories.TryGetValue(category, out TipsCategoryInfo categoryInfo))
				{
					categoryInfo = new TipsCategoryInfo { Id = category };
					TipsCategories.Add(category,categoryInfo);
				}
				categoryInfo.TipsList.Add(tipsInfo);
			}
		}
		
		//カテゴリボタンを初期化
		protected virtual void InitCategoryButtons()
		{
			List<SampleUiTipsMasterDetailCategoryButton> buttons = new ();
			categoryToggleGroup.GetComponentsInChildren<SampleUiTipsMasterDetailCategoryButton>(true, buttons);
			foreach (var button in buttons)
			{
				button.Init();
			}

			foreach (var category in TipsCategories)
			{
				if (!buttons.Exists(x => x.CategoryInfo == category.Value))
				{
					//カテゴリボタンが見つからない
					Debug.LogError($" CategoryButton is not found categoryId={category.Key}");
				}
			}

			foreach (var button in buttons)
			{
				if (buttons.Exists(x => x != button && x.CategoryInfo == button.CategoryInfo))
				{
					//カテゴリボタンが重複している
					Debug.LogError($" CategoryButton is duplicated categoryId={button.CategoryInfo.Id}");
				}
			}
		}

		void InitSelected()
		{
			int indexCategory = 0;
			int indexTips = 0;
			
			TipsCategoryInfo categoryInfo = null;
			
			bool reserved = TipsInfo != null;
			if (reserved)
			{
				bool isFound = false;
				//TIPSが指定済みの場合はそのインデックスを計算
				var buttons = categoryToggleGroup.GetComponentsInChildren<SampleUiTipsMasterDetailCategoryButton>(true);
				for (int i = 0; i < buttons.Length; i++)
				{
					categoryInfo = buttons[i].CategoryInfo;
					if (categoryInfo.TipsList.Contains(TipsInfo))
					{
						isFound = true;
						indexCategory = i;
						indexTips = categoryInfo.TipsList.IndexOf(TipsInfo);
						break;
					}
				}
				if (!isFound)
				{
					//指定されたTIPSが見つからない
					Debug.LogError($"TipsInfo is not found. tipsId={TipsInfo.Data.Id}");
				}
			}
			else
			{
				indexCategory = 0;
				indexTips = 0;
				categoryInfo = GetTipsCategoryInfo(indexCategory);
				TipsInfo = categoryInfo.TipsList[indexTips];
			}
			
			//指定のインデックスのカテゴリボタンを選択
			categoryToggleGroup.CurrentIndex = indexCategory;
			AdjustCategoryGroupScroll();
			
			//カテゴリに応じてTIPS選択ボタンを作成
			CreateTipsButtons(categoryInfo);
			//指定のインデックスのTIPSボタンを選択
			tipsToggleGroup.CurrentIndex = indexTips;
			AdjustTipsGroupScroll();
			
			//選択されたTIPSを表示
			SetSelectedTips(TipsInfo);
		}

		void AdjustCategoryGroupScroll()
		{
			var scrollRect = categoryToggleGroup.GetComponentInParent<ScrollRect>();
			var target = categoryToggleGroup.GetComponentsInChildren<Toggle>(true)[categoryToggleGroup.CurrentIndex].GetComponent<RectTransform>();
			StartCoroutine(HorizontalScrollTo(scrollRect, target));
		}

		void AdjustTipsGroupScroll()
		{
			var scrollRect = tipsToggleGroup.GetComponentInParent<ScrollRect>();
			var target = tipsToggleGroup.GetComponentsInChildren<Toggle>(true)[tipsToggleGroup.CurrentIndex].GetComponent<RectTransform>();
			StartCoroutine(VerticalScrollTo(scrollRect, target));
		}

		//選択カテゴリが変更された
		void OnChangedCategory(int index)
		{
			if(!IsInit) return;
			
			var categoryInfo = GetTipsCategoryInfo(index);
			CreateTipsButtons(categoryInfo);

			//TIPSの選択インデックスを初期化
			tipsToggleGroup.CurrentIndex = 0;
			AdjustTipsGroupScroll();
			var select = categoryInfo.TipsList[0];
			if (TipsInfo != select)
			{
				SetSelectedTips(select);
			}
		}

		TipsCategoryInfo GetTipsCategoryInfo(int index)
		{
			var buttons = categoryToggleGroup.GetComponentsInChildren<SampleUiTipsMasterDetailCategoryButton>(true);
			var categoryInfo = buttons[index].CategoryInfo;
			return categoryInfo;
		}


		//TIPSボタンを作成
		void CreateTipsButtons(TipsCategoryInfo categoryInfo)
		{
			//TIPSボタンの初期化
			tipsToggleGroup.transform.DestroyChildren();
			foreach (var tipsInfo in categoryInfo.TipsList)
			{
				SampleUiTipsMasterDetailButton button = tipsToggleGroup.transform.AddChildPrefab(prefabTipsButton);
				button.Init(tipsInfo);
			}

			tipsToggleGroup.ClearToggles();
			tipsToggleGroup.AddToggles(tipsToggleGroup.GetComponentsInChildren<Toggle>(true));
		}
		
		
		//TIPSの選択インデックスが変更された
		void OnChangedTips(int index)
		{
			if(!IsInit) return;
			var categoryInfo = GetTipsCategoryInfo(categoryToggleGroup.CurrentIndex);
			SetSelectedTips(categoryInfo.TipsList[index]);
		}

		//選択されたTIPSを設定
		void SetSelectedTips(TipsInfo tipsInfo)
		{
			TipsInfo = tipsInfo;
			if (!TipsInfo.IsOpened)
			{
				//未開放
				if (detailIfNotOpen)
				{
					//未解放画面を表示
					detailIfNotOpen.SetActive(true);
				}
				return;
			}
			else
			{
				if (detailIfNotOpen)
				{
					//未解放画面を表示
					detailIfNotOpen.SetActive(false);
				}
				//既読済みにする
				TipsInfo.Read();
				//各ボタンのNewアイコンを更新
				RefreshNew();
				InitTipsDetail();
			}
		}

		//Tipsの詳細表示の初期化
		protected virtual void InitTipsDetail()
		{
			if (tipsTitle)
			{
				tipsTitle.text = TipsInfo.Data.LocalizedTitle();
			}

			if (tipsText)
			{
				tipsText.SetText(TipsInfo.Data.LocalizedText());
			}

			if (tipsImage != null)
			{
				var path = TipsInfo.Data.ImageFilePath;
				if (!string.IsNullOrEmpty(path))
				{
					tipsImage.gameObject.SetActive(true);
					tipsImage.LoadTextureFile(path);
				}
				else
				{
					tipsImage.gameObject.SetActive(false);
				}
			}
		}

		//New情報を更新
		void RefreshNew()
		{
			foreach (var button in categoryToggleGroup.GetComponentsInChildren<SampleUiTipsMasterDetailCategoryButton>(true))
			{
				button.RefreshNew();
			}
			foreach (var button in tipsToggleGroup.GetComponentsInChildren<SampleUiTipsMasterDetailButton>(true))
			{
				button.RefreshNew();
			}
		}

		protected virtual void Update()
		{
			//右クリックで戻る
			if (IsInit && InputUtil.IsInputGuiClose())
			{
				Back();
			}
		}
		
		//垂直方向のスクロール位置を指定のRectTransformに移動
		IEnumerator VerticalScrollTo(ScrollRect　scrollRect, RectTransform target)
		{
			yield return new WaitForEndOfFrame();
			RectTransform content = scrollRect.content;
			var layoutGroup = content.GetComponent<VerticalLayoutGroup>();
			var padding = layoutGroup.padding;

			float viePortHeight = scrollRect.viewport.rect.height;
			float contentHeight = content.rect.height;
			scrollRect.verticalNormalizedPosition = GetNormalizedPosition();
			yield break;

			float GetNormalizedPosition()
			{
				if (contentHeight <= viePortHeight)
				{
					return 0;
				}
				float height = contentHeight - viePortHeight;

				// Content内のtargetのY座標を取得（Pivotが1想定）
				Vector2 localPosition = content.InverseTransformPoint(target.position);
				float offset = padding.top + ((1.0f- target.pivot.y) *  target.rect.height);
				float y = -localPosition.y - offset;
				return 1.0f - Mathf.Clamp01(y / height);
			}
		}
		
		
		//水平方向のスクロール位置を指定のRectTransformに移動
		IEnumerator HorizontalScrollTo(ScrollRect scrollRect, RectTransform target)
		{
			yield return new WaitForEndOfFrame();
			RectTransform content = scrollRect.content;
			var layoutGroup = content.GetComponent<HorizontalLayoutGroup>();
			var padding = layoutGroup.padding;

			float viewportWidth = scrollRect.viewport.rect.width;
			float contentWidth = content.rect.width;
			scrollRect.horizontalNormalizedPosition = GetNormalizedPosition();

			yield break;

			float GetNormalizedPosition()
			{
				if (contentWidth <= viewportWidth)
				{
					return 0;
				}
				float width = contentWidth - viewportWidth;

				// Content内のtargetのX座標を取得（Pivotが0想定）
				Vector2 localPosition = content.InverseTransformPoint(target.position);
				float offset = padding.left + (target.pivot.x * target.rect.width);
				float x = localPosition.x - offset;

				return Mathf.Clamp01(x / width);
			}
		}
	}
}
