// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Events;
using UnityEngine;
using UnityEngine.Audio;
using UtageExtensions;

namespace Utage
{

	/// <summary>
	/// サウンド管理
	/// </summary>
	[AddComponentMenu("Utage/Lib/Sound/SoundManager")]
	public class SoundManager : MonoBehaviour, IBinaryIO
	{
		public const string IdBgm = "Bgm";
		public const string IdAmbience = "Ambience";
		public const string IdVoice = "Voice";
		public const string IdSe = "Se";

		/// <summary>
		/// シングルトンなインスタンスの取得
		/// </summary>
		/// <returns></returns>
		public static SoundManager GetInstance()
		{
			if (null == instance)
			{
				instance = WrapperFindObject.FindObjectOfType<SoundManager>();
			}
			return instance;
		}
		[StaticField] static SoundManager instance;

		void OnDestroy()
		{
			instance = null;
		}

		/// <summary>
		/// マスターボリューム
		/// </summary>
		public float MasterVolume
		{
			get { return this.masterVolume; }
			set
			{
				masterVolume = value;
				if(SystemEx!=null)
				{
					SystemEx.OnChangedMasterVolume();
				}
			}
		}
		[SerializeField, Range(0, 1)]
		float masterVolume = 1;

		/// <summary>
		/// BGMのボリューム
		/// </summary>
		public float BgmVolume
		{
			get { return System.GetMasterVolume(IdBgm); }
			set { System.SetMasterVolume(IdBgm,value); }
		}

		/// <summary>
		/// 環境音のボリューム
		/// </summary>
		public float AmbienceVolume
		{
			get { return System.GetMasterVolume(IdAmbience); }
			set { System.SetMasterVolume(IdAmbience, value); }
		}

		/// <summary>
		/// ボイスのボリューム
		/// </summary>
		public float VoiceVolume
		{
			get { return System.GetMasterVolume(IdVoice); }
			set { System.SetMasterVolume(IdVoice, value); }
		}

		/// <summary>
		/// SEのボリューム
		/// </summary>
		public float SeVolume
		{
			get { return System.GetMasterVolume(IdSe); }
			set { System.SetMasterVolume(IdSe, value); }
		}


		/// キャラ別の音量設定など、タグつきのボリューム設定
		public List<TaggedMasterVolume> TaggedMasterVolumes { get { return taggedMasterVolumes; } }
		[SerializeField]
		List<TaggedMasterVolume> taggedMasterVolumes = new List<TaggedMasterVolume>(){};

		public const string TaggedMasterVolumeOthers = "Others";

		/// キャラ別の音量設定など、タグつきのボリューム設定
		public void SetTaggedMasterVolume(string tag, float volmue)
		{
			TaggedMasterVolume data = TaggedMasterVolumes.Find(x => x.Tag == tag);
			if (data == null)
			{
				data = new TaggedMasterVolume() { Tag = tag, Volume = volmue };
				TaggedMasterVolumes.Add(data);
			}
			else
			{
				data.Volume = volmue;
			}
			if (SystemEx != null)
			{
				SystemEx.OnChangedTaggedMasterVolume(data);
			}
		}
		
		/// <summary>
		/// キャラ別の音量設定など、個別のボリューム設定
		/// </summary>
		[System.Serializable]
		public class TaggedMasterVolume
		{
			//タグ名
			public string Tag { get { return tag; } set { tag = value; } }
			[SerializeField]
			string tag;

			//タグに対応するラベル名のリスト
			//通常はタグ名＝ラベル名なので、設定不要
			//ラベル名が複数ある場合は、ここに追加設定する
			public List<string> Labels => labels;
			[SerializeField] List<string> labels = new ();

			//ボリューム
			public float Volume { get { return volume; } set { volume = value; } }
			[Range(0, 1), SerializeField]
			float volume = 1;
			
			//オーディオミキサーのグループ設定
			public AudioMixerGroup AudioMixerGroup => audioMixerGroup;
			[SerializeField] AudioMixerGroup audioMixerGroup = null;
			
