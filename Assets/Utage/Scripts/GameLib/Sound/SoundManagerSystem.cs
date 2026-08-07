// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.Audio;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// サウンド管理
	/// </summary>
	public class SoundManagerSystem : SoundManagerSystemInterface, SoundManagerSystemInterfaceEx
	{
		private Dictionary<string, SoundGroup> Groups { get; } = new ();

		//サウンドマネージャー
		internal SoundManager SoundManager { get; private set; }

		public void Init(SoundManager soundManager, List<string> saveStreamNameList)
		{
			SoundManager = soundManager;
			if (SoundManager.AudioMixer == null)
			{
				string msg = LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.Utage4VersionErrorSoundManager);
				string url = @"https://madnesslabo.net/utage/?page_id=468";
				Debug.LogError($"{msg}\n{StringTagUtil.HyperLinkTag(url)}",soundManager);
			}
		}

		//指定の名前のグループを取得。なければ作成。
		SoundGroup GetGroupAndCreateIfMissing(string name)
		{
			SoundGroup group = GetGroup(name);
			if (group == null)
			{
				//自分の子供以下にあればそれを、なければエラー
				group = SoundManager.transform.Find<SoundGroup>(name);
				if (group == null)
				{
					Debug.LogError($"Sound Group {name} is not found=" + name + "");
					return null;
				}
				group.Init(this);
				Groups.Add(name, group);
			}
			return group;
		}

		//指定の名前のグループを取得
		public SoundGroup GetGroup(string name)
		{
			SoundGroup group;
			if (!Groups.TryGetValue(name, out group))
			{
				return null;
			}
			return group;
		}

		public void Play(string groupName, string label, SoundData data, float fadeInTime, float fadeOutTime)
		{
			SoundGroup group = GetGroupAndCreateIfMissing(groupName);
			group.Play(label, data, fadeInTime, fadeOutTime);
		}

		public void Stop(string groupName, string label, float fadeTime)
		{
			SoundGroup group = GetGroup(groupName);
			if (group == null) return;
			group.Stop(label,fadeTime);
		}

		public bool IsPlaying(string groupName, string label)
		{
			SoundGroup group = GetGroup(groupName);
			if (group == null) return false;
			return group.IsPlaying(label);
		}

		public bool IsPlaying(string groupName, string label, AssetFile file)
		{
			SoundGroup group = GetGroup(groupName);
			if (group == null) return false;
			return group.IsPlaying(label,file);
		}

		public bool IsPlaying(string groupName)
		{
			SoundGroup group = GetGroup(groupName);
			if (group == null) return false;
			return group.IsPlaying();
		}

		public AudioSource GetAudioSource(string groupName, string label)
		{
			SoundGroup group = GetGroup(groupName);
			if (group == null) return null;
			return group.GetAudioSource(label);
		}

		public float GetSamplesVolume(string groupName, string label)
		{
			SoundGroup group = GetGroup(groupName);
			if (group == null) return 0;
			return group.GetSamplesVolume(label);
		}

		public void StopGroup(string groupName, float fadeTime)
		{
			SoundGroup group = GetGroup(groupName);
			if (group == null) return;
			group.StopAll(fadeTime);
		}

		public void StopGroupIgnoreLoop(string groupName, float fadeTime)
		{
			SoundGroup group = GetGroup(groupName);
			if (group == null) return;
			group.StopAllIgnoreLoop(fadeTime);
		}
		
		public void StopAll(float fadeTime)
		{
			foreach (var group in Groups)
			{
				group.Value.StopAll(fadeTime);
			}
		}

		public void StopAllLoop(float fadeTime)
		{
			foreach (var group in Groups)
			{
				group.Value.StopAllLoop(fadeTime);
			}
		}

		public float GetMasterVolume(string groupName)
		{
			SoundGroup group = GetGroupAndCreateIfMissing(groupName);
			if (group == null) 
			{
				Debug.LogError (groupName + " is not created");
				return 1;
			}
			return group.MasterVolume;
		}

		public void SetMasterVolume(string groupName, float volume)
		{
			SoundGroup group = GetGroupAndCreateIfMissing(groupName);
			group.MasterVolume = volume;
		}

		public float GetGroupVolume(string groupName)
		{
			SoundGroup group = GetGroupAndCreateIfMissing(groupName);
			if (group == null)
			{
				Debug.LogError(groupName + " is not created");
				return 1;
			}
			return group.GroupVolume;
		}

		public void SetGroupVolume(string groupName, float volume, float fadeTime)
		{
			SoundGroup group = GetGroupAndCreateIfMissing(groupName);
			group.GroupVolume = volume;
			group.GroupVolumeFadeTime = fadeTime;
		}

		/// <summary>
		/// グループ内で複数のオーディオを再生するかどうか
		/// </summary>
		public bool IsMultiPlay(string groupName)
		{
			SoundGroup group = GetGroupAndCreateIfMissing(groupName);
			if (group == null)
			{
				Debug.LogError(groupName + " is not created");
				return false;
			}
			return group.MultiPlay;
		}
		public void SetMultiPlay(string groupName, bool multiPlay)
		{
			SoundGroup group = GetGroupAndCreateIfMissing(groupName);
			group.MultiPlay = multiPlay;
		}

		public bool IsLoading
		{
			get
			{
				foreach (var group in Groups)
				{
					if (group.Value.IsLoading)
					{
						return true;
					}
				}
				return false;
			}
		}

		// マスターボリューム変更時に呼ばれる
		public void OnChangedMasterVolume()
		{
			if (SoundManager.AudioMixer != null)
			{
				SoundManager.AudioMixer.SetAudioMixerVolume(string.Format(SoundManager.MasterVolumeFormat,""), SoundManager.MasterVolume);
			}
		}

		/// キャラ別の音量設定など、タグつきのボリューム変更時に呼ばれる
		public void OnChangedTaggedMasterVolume(SoundManager.TaggedMasterVolume taggedMasterVolume)
		{
			if (taggedMasterVolume.AudioMixerGroup != null)
			{
				SetAudioMixerVolumeUsingFormat(taggedMasterVolume.AudioMixerGroup, taggedMasterVolume.Volume);
			}
		}
		
		public void SetAudioMixerVolumeUsingFormat( AudioMixerGroup target, float volume)
		{
			target.SetAudioMixerVolumeUsingFormat(SoundManager.MasterVolumeFormat, volume);
		}


		const int Version = 0;
		//セーブデータ用のバイナリ書き込み
		public void WriteSaveData(BinaryWriter writer)
		{
			writer.Write(Version);
			writer.Write(Groups.Count);
			foreach (var keyValue in Groups)
			{
				writer.Write(keyValue.Key);
			}
			foreach (var keyValue in Groups)
			{
				writer.WriteBuffer(keyValue.Value.Write);
			}
		}

		//セーブデータ用のバイナリ読み込み
		public void ReadSaveDataBuffer(BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version <= Version)
			{
				int count = reader.ReadInt32();
				//グループは初期化前にすべて作成済みである必要があるので、いったん実行
				List<SoundGroup> list = new List<SoundGroup>();
				for (int i = 0; i < count; ++i)
				{
					string name = reader.ReadString();
					list.Add(GetGroupAndCreateIfMissing(name));
				}
				for (int i = 0; i < count; ++i)
				{
					reader.ReadBuffer(list[i].Read);
				}
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}
	}
}
