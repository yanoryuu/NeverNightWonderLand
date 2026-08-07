// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.IO;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// 管理中のファイル情報
	/// これはシステム内部で使うので外から使うことは想定していない
	/// </summary>
	public class AssetFileInfo
	{
		//ファイルパス
		public string FileName { get; private set; }
		//アセットバンドル用のハッシュ値
		public AssetBundleInfo AssetBundleInfo { get; set; }
		//ファイルタイプ
		public AssetFileType FileType { get { return this.Setting.FileType; } }
		//ファイル設定
		public AssetFileSetting Setting { get; private set; }
		//ファイルのおき場所のタイプ
		public AssetFileStrageType StrageType { get; set; }
		
		public AssetFileInfo(string path, AssetFileManagerSettings settings, AssetBundleInfo assetBundleInfo)
		{
			this.FileName = path;
			this.Setting = settings.FindSettingFromPath(path);
			this.AssetBundleInfo = assetBundleInfo;
			this.StrageType = ParseStrageType();
		}

		//ストレージタイプを解析
		AssetFileStrageType ParseStrageType()
		{
			if (Setting.IsStreamingAssets)
			{
				//StreamingAssets
				return AssetFileStrageType.StreamingAssets;
			}
			else
			{
				if (FilePathUtil.IsAbsoluteUri(FileName))
				{
					//URLならサーバーから
					return AssetFileStrageType.Server;
				}
				else
				{
					if(Setting.LoadType == AssetFileManagerSettings.LoadType.Server)
					{
						return AssetFileStrageType.Server;
					}
					else
					{
						return AssetFileStrageType.Resources;
					}
				}
			}
		}
	}
}