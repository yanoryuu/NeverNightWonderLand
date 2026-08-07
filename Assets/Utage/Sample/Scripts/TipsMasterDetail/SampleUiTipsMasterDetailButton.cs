// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using Utage;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Events;


namespace Utage
{
	// TIPSのマスターディテールタイプの各TIPS名のボタン
	public class SampleUiTipsMasterDetailButton : MonoBehaviour
	{
		protected SampleUiTipsMasterDetail MasterDetail => this.GetComponentCacheInParent(ref masterDetail);
		SampleUiTipsMasterDetail masterDetail;
		public TipsInfo TipsInfo { get; protected set; }

		[SerializeField] protected TextMeshProUGUI tipsTitle;
		[SerializeField] protected GameObject newIcon;

		public UnityEvent OnInit => onInit;
		[SerializeField] UnityEvent onInit = new();

		public virtual void Init(TipsInfo tipsInfo)
		{
			TipsInfo = tipsInfo;
			if (TipsInfo.IsOpened)
			{
				//開放済みの情報設定
				
				if (tipsTitle != null)
				{
					tipsTitle.text = TipsInfo.Data.LocalizedTitle();
				}
			}
			else
			{
				//未開放の場合はそのまま表示
			}
			RefreshNew();
			OnInit.Invoke();
		}

		//Newアイコンのオンオフを更新
		public virtual void RefreshNew()
		{
			//Newアイコンのオンオフ
			if (newIcon != null)
			{
				newIcon.SetActive(!TipsInfo.HasRead && TipsInfo.IsOpened);
			}
		}
	}
}
