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

	// TIPSの一覧表示画面の各TIPSのボタン
	public class AdvUguiTipsListButton : MonoBehaviour
	{
		protected AdvUguiTipsList TipsList => this.GetComponentCacheInParent(ref tipsList);
		AdvUguiTipsList tipsList;
		public TipsInfo TipsInfo { get; protected set; }

		[SerializeField] protected TextMeshProUGUI tipsTitle;
		[SerializeField] protected AdvUguiLoadGraphicFile tipsImage;
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
			else
			{
				//未開放の場合はそのまま表示
			}
			
			//Newアイコンのオンオフ
			if (newIcon != null)
			{
				newIcon.SetActive(!TipsInfo.HasRead && TipsInfo.IsOpened);
			}
			OnInit.Invoke();
		}

		public virtual void OnClick()
		{
			TipsList.OnClickTipsButton(this);
		}
	}
}
