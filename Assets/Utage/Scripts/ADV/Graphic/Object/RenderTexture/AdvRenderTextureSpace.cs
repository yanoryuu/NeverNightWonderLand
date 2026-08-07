// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

using UtageExtensions;


namespace Utage
{

    /// <summary>
    /// テクスチャ書き込みをするオブジェクトの描画空間
    /// </summary>
    [AddComponentMenu("Utage/ADV/Internal/AdvRenderTextureSpace")]
    public class AdvRenderTextureSpace : MonoBehaviour
    {
		//描画先とするバックバッファ
		public RenderTexture RenderTexture { get; private set; }
		//描画カメラ
		Camera RenderCamera { get; set; }
		//描画キャンバス
		Canvas Canvas { get; set; }
		//描画キャンバススケーラー
		CanvasScaler CanvasScaler { get; set; }

		//描画オブジェクトのルート
		public GameObject RenderRoot { get; private set; }

		internal AdvRenderTextureSetting Setting { get; private set; }
		//複数スプライトを重ねる場合には、特殊なシェーダー・設定が必要になる
		public AdvRenderTextureMode RenderTextureType { get { return Setting.RenderTextureType; } }
		
		//描画が一度でもされたか
		public bool HasRendered { get; protected set; }


		//初期化
		internal void Init(AdvGraphicInfo graphic, float pixelsToUnits)
		{
			this.Setting = graphic.RenderTextureSetting;
			var customPixelsToUnits = this.GetComponentInParent<IRenderTextureCustomPixelsToUnits>();
			if (customPixelsToUnits != null)
			{
				pixelsToUnits = customPixelsToUnits.GetRenderTexturesPixelsToUnits(graphic, pixelsToUnits);
			}
			CreateCamera(pixelsToUnits);
			CreateTexture();
			CreateRoot(graphic, pixelsToUnits);
		}

		//破棄
		void OnDestroy()
		{
			if (this.RenderTexture)
			{
				this.RenderTexture.Release();
				Destroy(this.RenderTexture);
			}
		}

		//カメラの作成
		void CreateCamera(float pixelsToUnits)
		{
			this.RenderCamera = this.gameObject.AddComponent<Camera>();
			this.RenderCamera.gameObject.layer = this.gameObject.layer;
			this.RenderCamera.cullingMask = 1 << this.gameObject.layer;
			this.RenderCamera.depth = -100;
			this.RenderCamera.clearFlags = CameraClearFlags.SolidColor;
			this.RenderCamera.backgroundColor = (RenderTextureType == AdvRenderTextureMode.Image) ? new Color(0, 0, 0, 1) : new Color(0, 0, 0, 0);
			this.RenderCamera.orthographic = true;
			this.RenderCamera.orthographicSize = Setting.RenderTextureSize.y/pixelsToUnits/2/Setting.RenderTextureScale;
		}

		//RenderTextureの作成
		void CreateTexture()
		{
			int w = (int)Setting.RenderTextureSize.x;
			int h = (int)Setting.RenderTextureSize.y;
			this.RenderTexture = new RenderTexture(w, h, 16, RenderTextureFormat.ARGB32);
			this.RenderCamera.targetTexture = this.RenderTexture;
		}

		//ルートオブジェクトの作成
		void CreateRoot(AdvGraphicInfo graphic, float pixelsToUnits)
		{
			
			if (graphic.IsUguiComponentType)
			{
				CreateCanvas ();
			}
			else
			{
				this.RenderRoot = this.RenderCamera.transform.AddChildGameObject("Root");
				this.RenderRoot.transform.localPosition = Setting.RenderTextureOffset/ pixelsToUnits;
				this.RenderRoot.transform.localScale = graphic.Scale;
			}
		}

		//描画キャンバスの作成
		void CreateCanvas()
		{
			GameObject go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
			this.RenderCamera.transform.AddChild(go);
			this.Canvas = go.GetComponent<Canvas>();
#if UNITY_5_6_OR_NEWER
			this.Canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;
			this.RenderCamera.nearClipPlane = -1;
#endif
			this.Canvas.renderMode = RenderMode.ScreenSpaceCamera;
			this.Canvas.worldCamera = this.RenderCamera;

			this.CanvasScaler = this.Canvas.gameObject.AddComponent<CanvasScaler>();
			this.CanvasScaler.referenceResolution = Setting.RenderTextureSize;
			this.CanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
			this.CanvasScaler.scaleFactor = Setting.RenderTextureScale;
			this.CanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
			this.RenderRoot = this.Canvas.transform.AddChildGameObjectComponent<RectTransform>("Root").gameObject;
		}


		//何らかの原因でテクスチャが破壊されてる場合の対策
		void Update()
		{
			if (!RenderTexture.IsCreated())
			{
				RenderTexture.Create();
			}
		}

		private void OnPostRender()
		{
			if (RenderTexture != null)
			{
				HasRendered = true;
			}
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(AdvRenderTextureSpace))]
		public class AdvGraphicRenderTextureInspector : Editor
		{
			AdvRenderTextureSpace Obj { get { return this.target as AdvRenderTextureSpace; } }

			//プレビュー表示する場合true
			public override bool HasPreviewGUI()
			{
				return true;
			}

			public override void OnPreviewGUI(Rect r, GUIStyle background)
			{
				GUI.DrawTexture(r, Obj.RenderTexture, ScaleMode.ScaleToFit, true);

			}
		}
#endif

	}
}
