// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions; 
using System;

using UnityEngine;

namespace Utage
{

	/// <summary>
	/// 各コマンドの基底クラス
	/// </summary>
	public abstract class AdvCommand
	{	
		/// <summary>
		/// エディタ上のエラーチェックのために起動してるか
		/// </summary>
		public static bool IsEditorErrorCheck
		{
			get { return isEditorErrorCheck; }
			set { isEditorErrorCheck = value; }
		}
		[RuntimeInitializeStaticField] static bool isEditorErrorCheck = false;

		/// <summary>
		/// エディタ上のエラーチェック中に、WaitTypeの古い書式のチェックをするか
		/// </summary>
		[field: RuntimeInitializeStaticField] public static bool IsEditorErrorCheckWaitType{ get; set; }
		
		//コンストラクタ。シナリオシートの行データを受け取る
		protected AdvCommand(StringGridRow row)
		{
			this.RowData = row;
		}

		//データ
		public StringGridRow RowData { get; set; }

		//完全にオリジナルのデータ（エンティティ処理前）
		public AdvEntityData EntityData { get; set; }

		//コマンドID（本当ならコンストラクタに置きたいけど、互換性のために）
		public string Id { get; set; }

		//エンティティが設定されている
		public bool IsEntityType { get { return EntityData != null; } }

		//ロードの必要があるファイルリスト
		public List<AssetFile> LoadFileList { get { return loadFileList; } }
		List<AssetFile> loadFileList = null;

		//このシナリオからリンクするジャンプ先のシナリオラベル
		public virtual string[] GetJumpLabels() { return null; }

		//ロードの必要があるファイルがあるか
		public bool IsExistLoadFile()
		{
			if (null != loadFileList)
			{
				return loadFileList.Count > 0;
			}
			return false;
		}

		//ロードの必要があるファイルを追加
		public AssetFile AddLoadFile(string path, IAssetFileSettingData settingData )
		{
			if (IsEntityType) return null;
			return AddLoadFileSub(AssetFileManager.GetFileCreateIfMissing(path, settingData));
		}

		//ロードの必要があるファイルを追加
		public void AddLoadGraphic(AdvGraphicInfoList graphic)
		{
			foreach (var item in graphic.InfoList)
			{
				AddLoadGraphic(item);
			}
		}

		//ロードの必要があるファイルを追加
		public void AddLoadGraphic(AdvGraphicInfo graphic)
		{
			if (graphic == null) return;
			if (IsEntityType) return;

			AddLoadFileSub(graphic.File);

			//キャラクターの場合はアイコンファイルもロード
			AddLoadIconFile(graphic);
		}

			//キャラクターの場合はアイコンファイルもロード
		void AddLoadIconFile(AdvGraphicInfo graphic)
		{
			if (graphic.SettingData is AdvCharacterSettingData settingData)
			{
				foreach (var info in settingData.Graphic.InfoList)
				{
					var icon = info.Icon;
					if (icon != null && icon.File != null)
					{
						AddLoadFileSub(icon.File);
					}
				}
			}
		}

		//ロードの必要があるファイルを追加
		public void AddLoadFile(AssetFile file)
		{
			if (IsEntityType) return;
			AddLoadFileSub(file);
		}

		//ロードの必要があるファイルを追加
		AssetFile AddLoadFileSub(AssetFile file)
		{
			if (loadFileList == null) loadFileList = new List<AssetFile>();
			if (file == null)
			{
				if (!IsEditorErrorCheck)
				{
					Debug.LogError("file is not found");
				}
			}
			else
			{
				if (!loadFileList.Contains(file))
				{
					loadFileList.Add(file);
				}
			}
			return file;
		}

		//DL処理する
		public void Download(AdvDataManager dataManager)
		{
			if (null != loadFileList)
			{
				foreach (AssetFile file in loadFileList)
				{
					AssetFileManager.Download(file);
				}
			}
		}

