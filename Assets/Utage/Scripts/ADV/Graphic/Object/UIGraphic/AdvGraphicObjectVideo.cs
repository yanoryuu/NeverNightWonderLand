// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#if UNITY_5_6_OR_NEWER && !UTAGE_DISABLE_VIDEO
#define UTAGE_ENABLE_VIDEO
#endif

using System;
using UnityEngine;
using UnityEngine.UI;
#if UTAGE_ENABLE_VIDEO
using UnityEngine.Video;
#endif
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// ビデオ（ムービー）オブジェクトの表示
	/// </summary>
	[AddComponentMenu("Utage/ADV/Internal/GraphicObject/AdvGraphicObjectVideo")]
	public class AdvGraphicObjectVideo : AdvGraphicObjectUguiBase
	{
#if UTAGE_ENABLE_VIDEO
		protected override Material Material { get { return RawImage.material; } set { RawImage.material = value; } }
		RawImage RawImage { get; set; }

		//クロスフェード用のファイル参照
		AssetFileReference crossFadeReference;
		void ReleaseCrossFadeReference()
		{
			if (crossFadeReference != null)
			{
				Destroy(crossFadeReference);
				crossFadeReference = null;
			}
		}
		VideoClip VideoClip { get; set; }
		public VideoPlayer VideoPlayer { get; private set; }
		protected Timer FadeTimer { get; set; }
		//描画先とするバックバッファ
		RenderTexture RenderTexture { get; set; }
		
		public bool IsPlayingOrPreparing { get { return VideoPlayer.isPlaying || IsPreparing; } }
		public bool IsPreparing { get; set; }

		int Width { get; set; }
		int Height { get; set; }

		protected AdvVideoManager VideoManager => Engine.GraphicManager.VideoManager;

		//初期化処理
		protected override void AddGraphicComponentOnInit()
		{
			this.RawImage = this.GetComponentCreateIfMissing<RawImage>();
			this.FadeTimer = this.gameObject.AddComponent<Timer>();
			this.FadeTimer.AutoDestroy = false;
			this.VideoPlayer = this.gameObject.AddComponent<VideoPlayer>();
		}
		//破棄
		void OnDisable()
		{
			UnityEngine.Profiling.Profiler.BeginSample("OnDisAble");
			this.VideoPlayer.Stop();
			UnityEngine.Profiling.Profiler.EndSample();
		}

		//破棄
		void OnDestroy()
		{
			UnityEngine.Profiling.Profiler.BeginSample("ReleaseTexture");
			ReleaseTexture();
			UnityEngine.Profiling.Profiler.EndSample();
		}

		void ReleaseTexture()
		{
			if (this.RenderTexture)
			{
				if (this.VideoPlayer.isPlaying)
				{
					this.VideoPlayer.Stop();
				}
				if (RenderTexture.active == this.RenderTexture)
				{
					RenderTexture.active = null;
				}
				this.RenderTexture.Release();
				Destroy(this.RenderTexture);
			}
		}


		//********描画時にクロスフェードが失敗するであろうかのチェック********//
		public override bool CheckFailedCrossFade(AdvGraphicInfo graphic)
		{
			return true;
		}

		//********描画時のリソース変更********//
		public override void ChangeResourceOnDraw(AdvGraphicInfo graphic, float fadeTime)
		{
			this.VideoClip = graphic.File.UnityObject as VideoClip;
			this.VideoPlayer.clip = this.VideoClip;
			this.VideoPlayer.isLooping = graphic.Loop;
			this.VideoPlayer.SetDirectAudioVolume(0, GetVolume());
			this.VideoPlayer.renderMode = VideoRenderMode.RenderTexture;
			ReleaseTexture();
			this.RenderTexture = new RenderTexture((int)VideoClip.width, (int)VideoClip.height, 16, RenderTextureFormat.ARGB32);
			this.VideoPlayer.targetTexture = this.RenderTexture;

			IsPreparing = true;
			this.VideoPlayer.prepareCompleted += OnPrepareCompleted;
			this.VideoPlayer.Prepare();


			this.RawImage.texture = this.RenderTexture;
			this.RawImage.SetNativeSize();
			this.RawImage.enabled = false;

			if (LastResource == null)
			{
				ParentObject.FadeIn(fadeTime);
			}
			return;
			
			void OnPrepareCompleted(VideoPlayer player)
			{
				IsPreparing = false;
				this.RawImage.enabled = true;
				player.prepareCompleted -= OnPrepareCompleted;
				player.Play();
			}
		}

		protected virtual void Update()
		{
			var player = this.VideoPlayer;
			if (player == null || !player.isPlaying) return;

			player.SetDirectAudioVolume(0, GetVolume());
		}
		
		//サウンド用のボリューム値を取得
		protected virtual float GetVolume()
		{
			float volume = VideoManager.GetVideAudioVolume();
			return volume;
		}
#else
		public UnityEngine.Video.VideoPlayer VideoPlayer { get; } = null;
		public bool IsPlayingOrPreparing { get;  } = false;
		protected override Material Material
		{
			get
			{
				throw new NotImplementedException();
			}

			set
			{
				throw new NotImplementedException();
			}
		}

		protected override void AddGraphicComponentOnInit()
		{
			throw new NotImplementedException();
		}

		public override void ChangeResourceOnDraw(AdvGraphicInfo graphic, float fadeTime)
		{
			throw new NotImplementedException();
		}

		public override bool CheckFailedCrossFade(AdvGraphicInfo graphic)
		{
			throw new NotImplementedException();
		}
#endif
	}
}
