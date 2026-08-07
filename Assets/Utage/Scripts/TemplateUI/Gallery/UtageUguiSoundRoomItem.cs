// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Utage;
using System;
using TMPro;

namespace Utage
{

	/// <summary>
	/// サウンドルーム用のUIのサンプル
	/// </summary>
	[AddComponentMenu("Utage/TemplateUI/UtageUguiSoundRoomItem")]
	public class UtageUguiSoundRoomItem : MonoBehaviour
	{
		/// <summary>本文</summary>
		[HideIfTMP] public Text title;

		[HideIfLegacyText] public TextMeshProUGUI titleTmp;
		
		//初期化時に呼ばれるイベント
		public UnityEvent OnInit => onInit;
		[SerializeField] UnityEvent onInit = new();

		public AdvSoundSettingData Data
		{
			get { return data; }
		}

		protected AdvSoundSettingData data;

		/// <summary>
		/// 初期化
		/// </summary>
		/// <param name="data">セーブデータ</param>
		/// <param name="index">インデックス</param>
		public virtual void Init(AdvSoundSettingData data, Action<UtageUguiSoundRoomItem> ButtonClickedEvent, int index)
		{
			this.data = data;
			SetTextTitle(data.LocalizedTitle);

			UnityEngine.UI.Button button = this.GetComponent<UnityEngine.UI.Button>();
			button.onClick.AddListener(() => ButtonClickedEvent(this));
			OnInit.Invoke();
		}

		public virtual void SetTextTitle(string text)
		{
			TextComponentWrapper.SetText(title, titleTmp, text);
		}
	}
}
