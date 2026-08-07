using System;
using System.IO;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// サウンドの再生用のデータ定義
	/// </summary>
	public class SoundData
	{
		// オーディオクリップ
		public AudioClip Clip
		{
			get
			{
				if (clip == null)
				{
					clip = File.Sound;
				}
				return clip;
			}
		}
		AudioClip clip;

		// ファイル
		public AssetFile File { get; private set; }

		// 名前
		public string Name
		{
			get
			{
				return ( File != null ) ? File.FileName : Clip.name;
			}
		}

		// プレイモード
		public SoundPlayMode PlayMode { get; private set; }		

		// ループするかどうか
		public bool IsLoop { get; set; }

		//再生時に指定されたボリューム
		public float PlayVolume { get; set; }

		//基本的なボリューム
		public float ResourceVolume { get; set; }

		// イントロ時間
		public float IntroTime { get; set; }

		//キャラ名などを区別するタグ
		public string Tag { get; set; }

		//基本的なボリューム
		public float Volume { get { return ResourceVolume * PlayVolume; } }

		// イントロループが有効か
		public bool EnableIntroLoop { get { return IsLoop && IntroTime > 0; } }

		public SoundData(AudioClip clip, SoundPlayMode playmode, float playVolume, bool isLoop)
		{
			this.clip = clip;
			this.PlayMode = playmode;
			this.PlayVolume = playVolume;
			this.IsLoop = isLoop;
			this.ResourceVolume = 1;
			this.Tag = "";
		}

		public SoundData(AssetFile file, SoundPlayMode playmode, float playVolume, bool isLoop)
		{
			this.File = file;
			this.PlayMode = playmode;
			this.PlayVolume = playVolume;
			this.IsLoop = isLoop;
			if (file.SettingData is IAssetFileSoundSettingData)
			{
				IAssetFileSoundSettingData setting = file.SettingData as IAssetFileSoundSettingData;
				this.IntroTime = setting.IntroTime;
				this.ResourceVolume = setting.Volume;
			}
			else
			{
				this.IntroTime = 0;
				this.ResourceVolume = 1;
			}
			this.Tag = "";
		}

		//セーブが有効かどうか
		internal bool EnableSave
		{
			get
			{
				return (File != null) && IsLoop;
			}
		}

		public SoundData() { }

		const int Version = 1;
		const int Version0 = 0;
		//セーブデータ用のバイナリ書き込み
		public void Write(BinaryWriter writer)
		{
			writer.Write(Version);
			writer.Write((int)PlayMode);
			writer.Write(IsLoop);
			writer.Write(PlayVolume);
			writer.Write(ResourceVolume);
			writer.Write(IntroTime);
			writer.Write(Tag);
			writer.Write(File.FileName);

			IAssetFileSoundSettingDataKey seting = File.SettingData as IAssetFileSoundSettingDataKey;
			if (seting != null)
			{
				writer.Write(seting.Key);
			}
			else
			{
				writer.Write("");
			}
		}

		//セーブデータ用のバイナリ読み込み
		public void Read(BinaryReader reader)
		{
			int version = reader.ReadInt32();
			if (version <= Version)
			{
				PlayMode = (SoundPlayMode)reader.ReadInt32();
				IsLoop = reader.ReadBoolean();
				PlayVolume = reader.ReadSingle();
				ResourceVolume = reader.ReadSingle();
				IntroTime = reader.ReadSingle();
				Tag = reader.ReadString();
				string filePath = reader.ReadString();
				if (version <= Version0)
				{
					File = AssetFileManager.GetFileCreateIfMissing(filePath);
				}
				else
				{
					string key = reader.ReadString();
					File = AssetFileManager.GetSoundFileCreateIfMissing(filePath, key);
				}
			}
			else
			{
				Debug.LogError(LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.UnknownVersion, version));
			}
		}

	};
}
