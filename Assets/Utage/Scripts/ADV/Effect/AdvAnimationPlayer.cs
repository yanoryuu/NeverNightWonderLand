// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// AnimationClipのプレイヤー
	/// </summary>
	[AddComponentMenu("Utage/ADV/Internal/AdvAnimationPlayer")]
	public class AdvAnimationPlayer : MonoBehaviour
		, IAdvSkipSpeed
	{
		public const WrapMode NoneOverrideWrapMode = (WrapMode)(-1);

		public bool AutoDestory { get; set; }
		public bool EnableSave { get; set; }

		AnimationClip Clip { get; set; }
		float Speed { get; set; }
		public bool IsLoop
		{
			get
			{
				if (Clip == null) return false;
				return Clip.isLooping || Clip.wrapMode == WrapMode.Loop || Clip.wrapMode == WrapMode.PingPong;
			}
		}

		Action onComplete;
		Animation lecayAnimation;
		Animator animator = null;

		internal void Play(AnimationClip clip, float speed, Action onComplete = null)
		{
			this.Clip = clip;
			this.Speed = speed;
			this.onComplete = onComplete;
			if (clip.legacy)
			{
				PlayAnimatinLegacy(clip, speed);
			}
			else
			{
				Debug.LogError("Not Support");
			}
		}

		internal void Cancel()
		{
			if (lecayAnimation != null)
			{
				lecayAnimation.Stop();
			}
			OnComplete();
		}

		internal void SkipToEnd()
		{
			if (lecayAnimation != null)
			{
				var anim = lecayAnimation[Clip.name]; 
				anim.time = Clip.length;
				if (this.IsLoop)
				{
					anim.wrapMode = WrapMode.Default;
				}
				lecayAnimation.Sample();
				Cancel();
			}
		}

		//ループアニメーションの場合、スキップスピードをリセットする必要がある
		public void OnChangeSkipSpeed(float speed)
		{
			if (lecayAnimation != null)
			{
				var wrapMode = Clip.wrapMode;
				if (wrapMode == WrapMode.Loop || wrapMode == WrapMode.PingPong)
				{
					Speed = speed;
					lecayAnimation[Clip.name].speed = speed;
				}
			}
		}

		//レガシーアニメーションでアニメーションClip再生
		void PlayAnimatinLegacy(AnimationClip clip, float speed)
		{
			if(this.lecayAnimation==null)
			{
				this.lecayAnimation = this.gameObject.GetComponentCreateIfMissing<Animation>();
			}
			this.lecayAnimation.AddClip(clip, clip.name);
			this.lecayAnimation[clip.name].speed = speed;
			this.lecayAnimation.Play(clip.name);
		}

		float GetTime()
		{
			if (this.lecayAnimation != null)
			{
				return lecayAnimation[Clip.name].time;
			}
			else if (this.animator)
			{
				Debug.Log("Not Support");
				return 0;
			}
			else
			{
				return 0;
			}
		}

		void SetTime(float time)
		{
			if (this.lecayAnimation != null)
			{
				lecayAnimation[Clip.name].time = time;
			}
			else if (this.animator)
			{
				Debug.Log("Not Support");
			}
			else
			{
			}
		}

		void Update()
		{
			if (this.lecayAnimation != null)
			{
				if (!lecayAnimation.isPlaying)
				{
					OnComplete();
				}
			}
			else if(this.animator)
			{
				Debug.LogError("Not Support");
			}
		}

		void OnComplete()
		{
			DestroyComponent(true);
			if (onComplete != null) onComplete();
			if (AutoDestory) Destroy(this);
		}

		void OnDestroy()
		{
			DestroyComponent(false);
		}

		public void DestroyComponentImmediate()
		{
			DestroyComponent(true);
		}
		void DestroyComponent(bool immediate)
		{
			EnableSave = false;
			if (lecayAnimation != null)
			{
				lecayAnimation.RemoveComponentMySelf(immediate);
				lecayAnimation = null;
			}
			if (this.animator)
			{
				animator.RemoveComponentMySelf(immediate);
				animator = null;
			}
		}

		const int Version = 0;
		//セーブデータ用のバイナリ書き込み
		public void Write(BinaryWriter writer)
		{
			writer.Write(Version);

			writer.Write(Clip.name);
			writer.Write(Speed);
			writer.Write(GetTime());
		}

		//セーブデータ用のバイナリ読み込み
		public void Read(BinaryReader reader, AdvEngine engine)
		{
			int version = reader.ReadInt32();
			if (version < 0 || version > Version)
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
				return;
			}

			string animationName = reader.ReadString();
//			float speed = reader.ReadSingle();
			reader.ReadSingle();
			float time = reader.ReadSingle();

			AdvAnimationData data = engine.DataManager.SettingDataManager.AnimationSetting.Find(animationName);
			if (data == null)
			{
				Debug.LogError(animationName + " is not found in Animation sheet");
				Destroy(this);
			}
			else
			{
				this.EnableSave = true;
				this.AutoDestory = true;
				//終了コールバックはロードされないが
				//基本的にはセーブされている場合は、NoWaitなエフェクトで
				//終了コールバックを受けなくて良いものだけのはず
//				Play(data.Clip, speed, null);

				//speedはスキップ状態によるので1に戻す
				Play(data.Clip, 1, null);
				SetTime(time);
			}
		}


		internal static void WriteSaveData(BinaryWriter writer, GameObject go)
		{
			//AnimationPlayerの数だけ書き込み
			AdvAnimationPlayer[] array = go.GetComponents<AdvAnimationPlayer>();
			int count = 0;
			foreach (var player in array)
			{
				if (player.EnableSave) ++count;
			}
			writer.Write(count);
			foreach (var player in array)
			{
				if(player.EnableSave) player.Write(writer);
			}
		}

		internal static void ReadSaveData(BinaryReader reader, GameObject go, AdvEngine engine)
		{
			//AnimationPlayerの数だけ読みこみ
			int count = reader.ReadInt32();
			for (int i = 0; i < count; ++i)
			{
				AdvAnimationPlayer player = go.AddComponent<AdvAnimationPlayer>();
				player.Read(reader, engine);
			}
		}
	}
}
