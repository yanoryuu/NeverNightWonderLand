// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System;

namespace Utage
{
	/// 選択肢用UI(TextMeshPro版)
	[AddComponentMenu("Utage/ADV/TMP/AdvUguiSelectionTMP")]
	public class AdvUguiSelectionTMP 
		: AdvUguiSelection
			, IUsingTextMeshPro
	{
		public TextMeshProNovelText TextMeshPro => textMeshPro;
	}
}