		//ロード処理
		public void Load()
		{
			if (null != loadFileList)
			{
				foreach (AssetFile file in loadFileList)
				{
					AssetFileManager.Load(file, this);
				}
			}
		}

		//ロードが終わったか
		public bool IsLoadEnd()
		{
			if (null != loadFileList)
			{
				foreach (AssetFile file in loadFileList)
				{
					if (!file.IsLoadEnd)
					{
						return false;
					}
				}
			}
			return true;
		}

		//このコマンドを現在実行中のスレッド
		public AdvScenarioThread CurrentTread { get; set; }

		//コマンド実行
		public abstract void DoCommand(AdvEngine engine);

		//コマンド実行後に使ったファイル参照をクリア
		public void Unload()
		{
			if (null != loadFileList)
			{
				foreach (AssetFile file in loadFileList)
				{
					file.Unuse(this);
				}
			}
		}


		//コマンド終了待ち
		public virtual bool Wait(AdvEngine engine) { return false; }

		//ページ区切り系のコマンドか
		public virtual bool IsTypePage() { return false; }
		//ページ終端のコマンドか
		public virtual bool IsTypePageEnd() { return false; }

		//IF文タイプのコマンドか
		public virtual bool IsIfCommand { get { return false; } }


		//ページ用のデータからコマンドに必要な情報を初期化
		public virtual void InitFromPageData(AdvScenarioPageData pageData) { }

		// 選択肢終了などの特別なコマンドを自動生成する場合、そのIDを返す
		public virtual string[] GetExtraCommandIdArray( AdvCommand next ) { return null; }


		//言語変更時に呼ばれる処理（ボイスコマンドなどでファイルを入れ替えるために行う）
		public virtual void OnChangeLanguage(AdvEngine engine) { }

		/// <summary>
		/// エラー用の文字列を取得
		/// </summary>
		/// <param name="msg">エラーメッセージ</param>
		/// <returns>エラー用のテキスト</returns>
		public string ToErrorString(string msg)
		{
			return this.RowData.ToErrorString(msg);
		}

		//セルが空かどうか
		public bool IsEmptyCell(AdvColumnName name)
		{
			return IsEmptyCell(name.QuickToString());
		}
		public bool IsEmptyCell(string name)
		{
			return this.RowData.IsEmptyCell(name);
		}

		//指定の名前のセルを、型Tとして解析・取得（データがなかったらエラーメッセージを出す）
		public T ParseCell<T>(AdvColumnName name)
		{
			return ParseCell<T>(name.QuickToString());
		}
		public T ParseCell<T>(string name)
		{
			return this.RowData.ParseCell<T>(name);
		}

		//指定の名前のセルを、型Tとして解析・取得（データがなかったらデフォルト値を返す）
		public T ParseCellOptional<T>(AdvColumnName name, T defaultVal)
		{
			return ParseCellOptional<T>(name.QuickToString(), defaultVal);
		}
		public T ParseCellOptional<T>(string name, T defaultVal)
		{
			return this.RowData.ParseCellOptional<T>(name, defaultVal);
		}

		//指定の名前のセルを、型Tとして解析・取得（データがなかったらfalse）
		public bool TryParseCell<T>(AdvColumnName name, out T val)
		{
			return TryParseCell<T>(name.QuickToString(), out val);
		}
		public bool TryParseCell<T>(string name, out T val)
		{
			return this.RowData.TryParseCell<T>(name, out val);
		}

		//指定の名前のセルを、型Tのカンマ区切り配列として解析・取得（データがなかったらエラーメッセージを出す）
		public T[] ParseCellArray<T>(AdvColumnName name)
		{
			return ParseCellArray<T>(name.QuickToString());
		}
		public T[] ParseCellArray<T>(string name)
		{
			return this.RowData.ParseCellArray<T>(name);
		}

