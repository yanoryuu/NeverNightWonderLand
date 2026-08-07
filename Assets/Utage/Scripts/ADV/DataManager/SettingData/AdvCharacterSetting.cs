// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


namespace Utage
{

	/// <summary>
	/// キャラクタのテクスチャ設定（名前や表情と、テクスチャ名の対応）
	/// </summary>
	public class AdvCharacterSettingData : AdvSettingDictinoayItemBase
	{
		//独自にカスタムしたいファイルタイプの、ルートディレクトリを指定
		public delegate void ParseCustomFileTypeRootDir(string fileType, ref string rootDir);
		[field: RuntimeInitializeStaticField] public static ParseCustomFileTypeRootDir CallbackParseCustomFileTypeRootDir;

		/// <summary>
		/// キャラ名
		/// </summary>
		public string Name { get { return this.name; } }
		string name;

		/// <summary>
		/// 表示パターンターン
		/// </summary>
		public string Pattern { get { return this.pattern; } }
		string pattern;
	
		/// <summary>
		/// 表示名のテキスト
		/// </summary>
		public string NameText { get { return this.nameText; } }
		string nameText;

		//グラフィックの情報
		public AdvGraphicInfoList Graphic { get { return this.graphic; } }
		AdvGraphicInfoList graphic;

		public AdvFaceIconInfo Icon
		{
			get { return Graphic.Main.Icon; }
		}

		/// <summary>
		/// StringGridの一行からデータ初期化
		/// ただし、このクラスに限っては未使用
		/// </summary>
		/// <param name="row">初期化するためのデータ</param>
		/// <returns>成否</returns>
		public override bool InitFromStringGridRow(StringGridRow row)
		{
			Debug.LogError("Not Use");
			return false;
		}

		/// <summary>
		/// 初期化
		/// </summary>
		/// <param name="key">キー(キャラ名を)</param>
		/// <param name="fileName">ファイルネーム</param>
		internal void Init(string name, string pattern, string nameText, StringGridRow row )
		{
			this.name = name;
			this.pattern = pattern;
			this.RowData = row;
			this.InitKey( AdvCharacterSetting.ToDataKey(name, pattern));
			this.nameText = nameText;
			this.graphic = new AdvGraphicInfoList(Key);
			if (!AdvParser.IsEmptyCell(row, AdvColumnName.FileName))
			{
				AddGraphicInfo(row);
			}
		}

		/// <summary>
		/// 起動時の初期化
		/// </summary>
		/// <param name="settingData">設定データ</param>
		internal void BootInit(AdvSettingDataManager dataManager)
		{
			Graphic.BootInit( (fileName, fileType) => FileNameToPath(fileName, fileType, dataManager.BootSetting), dataManager);
		}

		string FileNameToPath(string fileName, string fileType, AdvBootSetting settingData)
		{
			string root = null;
			if (CallbackParseCustomFileTypeRootDir!=null)
			{
				CallbackParseCustomFileTypeRootDir(fileType, ref root);
				if (root != null)
				{
					return FilePathUtil.Combine(settingData.ResourceDir, root, fileName);
				}
			}

			return settingData.CharacterDirInfo.FileNameToPath(fileName);
		}


		internal void AddGraphicInfo(StringGridRow row)
		{
			Graphic.Add( AdvGraphicInfo.TypeCharacter, row, this );
		}
	};

	/// <summary>
	/// キャラクタのテクスチャ設定（名前や表情と、テクスチャ名の対応）
	/// </summary>
	public class AdvCharacterSetting : AdvSettingDataDictinoayBase<AdvCharacterSettingData>
	{
		/// <summary>
		/// 各キャラのデフォルト表情のキーの一覧
		/// </summary>
		DictionaryString defaultKey = new DictionaryString();

		//連続するデータとして追加できる場合はする。
		protected override bool TryParseContinues(AdvCharacterSettingData last, StringGridRow row)
		{
			if (last == null) return false;

			//キャラ名
			string name = AdvParser.ParseCellOptional<string>(row, AdvColumnName.CharacterName,"");
			//表示パターン
			string pattern = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Pattern, "");

