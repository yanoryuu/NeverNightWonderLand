// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// フェード切り替え機能つきのスプライト表示
	/// </summary>
	public abstract class AdvGraphicObjectUguiBase : AdvGraphicBase
	{
		//初期化処理
		public override void Init(AdvGraphicObject parentObject)
		{
			base.Init(parentObject);

			AddGraphicComponentOnInit();

			if (GetComponent<IAdvClickEvent>() == null)
			{
				this.gameObject.AddComponent<AdvClickEvent>();
			}
		}

		//初期化時のコンポーネント追加処理
		protected abstract void AddGraphicComponentOnInit();
		protected abstract Material Material { get; set; }

		//拡大縮小の設定
		public override void Scale(AdvGraphicInfo graphic)
		{
			RectTransform rectTransform = this.transform as RectTransform;
			rectTransform.localScale = graphic.Scale;
		}

		//配置
		public override void Alignment(Utage.Alignment alignment, AdvGraphicInfo graphic)
		{
			RectTransform t = this.transform as RectTransform;
			t.pivot = graphic.Pivot;
			if (alignment == Utage.Alignment.None)
			{
				//アラインメイント指定なし
				t.anchoredPosition = graphic.Position;
				Vector3 local = t.localPosition;
				local.z = graphic.Position.z;
				t.localPosition = local; 
				return;
			}
			//アラインメイントから、アンカーの値を取得
			Vector2 alignmentValue = AlignmentUtil.GetAlignmentValue(alignment);
			t.anchorMin = t.anchorMax = alignmentValue;

			//アラインメントする際の座標値オフセット
			Vector3 offset1 = t.pivot - alignmentValue;
			offset1.Scale(t.GetSizeScaled());
			//アンカーとピボットを考慮したポジション設定
			{
				Vector3 tmp = graphic.Position + offset1;
				t.anchoredPosition = tmp;
				Vector3 local = t.localPosition;
				local.z = tmp.z;
				t.localPosition = local;
			}
		}
	}
}
