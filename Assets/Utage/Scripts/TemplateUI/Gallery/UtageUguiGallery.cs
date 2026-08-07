// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using Utage;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// ギャラリー表示のサンプル
	/// </summary>
	[AddComponentMenu("Utage/TemplateUI/UtageUguiGallery")]
	public class UtageUguiGallery : UguiView
	{
		public UguiView[] views;
		protected int tabIndex = -1;

		// タブインデックスを全てリセットしてオープン
		public virtual void OpenAndResetAllTabIndex(UguiView prev)
		{
			var lastTabIndex = tabIndex;
			tabIndex = -1;
			this.Open(prev);
			
			//全てのタブインデックスを0にリセット
			foreach (var toggleGroup in GetComponentsInChildren<UguiToggleGroupIndexed>(true))
			{
				//トグル変更のアニメーションをいったん無効化
				var toggles = toggleGroup.TogglesToArray;
				List<Toggle.ToggleTransition> toggleTransitions = new List<Toggle.ToggleTransition>();  
				foreach (var toggle in toggles)
				{
					toggleTransitions.Add(toggle.toggleTransition);
					toggle.toggleTransition = Toggle.ToggleTransition.None;
				}
				//タブインデックスを0にリセット
				toggleGroup.CurrentIndex = 0;
				//トグル変更のアニメーションを戻しておく
				for (var i = 0; i < toggles.Length; i++)
				{
					toggles[i].toggleTransition = toggleTransitions[i];
				}
			}
			//今の画面を開く
			tabIndex = 0;
			if (lastTabIndex == 0)
			{
				views[lastTabIndex].ToggleOpen(true);
			}
		}

		/// <summary>
		/// オープンしたときに呼ばれる
		/// </summary>
		protected virtual void OnOpen()
		{
			if (tabIndex >= 0)
			{
				views[tabIndex].ToggleOpen(true);
			}
		}
		

		//一時的に表示オフ
		public virtual void Sleep()
		{
			this.gameObject.SetActive(false);
		}

		//一時的な表示オフを解除
		public virtual void WakeUp()
		{
			this.gameObject.SetActive(true);
		}

		public virtual void OnTabIndexChanged(int index)
		{
			
			if (index >= views.Length)
			{
				Debug.LogError("index < views.Length");
				return;
			}
			
			//ほかの画面を閉じてから、次の画面を開く
			for (int i = 0; i < views.Length; ++i)
			{
				if (views[i] == null)
				{
					Debug.LogError($"views[{i}] is null",this);
					continue;
				}
				if (i == index) continue;
				views[i].ToggleOpen(false);
			}
			
			views[index].ToggleOpen(true);
			tabIndex = index;
		}
	}
}
