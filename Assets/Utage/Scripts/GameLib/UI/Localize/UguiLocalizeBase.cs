// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Utage
{
	/// <summary>
	/// UI表示言語切り替え用の基底クラス
	/// </summary>
	public abstract class UguiLocalizeBase : MonoBehaviour
	{
		[NonSerialized]
		protected string currentLanguage;
		[NonSerialized]
		protected bool isInit = false;

		public virtual void OnLocalize() { ForceRefresh(); }
		protected virtual void OnEnable()
		{
			if (!isInit)
			{
				isInit = true;
				InitDefault();
			}
			ForceRefresh();
		}

		protected virtual void OnValidate()
		{
			ForceRefresh();
		}

		protected virtual void ForceRefresh()
		{
			currentLanguage = "";
			Refresh();
		}

		protected virtual bool IsEnable()
		{
			//実行中のみ動作する（エディター上ではテキストの値を変えることになるので無効化する）
			if (!Application.isPlaying) return false;
			if (!this.gameObject.activeInHierarchy) return false;

			return true;
		}

		protected virtual void Refresh()
		{
			LanguageManagerBase langManager = LanguageManagerBase.Instance;
			if (langManager==null) return;
			if (currentLanguage == langManager.CurrentLanguage) return;

			if( IsEnable() )
			{
				currentLanguage = langManager.CurrentLanguage;
				RefreshSub();
			}
		}

		protected abstract void RefreshSub();

		public virtual void EditorRefresh()
		{
			LanguageManagerBase langManager = LanguageManagerBase.Instance;
			if (langManager==null) return;
			currentLanguage = langManager.CurrentLanguage;

			InitDefault();
			RefreshSub();
		}

		protected abstract void InitDefault();
		public abstract void ResetDefault();
	}
}

