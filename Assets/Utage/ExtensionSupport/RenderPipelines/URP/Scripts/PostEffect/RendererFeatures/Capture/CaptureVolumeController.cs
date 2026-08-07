#if UTAGE_URP
using UnityEngine;
using UtageExtensions;
using System;
using UnityEngine.Rendering;

namespace Utage.RenderPipeline.Urp
{
	//カメラキャプチャー用のVolumeを制御するコンポーネント
	public class CaptureVolumeController : MonoBehaviour
	{
		public RenderTexture CaptureTextureToWrite { get => captureTextureToWrite; set => captureTextureToWrite = value; }
        [SerializeField] RenderTexture captureTextureToWrite;
		Action OnComplete { get; set; }

		Volume Volume => this.GetComponentCache(ref volume);
		Volume volume;

		CaptureVolume CaptureVolume => captureVolume;
		CaptureVolume captureVolume;

		void Awake()
		{
			if (!Volume.profile.TryGet(out captureVolume))
			{
				Debug.LogError($"{nameof(CaptureVolume)} is not found in Volume", Volume);
			}
		}

		public void Capture(RenderTexture captureTextureToWrite, Action onComplete)
		{
			if (CaptureVolume==null)
			{
				Debug.LogError($"{nameof(CaptureVolume)} is not found in Volume", Volume);
				onComplete?.Invoke();
				return;
			}
			CaptureVolume.active = true;
			CaptureTextureToWrite = captureTextureToWrite;
			OnComplete = onComplete;
		}

		public void OnCaptured()
		{
			OnComplete?.Invoke();
			OnComplete = null;
			CaptureTextureToWrite = null;
			if(CaptureVolume!=null)
			{
				CaptureVolume.active = false;
			}
		}
	}
}
#endif
