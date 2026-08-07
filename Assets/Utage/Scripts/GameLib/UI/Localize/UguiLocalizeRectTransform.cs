// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Utage
{

	/// <summary>
	/// 表示言語切り替え用のクラス
	/// </summary>
	[AddComponentMenu("Utage/Lib/UI/UguiLocalizeRectTransform")]
	public class UguiLocalizeRectTransform : UguiLocalizeBase
	{
		[Serializable]
		public class Setting
		{
			public string language;
			public Vector2 anchoredPosition;
			public Vector2 size = new Vector2(100,100);
		};

		[SerializeField]
		List<Setting> settingList = new List<Setting>();

		[NonSerialized]
		Setting defaultSetting = null;

		/// <summary>
		/// スプライトコンポーネント(アタッチされてない場合はnull)
		/// </summary>
		RectTransform CachedRectTransform { get { if (null == cachedRectTransform) cachedRectTransform = this.GetComponent<RectTransform>(); return cachedRectTransform; } }
		RectTransform cachedRectTransform;

		//RectTransformはStartやAwakeでは正しい値がとれないので苦肉の策
		bool HasChanged { get; set; }

		protected override void RefreshSub()
		{
			HasChanged = true;
		}

		protected override void InitDefault()
		{
		}

		public override void ResetDefault()
		{
			if (defaultSetting == null) return;

			CachedRectTransform.anchoredPosition = defaultSetting.anchoredPosition;
			CachedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, defaultSetting.size.x);
			CachedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, defaultSetting.size.y);
		}

		void Update()
		{
			if (defaultSetting==null)
			{
				InitDefaultSetting();
			}
			if (HasChanged)
			{
				Setting setting = settingList.Find(x => x.language == currentLanguage);
				if (setting == null)
				{
					setting = defaultSetting;
				}
				if (setting == null) return;

				CachedRectTransform.anchoredPosition = setting.anchoredPosition;
				CachedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, setting.size.x);
				CachedRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, setting.size.y);
				HasChanged = false;
			}
		}

		void InitDefaultSetting()
		{
			defaultSetting = new Setting();
			defaultSetting.anchoredPosition = CachedRectTransform.anchoredPosition;
			defaultSetting.size = CachedRectTransform.rect.size;
		}
	}
}

