// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// シーン回想のデータ
	/// </summary>
	public class AdvSceneGallerySettingData : AdvSettingDictinoayItemBase
	{
		/// <summary>
		/// シナリオラベル
		/// </summary>
		public string ScenarioLabel { get { return this.Key; } }
		
		/// <summary>
		/// タイトル
		/// </summary>
		public string Title { get { return this.title; } }
		string title;

		/// <summary>
		/// タイトル(ローカライズ対応済み)
		/// </summary>
		public string LocalizedTitle { get { return AdvParser.ParseCellLocalizedText( this.RowData, AdvColumnName.Title); } }

		/// <summary>
		/// カテゴリ名
		/// </summary>
		public string Category { get { return this.category; } }
		string category;

		/// <summary>
		/// サムネイル用ファイル名
		/// </summary>
		string thumbnailName;

		/// <summary>
		/// サムネイル用ファイルパス
		/// </summary>
		public string ThumbnailPath { get { return this.thumbnailPath; } }
		string thumbnailPath;

		/// <summary>
		/// サムネイル用ファイルのバージョン
		/// </summary>
		public int ThumbnailVersion { get { return this.thumbnailVersion; } }
		int thumbnailVersion;

		/// <summary>
		/// StringGridの一行からデータ初期化
		/// </summary>
		/// <param name="row">初期化するためのデータ</param>
		/// <returns>成否</returns>
		public override bool InitFromStringGridRow(StringGridRow row)
		{
			string key = AdvCommandParser.ParseScenarioLabel(row, AdvColumnName.ScenarioLabel);
			InitKey(key);
			this.title = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Title,"");
			this.thumbnailName = AdvParser.ParseCell<string>(row, AdvColumnName.Thumbnail);
			this.thumbnailVersion = AdvParser.ParseCellOptional<int>(row, AdvColumnName.ThumbnailVersion, 0);
			this.category = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Categolly, "");
			this.RowData = row;

			return true;
		}

		public void BootInit(AdvSettingDataManager dataManager)
		{
			this.thumbnailPath = dataManager.BootSetting.ThumbnailDirInfo.FileNameToPath(thumbnailName);
		}
	}

	/// <summary>
	/// シーン回想のデータ
	/// </summary>
	public class AdvSceneGallerySetting : AdvSettingDataDictinoayBase<AdvSceneGallerySettingData>
	{
		/// <summary>
		/// 起動時の初期化
		/// </summary>
		public override void BootInit( AdvSettingDataManager dataManager )
		{
			foreach (AdvSceneGallerySettingData data in List)
			{
				data.BootInit(dataManager);
			}
		}

		/// <summary>
		/// 全てのリソースをダウンロード
		/// </summary>
		public override void DownloadAll()
		{
			//ファイルマネージャーにバージョンの登録
			foreach (AdvSceneGallerySettingData data in List)
			{
				AssetFileManager.Download(data.ThumbnailPath);
			}
		}

		/// <summary>
		/// ギャラリー用のデータを取得
		/// </summary>
		/// <param name="category">カテゴリ</param>
		public List<AdvSceneGallerySettingData> CreateGalleryDataList(string category)
		{
			List<AdvSceneGallerySettingData> list = new List<AdvSceneGallerySettingData>();
			foreach (var item in List)
			{
				if (item.Category == category)
				{
					list.Add(item);
				}
			}
			return list;
		}

		/// <summary>
		/// カテゴリのリストを取得
		/// </summary>
		public List<string> CreateCategoryList()
		{
			List<string> list = new List<string>();
			foreach (var item in List)
			{
				if (string.IsNullOrEmpty(item.ThumbnailPath)) continue;
				if (!list.Contains(item.Category))
				{
					list.Add(item.Category);
				}
			}
			return list;
		}

		public bool Contains(string key)
		{
			return Dictionary.ContainsKey(key);
		}
	}
}