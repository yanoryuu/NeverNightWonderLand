// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Utage;
using System;
using TMPro;


namespace Utage
{

	/// <summary>
	/// シーン回想用のUIのサンプル
	/// </summary>
	[AddComponentMenu("Utage/TemplateUI/UtageUguiSceneGalleryItem")]
	public class UtageUguiSceneGalleryItem : MonoBehaviour
	{
		public AdvUguiLoadGraphicFile texture;
		[HideIfTMP] public Text title;
		[HideIfLegacyText] public TextMeshProUGUI titleTmp;
		[SerializeField] bool keepTextureActive; //テクスチャのアクティブのオンオフを切り替えるか
		
		//初期化時に呼ばれるイベント
		public UnityEvent OnInit => onInit;
		[SerializeField] UnityEvent onInit = new();

		public AdvSceneGallerySettingData Data
		{
			get { return data; }
		}

		protected AdvSceneGallerySettingData data;

		/// <summary>
		/// 初期化
		/// </summary>
		/// <param name="data">セーブデータ</param>
		/// <param name="index">インデックス</param>
		public virtual void Init(AdvSceneGallerySettingData data, Action<UtageUguiSceneGalleryItem> ButtonClickedEvent,
			AdvSystemSaveData saveData)
		{
			Init(data, saveData);
			UnityEngine.UI.Button button = this.GetComponent<UnityEngine.UI.Button>();
			button.onClick.AddListener(() => ButtonClickedEvent(this));
			bool isOpened = saveData.GalleryData.CheckSceneLabels(data.ScenarioLabel);
			button.interactable = isOpened;
		}

		public virtual void Init(AdvSceneGallerySettingData data, AdvSystemSaveData saveData)
		{
			this.data = data;

			bool isOpened = saveData.GalleryData.CheckSceneLabels(data.ScenarioLabel);
			if (!isOpened)
			{
				if(!keepTextureActive) texture.gameObject.SetActive(false);
				SetTextTitle("");
			}
			else
			{
				if (!keepTextureActive) texture.gameObject.SetActive(true);
				texture.LoadTextureFile(data.ThumbnailPath);
				SetTextTitle(data.LocalizedTitle);
			}
			OnInit.Invoke();
		}

		//クリックイベントを登録しない場合はこちら経由で
		//プレハブ上で、Buttonコンポーネントのインスペクターから登録しておく想定
		public virtual void OnClicked()
		{
			this.GetComponentInParent<UtageUguiSceneGallery>().OnClickedButton(this);
		}


		public virtual void SetTextTitle(string text)
		{
			TextComponentWrapper.SetText(title, titleTmp, text);
		}
	}
}