			//キャラ名と表示パターンが空白なら、グラフィック情報のみを追加
			if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(pattern))
			{
				last.AddGraphicInfo(row);
				return true;
			}
			else
			{
				return false;
			}
		}

		//連続するデータとして追加できる場合はする。基本はしない
		protected override AdvCharacterSettingData ParseFromStringGridRow(AdvCharacterSettingData last, StringGridRow row)
		{
			//キャラ名
			string name = AdvParser.ParseCellOptional<string>(row, AdvColumnName.CharacterName,"");
			//表示パターン
			string pattern = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Pattern, "");
			//表示名
			string nameText = AdvParser.ParseCellOptional<string>(row, AdvColumnName.NameText, "");

			//キャラ名が空白なら、直前と同じキャラ名を使う
			if(string.IsNullOrEmpty(name) )
			{
				if( last == null )
				{
					Debug.LogError(row.ToErrorString("Not Found Chacter Name"));
					return null;
				}
				name = last.Name;
			}

			//表示名が空で、直前のデータとキャラ名が同じならその名前を使う
			if(string.IsNullOrEmpty(nameText))
			{
				if(last!=null && (name == last.Name) )
				{
					nameText = last.NameText;
				}
				else
				{
					nameText = name;
				}
			}
 
			AdvCharacterSettingData data = new AdvCharacterSettingData();
			data.Init( name, pattern, nameText, row);

			if (!Dictionary.ContainsKey(data.Key))
			{
				AddData(data);
				if (!defaultKey.ContainsKey(name))
				{
					defaultKey.Add(name, data.Key);
				}
				return data;
			}
			else
			{
				string errorMsg = "";
				errorMsg += row.ToErrorString(ColorUtil.ToColorTagErrorMsg(data.Key) + "  is already contains");
				Debug.LogError(errorMsg);
			}
			return null;
		}

		public override void BootInit(AdvSettingDataManager dataManager)
		{
			foreach (AdvCharacterSettingData data in List)
			{
				data.BootInit(dataManager);
			}
		}

		/// <summary>
		/// 全てのリソースをダウンロード
		/// </summary>
		public override void DownloadAll()
		{
			foreach (AdvCharacterSettingData data in List)
			{
				data.Graphic.DownloadAll();
			}
		}


		/// <summary>
		/// 指定のキャラ名の立ち絵があるか
		/// </summary>
		/// <param name="name">キャラ名</param>
		/// <returns>ファイルパス</returns>
		public bool Contains(string name)
		{
			return defaultKey.ContainsKey(name);
		}
		/// <summary>
		/// キーからグラフィック情報を取得
		/// </summary>
		/// <param name="label">ラベル</param>
		/// <returns>スプライト情報</returns>
		public AdvGraphicInfoList KeyToGraphicInfo(string key)
		{
			AdvCharacterSettingData data = FindData(key);
			if (data == null)
			{
				Debug.LogError("Not contains " + key + " in Character sheet");
				return null;
			}
			else
			{
				return data.Graphic;
			}
		}

		internal AdvCharacterSettingData GetCharacterData(string characterLabel, string patternLabel)
		{
			if (string.IsNullOrEmpty(patternLabel))
			{
				//パターン指定なし
				return FindData(defaultKey.Get(characterLabel));
			}
			else
			{
				//パターン指定あり
				AdvCharacterSettingData data = FindData(ToDataKey(characterLabel, patternLabel));
				if (data == null)
				{
					//パターン指定のデータがなかった（モーション名の可能性あり）
					data = FindData(defaultKey.Get(characterLabel));
					if (data != null && data.Graphic.IsDefaultFileType)
					{
						//デフォルトのファイルタイプ（2Dの場合は、モーションとかない）
						return null;
					}
				}
				return data;
			}
		}

		internal AdvCharacterSettingData GetCharacterData(string characterLabel, string patternLabel, out string pattern, out string motion)
		{
			pattern = patternLabel;
			motion = "";
			if (string.IsNullOrEmpty(patternLabel))
			{
				//パターン指定なし
				return FindData(defaultKey.Get(characterLabel));
			}
			else
			{
				//パターン指定あり
				AdvCharacterSettingData data = FindData(ToDataKey(characterLabel, patternLabel));
				if (data == null)
				{
					//パターン指定のデータがなかった（モーション名の可能性あり）
					data = FindData(defaultKey.Get(characterLabel));
					if (data != null && data.Graphic.IsDefaultFileType)
					{
						//デフォルトのファイルタイプ（2Dの場合は、モーションとかない）
						return null;
					}
					pattern = "";
					motion = pattern;
				}
				return data;
			}
		}

		//キーからファイルデータを取得
		AdvCharacterSettingData FindData(string key)
		{
			AdvCharacterSettingData data;
			if (!Dictionary.TryGetValue(key, out data))
			{
				return null;
			}
			else
			{
				return data;
			}
		}

		//キーの変更
		public static string ToDataKey(string name, string label)
		{
			//名前とラベルからキーを
			string key = string.Format(
				"{0},{1}",
				name,
				label
				);
			return key;
		}
	}
}
