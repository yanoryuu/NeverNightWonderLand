// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.Pool;
using UtageExtensions;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

namespace UtageExtensions
{
	//拡張メソッド(Input関連)
	public static class UtageExtensionMethodsInput
	{
		//左ボタンかチェック
		//左クリックのみに反応させたいときに使う
		public static bool IsLeftButton (this PointerEventData eventData)
		{
			return (eventData.button == PointerEventData.InputButton.Left);
		}

	}
}