		//指定の名前のセルを、型Tのカンマ区切り配列として解析・取得（データがなかったらデフォルト値を返す）
		public T[] ParseCellOptionalArray<T>(AdvColumnName name, T[] defaultArray)
		{
			return ParseCellOptionalArray<T>(name.QuickToString(), defaultArray);
		}
		public T[] ParseCellOptionalArray<T>(string name, T[] defaultArray)
		{
			return this.RowData.ParseCellOptionalArray<T>(name, defaultArray);
		}

		//指定の名前のセルを、型Tのカンマ区切り配列として解析・取得（データがなかったらfalse）
		public bool TryParseCellArray<T>(AdvColumnName name, out T[] array)
		{
			return TryParseCellArray<T>(name.QuickToString(), out array);
		}
		public bool TryParseCellArray<T>(string name, out T[] array)
		{
			return this.RowData.TryParseCellArray<T>(name, out array);
		}


		//指定の名前のセルを、型Tとして解析・取得（データがなかったらnull値を返す）
		public System.Nullable<T> ParseCellOptionalNull<T>(AdvColumnName name)where T : struct
		{
			return ParseCellOptionalNull<T>(name.QuickToString());
		}
		public System.Nullable<T> ParseCellOptionalNull<T>(string name)where T : struct
		{
			if (IsEmptyCell(name)) return null;
			return this.RowData.ParseCell<T>(name);
		}

		//現在の設定言語にローカライズされたテキストを取得
		public string ParseCellLocalizedText()
		{
			return ParseCellLocalized(AdvColumnName.Text.QuickToString());
		}

		//現在の設定言語にローカライズされたテキストを取得
		public string ParseCellLocalized( string defaultColumnName)
		{
			return AdvParser.ParseCellLocalizedText(this.RowData, defaultColumnName);
		}

		//ローカライズによってスキップすべきテキストデータかチェック
		public bool CheckSkipPage()
		{
			LanguageManagerBase languageManager = LanguageManagerBase.Instance;  
			if (languageManager == null) return false;

			return  languageManager.CheckSkipPage(this.RowData, AdvColumnName.Text.QuickToString());
		}
		//ローカライズによってスキップすべきテキストデータかチェック
		public bool CheckSkipByLocalize()
		{
			LanguageManagerBase languageManager = LanguageManagerBase.Instance;  
			if (languageManager == null) return false;

			return  languageManager.CheckSkipByLocalize(this.RowData, AdvColumnName.Text.QuickToString());
		}

		//シナリオラベルを解析・取得
		public virtual string ParseScenarioLabel(AdvColumnName name)
		{
			string label;
			if (!AdvCommandParser.TryParseScenarioLabel(this.RowData, name, out label))
			{
				Debug.LogError(ToErrorString(LanguageAdvErrorMsg.LocalizeTextFormat(AdvErrorMsg.NotScenarioLabel, ParseCell<string>(name))));
			}
			return label;
		}

		protected AssetFile ParseVoiceSub(AdvSettingDataManager dataManager, string voiceName)
		{
			AssetFile voiceFile = null;
			if (!AssetFileManager.IsEditorErrorCheck)
			{
				//通常時は現在のローカライズに従ってボイスをロード
				voiceFile = AddLoadFile(dataManager.BootSetting.GetLocalizeVoiceFilePath(voiceName), new AdvVoiceSetting(this.RowData));
			}
			else
			{
				//インポート時のエラーチェックが入るため、全対応言語でロード
				var pathList = dataManager.BootSetting.GetAllLocalizeVoiceFilePathList(voiceName);
				foreach (var path in pathList)
				{
					voiceFile = AddLoadFile(path, new AdvVoiceSetting(this.RowData));
				}
			}
			return voiceFile;
		}


#if UNITY_EDITOR
		//エディタ表示で使うコマンドラベル
		public string CommandLabel
		{
			get
			{
				string commandName = this.GetType().ToString().Replace("Utage.AdvCommand", "");
				string no = (RowData == null) ? "?" : RowData.DebugIndex.ToString();
				return no + " : " + commandName;				
			}
		}
#endif
	}
}
