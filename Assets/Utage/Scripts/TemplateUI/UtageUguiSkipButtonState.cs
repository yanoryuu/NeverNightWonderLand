// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Utage;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// スキップボタンのステートチェンジ
	/// </summary>
	[AddComponentMenu("Utage/TemplateUI/UtageUguiSkipButtonState")]
	public class UtageUguiSkipButtonState : MonoBehaviour
	{
		/// <summary>ADVエンジン</summary>
		public AdvEngine Engine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] protected AdvEngine engine;

		public Toggle target;

		protected virtual void Update()
		{
			if (target == null) return;

			//スキップ可能なページか
			target.interactable = Engine.Page.EnableSkip();
		}
	}

}
