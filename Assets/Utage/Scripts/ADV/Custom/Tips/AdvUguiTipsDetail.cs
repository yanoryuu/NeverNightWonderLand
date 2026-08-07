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

	// TIPSの詳細表示画面
	public class AdvUguiTipsDetail : UguiView
	{
		//ADVエンジン
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] AdvEngine engine;
		
		//TIPS管理
		protected TipsManager TipsManager => Engine.GetComponentCacheInChildren(ref tipsManager);
		[SerializeField] TipsManager tipsManager;

		[SerializeField] TextMeshProUGUI tipsTitle;
		[SerializeField] AdvUguiLoadGraphicFile tipsImage;
		[SerializeField] TextMeshProNovelText tipsText;

		public UnityEvent OnInit => onInit;
		[SerializeField] UnityEvent onInit = new();

		protected bool IsInit { get; set; }

		public TipsInfo TipsInfo { get; protected set; }
		
		public void Open(TipsInfo tipsInfo, UguiView prevView)
		{
			TipsInfo = tipsInfo;
			base.Open(prevView);
		}
		
		protected virtual void OnOpen()
		{
			IsInit = false;
			StartCoroutine(CoWaitOpen());
		}

		//起動待ちしてから開く
		protected virtual IEnumerator CoWaitOpen()
		{
			while (Engine.IsWaitBootLoading) yield break;
			InitTipsInto();
		}
		
		//Tips情報で初期化
		protected virtual void InitTipsInto()
		{
			IsInit = true;
			//既読済みにする
			TipsInfo.Read();

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
	}
}
