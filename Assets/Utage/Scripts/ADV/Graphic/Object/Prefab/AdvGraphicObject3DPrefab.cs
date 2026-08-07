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
	/// フェード切り替え機能つきのスプライト表示
	/// </summary>
	[AddComponentMenu("Utage/ADV/Internal/GraphicObject/AdvGraphicObject3DPrefab")]
	public class AdvGraphicObject3DPrefab : AdvGraphicObjectPrefabBase
	{
		//初期化処理
		public override void Init(AdvGraphicObject parentObject)
		{
			base.Init(parentObject);

			//3Dモデルは自分のほうを向く
			this.transform.localEulerAngles = Vector3.up * 180;
		}

		//********描画時のリソース変更********//
		protected override void ChangeResourceOnDrawSub(AdvGraphicInfo grapic)
		{
		}

		//エフェクト用の色が変化したとき
		public override void OnEffectColorsChange(AdvEffectColor color)
		{
			if (currentObject)
			{
				//プレハブにカラー変更を受け取るコンポーネントがある場合、そちらを優先
				var colorChanged = currentObject.GetComponent<IAdvGraphicObject3DPrefabEffectColorChanged>();
				if (colorChanged!=null)
				{
					colorChanged.OnEffectColorsChange(color);
				}
				else
				{
					Color mulColor = color.MulColor;
					mulColor.a = 1;
					foreach (Renderer renderer in currentObject.GetComponentsInChildren<Renderer>())
					{
						renderer.material.color = mulColor;
					}
				}
			}
		}
	}
}
