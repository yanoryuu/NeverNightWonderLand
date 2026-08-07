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

	/// <summary>
	/// グラフィックのレイヤー管理
	/// </summary>
	[AddComponentMenu("Utage/ADV/Internal/AdvGraphicLayerDefault")]
	public class AdvGraphicLayerDefault : AdvGraphicLayer
	{
		internal override AdvLayerSettingData.LayerType LayerType { get { return this.SettingData.Type; } }
		Camera Camera { get; set; }
		LetterBoxCamera LetterBoxCamera { get; set; }

		Vector2 GameScreenSize
		{
			get
			{
				return LetterBoxCamera.CurrentSize;
			}
		}

		//初期化
		internal override void Init(AdvGraphicManager manager)
		{
			this.Manager = manager;
		}

		internal void Init(AdvLayerSettingData settingData)
		{
			this.SettingData = settingData;

			//UI用のコード
			this.Canvas = this.GetComponent<Canvas>();
#if UNITY_5_6_OR_NEWER
			this.Canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
#endif

			if (!string.IsNullOrEmpty(SettingData.LayerMask))
			{
#if UNITY_EDITOR
				if (!LayerMaskEditor.ContainsInLayerNames(SettingData.LayerMask))
				{
					Debug.LogWarning("Please add Layer name [ " + SettingData.LayerMask + " ]");
					this.Canvas.gameObject.layer = 8;
				}
				else
				{ 
					this.Canvas.gameObject.layer = LayerMask.NameToLayer(SettingData.LayerMask);
				}
#else
				this.Canvas.gameObject.layer = LayerMask.NameToLayer(SettingData.LayerMask);
#endif
			}
			this.Canvas.sortingOrder = SettingData.Order;

			//入力受け付ける可能性があるので、イベントカメラとRaycasterを設定
			this.Camera = Engine.CameraManager.FindCameraByLayer(this.Canvas.gameObject.layer);
			if (Camera == null)
			{
				Debug.LogError("Cant find camera");
				this.Camera = Engine.CameraManager.FindCameraByLayer(0);
			}
			this.LetterBoxCamera = Camera.gameObject.GetComponent<LetterBoxCamera> ();
			this.Canvas.worldCamera = Camera;
			GraphicRaycaster raycaster = this.Canvas.gameObject.AddComponent<GraphicRaycaster>();
			raycaster.enabled = false;

			this.RootObjects = this.Canvas.transform;
			ResetCanvasRectTransform();
			//ToDo
			//キャンバスのアニメーションの最中でリセットされると破綻するが・・・
			if (Manager.DebugAutoResetCanvasPosition)
			{
				this.LetterBoxCamera.OnGameScreenSizeChange.AddListener(x => ResetCanvasRectTransform());
			}
		}


		//　キャンバスのRectTransformをリセットして初期状態に
		internal override void ResetCanvasRectTransform()
		{
			DestroyAllAnimations();
			RectTransform rectTransform = this.Canvas.transform as RectTransform;

			//今のゲーム画面の大きさと、宴のLayerシートの設定データから
			//キャンバスのサイズと位置を取得
			float x, width;
			SettingData.Horizontal.GetBorderdPositionAndSize(GameScreenSize.x, out x, out width);
			float y, height;
			SettingData.Vertical.GetBorderdPositionAndSize(GameScreenSize.y, out y, out height);

			//テクスチャ書き込みが無効な場合、位置をそのまま設定
			rectTransform.localPosition = new Vector3(x, y, SettingData.Z) / Manager.PixelsToUnits;
			//サイズ設定
			rectTransform.SetSize(width, height);
			//スケーリング値の設定
			rectTransform.localScale = SettingData.Scale / Manager.PixelsToUnits;
			//回転値の設定
			rectTransform.localRotation = Quaternion.identity;
		}
	}
}
