// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections.Generic;
using System.IO;
using UnityEngine.Events;
using UnityEngine;
using System;
using UnityEngine.Audio;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// サウンドのグループ管理
	/// </summary>
	[AddComponentMenu("Utage/Lib/Sound/SoundGroup")]
	public class SoundGroup : MonoBehaviour
	{
		internal SoundManager SoundManager { get { return SoundManagerSystem.SoundManager;  } }
		internal SoundManagerSystem SoundManagerSystem { get; private set; }

		internal Dictionary<string,SoundAudioPlayer> PlayerList { get; } = new ();

		public string GroupName { get { return gameObject.name; } }

		//グループ内で複数のオーディオを鳴らすか
		public bool MultiPlay
		{
			get { return multiPlay; }
			set { multiPlay = value; }
		}
		[SerializeField]
		bool multiPlay;

		//プレイヤーが終了したら自動削除するか
		public bool AutoDestoryPlayer
		{
			get { return autoDestoryPlayer; }
			set { autoDestoryPlayer = value; }
		}
		[SerializeField]
		bool autoDestoryPlayer;

		//マスターボリューム
		public float MasterVolume
		{
			get { return masterVolume; }
			set
			{
				bool changed = !Mathf.Approximately(masterVolume, value); 
				masterVolume = value;
				if (changed)
				{
					UpdateAudioMixerVolume();
				}
			}
		}
		[Range(0, 1), SerializeField]
		float masterVolume = 1;

		//グループボリューム
		public float GroupVolume { get { return groupVolume; } set { groupVolume = value; } }
		[Range(0, 1), SerializeField]
		float groupVolume = 1;

		//グループボリュームのフェード時間
		public float GroupVolumeFadeTime { get { return groupVolumeFadeTime; } set { groupVolumeFadeTime = value; } }
		[SerializeField]
		float groupVolumeFadeTime = 1;

		//現在のグループボリューム
		public float CurrentGroupVolume
		{
			get => currentGroupVolume;
			set
			{
				bool changed = !Mathf.Approximately(currentGroupVolume, value); 
				currentGroupVolume = value;
				if (changed)
				{
					UpdateAudioMixerVolume();
				}
			}
		}
		float currentGroupVolume;
		float groupVolumeVelocity = 1;

		//オーディオミキサーのグループ設定
		public AudioMixerGroup AudioMixerGroup
		{
			get => audioMixerGroup;
			set => audioMixerGroup = value;
		}
		[SerializeField]
		AudioMixerGroup audioMixerGroup = null;

		string MasterVolumeExposedName { get; set; }

		internal void Init(SoundManagerSystem soundManagerSystem)
		{
			SoundManagerSystem = soundManagerSystem;
			this.groupVolumeVelocity = 1;
			if (AudioMixerGroup == null)
			{
				Debug.LogError("Set Audio Mixer Group", this);
			}
			MasterVolumeExposedName = string.Format(SoundManager.MasterVolumeFormat,AudioMixerGroup.name);
			this.CurrentGroupVolume = this.GroupVolume;
		}
/*
		internal float GetVolume(string volumeTag)
		{
			return GetGroupVolume(volumeTag) * this.MasterVolume * SoundManager.MasterVolume;
		}
		internal float GetGroupVolume(string volumeTag)
		{
			float volume = this.CurrentGroupVolume;
			foreach(var taggedVolume in SoundManager.TaggedMasterVolumes)
			{
				if (taggedVolume.Tag == volumeTag)
				{
					volume *= taggedVolume.Volume;
				}
			}
			return volume;
		}
*/		
						
		void UpdateAudioMixerVolume()
		{
			float volume = MasterVolume *CurrentGroupVolume;
			AudioMixerGroup.SetAudioMixerVolume(MasterVolumeExposedName,volume);
		}

		void Update()
		{
			if(SoundManagerSystem==null) return;
			CurrentGroupVolume = UpdateFade(CurrentGroupVolume, GroupVolume, GroupVolumeFadeTime, ref groupVolumeVelocity);
		}

		float UpdateFade(float from, float to, float fadeTime, ref float velocity)
		{
			if (fadeTime <= 0)
			{
				velocity = 0;
				return to;
			}

			if (Mathf.Abs(to - from) < 0.001f)
			{
				//目標値に近づいた
				velocity = 0;
				return to;
			}
			else
			{

				return Mathf.SmoothDamp(from, to, ref velocity, fadeTime);
			}
		}

		internal void Remove(string label)
		{
			PlayerList.Remove(label);
		}

		public bool IsLoading
		{
			get
			{
				foreach (var keyValue in PlayerList)
				{
					if (keyValue.Value.IsLoading) return true;
				}
				return false;
			}
		}

		SoundAudioPlayer GetPlayer(string label)
		{
			SoundAudioPlayer player;
			if( PlayerList.TryGetValue(label, out player))
			{
				return player;
			}
			return null;
		}

		SoundAudioPlayer GetPlayerOrCreateIfMissing(string label)
		{
			SoundAudioPlayer player = GetPlayer(label);
			if (player == null)
			{
				player = this.transform.AddChildGameObjectComponent<SoundAudioPlayer>(label);
				player.Init(label,this);
				PlayerList.Add(label, player);
			}
			return player;
		}

		SoundAudioPlayer GetOnlyOnePlayer(string label, float fadeOutTime)
		{
			SoundAudioPlayer player = GetPlayerOrCreateIfMissing(label);
			if (PlayerList.Count > 1)
			{
				foreach (var keyValue in PlayerList)
				{
					if (keyValue.Value != player)
					{
						keyValue.Value.Stop(fadeOutTime);
					}
				}
			}
			return player;
		}

		internal bool IsPlaying()
		{
			foreach (var keyValue in PlayerList)
			{
				if (keyValue.Value.IsPlaying()) return true;
			}
			return false;
		}

		internal bool IsPlaying(string label)
		{
			SoundAudioPlayer player = GetPlayer(label);
			if (player == null) return false;
			return player.IsPlaying();
		}

		internal bool IsPlaying(string label, AssetFile file)
		{
			SoundAudioPlayer player = GetPlayer(label);
			if (player == null) return false;
			return player.IsPlaying(file);
		}

		internal bool IsPlayingLoop(string label)
		{
			SoundAudioPlayer player = GetPlayer(label);
			if (player == null) return false;
			return player.IsPlayingLoop();
		}

		internal void Play( string label, SoundData data, float fadeInTime, float fadeOutTime)
		{
			SoundAudioPlayer player = ( MultiPlay ) ? GetPlayerOrCreateIfMissing(label) : GetOnlyOnePlayer(label, fadeOutTime);
			player.Play(data, fadeInTime, fadeOutTime);
		}

		internal void Stop(string label, float fadeTime )
		{
			SoundAudioPlayer player = GetPlayer(label);
			if (player == null) return;
			player.Stop(fadeTime);
		}
		internal void StopAll(float fadeTime)
		{
			foreach (var keyValue in PlayerList)
			{
				keyValue.Value.Stop(fadeTime);
			}
		}

		internal void StopAllLoop(float fadeTime)
		{
			foreach (var keyValue in PlayerList)
			{
				if (!keyValue.Value.IsPlayingLoop()) continue;
				keyValue.Value.Stop(fadeTime);
			}
		}

		internal void StopAllIgnoreLoop(float fadeTime)
		{
			foreach (var keyValue in PlayerList)
			{
				if (keyValue.Value.IsPlayingLoop()) continue;
				keyValue.Value.Stop(fadeTime);
			}
		}

		
		internal AudioSource GetAudioSource(string label)
		{
			SoundAudioPlayer player = GetPlayer(label);
			if (player == null) return null;
			return player.Audio.AudioSource;
		}

		internal float GetSamplesVolume(string label)
		{
			SoundAudioPlayer player = GetPlayer(label);
			if (player == null) return 0;
			return player.GetSamplesVolume();
		}

		const int Version = 1;
		const int Version0 = 0;
		//セーブデータ用のバイナリ書き込み
		internal void Write(BinaryWriter writer)
		{
			writer.Write(Version);
			writer.Write(GroupVolume);
			writer.Write(PlayerList.Count);
			foreach (var keyValue in PlayerList)
			{
				writer.Write(keyValue.Key);
				writer.WriteBuffer(keyValue.Value.Write);
			}
		}

		//セーブデータ用のバイナリ読み込み
		internal void Read(BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version <= Version)
			{
				if (version > Version0)
				{
					GroupVolume = reader.ReadSingle();
				}
				int playerCount = reader.ReadInt32();
				for (int i = 0; i < playerCount; ++i)
				{
					string label = reader.ReadString();
					SoundAudioPlayer player = GetPlayerOrCreateIfMissing(label);
					reader.ReadBuffer(player.Read);
				}
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}
	}
}
