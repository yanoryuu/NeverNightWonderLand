// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// サウンドのタイプ
	/// </summary>
	public enum SoundType
	{	
		/// <summary>BGM</summary>
		Bgm,
		/// <summary>SE</summary>
		Se,
		/// <summary>環境音</summary>
		Ambience,
		/// <summary>ボイス</summary>
		Voice,
		/// <summary>タイプの数</summary>
		Max,
	};


	/// <summary>
	/// サウンドファイル設定（ラベルとファイルの対応）
	/// </summary>
	public class AdvSoundSettingData : AdvSettingDictinoayItemBase, IAssetFileSoundSettingData, IAssetFileSoundSettingDataKey
	{
		/// <summary>
		/// サウンドのタイプ
		/// </summary>
		public SoundType Type { get; private set; }

		/// <summary>
		/// 表示タイトル
		/// </summary>
		public string Title { get; private set; }

        // タイトル(ローカライズ対応済み)
        public string LocalizedTitle => AdvParser.ParseCellLocalizedText(this.RowData, AdvColumnName.Title);
        
		/// <summary>
		/// ファイル名
		/// </summary>
		string fileName;

		/// <summary>
		/// ファイル名
		/// </summary>
		public string FilePath { get; private set; }

		/// <summary>
		/// イントロループ用のループポイント
		/// </summary>
		public float IntroTime { get; private set; }

		/// <summary>
		/// ボリューム
		/// </summary>
		public float Volume { get; private set; }

		/// <summary>
		/// バージョン
		/// </summary>
//		public int Version { get; private set; }


		/// <summary>
		/// StringGridの一行からデータ初期化
		/// </summary>
		/// <param name="row">初期化するためのデータ</param>
		/// <returns>成否</returns>
		public override bool InitFromStringGridRow(StringGridRow row)
		{
			if (row.IsEmptyOrCommantOut) return false;

			this.RowData = row;
			string key = AdvParser.ParseCell<string>(row, AdvColumnName.Label);
			if (string.IsNullOrEmpty(key))
			{
				return false;
			}
			else
			{
				InitKey(key);
				this.Type = AdvParser.ParseCell<SoundType>(row, AdvColumnName.Type);
				this.fileName = AdvParser.ParseCell<string>(row, AdvColumnName.FileName);
//				this.isStreaming = AdvParser.ParseCellOptional<bool>(row, AdvColumnName.Streaming, false);
//				this.Version = AdvParser.ParseCellOptional<int>(row, AdvColumnName.Version, 0);
				this.Title = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Title, "");
				this.IntroTime = AdvParser.ParseCellOptional<float>(row, AdvColumnName.IntroTime, 0);
				this.Volume = AdvParser.ParseCellOptional<float>(row, AdvColumnName.Volume, 1);
				return true;
			}
		}

		public void BootInit(AdvSettingDataManager dataManager)
		{
			this.FilePath = FileNameToPath(fileName, dataManager.BootSetting);
			AssetFileManager.GetFileCreateIfMissing(this.FilePath, this);
/*			if (file != null)
			{
				file.Version = this.Version;
				//ロードタイプをストリーミングにする場合、
				if (this.IsStreaming)
				{
					file.AddLoadFlag(AssetFileLoadFlags.Streaming);
				}
			}*/
		}

		string FileNameToPath(string fileName, AdvBootSetting settingData)
		{
			switch (Type)
			{
				case SoundType.Se:
					return settingData.SeDirInfo.FileNameToPath(fileName);
				case SoundType.Ambience:
					return settingData.AmbienceDirInfo.FileNameToPath(fileName);
				case SoundType.Bgm:
				default:
					return settingData.BgmDirInfo.FileNameToPath(fileName);
			}
		}
	}


	/// <summary>
	/// サウンドの設定
	/// </summary>
	public class AdvSoundSetting : AdvSettingDataDictinoayBase<AdvSoundSettingData>
	{
		/// <summary>
		/// 起動時の初期化
		/// </summary>
		public override void BootInit(AdvSettingDataManager dataManager)
		{
			foreach (AdvSoundSettingData data in List)
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
			foreach (AdvSoundSettingData data in List)
			{
				AssetFileManager.Download(data.FilePath);
			}
		}


		/// <summary>
		/// ラベルが登録されているか
		/// </summary>
		/// <param name="label">ラベル</param>
		/// <param name="type">サウンドのタイプ</param>
		/// <returns>ファイルパス</returns>
		public bool Contains(string label, SoundType type)
		{
/*			//既に絶対URLならそのまま
			if (FilePathUtil.IsAbsoluteUri(label))
			{
				return true;
			}
			else*/
			{
				AdvSoundSettingData data = FindData(label);
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

		/// <summary>
		/// ラベルからファイルパスを取得
		/// </summary>
		/// <param name="label">ラベル</param>
		/// <param name="type">サウンドのタイプ</param>
		/// <returns>ファイルパス</returns>
		public string LabelToFilePath(string label, SoundType type)
		{
			//既に絶対URLならそのまま
/*			if (FilePathUtil.IsAbsoluteUri(label))
			{
				//プラットフォームが対応する拡張子にする
				return ExtensionUtil.ChangeSoundExt(label);
			}
			else
*/			{
				AdvSoundSettingData data = FindData(label);
				if (data == null)
				{
					//ラベルをそのままファイル名扱いに
					return label;
				}
				else
				{
					return data.FilePath;
				}
			}
		}

		//ラベルからデータを取得
		public AdvSoundSettingData FindData(string label)
		{
			AdvSoundSettingData data;
			if (!Dictionary.TryGetValue(label, out data))
			{
				return null;
			}
			else
			{
				return data;
			}
		}

		//元となるデータを取得（拡張性のために）
		public StringGridRow FindRowData(string label)
		{
			AdvSoundSettingData data = FindData(label);
			if (data == null)
			{
				return null;
			}
			else
			{
				return data.RowData;
			}
		}


		/// <summary>
		/// サウンドルームに表示するデータのリスト
		/// </summary>
		/// <returns></returns>
		public List<AdvSoundSettingData> GetSoundRoomList()
		{
			List<AdvSoundSettingData> list = new List<AdvSoundSettingData>();
			foreach (AdvSoundSettingData item in List)
			{
				if (!string.IsNullOrEmpty(item.Title))
				{
					list.Add(item);
				}
			}
			return list;
		}
	}
}
