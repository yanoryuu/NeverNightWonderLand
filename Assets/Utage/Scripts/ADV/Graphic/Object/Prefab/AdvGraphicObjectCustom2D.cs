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
	[AddComponentMenu("Utage/ADV/Internal/GraphicObject/AdvGraphicObjectCustom2D")]
	public class AdvGraphicObjectCustom2D : AdvGraphicObjectUguiBase
	{
		//初期化時のコンポーネント追加処理
		protected override void AddGraphicComponentOnInit() { }
		protected override Material Material { get; set; }

		//********描画時にクロスフェードが失敗するであろうかのチェック********//
		public override bool CheckFailedCrossFade(AdvGraphicInfo grapic)
		{
			return LastResource != grapic;
		}

		//********描画時のリソース変更********//
		public override void ChangeResourceOnDraw(AdvGraphicInfo grapic, float fadeTime)
		{
			//新しくリソースを設定
			if (LastResource != grapic)
			{
				GameObject currentObject = GameObject.Instantiate(grapic.File.UnityObject) as GameObject;
				Vector3 localPostion = currentObject.transform.localPosition;
				Vector3 localEulerAngles = currentObject.transform.localEulerAngles;
				Vector3 localScale = currentObject.transform.localScale;
				currentObject.transform.SetParent(this.transform);
				currentObject.transform.localPosition = localPostion;
				currentObject.transform.localScale = localScale;
				currentObject.transform.localEulerAngles = localEulerAngles;
				currentObject.ChangeLayerDeep(this.gameObject.layer);
				currentObject.gameObject.SetActive(true);

				ChangeResourceOnDrawSub(grapic);
			}

			if (LastResource == null)
			{
				ParentObject.FadeIn(fadeTime);
			}
		}

		//********描画時のリソース変更********//
		public virtual void ChangeResourceOnDrawSub(AdvGraphicInfo graphic)
		{
			foreach (var item in this.GetComponentsInChildren<IAdvGraphicObjectCustom>())
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
