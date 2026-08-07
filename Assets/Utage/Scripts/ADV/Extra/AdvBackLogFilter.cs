// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UtageExtensions;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// バックログに残すかどうかの制御をする
/// </summary>
namespace Utage
{
	[AddComponentMenu("Utage/ADV/Extra/AdvBackLogFilter")]
	public class AdvBackLogFilter : MonoBehaviour
	{
		//無効化フラグ
		[SerializeField]
		bool disable = false;
		public bool Disable
		{
			get { return disable; }
			set { disable = value; }
		}

		public List<string> filterMessageWindowNames
			= new List<string>(new string[] { "MessageWindow" });

		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField]
		protected AdvEngine engine;

		void Awake()
		{
			Engine.BacklogManager.OnAddPage.AddListener(OnAddPage);
		}

		void OnAddPage(AdvBacklogManager backlogManager)
		{
			backlogManager.IgnoreLog = !filterMessageWindowNames.Contains(Engine.MessageWindowManager.CurrentWindow.Name);
		}
	}
}

