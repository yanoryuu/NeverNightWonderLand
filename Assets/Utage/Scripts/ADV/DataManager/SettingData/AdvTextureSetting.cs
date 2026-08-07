// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// テクスチャ設定（ラベルとファイルの対応）
	/// </summary>
	public class AdvTextureSettingData : AdvSettingDictinoayItemBase
	{
		//独自にカスタムしたいファイルタイプの、ルートディレクトリを指定
		public delegate void ParseCustomFileTypeRootDir(string fileType, ref string rootDir);
		[field: RuntimeInitializeStaticField] public static ParseCustomFileTypeRootDir CallbackParseCustomFileTypeRootDir;

		/// <summary>
		/// テクスチャのタイプ
		/// </summary>
		public enum Type
		{
			/// <summary>背景</summary>
			Bg,
			/// <summary>イベントCG</summary>
			Event,
			/// <summary>スプライト</summary>
			Sprite,
		}

		/// <summary>テクスチャのタイプ</summary>
		public Type TextureType { get; private set; }

		//グラフィックの情報
		public AdvGraphicInfoList Graphic { get; private set; }
		
		/// <summary>
		/// サムネイル用ファイル名
		/// </summary>
		string thumbnailName;

		/// <summary>
		/// サムネイル用ファイルパス
		/// </summary>
		public string ThumbnailPath { get; private set; }

		/// <summary>
		/// サムネイル用ファイルのバージョン
		/// </summary>
		public int ThumbnailVersion { get; private set; }

		/// <summary>
		/// CGギャラリーでのカテゴリ
		/// </summary>
		public string CgCategory { get; private set; }

		/// <summary>
		/// StringGridの一行からデータ初期化
		/// </summary>
		/// <param name="row">初期化するためのデータ</param>
		/// <returns>成否</returns>
		public override bool InitFromStringGridRow(StringGridRow row)
		{
			this.RowData = row;
			string key = AdvParser.ParseCell<string>(row, AdvColumnName.Label);
			InitKey(key);
			this.TextureType = AdvParser.ParseCell<Type>(row, AdvColumnName.Type);
			this.Graphic = new AdvGraphicInfoList(key);
			this.thumbnailName = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Thumbnail, "");
			this.ThumbnailVersion = AdvParser.ParseCellOptional<int>(row, AdvColumnName.ThumbnailVersion, 0);
			this.CgCategory = AdvParser.ParseCellOptional<string>(row, AdvColumnName.CgCategolly, "");
			this.AddGraphicInfo(row);
			return true;
		}

		/// <summary>
		/// 起動時の初期化
		/// </summary>
		/// <param name="settingData">設定データ</param>
		internal void BootInit(AdvSettingDataManager dataManager)
		{
			Graphic.BootInit((fileName, fileType) => FileNameToPath(fileName, fileType, dataManager.BootSetting), dataManager);

			ThumbnailPath = dataManager.BootSetting.ThumbnailDirInfo.FileNameToPath(thumbnailName);
			if (!string.IsNullOrEmpty(ThumbnailPath))
			{
				AssetFileManager.GetFileCreateIfMissing(ThumbnailPath);
			}
		}

		string FileNameToPath(string fileName, string fileType, AdvBootSetting settingData)
		{
			string root = null;
			if (CallbackParseCustomFileTypeRootDir != null)
			{
				CallbackParseCustomFileTypeRootDir(fileType, ref root);
				if (root != null)
				{
					return FilePathUtil.Combine(settingData.ResourceDir, root, fileName);
				}
			}

			switch (TextureType)
			{
				case AdvTextureSettingData.Type.Event:
					return settingData.EventDirInfo.FileNameToPath(fileName);
				case AdvTextureSettingData.Type.Sprite:
					return settingData.SpriteDirInfo.FileNameToPath(fileName);
				case AdvTextureSettingData.Type.Bg:
				default:
					return settingData.BgDirInfo.FileNameToPath(fileName);
			}
		}

		internal void AddGraphicInfo(StringGridRow row)
		{
			Graphic.Add(AdvGraphicInfo.TypeTexture, row, this);
		}
	}

	/// <summary>
	/// テクスチャ設定
	/// </summary>
	public class AdvTextureSetting : AdvSettingDataDictinoayBase<AdvTextureSettingData>
	{
		//連続するデータとして追加できる場合はする。
		protected override bool TryParseContinues(AdvTextureSettingData last, StringGridRow row)
		{
			if (last == null) return false;

			string key = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Label,"");
			if (!string.IsNullOrEmpty(key)) return false;

			last.AddGraphicInfo(row);
			return true;
		}


		/// <summary>
		/// 起動時の初期化
		/// </summary>
		public override void BootInit(AdvSettingDataManager dataManager)
		{
			foreach (AdvTextureSettingData data in List)
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
			foreach (AdvTextureSettingData data in List)
			{
				data.Graphic.DownloadAll();
				if (!string.IsNullOrEmpty(data.ThumbnailPath))
				{
					AssetFileManager.Download(data.ThumbnailPath);
				}
			}
		}

		/// <summary>
		/// ラベルからグラフィック情報を取得
		/// </summary>
		/// <param name="label">ラベル</param>
		/// <returns>グラフィック情報</returns>
		public AdvGraphicInfoList LabelToGraphic(string label)
		{
			AdvTextureSettingData data = FindData(label);
			if (data == null)
			{
				Debug.LogError("Not contains " + label + " in Texture sheet");
				return null;
			}
			return data.Graphic;
		}

		/// <summary>
		/// ラベルからファイルパスを取得
		/// </summary>
		/// <param name="label">ラベル</param>
		/// <returns>ファイルパス</returns>
		public bool ContainsLabel(string label)
		{
/*			//既に絶対URLならそのまま
			if (FilePathUtil.IsAbsoluteUri(label))
			{
				return true;
			}
			else*/
			{
				AdvTextureSettingData data = FindData(label);
				if (data == null)
				{
					return false;
				}
				else
				{
					return true;
				}
			}
		}

		//ラベルからファイル名を取得
		AdvTextureSettingData FindData(string label)
		{
			AdvTextureSettingData data;
			if (!Dictionary.TryGetValue(label, out data))
			{
				return null;
			}
			else
			{
				return data;
			}
		}

		/// <summary>
		/// CGギャラリー用のデータを取得
		/// </summary>
		/// <param name="saveData">セーブデータ</param>
		/// <param name="gallery">ギャリーのデータ</param>
		public List<AdvCgGalleryData> CreateCgGalleryList( AdvGallerySaveData saveData )
		{
			return CreateCgGalleryList(saveData, "");
		}

		/// <summary>
		/// CGギャラリー用のデータを取得
		/// </summary>
		/// <param name="saveData">セーブデータ</param>
		/// <param name="category">セーブデータ</param>
		/// <param name="gallery">ギャリーのデータ</param>
		public List<AdvCgGalleryData> CreateCgGalleryList(AdvGallerySaveData saveData, string category)
		{
			List<AdvCgGalleryData> list = new List<AdvCgGalleryData>();
			AdvCgGalleryData currentData = null;
			foreach (var item in List)
			{
				if (item.TextureType == AdvTextureSettingData.Type.Event)
				{
					if (string.IsNullOrEmpty(item.ThumbnailPath)) continue;
					if (!string.IsNullOrEmpty(category) && item.CgCategory != category) continue;

					string path = item.ThumbnailPath;
					if (currentData == null)
					{
						currentData = new AdvCgGalleryData(path, saveData);
						list.Add(currentData);
					}
					else
					{
						if (path != currentData.ThumbnailPath)
						{
							currentData = new AdvCgGalleryData(path, saveData);
							list.Add(currentData);
						}
					}
					currentData.AddTextureData(item);
				}
			}
			return list;
		}

		/// <summary>
		/// CGギャラリー用のカテゴリのリストを取得
		/// </summary>
		public List<string> CreateCgGalleryCategoryList()
		{
			List<string> list = new List<string>();
			foreach (var item in List)
			{
				if (item.TextureType == AdvTextureSettingData.Type.Event)
				{
					if (string.IsNullOrEmpty(item.ThumbnailPath)) continue;
					if (string.IsNullOrEmpty(item.CgCategory)) continue;
					if (!list.Contains(item.CgCategory))
					{
						list.Add(item.CgCategory);
					}
				}
			}
			return list;
		}
	}
}
