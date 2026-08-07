// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{

	/// <summary>
	/// ノベル用の改ページアイコン
	/// </summary>
	[AddComponentMenu("Utage/Lib/UI/UguiNovelTextBrPageIcon")]
	public class UguiNovelTextBrPageIcon : Text
	{
		public UguiNovelText novelText;

		void Update()
		{
			Vector2 pos = novelText.EndPosition;
			this.gameObject.transform.localPosition = pos;
		}
	}
}
