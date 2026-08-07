using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UtageExtensions;
using System;

namespace Utage
{
	[AddComponentMenu("Utage/Lib/Camera/CameraCaptureManagerBuiltin")]
	public class CameraCaptureManagerBuiltin : MonoBehaviour, ICameraCaptureManager
	{
		public void Capture(Camera targetCamera, RenderTexture captureTextureToWrite, Action onComplete)
		{
			CaptureCamera captureCamera = targetCamera.gameObject.AddComponent<CaptureCamera>();
			captureCamera.enabled = true;
			captureCamera.CaptureImage = captureTextureToWrite;
			captureCamera.OnCaptured.AddListener(
				(x) =>
				{
					onComplete?.Invoke();
					Destroy(captureCamera);
				});
		}
	}
}