			//指定のラベル名が対応しているか
			public bool ContainsLabel(string label)
			{
				return (Tag==label) || Labels.Contains(label);
			}
		};

		/// <summary>
		/// 違うキャラクターラベルの再生を複数同時に行うか
		/// </summary>
		public bool MultiVoice
		{
			get { return System.IsMultiPlay(IdVoice); }
			set { System.SetMultiPlay(IdVoice, value); }
		}

		/// <summary>
		/// ダッキングする際のボリューム値
		/// </summary>
		public float DuckVolume
		{
			get { return this.duckVolume; }
			set { duckVolume = value; }
		}
		[SerializeField, Range(0, 1)]
		float duckVolume = 0.5f;

		/// <summary>
		/// ダッキングする際のフェード時間
		/// </summary>
		public float DuckFadeTime
		{
			get { return this.duckFadeTime; }
			set { duckFadeTime = value; }
		}
		[SerializeField, Range(0, 1)]
		float duckFadeTime = 0.1f;

		/// <summary>
		/// BGMなどのフェード時間のデフォルト値
		/// </summary>
		public float DefaultFadeTime
		{
			get { return this.defaultFadeTime; }
			set { TryChangeFloat(ref defaultFadeTime, value); }
		}
		[SerializeField]
		float defaultFadeTime = 0.2f;

		/// <summary>
		/// ボイスのフェード時間のデフォルト値
		/// </summary>
		public float DefaultVoiceFadeTime
		{
			get { return this.defaultVoiceFadeTime; }
			set { TryChangeFloat(ref defaultVoiceFadeTime, value); }
		}
		[SerializeField]
		float defaultVoiceFadeTime = 0.05f;

		/// <summary>
		/// ボリュームのデフォルト値
		/// </summary>
		public float DefaultVolume
		{
			get { return this.defaultVolume; }
			set { TryChangeFloat(ref defaultVolume, value); }
		}
		[SerializeField, Range(0, 1)]
		float defaultVolume = 1.0f;
		
		//現在のボイスのキャラクターラベル
		public string CurrentVoiceCharacterLabel { get; set; }

		//ボイスの再生モード
		public SoundPlayMode VoicePlayMode
		{
			get { return this.voicePlayMode; }
			set { voicePlayMode = value; }
		}
		[SerializeField]
		SoundPlayMode voicePlayMode = SoundPlayMode.Replay;
		
		public AudioMixer AudioMixer => audioMixer;
		[SerializeField] AudioMixer audioMixer = null;
		
		public string MasterVolumeFormat => masterVolumeFormat;
		[SerializeField] string masterVolumeFormat = "MasterVolume{0}";
		
		[System.Serializable]
		public class SoundManagerEvent : UnityEvent<SoundManager> { }
		public SoundManagerEvent OnCreateSoundSystem
		{
			get { return onCreateSoundSystem; }
			set { onCreateSoundSystem = value; }
		}
		[SerializeField]
		SoundManagerEvent onCreateSoundSystem = new SoundManagerEvent();

		bool TryChangeFloat(ref float volume, float value)
		{
			if (Mathf.Approximately(volume, value)) return false;
			volume = value;
			return true;
		}

		//実際の処理をするシステム部分
		public SoundManagerSystemInterface System
		{
			get
			{
				if (system == null)
				{
					OnCreateSoundSystem.Invoke(this);
					if (system == null)
					{
						system = new SoundManagerSystem();
					}
					//BGMと環境音のみを再生
					List<string> saveStreamNameList = new List<string>( new string[]{ IdBgm, IdAmbience } );
					system.Init(this, saveStreamNameList);
				}
				return system;
			}
			set
			{
				system = value;
			}
		}
		SoundManagerSystemInterface system;

		//拡張機能に対応したインターフェースがあれば取得
		public SoundManagerSystemInterfaceEx SystemEx
		{
			get
			{
				return (System as SoundManagerSystemInterfaceEx);
			}
		}

