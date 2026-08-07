// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace Utage
{
	/// <summary>
	/// 表示言語切り替え用のクラス
	/// </summary>
	public abstract class LanguageManagerBase : ScriptableObject
	{
		[StaticField] static LanguageManagerBase instance;
		/// <summary>
		/// シングルトンなインスタンスの取得
		/// </summary>
		/// <returns></returns>
		public static LanguageManagerBase Instance
		{
			get
			{
				if (CustomProjectSetting.Instance == null) return null;
				var current = CustomProjectSetting.Instance.Language;
				if (instance != current)
				{
					instance = current;
					if (instance!=null)
					{
						instance.Init();
					}
				}
				return instance;
			}
		}

		//言語がオート設定のときは、システム環境に依存する

		const string Auto = "Auto";
		// 起動時の言語（言語名の場合は、その言語で起動する）
		public string Language
		{
			get => language;
			set => language = value;
		}
		[SerializeField]
		protected string language = Auto;

		//デフォルト言語
		public string DefaultLanguage
		{
			get => defaultLanguage;
			set => defaultLanguage = value;
		}
		[SerializeField]
		protected string defaultLanguage = "English";

		//データの言語指定
		public string DataLanguage
		{
			get => dataLanguage;
			set => dataLanguage = value;
		}
		[SerializeField]
		protected string dataLanguage = "";

		//翻訳テキストのデータ
		[SerializeField]
		List<TextAsset> languageData = new List<TextAsset>();


		//UIのテキストローカライズを無視する
		public bool IgnoreLocalizeUiText
		{
			get => ignoreLocalizeUiText;
			set => ignoreLocalizeUiText = value;
		}
		[SerializeField]
		bool ignoreLocalizeUiText = false;

		//ボイスのローカライズを無視する
		public bool IgnoreLocalizeVoice
		{
			get => ignoreLocalizeVoice;
			set => ignoreLocalizeVoice = value;
		}
		[SerializeField]
		bool ignoreLocalizeVoice = true;
		
		//ボイスの対応言語
		public List<string> VoiceLanguages { get { return voiceLanguages; } }
		[SerializeField]
		List<string> voiceLanguages = new List<string>();

		//空テキストの対応タイプ
		public LanguageBlankTextType BlankTextType{ get { return blankTextType; } }
		[SerializeField]
		LanguageBlankTextType blankTextType = LanguageBlankTextType.SwapDefaultLanguage;

		//テキスト列の言語
		public List<string> TextColumnLanguages { get { return textColumnLanguages; } }
		[FormerlySerializedAs("textColumnErrorCheckLanguages")] [SerializeField]
		List<string> textColumnLanguages = new List<string>();

		//言語切り替え時のイベントを、ScriptableObjectとして拡張管理可能にする
		public List<LanguageChangeEventFactory> ChangeEvents => changeEvents;
		[SerializeField] List<LanguageChangeEventFactory> changeEvents = new ();

		//イベントとして実行するインスタンス
		public List<ILanguageChangeEvent> OnChangeEvents { get; } = new(); 

		//言語切り替えで呼ばれるコールバック
		public Action OnChangeLanguage {
			get;
			set;
		}

		

		/// <summary>
		/// 現在の設定言語
		/// </summary>
		public string CurrentLanguage
		{
			get
			{
				return currentLanguage;
			}
			set
			{
				if (currentLanguage != value)
				{
					currentLanguage = value;
					RefreshCurrentLanguage();
				}
			}
		}
		string currentLanguage;

		// ボイスの言語指定
		public string VoiceLanguage
		{
			get
			{
				return voiceLanguage;
			}
			set
			{
				if (voiceLanguage != value)
				{
					voiceLanguage = value;
					RefreshCurrentLanguage();
				}
			}
		}
		string voiceLanguage = "";

		//ボイス言語を独立される可能性を考慮
		public string CurrentVoiceLanguage
		{
			get
			{
				if(!string.IsNullOrEmpty(VoiceLanguage))
				{
					return VoiceLanguage;
				}
				else
				{
					return CurrentLanguage;
				}
			}
		}



		LanguageData Data { get; set; }

		//現在設定されている言語名のリスト
		//宴4からは、TextColumnLanguagesとData.Languageを合わせたもの
		public List<string> Languages { get; } = new (); 


		//デバッグ表示用の文字列を取得
		public string GetDebugString()
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendLine($"Language: {CurrentLanguage}  VoiceLang: {CurrentVoiceLanguage}");
			builder.Append("Languages{ ");
			for (var i = 0; i < Languages.Count; i++)
			{
				builder.Append( Languages[i].ToString());
				if (i + 1 >= Languages.Count) break;
				builder.Append(", ");
			}
			builder.Append(" }\n");
			builder.AppendLine($"DataLang: {DataLanguage}  DefaultLang: {DefaultLanguage}");
			return builder.ToString();
		}

		void OnEnable()
		{
			Init();
		}
		
		//不使用になるので、後始末
		public void OnFinalize()
		{
			foreach (var changeEvent in OnChangeEvents)
			{
				changeEvent.OnFinalize();
			}
			OnChangeEvents.Clear();
			this.OnChangeLanguage = null;
		}

		//強制初期化
		public void ForceInit()
		{
			Init();
		}
		//初期化処理
		void Init()
		{
			Data = new LanguageData();
			foreach (var item in languageData)
			{
				if (item == null) continue;

				Data.OverwriteData(item);
			}

			Languages.Clear();
			Languages.Add(this.dataLanguage);
			foreach (var item in TextColumnLanguages)
			{
				if (!Languages.Contains(item))
				{
					Languages.Add(item);
				}
			}

			//設定された言語か、システムの言語に変更
			currentLanguage = (string.IsNullOrEmpty(language) || language == Auto) ? Application.systemLanguage.ToString() : language;
			voiceLanguage = "";

			//イベント用のinstanceを作成
			OnChangeEvents.Clear();
			foreach (var changeEvent in ChangeEvents)
			{
				if (changeEvent == null) continue;
				OnChangeEvents.Add(changeEvent.CreateEvent(this));
			}
			RefreshCurrentLanguage();
		}

		//現在の言語が変わったときの処理
		protected void RefreshCurrentLanguage()
		{
			if (Instance != this) return;

			foreach (var changeEvent in OnChangeEvents)
			{
				changeEvent.OnChangeLanguage();
			}
			if (OnChangeLanguage != null)
				OnChangeLanguage();
			OnRefreshCurrentLanguage();
		}
		//現在の言語が変わったときの処理
		protected abstract void OnRefreshCurrentLanguage();

		/// <summary>
		/// 指定のキーのテキストを、指定のデータの、設定された言語に翻訳して取得
		/// </summary>
		/// <param name="dataName">データ名</param>
		/// <param name="key">テキストのキー</param>
		/// <returns>翻訳したテキスト</returns>
		public string LocalizeText(string dataName, string key)
		{
			if (Data.ContainsKey(key))
			{
				if (Data.TryLocalizeText(out string text, CurrentLanguage, DefaultLanguage, key, dataName))
				{
					return text;
				}
			}
			Debug.LogError(key + " is not found in " + dataName);
			return key;
		}

		/// <summary>
		/// 指定のキーのテキストを、全データ内から検索して、設定された言語に翻訳して取得
		/// </summary>
		/// <param name="key">テキストのキー</param>
		/// <returns>翻訳したテキスト</returns>
		public string LocalizeText(string key)
		{
			string text = key;
			TryLocalizeText(key, out text);
			return text;
		}

		/// <summary>
		/// 指定のキーのテキストを、全データ内から検索して、設定された言語に翻訳して取得
		/// </summary>
		/// <param name="key">テキストのキー</param>
		/// <param name="lang">言語名</param>
		/// <param name="text">翻訳したテキスト</param>
		public bool TryLocalizeText(string key, out string text)
		{
			return TryLocalizeText(key, CurrentLanguage, out text);
		}
		public bool TryLocalizeText(string key, string lang, out string text)
		{
			text = key;
			if (Data.ContainsKey(key))
			{
				if (Data.TryLocalizeText(out text, lang, DefaultLanguage, key))
				{
					return true;
				}
			}

			return false;
		}

		public string DefaultLanuguageText(string key)
		{
			if (Data.ContainsKey(key))
			{
				if (Data.TryLocalizeText(out string text, DefaultLanguage, DefaultLanguage, key))
				{
					return text;
				}
			}
			Debug.LogError(key + " is not found in language key");
			return "";
		}

		internal void OverwriteData(StringGrid grid)
		{
			Data.OverwriteData(grid);
			RefreshCurrentLanguage();
		}

		//ローカライズ対象のテキスト系コマンドデータが空か？
		public bool IsEmptyTextCommand(StringGridRow row)
		{
			switch (this.BlankTextType)
			{
				case LanguageBlankTextType.NoBlankText:
				case LanguageBlankTextType.AllowBlankText:
					foreach (var column in TextColumnLanguages)
					{
						if (!row.IsEmptyCell(column))
						{
							return false;
						}
					}
					return true;
				default:
					return true;
			}
		}
		
		//現在の設定言語にローカライズされたテキストを取得
		public string ParseCellLocalizedText(StringGridRow row, string defaultColumnName)
		{
			switch (this.BlankTextType)
			{
				case LanguageBlankTextType.SwapDefaultLanguage:
					return ParseCellLocalizedTextBySwapDefaultLanguage(row,defaultColumnName);
				case LanguageBlankTextType.NoBlankText:
					return ParseCellLocalizedTextByNoSwap(row,defaultColumnName);
				case LanguageBlankTextType.AllowBlankText:
					return ParseCellLocalizedTextByNoSwap(row,defaultColumnName);
				default:
					Debug.LogError( row.ToErrorString( this.BlankTextType.ToString() + " is Unknown Type"));
					return "";
			}
		}

		//現在の設定言語にローカライズされたテキストを取得
		string ParseCellLocalizedTextBySwapDefaultLanguage(StringGridRow row, string defaultColumnName)
		{
			string columnName = defaultColumnName;
			if (row.Grid.ContainsColumn(CurrentLanguage))
			{
				//現在の言語があるなら、その列を
				columnName = currentLanguage;
			}
			else
			{
				if (DataLanguage==CurrentLanguage)
				{
					columnName = defaultColumnName;
				}
				else if (!string.IsNullOrEmpty(DefaultLanguage))
				{
					columnName = DefaultLanguage;
				}
				else
				{
					if (!string.IsNullOrEmpty(DataLanguage))
					{
						if (CurrentLanguage == DataLanguage)
						{
							//「DataLanguage」で言語指定がある場合、Text列は指定言語の場合にのみ表示されるようになります。
							columnName = defaultColumnName;
						}
						else
						{
							//DefaultLanguageの列のテキストが基本の表示テキストとして使用されます。
							columnName = DefaultLanguage;
						}
					}
				}
			}
			if (row.IsEmptyCell(columnName))
			{   //指定の言語が空なら、デフォルトのText列を
				//(DefaultLanguageの列のテキストが空の場合は、やはりText列のテキストを表示)
				return row.ParseCellOptional<string>(defaultColumnName, "");
			}
			else
			{   //指定の言語を
				return row.ParseCellOptional<string>(columnName, "");
			}
		}

		//現在の設定言語にローカライズされたテキストを取得
		string ParseCellLocalizedTextByNoSwap(StringGridRow row, string defaultColumnName)
		{
			string columnName = GetLocalizedColumnName(defaultColumnName);
			if (!row.Grid.ContainsColumn(columnName))
			{
				Debug.LogError( row.ToErrorString(columnName + " is empty column. Set localize text column"));
				return "";
			}

			if (this.BlankTextType == LanguageBlankTextType.NoBlankText)
			{
				//テキストセルの内容が空で、PageCtrlの設定もない場合はエラーを出す
				if (row.IsEmptyCell(columnName) && row.IsEmptyCell(AdvColumnName.PageCtrl.QuickToString()))
				{ 
					Debug.LogError( row.ToErrorString(columnName + " is empty cell. Set localize text"));
					return "";
				}
			}

			//指定の言語を
			return row.ParseCellOptional<string>(columnName, "");
		}

		string GetLocalizedColumnName(string defaultColumnName)
		{
			//ローカライズテキスト行が定義されているなら
			if (this.TextColumnLanguages.Contains(this.CurrentLanguage))
			{
				return CurrentLanguage;
			}

			//「DataLanguage」（Text列の言語指定）がないなら、デフォルト行名をそのまま
			if (string.IsNullOrEmpty(DataLanguage)) return defaultColumnName;
			
			//「DataLanguage」で言語指定がある場合、Text列は指定言語の場合にのみ表示されるようになります。
			if (DataLanguage!=CurrentLanguage && !string.IsNullOrEmpty(DefaultLanguage))
			{
				return DefaultLanguage;
			}
			return defaultColumnName;
		}



		//「指定の名前＋接尾辞として言語名」をセル名としたテキストを取得
		public string ParseCellSuffixedLanguageNameToLocalizedText(StringGridRow row, string cellName)
		{
			return row.ParseCell<string>(GetCellNameSuffixed(cellName));
		}
		public string ParseCellSuffixedLanguageNameToLocalizedTextOptional(StringGridRow row, string cellName,string defaultValue)
		{
			return row.ParseCellOptional<string>(GetCellNameSuffixed(cellName), defaultValue);
		}
		
		//指定の
		string GetCellNameSuffixed(string cellName)
		{
			//現在の言語キー
			string languageKey = GetCurrentLanguageKey();
			
			//言語キー見つからない場合や、データ言語と同じ場合は、cellNameをそのまま返す
			if (string.IsNullOrEmpty(languageKey) || DataLanguage == languageKey)
			{
				return cellName;
			}
			else
			{
				//接尾辞として言語名を追加したものを返す
				return cellName + languageKey;
			}
		}
		
		//現在の言語キーを取得
		string GetCurrentLanguageKey()
		{
			//対応言語名内にあれば、その言語名を返す
			if (this.TextColumnLanguages.Contains(this.CurrentLanguage))
			{
				return CurrentLanguage;
			}
			//開発環境の言語と同じなら、その言語名を返す
			if(DataLanguage==CurrentLanguage)
			{
				return CurrentLanguage;
			}
			
			//開発環境言語がないなら、空文字を返す
			if (string.IsNullOrEmpty(DataLanguage))
			{
				return "";
			}
			//ない場合はDefaultLanguageを返す
			return DefaultLanguage;
		}

		//ローカライズによってスキップページかどうかチェック
		public bool CheckSkipPage(StringGridRow row, string defaultColumnName)
		{
			if (!ContainsLocalizeText(row, defaultColumnName)) return false;
			return ParseCellLocalizedTextByNoSwap(row, defaultColumnName) == "<skip_page>";
		}

		//ローカライズによってスキップしてよいかチェック
		public bool CheckSkipByLocalize(StringGridRow row, string defaultColumnName)
		{
			if (!ContainsLocalizeText(row, defaultColumnName)) return false;
			bool isEmpty = ParseCellLocalizedTextByNoSwap(row, defaultColumnName).Length == 0;
			return isEmpty;
		}

		//ローカライズテキストデータが何らかの言語に存在するか？
		bool ContainsLocalizeText(StringGridRow row, string defaultColumnName)
		{
			if (!row.IsEmptyCell(defaultColumnName)) return true;
			foreach (var column in TextColumnLanguages)
			{
				if (!row.IsEmptyCell(column))
				{
					return true;
				}
			}
			return false;
		}
	}
}
