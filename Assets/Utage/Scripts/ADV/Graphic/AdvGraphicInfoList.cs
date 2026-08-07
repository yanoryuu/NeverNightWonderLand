// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// グラフィック情報のリスト(条件によって表示するものを変える場合に応じて複数持つ)
	/// </summary>
	public class AdvGraphicInfoList
	{
		public string Key { get; protected set; }

		public List<AdvGraphicInfo> InfoList { get { return infoList; } }
		List<AdvGraphicInfo> infoList = new List<AdvGraphicInfo>();

		public AdvGraphicInfo Main {
			get
			{
				//存在しないならnull
				if (InfoList.Count == 0 ) return null;

				//1つしかないならそれを
				if (InfoList.Count ==1 ) return InfoList[0];

				//複数持っている場合を考慮して条件判定を行う
				AdvGraphicInfo main = null;
				foreach (AdvGraphicInfo graphic in InfoList)
				{
					if (string.IsNullOrEmpty(graphic.ConditionalExpression))
					{
						//条件式未設定なら、最初の一つを候補としてストック
						if (main == null)
						{
							main = graphic;
						}
					}
					else if (graphic.CheckConditionalExpression)
					{
						//条件式を満たすなら、それを
						return graphic;
					}
				}
				return main;
			}		
		}

		public AdvGraphicInfoList( string key )
		{
			this.Key = key;
		}


		internal void Add(string dataType, StringGridRow row, IAdvSettingData settingData)
		{
			infoList.Add( new AdvGraphicInfo( dataType, InfoList.Count, this.Key, row, settingData));
		}

		internal void BootInit(System.Func<string, string, string> FileNameToPath, AdvSettingDataManager dataManager)
		{
			foreach (var item in infoList)
			{
				item.BootInit(FileNameToPath, dataManager);
			}
		}

#if false
		//独自にカスタムしたいファイルタイプの、LoadCompleteを指定
		public delegate void ParseCustomFileTypeLoadComplete(string fileType, ref AssetFileEvent onLoadCmplete);
		public static ParseCustomFileTypeLoadComplete CallbackParseCustomFileTypeLoadComplete;

		//独自にカスタムしたいファイルタイプの、LoadSubfilesを指定
		public delegate void ParseCustomFileTypeLoadSubfiles(string fileType, ref AssetFileEvent onLoadSubfiles);
		public static ParseCustomFileTypeLoadSubfiles CallbackParseCustomFileTypeLoadSubfiles;

		internal void BootInit(System.Func<string, string, string> FileNameToPath, AdvSettingDataManager dataManager)
		{
			foreach (var item in infoList)
			{
				item.BootInit(FileNameToPath, dataManager);
			}

			//特定のファイルタイプなら、ロード終了時の処理をする
			if (CallbackParseCustomFileTypeLoadComplete != null && !AssetFileManager.IsEditorErrorCheck)
			{
				AssetFileEvent onLoadComplete = null;
				CallbackParseCustomFileTypeLoadComplete(this.FileType, ref onLoadComplete);
				if (onLoadComplete != null)
				{
					foreach (AdvGraphicInfo info in InfoList)
					{
						info.File.OnLoadComplete += onLoadComplete;
					}
				}
			}
			//特定のファイルタイプなら、サブファイルロードの処理をする
			if (CallbackParseCustomFileTypeLoadSubfiles != null && !AssetFileManager.IsEditorErrorCheck)
			{
				AssetFileEvent onLoadSubfiles = null;
				CallbackParseCustomFileTypeLoadSubfiles(this.FileType, ref onLoadSubfiles);
				if (onLoadSubfiles != null)
				{
					foreach (AdvGraphicInfo info in InfoList)
					{
						info.File.OnLoadSubFiles += onLoadSubfiles;
					}
				}
			}
		}
#endif

		internal void DownloadAll()
		{
			foreach( var item in infoList )
			{
				AssetFileManager.Download(item.File);
			}
		}

		public bool IsLoadEnd
		{
			get
			{
				foreach (var item in infoList)
				{
					if (!item.File.IsLoadEnd) return false;
				}
				return true;
			}
		}

		public bool IsDefaultFileType
		{
			get
			{
				foreach (var item in infoList)
				{
					if (!string.IsNullOrEmpty(item.FileType)) return false;
				}
				return true;
			}
		}
	}
}