		//************ BGM ************//
		public void PlayBgm(AudioClip clip, bool isLoop, float fadeInTime = 0)
		{
			System.Play(IdBgm, IdBgm, new SoundData( clip, SoundPlayMode.NotPlaySame, DefaultVolume, isLoop), fadeInTime,  DefaultFadeTime );
		}
		public void PlayBgm(AudioClip clip, float introTime, float fadeInTime = 0)
		{
			SoundData data = new SoundData(clip, SoundPlayMode.NotPlaySame, DefaultVolume, true);
			data.IntroTime = introTime;
			System.Play(IdBgm, IdBgm, data, fadeInTime, DefaultFadeTime);
		}

		public void PlayBgm(AssetFile file)
		{
			PlayBgm(file, 0, DefaultFadeTime );
		}

		public void PlayBgm(AssetFile file, float fadeInTime, float fadeOutTime)
		{
			PlayBgm(file, DefaultVolume, fadeInTime, fadeOutTime);
		}

		public void PlayBgm(AssetFile file, float volume, float fadeInTime, float fadeOutTime)
		{
			System.Play(IdBgm, IdBgm, new SoundData(file, SoundPlayMode.NotPlaySame, volume, true), fadeInTime, fadeOutTime);
		}

		public void PlayBgm(AssetFile file, float volume, bool isLoop, float fadeInTime, float fadeOutTime)
		{
			System.Play(IdBgm, IdBgm, new SoundData(file, SoundPlayMode.NotPlaySame, volume, isLoop), fadeInTime, fadeOutTime);
		}

		public void StopBgm()
		{
			StopBgm(DefaultFadeTime );
		}

		public void StopBgm(float fadeTime)
		{
			System.StopGroup(IdBgm, fadeTime);
		}


		public bool IsPlayingBgm()
		{
			if (SystemEx == null) return false;

			return SystemEx.IsPlaying(IdBgm);
		}

		//************ 環境音 ************//
		public void PlayAmbience(AssetFile file, bool isLoop)
		{
			PlayAmbience(file, isLoop, 0, DefaultFadeTime);
		}

		public void PlayAmbience(AssetFile file, bool isLoop, float fadeInTime, float fadeOutTime)
		{
			PlayAmbience(file, DefaultVolume, isLoop, fadeInTime, fadeOutTime);
		}
		public void PlayAmbience(AssetFile file, float volume, bool isLoop, float fadeInTime, float fadeOutTime)
		{
			System.Play(IdAmbience, IdAmbience, new SoundData(file, SoundPlayMode.NotPlaySame, volume, isLoop), fadeInTime, fadeOutTime);
		}

		public void PlayAmbience(AudioClip clip, bool isLoop)
		{
			PlayAmbience(clip, isLoop, 0, DefaultFadeTime);
		}

		public void PlayAmbience(AudioClip clip, bool isLoop, float fadeInTime, float fadeOutTime)
		{
			System.Play(IdAmbience, IdAmbience, new SoundData(clip, SoundPlayMode.NotPlaySame, DefaultVolume, isLoop), fadeInTime, fadeOutTime);
		}
		
		public void StopAmbience()
		{
			StopAmbience(DefaultFadeTime);
		}

		public void StopAmbience(float fadeTime)
		{
			System.StopGroup(IdAmbience, fadeTime);
		}

		public bool IsPlayingAmbience()
		{
			if (SystemEx == null) return false;

			return SystemEx.IsPlaying(IdAmbience);
		}

		//************ Voice ************//
		public void PlayVoice(string characterLabel, AssetFile file)
		{
			PlayVoice(characterLabel, file, false);
		}

		public void PlayVoice(string characterLabel, AssetFile file, float fadeInTime, float fadeOutTime)
		{
			PlayVoice(characterLabel, file, DefaultVolume, false, fadeInTime, fadeOutTime);
		}
		
		public void PlayVoice(string characterLabel, AssetFile file, bool isLoop)
		{
			PlayVoice(characterLabel, file, DefaultVolume, isLoop, 0, DefaultVoiceFadeTime);
		}

