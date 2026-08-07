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
	/// セーブロード用のUIのサンプル
	/// </summary>
	[AddComponentMenu("Utage/TemplateUI/UtageUguiSaveLoadItem")]
	public class UtageUguiSaveLoadItem : MonoBehaviour
	{
		/// <summary>本文</summary>
		[HideIfTMP] public Text text;

		[HideIfLegacyText] public TextMeshProNovelText textTmp;

		/// <summary>セーブ番号</summary>
		[HideIfTMP] public Text no;

		/// セーブ番号のテキスト表示のフォーマット
		[SerializeField] public string formatNo = "No.{0,3}";
		[HideIfLegacyText] public TextMeshProUGUI noTmp;

		/// セーブ番号のテキスト表示のフォーマット(オートセーブ時)
		[SerializeField] public string formatNoAuto = "Auto";

		/// <summary>日付</summary>
		[HideIfTMP] public Text date;

		[HideIfLegacyText] public TextMeshProUGUI dateTmp;

		/// <summary>スクショ</summary>
		public RawImage captureImage;

		/// <summary>オートセーブ用のテクスチャ</summary>
		public Texture2D autoSaveIcon;

		/// <summary>未セーブだった場合に表示するテキスト</summary>
		public string textEmpty = "Empty";

		//リフレッシュ時に呼ばれるイベント
		public UnityEvent OnRefresh => onRefresh;
		[SerializeField] UnityEvent onRefresh = new();

		protected UnityEngine.UI.Button button;

		public AdvSaveData Data
		{
			get { return data; }
		}

		protected AdvSaveData data;

		public int Index
		{
			get { return index; }
		}

		protected int index;

		protected Color defaultColor;

		/// <summary>
		/// 初期化
		/// </summary>
		/// <param name="data">セーブデータ</param>
		/// <param name="index">インデックス</param>
		/// <param name="isSave">セーブ画面用ならtrue、ロード画面用ならfalse</param>
		public virtual void Init(AdvSaveData data, Action<UtageUguiSaveLoadItem> ButtonClickedEvent, int index,
			bool isSave)
		{
			this.data = data;
			this.index = index;
			this.button = this.GetComponent<UnityEngine.UI.Button>();
			this.button.onClick.AddListener(() => ButtonClickedEvent(this));
			Refresh(isSave);
		}

		//初期化
		//クリックイベントを登録しない場合
		public virtual void Init(AdvSaveData data, int index, bool isSave)
		{
			this.data = data;
			this.index = index;
			Refresh(isSave);
			this.button = this.GetComponent<UnityEngine.UI.Button>();
		}

		public virtual void Refresh(bool isSave)
		{
			SetTextNo(string.Format(formatNo, index));
			if (data.IsSaved)
			{
				if (data.Type == AdvSaveData.SaveDataType.Auto || data.Texture == null)
				{
					if (data.Type == AdvSaveData.SaveDataType.Auto && autoSaveIcon != null)
					{
						//オートセーブ用のテクスチャ
						captureImage.texture = autoSaveIcon;
						captureImage.color = Color.white;
					}
					else if(this.TryGetComponent(out UtageUguiSaveLoadItemThumbnail thumbnail) && thumbnail.TrySetThumbnail(data))
					{
						//サムネイル画像がセーブデータ内のパラメーターに設定されている場合
						captureImage.color = Color.white;
					}
					else
					{
						//テクスチャがない
						captureImage.texture = null;
						captureImage.color = Color.black;
					}
				}
				else
				{
					captureImage.texture = data.Texture;
					captureImage.color = Color.white;
				}

				SetText(data.Title);
				SetTextDate(UtageToolKit.DateToStringJp(data.Date));
				if (button != null)
				{
					button.interactable = true;
				}
			}
			else
			{
				SetText(textEmpty);
				SetTextDate("");
				if (button != null)
				{
					button.interactable = isSave;
				}
			}


			//オートセーブデータ
			if (data.Type == AdvSaveData.SaveDataType.Auto)
			{
				SetTextNo(string.Format(formatNoAuto));
				//セーブはできない
				if (isSave)
				{
					if (button != null)
					{
						button.interactable = false;
					}
				}
			}
			OnRefresh.Invoke();
		}

		protected virtual void OnDestroy()
		{
			if (captureImage != null && captureImage.texture != null)
			{
				captureImage.texture = null;
			}
		}
		
		public virtual void SetText(string str)
		{
			NovelTextComponentWrapper.SetText(text, textTmp, str);
		}

		public virtual void SetTextDate(string str)
		{
			TextComponentWrapper.SetText(date, dateTmp, str);
		}

		public virtual void SetTextNo(string str)
		{
			TextComponentWrapper.SetText(no, noTmp, str);
		}

		public void OnClicked()
		{
			this.GetComponentInParent<UtageUguiSaveLoad>().OnClicked(this);
		}
	}
}
