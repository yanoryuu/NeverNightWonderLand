// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Utage
{

	// Imageの表示言語切り替え用のクラス
	[ExecuteInEditMode]
	[AddComponentMenu("Utage/Lib/UI/UguiLocalizeImage")]
	public class UguiLocalizeImage : UguiLocalizeBase
	{
		[System.Serializable]
		public class LocalizeSprite
		{
			public string language = "";
			public Sprite sprite = null;
		}

		List<LocalizeSprite> LocalizeSprites { get { return localizeSprites; } }
		[SerializeField]
		List<LocalizeSprite> localizeSprites = new List<LocalizeSprite>();

		[NonSerialized]
		protected Sprite defaultSprite;


		protected Image CachedImage { get { if (null == cachedImage) cachedImage = this.GetComponent<Image>(); return cachedImage; } }
		Image cachedImage;

		protected override void RefreshSub()
		{
			if (CachedImage != null && !LanguageManagerBase.Instance.IgnoreLocalizeUiText )
			{
				CachedImage.sprite = FindSprite(LanguageManagerBase.Instance.CurrentLanguage);
			}
		}

		Sprite FindSprite(string language)
		{
			var sprite = LocalizeSprites.Find(x => x.language == language);
			if (sprite != null)
			{
				return sprite.sprite;
			}

			if (defaultSprite != null)
			{
				return defaultSprite;
			}
			else if (LocalizeSprites.Count > 0)
			{
				return LocalizeSprites[0].sprite;
			}
			else
			{
				return null;
			}
		}

		protected override void InitDefault()
		{
			if (CachedImage != null)
			{
				defaultSprite = CachedImage.sprite;
			}
		}
		public override void ResetDefault()
		{
			if (CachedImage != null)
			{
				CachedImage.sprite = defaultSprite;
			}
		}
	}
}

