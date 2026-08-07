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
	[AddComponentMenu("Utage/ADV/Internal/GraphicObject/AdvGraphicObjectRawImage")]
	public class AdvGraphicObjectRawImage : AdvGraphicObjectUguiBase, IAdvCrossFadeImageObject
	{
		protected override Material Material { get { return RawImage.material; } set { RawImage.material = value; } }
		RawImage RawImage { get; set; }

		protected RenderTexture CaptureImage { get; set; } 
		UguiCrossFadeRawImage crossFade;
		//クロスフェード用のファイル参照
		AssetFileReference crossFadeReference;
		void ReleaseCrossFadeReference()
		{
			if (crossFadeReference != null)
			{
				DestroyImmediate(crossFadeReference);
				crossFadeReference = null;
			}
			if (crossFade != null)
			{
				crossFade.RemoveComponentMySelf();
				crossFade = null;
			}
		}

		
		protected virtual void OnDestroy()
		{
			ReleaseCaptureImage();
		}

		protected virtual void ReleaseCaptureImage()
		{
			if (CaptureImage != null)
			{
				CaptureImage.Release();
				CaptureImage = null;
			}
		}

		//初期化処理
		protected override void AddGraphicComponentOnInit()
		{
			RawImage = this.GetComponentCreateIfMissing<RawImage>();
		}

		//********描画時にクロスフェードが失敗するであろうかのチェック********//
		public override bool CheckFailedCrossFade(AdvGraphicInfo graphic)
		{
			return !EnableCrossFade(graphic);
		}

		//********描画時のリソース変更********//
		public override void ChangeResourceOnDraw(AdvGraphicInfo graphic, float fadeTime)
		{
			Material = graphic.RenderTextureSetting.GetRenderMaterialIfEnable(Material);

			//既に描画されている場合は、クロスフェード用のイメージを作成
			bool crossFade = TryCreateCrossFadeImage(fadeTime, graphic);
			if (!crossFade)
			{
				ReleaseCrossFadeReference();
			}
			//新しくリソースを設定
			RawImage.texture = graphic.File.Texture;
			RawImage.SetNativeSize();
			if (!crossFade)
			{
				ParentObject.FadeIn(fadeTime);
			}
		}

		//クロスフェード用のイメージを作成
		protected bool TryCreateCrossFadeImage(float fadeTime, AdvGraphicInfo graphic)
		{
			if (LastResource == null) return false;

			if (EnableCrossFade(graphic))
			{
				StartCrossFadeImage(fadeTime);
				return true;
			}
			else
			{
				return false;
			}
		}

		//今の表示状態と比較して、クロスフェード可能か
		protected bool EnableCrossFade(AdvGraphicInfo graphic)
		{
			Texture texture = graphic.File.Texture as Texture;
			if (texture == null) return false;
			if (RawImage.texture == null) return false;
			return RawImage.rectTransform.pivot == graphic.Pivot
				&& RawImage.texture.width == texture.width
				&& RawImage.texture.height == texture.height;
		}

		//前フレームのテクスチャを使ってクロスフェード処理を行う
		internal void StartCrossFadeImage(float time)
		{
			Texture lastTexture = LastResource.File.Texture;
			ReleaseCrossFadeReference();
			crossFadeReference = this.gameObject.AddComponent<AssetFileReference>();
			crossFadeReference.Init(LastResource.File);

			crossFade = this.gameObject.AddComponent<UguiCrossFadeRawImage>();
			crossFade.Timer.Unscaled = Engine.Time.Unscaled;
			crossFade.CrossFade(
				lastTexture,
				time,
				() =>
				{
					ReleaseCrossFadeReference();
				}
			);
		}

		//カメラのキャプチャ画像を、Imageとして設定
		internal void CaptureCamera(Camera targetCamera)
		{
			RawImage.enabled = false;

			//カメラのキャプチャコンポーネントを有効に
			var captureManager = targetCamera.gameObject.GetComponentInParent<CameraManager>().CaptureManager;
			if (CaptureImage != null)
			{
				RenderTexture.ReleaseTemporary(CaptureImage);
			}
			CaptureImage = RenderTexture.GetTemporary (targetCamera.scaledPixelWidth, targetCamera.scaledPixelHeight);
			captureManager.Capture(targetCamera,CaptureImage,()=>OnCaptured(targetCamera));
		}

		void OnCaptured(Camera targetCamera)
		{
			RawImage.enabled = true;
			RawImage.texture = CaptureImage;
			LetterBoxCamera letterBoxCamera = targetCamera.GetComponent<LetterBoxCamera>();
			if (letterBoxCamera != null)
			{
				RawImage.rectTransform.SetSize(letterBoxCamera.CurrentSize);
				//ズームが1ではなく、このイメージを描画するカメラのキャプチャ画像かどうか
				if (letterBoxCamera.Zoom2D != 1)
				{
					int layerMask = 1 << this.gameObject.layer;
					if ((letterBoxCamera.CachedCamera.cullingMask & layerMask) != 0)
					{
						Vector2 pivot = letterBoxCamera.Zoom2DCenter;
						pivot.x /= letterBoxCamera.CurrentSize.x;
						pivot.y /= letterBoxCamera.CurrentSize.y;
						pivot = -pivot + Vector2.one * 0.5f;
						RawImage.rectTransform.pivot = pivot;
						RawImage.rectTransform.localScale = Vector3.one / letterBoxCamera.Zoom2D;
					}
				}
			}
			else
			{
				RawImage.rectTransform.SetSize(Screen.width, Screen.height);
			}

		}
		public bool IsCrossFading
		{
			get
			{
				if (crossFade == null) return false;
				return true;
			}
		}

		public void RestartCrossFade(float fadeTime, Action onComplete)
		{
			if (crossFade == null)
			{
				Debug.LogError("CrossFadeComponent is not found",this);
				return;
			}

			crossFade.Restart(
				fadeTime,
				() =>
				{
					ReleaseCrossFadeReference();
					onComplete();
				});
		}

		public void SkipCrossFade()
		{
			if (crossFade == null)
			{
				Debug.LogError("CrossFadeComponent is not found",this);
				return;
			}
			crossFade.Timer.SkipToEnd();
		}
	}
}
