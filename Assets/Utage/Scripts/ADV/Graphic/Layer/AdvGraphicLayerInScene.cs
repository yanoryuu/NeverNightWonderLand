// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UtageExtensions;

namespace Utage
{

	// シーン内にあらかじめ配置してるレイヤー
	[AddComponentMenu("Utage/ADV/AdvGraphicLayerInScene")]
	public class AdvGraphicLayerInScene : AdvGraphicLayer
	{
		internal override AdvLayerSettingData.LayerType LayerType { get { return layerType; } }
		[SerializeField]
		AdvLayerSettingData.LayerType layerType = AdvLayerSettingData.LayerType.Bg;

		[SerializeField]
		Alignment alignment = Alignment.Center;

		[SerializeField]
		bool flipX = false;

		[SerializeField]
		bool flipY = false;

		[SerializeField]
		Transform rootObjects = null;

		Vector3 defaultPosition = new Vector3();
		Vector2 defaultSize = new Vector2();
		Vector3 defaultScale = new Vector3();
		Quaternion defaultRotation = new Quaternion();

		//初期化
		internal override void Init(AdvGraphicManager manager)
		{
			this.Manager = manager;
			this.Canvas = this.GetComponent<Canvas>();
			this.RootObjects = ( rootObjects ==null ) ? this.transform : rootObjects;
			this.SettingData = new AdvLayerSettingData();
			SettingData.InitFromCanvas(Canvas, layerType, alignment, flipX, flipY);

			RectTransform rectTransform = this.transform as RectTransform;

			defaultPosition = rectTransform.localPosition;
			defaultSize = rectTransform.GetSize();
			defaultScale = rectTransform.localScale;
			defaultRotation = rectTransform.rotation;
		}


		//　キャンバスのRectTransformをリセットして初期状態に
		internal override void ResetCanvasRectTransform()
		{
			DestroyAllAnimations();
			RectTransform rectTransform = this.transform as RectTransform;

			//テクスチャ書き込みが無効な場合、位置をそのまま設定
			rectTransform.localPosition = defaultPosition;
			//サイズ設定
			rectTransform.SetSize(defaultSize);
			//スケーリング値の設定
			rectTransform.localScale = defaultScale;
			//回転値の設定
			rectTransform.rotation = defaultRotation;
		}
	}
}
