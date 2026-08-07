// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// CGギャラリーのCG表示のサンプル
	/// </summary>
	[AddComponentMenu("Utage/TemplateUI/UtageUguiCgGalleryViewer")]
	public class UtageUguiCgGalleryViewer : UguiView, IPointerClickHandler, IDragHandler, IPointerDownHandler
	{
		/// <summary>
		/// ギャラリー選択画面
		/// </summary>
		public UtageUguiGallery gallery;

		/// <summary>
		/// CG表示画面
		/// </summary>
		public AdvUguiLoadGraphicFile texture;

		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] AdvEngine engine;

		/// <summary>スクロール対応</summary>
		public virtual ScrollRect ScrollRect
		{
			get
			{
				if (scrollRect == null)
				{
					scrollRect = GetComponent<ScrollRect>();
					if (scrollRect == null)
					{
						scrollRect = this.gameObject.AddComponent<ScrollRect>();
						scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
					}

					if (scrollRect.content == null)
					{
						scrollRect.content = texture.transform as RectTransform;
					}
				}

				return scrollRect;
			}
		}

		[SerializeField] ScrollRect scrollRect;

		[SerializeField] bool applyPosition = false;

		protected Vector3 startContentPosition;
		protected bool isEnableClick;
		protected bool isLoadEnd;

		public AdvCgGalleryData Data => data;
		protected AdvCgGalleryData data;

		public int CurrentIndex => currentIndex;
		protected int currentIndex = 0;

		protected virtual void Awake()
		{
			texture.OnLoadEnd.AddListener(OnLoadEnd);
		}

		/// <summary>
		/// オープンしたときに呼ばれる
		/// </summary>
		public virtual void Open(AdvCgGalleryData data)
		{
			gallery.Sleep();
			this.Open();
			this.data = data;
			this.currentIndex = 0;
			this.startContentPosition = ScrollRect.content.localPosition;
			LoadCurrentTexture();
		}

		/// <summary>
		/// クローズしたときに呼ばれる
		/// </summary>
		protected virtual void OnClose()
		{
			ScrollRect.content.localPosition = this.startContentPosition;
			texture.ClearFile();
			gallery.WakeUp();
		}

		protected virtual void Update()
		{
			//右クリックで戻る
			if (InputUtil.IsInputGuiClose())
			{
				Back();
			}
		}

		public virtual void OnPointerDown(PointerEventData eventData)
		{
			if (isLoadEnd) isEnableClick = true;
		}

		public virtual void OnPointerClick(PointerEventData eventData)
		{
			if (!isEnableClick) return;

			ChangeCurrentIndex(currentIndex+1);
		}


		public virtual void OnDrag(PointerEventData eventData)
		{
			isEnableClick = false;
		}

		protected virtual void LoadCurrentTexture()
		{
			isLoadEnd = false;
			isEnableClick = false;
			ScrollRect.enabled = false;
			ScrollRect.content.localPosition = this.startContentPosition;
			AdvTextureSettingData textureData = data.GetDataOpened(currentIndex);
			texture.LoadFile(Engine.DataManager.SettingDataManager.TextureSetting.LabelToGraphic(textureData.Key).Main);
		}

		protected virtual void OnLoadEnd()
		{
			isLoadEnd = true;
			isEnableClick = false;
			ScrollRect.enabled = true;
			if (applyPosition)
			{
				var graphic = data.GetDataOpened(currentIndex).Graphic.Main;
				texture.transform.localPosition = graphic.Position;
			}
		}

		public void ChangeCurrentIndex(int index)
		{
			if (index < 0)
			{
				//0未満になる場合、ギャラリー画面に戻る
				Back();
				return;
			}
			if (index >= Data.NumOpen)
			{
				//インデックスを超える場合はギャラリー画面に戻る
				Back();
				return;
			}
			//インデックスが変わった場合は、テクスチャを読み込む
			currentIndex = index;
			LoadCurrentTexture();
		}
		
		//インデックスを一つ進める
		public void ShitUpIndex()
		{
			ChangeCurrentIndex(CurrentIndex + 1);
		}
		
		//インデックスを一つ戻る
		public void ShitDownIndex()
		{
			ChangeCurrentIndex(CurrentIndex - 1);
		}
		
		//インデックスを最初に戻す
		public void JumpToFirstIndex()
		{
			ChangeCurrentIndex(0);
		}
		
		//インデックスを最後に飛ばす
		public void JumpToLastIndex()
		{
			ChangeCurrentIndex(Data.NumOpen-1);
		}
	}
}
