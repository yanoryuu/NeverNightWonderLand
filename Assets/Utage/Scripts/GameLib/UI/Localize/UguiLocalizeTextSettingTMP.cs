// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UtageExtensions;

namespace Utage
{
	/// 表示言語によるテキスト設定の切り替え（TextMeshPro版）
	[AddComponentMenu("Utage/Lib/UI/UguiLocalizeTextSettingTMP")]
	public class UguiLocalizeTextSettingTMP : UguiLocalizeBase
	{
		[Serializable]
		public class Setting
		{
			public string language;
			public TMP_FontAsset font;
			public Material fontMaterial;
			public float fontSize = 32;
			public float lineSpacing = 0;

			//テキストに設定されている値で初期化
			public void Init(TextMeshProUGUI text)
			{
				font = text.font;
				fontMaterial = text.fontMaterial;
				fontSize = text.fontSize;
				lineSpacing = text.lineSpacing;
			}
			
			//テキストに設定
			public void Apply(TextMeshProUGUI text)
			{
				if (font != null)
				{
					text.font = font;
				}
				if(fontMaterial!=null)
				{
					text.fontMaterial = fontMaterial;
				}
				text.fontSize = fontSize;
				text.lineSpacing = lineSpacing;
			}
		};

		public List<Setting> SettingList => settingList;
		[SerializeField]
		List<Setting> settingList = new List<Setting>();

		Setting DefaultSetting { get; set; }

		TextMeshProUGUI CachedText => this.GetComponentCache(ref cachedText);
		TextMeshProUGUI cachedText;

		TextMeshProNovelText CachedNovelText => this.GetComponentCache(ref cachedNovelText);
		TextMeshProNovelText cachedNovelText;


		protected override void RefreshSub()
		{
			var text = CachedText;
			if (text == null) return;

			Setting setting = settingList.Find(x => x.language == currentLanguage);
			if (setting == null)
			{
				setting = DefaultSetting;
			}
			if (setting == null) return;
			
			setting.Apply(CachedText);
			if (CachedText.font == null)
			{
				CachedText.font = DefaultSetting.font;
			}
		}

		protected override void InitDefault()
		{
			var setting = new Setting();
			setting.Init(CachedText);
			DefaultSetting = setting;
		}
		public override void ResetDefault()
		{
			DefaultSetting.Apply(CachedText);
		}
	}
}

