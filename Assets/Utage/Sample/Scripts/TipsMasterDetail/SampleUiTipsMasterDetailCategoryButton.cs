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

	// TIPSのマスターディテールタイプの各カテゴリ名のボタン
	public class SampleUiTipsMasterDetailCategoryButton : MonoBehaviour
	{
		protected SampleUiTipsMasterDetail MasterDetail => this.GetComponentCacheInParent(ref masterDetail);
		SampleUiTipsMasterDetail masterDetail;

		//カテゴリID
		[SerializeField] protected string categoryId;
		[SerializeField] protected GameObject newIcon;

		public UnityEvent OnInit => onInit;
		[SerializeField] UnityEvent onInit = new();

		public SampleUiTipsMasterDetail.TipsCategoryInfo CategoryInfo { get; private set; }
		
		public virtual void Init()
		{
			if (!MasterDetail.TipsCategories.TryGetValue(categoryId, out var categoryInfo))
			{
				Debug.LogError($"CategoryInfo is null. categoryId = {categoryId}");
			}
			CategoryInfo = categoryInfo;
			RefreshNew();
			OnInit.Invoke();
		}

		//Newアイコンのオンオフを更新
		public virtual void RefreshNew()
		{
			//Newアイコンのオンオフ
			if (newIcon != null)
			{
				newIcon.SetActive(CategoryInfo.IsNew());
			}
		}
	}
}
