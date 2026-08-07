//----------------------------------------------
// UTAGE: Unity Text Adventure Game Engine
// Copyright 2014 Ryohei Tokimura
//----------------------------------------------

using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// ビデオのロードパスを変更
	/// </summary>
	[AddComponentMenu("Utage/ADV/Extra/AdvVideoLoadPathChanger")]
	public class AdvVideoLoadPathChanger : MonoBehaviour
	{
		//Videoのルートパス
		public string RootPath => rootPath;
		[SerializeField] string rootPath = "";


		//ファイルのロードを上書きするコールバックを登録
		protected virtual void Awake()
		{
			AssetFileManager.GetCustomLoadManager().OnFindAsset += FindAsset;
		}

		//ファイルのロードを上書き
		protected virtual void FindAsset(AssetFileManager mangager, AssetFileInfo fileInfo, IAssetFileSettingData settingData, ref AssetFileBase asset)
		{
			if (IsVideoType(fileInfo, settingData))
			{
				//宴形式の通常ファイルロード
				asset = new AdvLocalVideoFile(this, mangager, fileInfo, settingData);
			}
		}

		protected virtual bool IsVideoType(AssetFileInfo fileInfo, IAssetFileSettingData settingData)
		{
			if (fileInfo.FileType != AssetFileType.UnityObject) return false;
			if (settingData is AdvCommandSetting setting)
			{
				//Videoコマンドか？
				return setting.Command is AdvCommandVideo;
			}
			else
			{
				//Videoオブジェクトか？
				AdvGraphicInfo info = settingData as AdvGraphicInfo;
				return (info != null && info.FileType == AdvGraphicInfo.FileTypeVideo);
			}
		}
	}

	//ビデオのみ強制的にローカルからロードする処理
	internal class AdvLocalVideoFile : AssetFileUtage
	{
		public AdvLocalVideoFile(AdvVideoLoadPathChanger pathChanger, AssetFileManager assetFileManager, AssetFileInfo fileInfo, IAssetFileSettingData settingData)
			: base(assetFileManager, fileInfo, settingData)
		{
			fileInfo.StrageType = AssetFileStrageType.Resources;
			if (settingData is AdvCommandSetting setting)
			{
				//Videoコマンド用
				string fileName = setting.Command.ParseCell<string>(AdvColumnName.Arg1);
				this.LoadPath = FilePathUtil.Combine(pathChanger.RootPath, fileName);
			}
			else if( settingData is AdvGraphicInfo info)
			{
				//Videoオブジェクト用
				string fileName = info.FileName;
				this.LoadPath = FilePathUtil.Combine(pathChanger.RootPath, fileName);
			}
			else
			{
				Debug.LogError("AdvLocalVideoFile: Invalid settingData type. Expected AdvCommandSetting or AdvGraphicInfo, but got " + settingData.GetType());
			}
		}
	}
}
