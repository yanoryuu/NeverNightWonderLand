// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Utage;
using System;
using TMPro;

namespace Utage
{

	/// <summary>
	/// セーブロード用のUIのサンプル
	/// </summary>
	[AddComponentMenu("Utage/TemplateUI/UtageUguiSaveLoadItemTMP")]
	public class UtageUguiSaveLoadItemTMP : UtageUguiSaveLoadItem
		, IUsingTextMeshPro
	{
		//　リッチテキストタグをどう扱うか 
		public enum RichTextType
		{
			Enable,		// テキストタグを有効にする
			Disable,	// テキストタグを無効にする
			EmojiOnly,	// 絵文字のみ有効にする
		}

		public RichTextType RichText => richTextType;
		[SerializeField] protected RichTextType richTextType = RichTextType.Enable;

		public override void SetText(string str)
		{
			switch (RichText)
			{
				case RichTextType.Disable:
					str = new TextData(str).NoneMetaString;
					break;
				case RichTextType.EmojiOnly:
					str = new TextData(str).MakeNoneMetaStringWithEmojiTMP();
					break;
				case RichTextType.Enable:
				default:
					break;
			}
			NovelTextComponentWrapper.SetText(text, textTmp, str);
		}

	}
}
