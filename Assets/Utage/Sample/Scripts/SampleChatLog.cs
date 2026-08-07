// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using Utage;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// メッセージウィンドウをチャット風の履歴として追加していくサンプル
	/// </summary>
	[AddComponentMenu("Utage/ADV/Examples/SampleChatLog")]
	public class SampleChatLog : MonoBehaviour
	{
		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] protected AdvEngine engine;

		/// <summary>ログウィンドウのプレハブ</summary>
		[SerializeField] protected GameObject itemPrefab;

		/// <summary>ログを追加するルート</summary>
		[SerializeField] protected Transform targetRoot;

		/// <summary>ログの最大数</summary>
		[SerializeField] protected int maxLog = 10;

		/// <summary>ログの最大数</summary>
		List<GameObject> logs = new List<GameObject>();

		void Awake()
		{
			Engine.Page.OnEndPage.AddListener(OnEndPage);
		}

		void OnEndPage(AdvPage page)
		{
			if (page.CurrentData.IsEmptyText) return;
			AdvBacklog log = page.Engine.BacklogManager.LastLog;
			if (log == null) return;

			if (itemPrefab == null || targetRoot == null)
			{
				Debug.LogError("itemPrefab or targetRoot is null");
				return;
			}

			if (logs.Count > 0 && logs.Count >= maxLog)
			{
				GameObject.Destroy(logs[0]);
				logs.RemoveAt(0);
			}

			GameObject go = targetRoot.AddChildPrefab(itemPrefab);
			go.SendMessage("OnInitData", log);
			go.transform.SetSiblingIndex(1);
			logs.Add(go);
		}
	}

}
