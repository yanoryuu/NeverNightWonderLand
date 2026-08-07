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

	// TIPSの一覧表示画面
	public class AdvUguiTipsList : UguiView
	{
		//ADVエンジン
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] AdvEngine engine;
		
		//TIPS管理
		protected TipsManager TipsManager => Engine.GetComponentCacheInChildren(ref tipsManager);
		[SerializeField] TipsManager tipsManager;

		[SerializeField] Transform rootButtons;
		[SerializeField] AdvUguiTipsListButton prefabTipsButton;

		public AdvUguiTipsDetail TipsDetail => this.GetComponentCacheFindIfMissing(ref tipsDetail);
		[SerializeField] AdvUguiTipsDetail tipsDetail;

		protected bool IsInit { get; set; }

		public UnityEvent OnInit => onInit;
		[SerializeField] UnityEvent onInit = new();

		
		protected virtual void OnOpen()
		{
			IsInit = false;
			StartCoroutine(CoWaitOpen());
		}

		//起動待ちしてから開く
		protected virtual IEnumerator CoWaitOpen()
		{
			rootButtons.DestroyChildren();
			while (Engine.IsWaitBootLoading) yield break;
			Init();
		}
		
		//初期化
		protected virtual void Init()
		{
			IsInit = true;
			foreach (var keyValuePair in TipsManager.TipsMap)
			{
				var tipsInfo = keyValuePair.Value;
				AdvUguiTipsListButton button = rootButtons.AddChildPrefab(prefabTipsButton);
				button.Init(tipsInfo);
			}
			OnInit.Invoke();
		}

		protected virtual void Update()
		{
			//右クリックで戻る
			if (IsInit && InputUtil.IsInputGuiClose())
			{
				Back();
			}
		}

		public virtual void OnClickTipsButton(AdvUguiTipsListButton button)
		{
			var tipsInfo = button.TipsInfo;
			if (!tipsInfo.IsOpened)
			{
				//未開放
				return;
			}
			else
			{
				this.Close();
				TipsDetail.Open(button.TipsInfo, this);
			}
		}
	}
}
