// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
	/// <summary>
	/// ファイル管理
	/// </summary>
	[System.Serializable]
	public class AssetFileManagerSettings
	{
		//ファイルのロードタイプの設定
		public enum LoadType
		{
			Local,					//ローカル（Resources)から読み込む
			Server,                 //サーバーから読み込む。全てアセットバンドル
			StreamingAssets,        //StreamingAssetsから読み込む。全てアセットバンドル
			Advanced,				//ファイルの種類ごとに設定を細かく決める
		};

		[SerializeField]
		LoadType loadType;
		public LoadType LoadTypeSetting
		{
			get { return loadType; }
			private set	{loadType = value;}
		}

		[SerializeField]
		List<AssetFileSetting> fileSettings = new List<AssetFileSetting>()
		{
				new AssetFileSetting(AssetFileType.Text, new string[] { ".txt", ".json", ".html", ".htm", ".xml", ".yaml", ".fnt", ".bin", ".bytes", ".csv", ".tsv" }),
				new AssetFileSetting(AssetFileType.Texture, new string[] { ".png", ".jpg", ".bmp", ".psd",".tif", ".tga", ".gif", ".iff", ".pict" }),
				new AssetFileSetting(AssetFileType.Sound, new string[] { ".mp3", ".ogg", ".wav", ".aif", ".aiff", ".xm", ".mod", ".it", ".s3m" }),
				new AssetFileSetting(AssetFileType.UnityObject, new string[] { "" }),
		};
		public List<AssetFileSetting> FileSettings
		{
			get
			{
				RebuildFileSettings();
				return rebuildFileSettings;
			}
		}

		//互換性を保つために
		void RebuildFileSettings()
		{
			if (rebuildFileSettings != null) return;
			if (fileSettings.Count != System.Enum.GetValues(typeof(AssetFileType)).Length)
			{
				rebuildFileSettings = fileSettings = DefaultFileSettings();
			}
			else
			{
				rebuildFileSettings = fileSettings;
			}
			foreach (AssetFileSetting setting in rebuildFileSettings)
			{
				setting.InitLink(this);
			}
		}
		[NonSerialized]
		List<AssetFileSetting> rebuildFileSettings = null;

		List<AssetFileSetting> DefaultFileSettings()
		{
			return new List<AssetFileSetting>()
			{
				new AssetFileSetting(AssetFileType.Text, new string[] { ".txt", ".json", ".html", ".htm", ".xml", ".yaml", ".fnt", ".bin", ".bytes", ".csv", ".tsv" }),
				new AssetFileSetting(AssetFileType.Texture, new string[] { ".png", ".jpg", ".bmp", ".psd",".tif", ".tga", ".gif", ".iff", ".pict" }),
				new AssetFileSetting(AssetFileType.Sound, new string[] { ".mp3", ".ogg", ".wav", ".aif", ".aiff", ".xm", ".mod", ".it", ".s3m" }),
				new AssetFileSetting(AssetFileType.UnityObject, new string[] { "" }),
			};
		}


		public void BootInit( LoadType loadType )
		{
			this.loadType = loadType;
			foreach (AssetFileSetting setting in FileSettings)
			{
				setting.InitLink(this);
			}
		}

		//拡張子を追加
		public void AddExtensions(AssetFileType type, string[] extensions)
		{
			Find(type).AddExtensions(extensions);
		}

		//ファイルタイプから設定データを取得
		public AssetFileSetting Find(AssetFileType type)
		{
			return FileSettings.Find(x => (x.FileType == type));
		}

		//ファイルパスから設定データを取得
		public AssetFileSetting FindSettingFromPath(string path)
		{
			AssetFileSetting setting = fileSettings.Find(x => (x.ContainsExtensions(path)));
			if (setting == null)
			{
				setting = Find(AssetFileType.UnityObject);
			}
			return setting;
		}		
	}

	/// <summary>
	/// ファイル管理
	/// </summary>
	[System.Serializable]
	public class AssetFileSetting
	{
		public AssetFileSetting(AssetFileType fileType, string[] extensions)
		{
			this.fileType = fileType;
			this.extensions = new List<string>(extensions);
		}

		//ファイルタイプ
		public AssetFileType FileType
		{
			get { return fileType; }
		}
		[SerializeField,HideInInspector]
		AssetFileType fileType;

		//StreamingAssetsから読み込むか
		[SerializeField]
		bool isStreamingAssets = false;
		public bool IsStreamingAssets
		{
			get
			{
				switch (LoadType)
				{
					case AssetFileManagerSettings.LoadType.Local:
					case AssetFileManagerSettings.LoadType.Server:
						return false;
					case AssetFileManagerSettings.LoadType.StreamingAssets:
						return true;
					case AssetFileManagerSettings.LoadType.Advanced:
					default:
						return isStreamingAssets;
				}
			}
			set
			{
				isStreamingAssets = value;
			}
		}

		//対象となる拡張子
		[SerializeField]
		List<string> extensions;
		public void AddExtensions(string[] extensions)
		{
			this.extensions.AddRange(extensions);
		}

		//拡張子を比較
		internal bool ContainsExtensions(string path)
		{
			//Utage用の二重拡張子を無視した拡張子を取得
			string ext = FilePathUtil.GetExtensionWithOutDouble(path, ExtensionUtil.UtageFile).ToLower();
			return this.extensions.Contains(ext);
		}

		[NonSerialized]
		AssetFileManagerSettings settings;
		AssetFileManagerSettings Settings { get { return settings; } }
		public void InitLink(AssetFileManagerSettings settings)
		{
			this.settings = settings;
		}

		public AssetFileManagerSettings.LoadType LoadType { get { return Settings.LoadTypeSetting; } }
	}
}
