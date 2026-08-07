using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// ラベルで区別された各オーディオを鳴らす
	/// 基本はシステム内部で使うので外から呼ばないこと
	/// </summary>
	[AddComponentMenu("Utage/Lib/Sound/SoundAudioPlayer")]
	internal class SoundAudioPlayer : MonoBehaviour
	{
		//ラベル
		internal string Label { get; private set; }
		//グループ情報
		internal SoundGroup Group { get; set; }

		public SoundAudio Audio { get; private set; }
		SoundAudio FadeOutAudio { get; set; }

		List<SoundAudio> AudioList { get; set; }
		List<SoundAudio> CurrentFrameAudioList { get; set; }

		internal void Init(string label, SoundGroup group)
		{
			this.Group = group;
			this.Label = label;
			this.AudioList = new List<SoundAudio>();
			this.CurrentFrameAudioList = new List<SoundAudio>();
		}

		void OnDestroy()
		{
			this.Group.Remove(Label);
		}

		internal void Remove(SoundAudio audio)
		{
			AudioList.Remove(audio);
			if (this.Group.AutoDestoryPlayer && AudioList.Count == 0)
			{
				GameObject.Destroy(this.gameObject);
			}
		}

		//曲が終わっているか
		public bool IsStop()
		{
			foreach (var audio in AudioList)
			{
				if (audio != null) return false;
			}
			return true;
		}

		//再生中か
		public bool IsPlaying()
		{
			foreach (var audio in AudioList)
			{
				if (audio != null && audio.IsPlaying()) return true;
			}
			return false;
		}

		//再生中か
		public bool IsPlaying(AssetFile assetFile)
		{
			foreach (var audio in AudioList)
			{
				if (audio == null ) continue;
				if (!audio.IsPlaying()) continue;
				if (audio.Data.File == assetFile)
				{
					return true;
				}
			}
			return false;
		}

		//ループ再生中か
		public bool IsPlayingLoop()
		{
			foreach (var audio in AudioList)
			{
				if (audio != null && audio.IsPlayingLoop()) return true;
			}
			return false;
		}

		void LateUpdate()
		{
			CurrentFrameAudioList.Clear();
		}

		//再生（直前があればフェードアウトしてから再生）
		internal void Play(SoundData data, float fadeInTime, float fadeOutTime)
		{
			switch (data.PlayMode)
			{
				case SoundPlayMode.Add:
					//重複して鳴らす（SEなど）
					PlayAdd(data, fadeInTime, fadeOutTime);
					break;
				case SoundPlayMode.Replay:
					//直前のをフェードアウトし、同時に先頭から鳴らしなおす（一部のSEなど）
					PlayFade(data, fadeInTime, fadeOutTime,true);
					break;
				case SoundPlayMode.NotPlaySame:
					//同じサウンドが鳴っている場合は、そのままにしてなにもしない（BGMや一部のSEなど）
					if ((Audio != null && Audio.IsPlaying(data.Clip)))
					{
						return;
					}
					PlayFade(data, fadeInTime, fadeOutTime, false);
					break;
			}
		}

		//再生（直前があればフェードアウトしてから再生）
		void PlayAdd(SoundData data, float fadeInTime, float fadeOutTime)
		{
			//今のフレームで同じサウンドを鳴らしていたらもう鳴らさない
			foreach (var item in CurrentFrameAudioList)
			{
				if (item != null && item.IsEqualClip(data.Clip))
				{
					return;
				}
			}

			SoundAudio audio = CreateNewAudio(data);
			//即時再生
			audio.Play(fadeInTime);
			CurrentFrameAudioList.Add(audio);
		}

		//再生（直前があればフェードアウトしてから再生）
		void PlayFade(SoundData data, float fadeInTime, float fadeOutTime, bool corssFade)
		{
			//フェードアウト中のがあったら消す
			if (FadeOutAudio != null)
			{
				GameObject.Destroy(FadeOutAudio.gameObject);
			}

			//現在のオーディオがないなら即座に鳴らす
			if (Audio == null)
			{
				Audio = CreateNewAudio(data);
				//即時再生
				Audio.Play(fadeInTime );
			}
			else
			{
				//今鳴っているものをフェードアウト
				FadeOutAudio = Audio;
				Audio = CreateNewAudio(data);
				FadeOutAudio.FadeOut(fadeOutTime);
				if (corssFade)
				{
					//即座に鳴らす
					Audio.Play(fadeInTime);
				}
				else
				{
					//フェードアウトを待ってから鳴らす
					if (Audio != null)
					{
						Audio.Play(fadeInTime, fadeOutTime);
					}
				}
			}
		}

		//新規でオーディオ作成
		SoundAudio CreateNewAudio(SoundData soundData)
		{
			SoundAudio audio = this.transform.AddChildGameObjectComponent<SoundAudio>(soundData.Name);
			audio.Init(this, soundData);
			AudioList.Add(audio);
			return audio;
		}

		//サウンドを終了
		public void Stop(float fadeTime)
		{
			foreach (var audio in AudioList)
			{
				if (audio == null) continue;
				audio.FadeOut(fadeTime);
			}
		}


		internal float GetSamplesVolume()
		{
			return IsPlaying() ? Audio.GetSamplesVolume(): 0;
		}

		public bool IsLoading{ get { return AudioList.Exists(x=>x.IsLoading); } }

		const int Version = 0;
		//セーブデータ用のバイナリ書き込み
		internal void Write(BinaryWriter writer)
		{
			writer.Write(Version);
			writer.Write(AudioList.Count);
			foreach (var audio in AudioList)
			{
				bool enableSave = audio.EnableSave;
				writer.Write(enableSave);
				if (!enableSave) continue;
				writer.WriteBuffer(audio.Data.Write);
			}
			writer.Write(Audio == null ? "" : Audio.gameObject.name);
		}

		//セーブデータ用のバイナリ読み込み
		internal void Read(BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version <= Version)
			{
				int audioCount = reader.ReadInt32();
				for (int i = 0; i < audioCount; ++i)
				{
					bool enableSave = reader.ReadBoolean();
					if (!enableSave) continue;

					SoundData soundData = new SoundData();
					reader.ReadBuffer(soundData.Read);
					Play(soundData, 0.1f, 0);
				}
				string audioName = reader.ReadString();
				if (!string.IsNullOrEmpty(audioName))
				{
					Audio = AudioList.Find(x => x != FadeOutAudio && x.gameObject.name == audioName);
				}
				if (this.Group.AutoDestoryPlayer && AudioList.Count == 0)
				{
					GameObject.Destroy(this.gameObject);
				}
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}
	}
}
