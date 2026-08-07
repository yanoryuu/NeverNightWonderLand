// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

namespace Utage
{
	/// 選択肢用UI
	[AddComponentMenu("Utage/ADV/AdvUguiSelection")]
	public class AdvUguiSelection : MonoBehaviour
	{
		/// <summary>本文テキスト</summary>
		[HideIfTMP] public Text text;
		[SerializeField, HideIfLegacyText] protected TextMeshProNovelText textMeshPro;

		//初期化時に呼ばれるイベント
		public UnityEvent OnInit => onInit;
		[SerializeField] UnityEvent onInit = new();

		/// <summary>選択肢データ</summary>
		public AdvSelection Data { get { return data; } }
		protected AdvSelection data;

		/// <summary>
		/// 初期化
		/// </summary>
		/// <param name="data">選択肢データ</param>
		public virtual void Init(AdvSelection data, Action<AdvUguiSelection> ButtonClickedEvent)
		{
			this.data = data;
			NovelTextComponentWrapper.SetText(text,textMeshPro, data.Text);

			UnityEngine.UI.Button button = this.GetComponent<UnityEngine.UI.Button> ();
			button.onClick.AddListener( ()=>ButtonClickedEvent(this) );
			
			OnInit.Invoke();
		}

		/// 選択済みの場合色を変える
		public virtual void OnInitSelected( Color color )
		{
			NovelTextComponentWrapper.SetColor(text, textMeshPro, color);
		}
	}
}
