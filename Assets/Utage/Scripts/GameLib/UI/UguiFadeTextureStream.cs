// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Video;
using UtageExtensions;


namespace Utage
{

	/// <summary>
	/// テクスチャをフェード切り替えしながら次々に表示する
	/// </summary>
	[RequireComponent(typeof(RawImage))]
	[AddComponentMenu("Utage/Lib/UI/UguiFadeTextureStream")]
	public class UguiFadeTextureStream : MonoBehaviour, IPointerClickHandler
	{
		public bool allowSkip = true;
		public bool allowAllSkip = false;
		public bool unscaledTime = false;

		[System.Serializable]
		public class FadeTextureInfo
		{
			public Texture texture;
			public string videoPath;
			public float fadeInTime = 0.5f;
			public float duration = 3.0f;
			public float fadeOutTime = 0.5f;
			public bool allowSkip = false;
		}

		public FadeTextureInfo[] fadeTextures = new FadeTextureInfo[1];

		public VideoPlayer videoPlayer;
		
		protected bool isInput;

		public void OnPointerClick(PointerEventData eventData)
		{
			isInput = true;
		}

		protected virtual bool IsInputSkip(FadeTextureInfo info)
		{
			return (isInput && (allowSkip || info.allowSkip));
		}

		protected virtual bool IsInputAllSkip
		{
			get { return isInput && allowAllSkip; }
		}

		protected virtual void LateUpdate()
		{
			isInput = false;
		}

		public virtual void Play()
		{
			StartCoroutine(CoPlay());
		}

		public bool IsPlaying
		{
			get { return isPlaying; }
		}

		protected bool isPlaying;
		protected bool AllSkip { get; set; }

		protected virtual IEnumerator CoPlay()
		{
			isPlaying = true;
			RawImage rawImage = GetComponent<RawImage>();
			rawImage.CrossFadeAlpha(0, 0, true);

			foreach (FadeTextureInfo info in fadeTextures)
			{
				rawImage.texture = info.texture;
				AllSkip = false;

				if (info.texture)
				{
					rawImage.CrossFadeAlpha(1, info.fadeInTime, true);
					float time = 0;
					while (!IsInputSkip(info))
					{
						yield return null;
						time += TimeUtil.GetDeltaTime(unscaledTime);
						if (time > info.fadeInTime) break;
					}

					time = 0;
					while (!IsInputSkip(info))
					{
						yield return null;
						time += TimeUtil.GetDeltaTime(unscaledTime);
						if (time > info.duration) break;
					}

					AllSkip = IsInputAllSkip;
					rawImage.CrossFadeAlpha(0, info.fadeOutTime, true);
					yield return TimeUtil.WaitForSeconds(unscaledTime, info.fadeOutTime);
				}
				else if (!string.IsNullOrEmpty(info.videoPath))
				{
					yield return PlayVideo(info);
				}

				if (AllSkip) break;
				yield return null;
			}

			isPlaying = false;
		}

		protected virtual IEnumerator PlayVideo(FadeTextureInfo info)
		{
			if (videoPlayer==null)
			{
				Debug.LogError("videoPlayer is null ",this);
				yield break;
			}
			VideoClip videoClip = Resources.Load<VideoClip>(info.videoPath);
			if (videoClip == null)
			{
				Debug.LogError("Video cant load from " + info.videoPath,this);
				yield break;
			}

			videoPlayer.clip = videoClip;
			// 準備
			videoPlayer.Prepare();
			while(!videoPlayer.isPrepared) yield return null;  
			yield return null;
			
			//音量設定
			var soundManager = SoundManager.GetInstance(); 
			float volume = soundManager.BgmVolume * soundManager.MasterVolume;
			videoPlayer.SetDirectAudioVolume(0, volume);
			
			// 再生
			videoPlayer.Play();
			while (videoPlayer.isPlaying)
			{
				if (IsInputSkip(info))
				{
					videoPlayer.Stop();
					AllSkip = IsInputAllSkip;
				}
				yield return null;
			}
			Resources.UnloadAsset(videoClip);
		}
	}
}
