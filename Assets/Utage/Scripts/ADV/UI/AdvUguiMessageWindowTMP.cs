// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Serialization;
using Utage;

namespace Utage
{

	/// メッセージウィドウ処理(TextMeshPro版)
	[AddComponentMenu("Utage/ADV/AdvUguiMessageWindowTMP")]
	public class AdvUguiMessageWindowTMP 
		: AdvUguiMessageWindow
			, IUsingTextMeshPro
	{
		//本文テキスト
		public TextMeshProNovelText TextPro { get { return textPro; } }
		//名前テキスト
		public TextMeshProNovelText NameTextPro { get { return nameTextPro; } }
	}
}
