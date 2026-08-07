// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEngine;
using UnityEngine.Events;
using UtageExtensions;

namespace Utage
{
	[System.Serializable]
	public class CaptureCameraEvent: UnityEvent<CaptureCamera> { }

	/// <summary>
	/// Cameraの画像を一時的にキャプチャする
	/// </summary>
	[RequireComponent(typeof(Camera))]
	[AddComponentMenu("Utage/Lib/Camera/CaptureCamera")]
    public class CaptureCamera : MonoBehaviour
	{
		public RenderTexture CaptureImage { get; set; }
		public CaptureCameraEvent OnCaptured = new CaptureCameraEvent();

		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (!this.enabled) return;
			if(CaptureImage==null) return;
			Graphics.Blit(source, CaptureImage);
			Graphics.Blit(source, destination);
			OnCaptured.Invoke(this);
		}

		void OnDestroy()
		{
			OnCaptured.RemoveAllListeners();
		}
	}
}
