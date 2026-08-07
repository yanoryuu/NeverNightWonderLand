#if UTAGE_RENDER_PIPELINE
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UtageExtensions;

namespace Utage.RenderPipeline
{
	//カメラごとのポストエフェクトの管理
	public class AdvCameraPostEffectManager : MonoBehaviour
	{
		public Camera TargetCamera => this.GetComponentCacheInParent(ref targetCamera);
		Camera targetCamera;

		public AdvEngine AdvEngine => this.GetAdvEngineCacheFindIfMissing(ref engine);
		[SerializeField] AdvEngine engine;

		public AdvPostEffectVolume[] PostEffectVolumes => this.GetComponentsCacheInChildren(ref postEffectVolumes);
		[NonSerialized] AdvPostEffectVolume[] postEffectVolumes;

		public AdvPostEffectVolume FadeVolume => fadeVolume;
		[SerializeField] AdvPostEffectVolume fadeVolume;

		public AdvPostEffectVolume ImageEffectVolume => imageEffectVolume;
		[SerializeField] AdvPostEffectVolume imageEffectVolume;
		
		const int Version = 0;

		//セーブデータ用のバイナリ書き込み
		public void Write(BinaryWriter writer)
		{
			writer.Write(Version);
			writer.Write(PostEffectVolumes.Length);
			foreach (var postEffectVolume in PostEffectVolumes)
			{
				writer.Write(postEffectVolume.name);
				writer.WriteBuffer(postEffectVolume.Write);
			}
		}

		//セーブデータ用のバイナリ読み込み
		public void Read(BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version < 0 || version > Version)
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
				return;
			}

			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				string volumeName = reader.ReadString();
				AdvPostEffectVolume postEffectVolume = PostEffectVolumes.FirstOrDefault(x => x.name == volumeName);
				if (postEffectVolume != null)
				{
					reader.ReadBuffer(postEffectVolume.Read);
				}
				else
				{
					//セーブされていたが、消えているので読み込まない
					reader.SkipBuffer();
				}
			}
		}

		public void Clear()
		{
			foreach (var postEffectVolume in PostEffectVolumes)
			{
				postEffectVolume.OnClear();
			}
		}

	}
}
#endif