		public void PlayVoice(string characterLabel, AssetFile file, float volume, bool isLoop)
		{
			PlayVoice(characterLabel, file, volume, isLoop, 0, DefaultVoiceFadeTime);
		}

		public void PlayVoice(string characterLabel, AssetFile file, float volume, bool isLoop, float fadeInTime, float fadeOutTime)
		{
			PlayVoice(characterLabel, new SoundData(file, VoicePlayMode, volume, isLoop), fadeInTime, fadeOutTime);
		}

		public void PlayVoice(string characterLabel, AudioClip clip, bool isLoop)
		{
			PlayVoice(characterLabel, clip, isLoop, 0, DefaultVoiceFadeTime);
		}

		public void PlayVoice(string characterLabel, AudioClip clip, bool isLoop, float fadeInTime, float fadeOutTime)
		{
			PlayVoice(characterLabel, new SoundData(clip, VoicePlayMode, DefaultVolume, isLoop), fadeInTime, fadeOutTime);
		}

		public void PlayVoice(string characterLabel, SoundData data, float fadeInTime, float fadeOutTime)
		{
			CurrentVoiceCharacterLabel = characterLabel;
			TaggedMasterVolume taggedMasterVolume = FindVoiceMasterVolumeTag(characterLabel);
			data.Tag = (taggedMasterVolume!=null) ? taggedMasterVolume.Tag : TaggedMasterVolumeOthers;
			System.Play(IdVoice, characterLabel, data, fadeInTime, fadeOutTime);
		}
		
		//指定したキャラクターラベルのボイスのボリュームの情報を探す
		protected TaggedMasterVolume FindVoiceMasterVolumeTag(string characterLabel)
		{
			foreach (var taggedMasterVolume in TaggedMasterVolumes)
			{
				if(taggedMasterVolume.ContainsLabel(characterLabel))
				{
					return taggedMasterVolume;
				}
			}
			return null;
		}

		//ボイスをすべて止める
		public void StopVoice()
		{
			StopVoice(DefaultVoiceFadeTime);
		}
		public void StopVoice(float fadeTime)
		{
			System.StopGroup(IdVoice, fadeTime);
		}

		//ループ以外のボイスをすべて止める
		public void StopVoiceIgnoreLoop()
		{
			StopVoiceIgnoreLoop(DefaultVoiceFadeTime);
		}
		public void StopVoiceIgnoreLoop(float fadeTime)
		{
			System.StopGroupIgnoreLoop(IdVoice, fadeTime);
		}
		

		//指定したキャラクターラベルのボイスを止める
		public void StopVoice(string characterLabel)
		{
			StopVoice(characterLabel, DefaultVoiceFadeTime);
		}
		public void StopVoice(string characterLabel, float fadeTime)
		{
			System.Stop(IdVoice, characterLabel, fadeTime);
		}

		//今のキャラクターのボイスが鳴っているか
		public bool IsPlayingVoice()
		{
			return IsPlayingVoice(CurrentVoiceCharacterLabel);
		}

		//指定したキャラクターラベルのボイスが鳴っているか
		internal bool IsPlayingVoice(string characterLabel)
		{
			if (characterLabel == null) return false;
			return System.IsPlaying(IdVoice, characterLabel);
		}

		internal float GetCurrentCharacterVoiceSamplesVolume()
		{
			return GetVoiceSamplesVolume(CurrentVoiceCharacterLabel);
		}
		internal float GetVoiceSamplesVolume(string characterLabel)
		{
			return System.GetSamplesVolume(IdVoice, characterLabel);
		}


		//************ SE ************//

		/// <summary>
		/// SEの再生
		/// </summary>
		/// <param name="file">サウンドファイル</param>
		/// <returns>再生をしているサウンドストリーム</returns>
		public void PlaySe(AssetFile file, string label = "", SoundPlayMode playMode = SoundPlayMode.Add, bool isLoop = false)
		{
			PlaySe(file, DefaultVolume, label, playMode, isLoop);
		}

