// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// ゲームの起動設定データ
	/// </summary>
	[System.Serializable]
	public partial class AdvBootSetting
	{
		[System.Serializable]
		public class DefaultDirInfo
		{
			public string defaultDir;		//デフォルトのディレクトリ
			public string defaultExt;       //デフォルトの拡張子
			public bool legacyAutoChangeSoundExt;       //昔の形式で、サウンド拡張子を自動変更する。ほぼ問題ないはずだが、念のために切り替えを残しておく

			public string FileNameToPath(string fileName)
			{
				return FileNameToPath(fileName, "");
			}
		
			public string FileNameToPath(string fileName, string LocalizeDir)
			{
				if (string.IsNullOrEmpty(fileName)) return fileName;

				string path;
/*				//既に絶対URLならそのまま
				if (FilePathUtil.IsAbsoluteUri(fileName))
				{
					path = fileName;
				}
				else*/
				{
					try
					{
						//拡張子がなければデフォルト拡張子を追加
						if (string.IsNullOrEmpty(FilePathUtil.GetExtension(fileName)))
						{
							fileName += defaultExt;
						}
						path = defaultDir + LocalizeDir + "/" + fileName;
					}
					catch (System.Exception e)
					{
						Debug.LogError(fileName + "  " + e.ToString());
						path = defaultDir + LocalizeDir + "/" + fileName;
					}
				}
				if (legacyAutoChangeSoundExt)
				{
					//プラットフォームが対応する拡張子にする(mp3とoggを入れ替え)
					//ファイルを直接使う場合は必要だった処理だが、デバッグログでも拡張子が変わって混乱ケースがあるので廃止
					return ExtensionUtil.ChangeSoundExt(path);
				}
				else
				{
					return path;
				}
			}
		};

		public string ResourceDir { get; set; }		//リソースのルートディレクトリ

		/// <summary>
		/// キャラクターテクスチャのパス情報
		/// </summary>
		public DefaultDirInfo CharacterDirInfo { get { return characterDirInfo; } }
		DefaultDirInfo characterDirInfo;

		/// <summary>
		/// 背景テクスチャのパス情報
		/// </summary>
		public DefaultDirInfo BgDirInfo { get { return bgDirInfo; } }
		DefaultDirInfo bgDirInfo;

		/// <summary>
		/// イベントCGテクスチャのパス情報
		/// </summary>
		public DefaultDirInfo EventDirInfo { get { return eventDirInfo; } }
		DefaultDirInfo eventDirInfo;

		/// <summary>
		/// スプライトテクスチャのパス情報
		/// </summary>
		public DefaultDirInfo SpriteDirInfo { get { return spriteDirInfo; } }
		DefaultDirInfo spriteDirInfo;

		/// <summary>
		/// サムネイルテクスチャのパス情報
		/// </summary>
		public DefaultDirInfo ThumbnailDirInfo { get { return thumbnailDirInfo; } }
		DefaultDirInfo thumbnailDirInfo;

		/// <summary>
		/// BGMのパス情報
		/// </summary>
		public DefaultDirInfo BgmDirInfo { get { return bgmDirInfo; } }
		DefaultDirInfo bgmDirInfo;

		/// <summary>
		/// SEのパス情報
		/// </summary>
		public DefaultDirInfo SeDirInfo { get { return seDirInfo; } }
		DefaultDirInfo seDirInfo;

		/// <summary>
		/// 環境音のパス情報
		/// </summary>
		public DefaultDirInfo AmbienceDirInfo { get { return ambienceDirInfo; } }
		DefaultDirInfo ambienceDirInfo;

		/// <summary>
		/// ボイスのパス情報
		/// </summary>
		public DefaultDirInfo VoiceDirInfo { get { return voiceDirInfo; } }
		DefaultDirInfo voiceDirInfo;

		/// <summary>
		/// パーティクルのパス情報
		/// </summary>
		public DefaultDirInfo ParticleDirInfo { get { return particleDirInfo; } }
		DefaultDirInfo particleDirInfo;

		/// <summary>
		/// パーティクルのパス情報
		/// </summary>
		public DefaultDirInfo VideoDirInfo { get { return videoDirInfo; } }
		DefaultDirInfo videoDirInfo;

		/// <summary>
		/// 起動時の初期化
		/// </summary>
		/// <param name="resourceDir">リソースディレクトリ</param>
		public void BootInit( string resourceDir, AdvDataManager dataManager = null)
		{
			this.ResourceDir = resourceDir;
			bool autoChangeSoundExt = false;
			if (dataManager != null)
			{
				autoChangeSoundExt = dataManager.LegacyAutoChangeSoundExt;
			}
			characterDirInfo = new DefaultDirInfo { defaultDir = @"Texture/Character", defaultExt = ".png" };
			bgDirInfo = new DefaultDirInfo { defaultDir = @"Texture/BG", defaultExt = ".jpg" };
			eventDirInfo = new DefaultDirInfo { defaultDir = @"Texture/Event", defaultExt = ".jpg" };
			spriteDirInfo = new DefaultDirInfo { defaultDir = @"Texture/Sprite", defaultExt = ".png" };
			thumbnailDirInfo = new DefaultDirInfo { defaultDir = @"Texture/Thumbnail", defaultExt = ".jpg" };
			bgmDirInfo = new DefaultDirInfo { defaultDir = @"Sound/BGM", defaultExt = ".wav", legacyAutoChangeSoundExt = autoChangeSoundExt };
			seDirInfo = new DefaultDirInfo { defaultDir = @"Sound/SE", defaultExt = ".wav", legacyAutoChangeSoundExt = autoChangeSoundExt };
			ambienceDirInfo = new DefaultDirInfo { defaultDir = @"Sound/Ambience", defaultExt = ".wav", legacyAutoChangeSoundExt = autoChangeSoundExt };
			voiceDirInfo = new DefaultDirInfo { defaultDir = @"Sound/Voice", defaultExt = ".wav", legacyAutoChangeSoundExt = autoChangeSoundExt };
			particleDirInfo = new DefaultDirInfo { defaultDir = @"Particle", defaultExt = ".prefab" };
			videoDirInfo = new DefaultDirInfo { defaultDir = @"Video", defaultExt = ".mp4" };

			InitDefaultDirInfo(ResourceDir, characterDirInfo);
			InitDefaultDirInfo(ResourceDir, bgDirInfo);
			InitDefaultDirInfo(ResourceDir, eventDirInfo);
			InitDefaultDirInfo(ResourceDir, spriteDirInfo);
			InitDefaultDirInfo(ResourceDir, thumbnailDirInfo);
			InitDefaultDirInfo(ResourceDir, bgmDirInfo);
			InitDefaultDirInfo(ResourceDir, seDirInfo);
			InitDefaultDirInfo(ResourceDir, ambienceDirInfo);
			InitDefaultDirInfo(ResourceDir, voiceDirInfo);
			InitDefaultDirInfo(ResourceDir, particleDirInfo);
			InitDefaultDirInfo(ResourceDir, videoDirInfo);
		}
		void InitDefaultDirInfo(string root, DefaultDirInfo info)
		{
			info.defaultDir = FilePathUtil.Combine( root,info.defaultDir );
		}

		public string GetLocalizeVoiceFilePath( string file )
		{
			if (LanguageManagerBase.Instance.IgnoreLocalizeVoice)
			{
				return VoiceDirInfo.FileNameToPath(file);
			}
			else
			{
				string language = LanguageManagerBase.Instance.CurrentVoiceLanguage;
				if (LanguageManagerBase.Instance.VoiceLanguages.Contains(language))
				{
					return VoiceDirInfo.FileNameToPath(file, language);
				}
				else
				{
					return VoiceDirInfo.FileNameToPath(file);
				}
			}
		}

		public List<string> GetAllLocalizeVoiceFilePathList(string file)
		{
			List<string> list = new List<string>();
			list.Add(VoiceDirInfo.FileNameToPath(file));
			if (LanguageManagerBase.Instance && !LanguageManagerBase.Instance.IgnoreLocalizeVoice)
			{
				foreach (var language in LanguageManagerBase.Instance.VoiceLanguages )
				{
					list.Add( VoiceDirInfo.FileNameToPath(file, language) );
				}
			}
			return list;
		}
	}
}