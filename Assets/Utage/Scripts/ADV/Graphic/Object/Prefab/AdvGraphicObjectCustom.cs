// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// カスタムオブジェクト（基本はプレハブ）
	/// </summary>
	[AddComponentMenu("Utage/ADV/Internal/GraphicObject/AdvGraphicObjectCustom")]
	public class AdvGraphicObjectCustom : AdvGraphicObjectPrefabBase
	{
		protected SpriteRenderer sprite;

		//********描画時のリソース変更********//
		protected override void ChangeResourceOnDrawSub(AdvGraphicInfo graphic)
		{
			foreach ( var item in this.GetComponentsInChildren<IAdvGraphicObjectCustom>())
			{
				item.ChangeResourceOnDrawSub(graphic);
			}
		}

		//エフェクト用の色が変化したとき
		public override void OnEffectColorsChange(AdvEffectColor color)
		{
			foreach (var item in this.GetComponentsInChildren<IAdvGraphicObjectCustom>())
			{
				item.OnEffectColorsChange(color);
			}
		}

		//********描画時の引数適用********//
		public override void SetCommandArg(AdvCommand command)
		{
			base.SetCommandArg(command);
			foreach (var item in this.GetComponentsInChildren<IAdvGraphicObjectCustomCommand>())
			{
				item.SetCommandArg(command);
			}
		}
	}
}
