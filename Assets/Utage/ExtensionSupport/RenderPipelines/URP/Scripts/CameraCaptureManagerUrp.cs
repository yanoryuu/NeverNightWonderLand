#if UTAGE_URP
using UnityEngine;
using UtageExtensions;
using System;

namespace Utage.RenderPipeline.Urp
{
	//URP用のカメラキャプチャ処理
	[AddComponentMenu("Utage/Lib/Camera/CameraCaptureManagerUrp")]
	public class CameraCaptureManagerUrp : MonoBehaviour, ICameraCaptureManager
	{
		public void Capture(Camera targetCamera, RenderTexture captureTextureToWrite, Action onComplete)
		{
			CaptureVolumeController captureCamera = targetCamera.gameObject.GetComponentInChildren<CaptureVolumeController>(true);
			captureCamera.Capture(captureTextureToWrite,onComplete);
		}
	}
}
#endif