		/// <summary>
		/// SE再生
		/// </summary>
		/// <param name="file">サウンドファイル</param>
		/// <param name="volume">再生ボリューム</param>
		/// <returns>再生をしているサウンドストリーム</returns>
		public void PlaySe(AssetFile file, float volume, string label = "", SoundPlayMode playMode = SoundPlayMode.Add, bool isLoop = false)
		{
			if (string.IsNullOrEmpty(label)) label = file.Sound.name;
			System.Play(IdSe, label, new SoundData(file, playMode, volume, isLoop), 0 ,0);
		}
		
		/// <summary>
		/// SEの再生
		/// </summary>
		/// <param name="clip">オーディオクリップ</param>
		/// <returns>再生をしているサウンドストリーム</returns>
		public void PlaySe(AudioClip clip, string label = "", SoundPlayMode playMode = SoundPlayMode.Add, bool isLoop = false)
		{
			PlaySe(clip, DefaultVolume, label, playMode, isLoop);
		}

		/// <summary>
		/// SE停止
		/// </summary>
		public void PlaySe(AudioClip clip, float volume, string label = "", SoundPlayMode playMode = SoundPlayMode.Add, bool isLoop = false)
		{
			if (string.IsNullOrEmpty(label)) label = clip.name;
			System.Play(IdSe, label, new SoundData(clip, playMode, volume, isLoop), 0, 0.02f);
		}

		public void StopSe(string label, float fadeTime)
		{
			System.Stop(IdSe, label, fadeTime);
		}

		public void StopSeAll(float fadeTime)
		{
			System.StopGroup(IdSe, fadeTime);
		}

		public bool IsPlayingSe()
		{
			if (SystemEx == null) return false;

			return SystemEx.IsPlaying(IdSe);
		}

		public bool IsPlayingSe(string label)
		{
			return System.IsPlaying(IdSe,label);
		}


		//************ All ************//

		public void SetGroupVolume(string groupName, float volume, float fadeTime = 0)
		{
			System.SetGroupVolume(groupName, volume, fadeTime);
		}

		public float GetGroupVolume(string groupName)
		{
			return System.GetGroupVolume(groupName);
		}

		/// <summary>
		/// フェードアウトして曲全てを停止
		/// </summary>
		/// <param name="fadeTime">フェードアウトの時間</param>
		public void StopGroups(string[] groups)
		{
			StopGroups(groups, DefaultFadeTime);
		}
		public void StopGroups(string[] groups, float fadeTime)
		{
			foreach (var group in groups)
			{
				System.StopGroup(group, fadeTime);
			}
		}

		/// <summary>
		/// フェードアウトして曲全てを停止
		/// </summary>
		/// <param name="fadeTime">フェードアウトの時間</param>
		public void StopAll()
		{
			StopAll(DefaultFadeTime);
		}
		public void StopAll(float fadeTime)
		{
			System.StopAll(fadeTime);
		}

		/// <summary>
		/// ループするものはすべて終了
		/// </summary>
		/// <param name="fadeTime">フェードアウトの時間</param>
		public void StopAllLoop()
		{
			StopAllLoop(DefaultFadeTime);
		}
		public void StopAllLoop(float fadeTime)
		{
			System.StopAllLoop(fadeTime);
		}

		//ロード中か
		public bool IsLoading { get { return System.IsLoading; } }

		public string SaveKey { get { return "SoundManager"; } }

		//サウンドのセーブデータだけ読み込みたくないケースで使用する
		//独自の拡張などで使用することを想定
		public bool IgnoreReadSaveData { get; set; }

		//バイナリ書き込み
		public void OnWrite(BinaryWriter writer)
		{
			System.WriteSaveData(writer);
		}
		//バイナリ読み込み
		public void OnRead(BinaryReader reader)
		{
			if(IgnoreReadSaveData) return;
			
			System.ReadSaveDataBuffer(reader);
		}
	}
}
