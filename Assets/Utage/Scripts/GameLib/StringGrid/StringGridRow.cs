// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// 文字列グリッドデータの行
	/// </summary>
	[System.Serializable]
	public class StringGridRow
	{
		/// <summary>
		/// 元になるグリッド
		/// </summary>
		public StringGrid Grid { get { return grid; } }
		[System.NonSerialized]
		StringGrid grid;

		/// <summary>
		/// 行番号
		/// </summary>
		public int RowIndex { get { return this.rowIndex; } }
		[SerializeField]
		int rowIndex;


		/// <summary>
		/// デバッグ用のインデックス
		/// </summary>
		public int DebugIndex
		{
			get { return debugIndex; }
			set { debugIndex = value; }
		}
#if UNITY_EDITOR
		[SerializeField]
#else
		[NonSerialized]
#endif
		int debugIndex = -1;

		/// <summary>
		/// 文字列データ
		/// </summary>
		public string[] Strings { get { return this.strings; } }
		[SerializeField]
		string[] strings;

		/// <summary>
		/// 文字列データの長さ
		/// </summary>
		public int Length { get { return strings.Length; } }

		/// <summary>
		/// データが空かどうか
		/// </summary>
		public bool IsEmpty { get { return isEmpty; } }
		[SerializeField]
		bool isEmpty;

		/// <summary>
		/// コメントアウトされているか
		/// </summary>
		public bool IsCommentOut { get { return isCommentOut; } }
		[SerializeField]
		bool isCommentOut;

		/// <summary>
		/// データが空かどうか
		/// </summary>
		public bool IsEmptyOrCommantOut { get { return IsEmpty || IsCommentOut; } }
		
		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="grid">元になる文字列グリッド</param>
		/// <param name="rowIndex">行番号</param>
		public StringGridRow(StringGrid gird, int rowIndex )
		{
			this.rowIndex = this.DebugIndex = rowIndex;
			InitLink(gird);
		}

		/// <summary>
		/// 親とのリンクを初期化
		/// ScriptableObjectなどで読み込んだ場合、参照が切れているのでそれを再設定するために
		/// </summary>
		/// <param name="grid">元になる文字列グリッド</param>
		public void InitLink(StringGrid grid)
		{
			this.grid = grid;
		}

		/// <summary>
		/// CSVテキストから初期化
		/// </summary>
		/// <param name="type">CSVタイプ</param>
		/// <param name="text">CSVテキスト</param>
		public void InitFromCsvText(CsvType type, string text )
		{
			this.strings = text.Split( type == CsvType.Tsv ? '\t' : ',');
			this.isEmpty = CheckEmpty();
			this.isCommentOut = CheckCommentOut();
		}

		/// <summary>
		/// 文字列リストから初期化
		/// </summary>
		/// <param name="stringList">文字列リスト</param>
		public void InitFromStringList(List<string> stringList)
		{
			InitFromStringArray(stringList.ToArray());
		}
		/// <summary>
		/// 文字列リストから初期化
		/// </summary>
		/// <param name="stringList">文字列リスト</param>
		public void InitFromStringArray(string[] strings)
		{
			this.strings = strings;
			this.isEmpty = CheckEmpty();
			this.isCommentOut = CheckCommentOut();
		}

		//空データかチェック
		bool CheckEmpty()
		{
			foreach (var str in strings)
			{
				if (!string.IsNullOrEmpty(str))
				{
					return false;
				}
			}
			return true;
		}
		//コメントアウトされているかチェック
		bool CheckCommentOut()
		{
			if (this.Strings.Length <= 0) return false;
			return this.Strings[0].StartsWith("//");
		}


		/// <summary>
		/// 指定した列名のセルが空かどうか
		/// </summary>
		/// <param name="columnName">列の名前</param>
		/// <returns>空ならture、データがあればfalse</returns>
		public bool IsEmptyCell(string columnName)
		{
			int index;
			if (Grid.TryGetColumnIndex(columnName, out index))
			{
				return IsEmptyCell(index);
			}
			else
			{
				return true;
			}
		}

		/// <summary>
		/// 列名がついたセル全て空かどうか
		/// </summary>
		/// <returns></returns>
		internal bool IsAllEmptyCellNamedColumn()
		{
			foreach( var keyValue in Grid.ColumnIndexTbl)
			{
				if (!IsEmptyCell(keyValue.Value) && !Grid.IsCommentOutCoulmn(keyValue.Value))
				{
					return false;
				}
			}
			return true;
		}


		//指定した列インデックスのセルが空かどうか
		public bool IsEmptyCell(int index)
		{
			return !(index < Length && !string.IsNullOrEmpty(strings[index]));
		}

		/// <summary>
		/// 指定した列名のセルを値に変換
		/// </summary>
		/// <typeparam name="T">値の型</typeparam>
		/// <param name="columnName">列の名前</param>
		/// <returns>変換後の値</returns>
		public T ParseCell<T>(string columnName)
		{
			T ret;
			if (!TryParseCell(columnName, out ret))
			{
				Debug.LogError(ToErrorStringWithPraseColumnName(columnName));
			}
			return ret;
		}
		public T ParseCell<T>(int index)
		{
			T ret;
			if (!TryParseCell(index, out ret))
			{
				Debug.LogError(ToErrorStringWithPraseColumnIndex(index));
			}
			return ret;
		}

		/// <summary>
		/// 指定した列名のセルを値に変換
		/// 要素が空だった場合は、デフォルト値を返す
		/// </summary>
		/// <typeparam name="T">値の型</typeparam>
		/// <param name="columnName">列の名前</param>
		/// <param name="defaultVal">デフォルト値</param>
		/// <returns>変換後の結果</returns>
		public T ParseCellOptional<T>(string columnName, T defaultVal)
		{
			T ret;
			return TryParseCell(columnName, out ret) ? ret : defaultVal;
		}

		public T ParseCellOptional<T>(int index, T defaultVal)
		{
			T ret;
			return TryParseCell(index, out ret) ? ret : defaultVal;
		}

		/// <summary>
		/// 指定した列名のセルを値に変換を試みる。
		/// </summary>
		/// <typeparam name="T">値の型</typeparam>
		/// <param name="columnName">列の名前</param>
		/// <param name="val">変換後の結果</param>
		/// <returns>成功したらtrue。失敗したらfalse</returns>
		public bool TryParseCell<T>(string columnName, out T val)
		{
			int index;
			if (Grid.TryGetColumnIndex(columnName, out index))
			{
				return TryParseCell(index, out val);
			}
			else
			{
				val = default(T);
				return false;
			}
		}

		//指定した列インデックスのセルを値に変換
		public bool TryParseCell<T>(int index, out T val)
		{
			if (!IsEmptyCell(index))
			{
				if (TryParse<T>(strings[index], out val))
				{
					return true;
				}
				else
				{
					Debug.LogError(ToErrorStringWithPrase(strings[index], index));
					return false;
				}
			}
			else
			{
				val = default(T);
				return false;
			}
		}

		//型間違いを許容して、解析できなかった場合はデフォルト値を設定する
		public bool TryParseCellTypeOptional<T>(int index, T defaultVal, out T val)
		{
			if (!IsEmptyCell(index))
			{
				if (TryParse<T>(strings[index], out val))
				{
					return true;
				}
				else
				{
					val = defaultVal;
					return false;
				}
			}
			else
			{
				val = defaultVal;
				return false;
			}
		}


		/// <summary>
		/// 文字列を値に変換
		/// </summary>
		/// <typeparam name="T">値の型</typeparam>
		/// <param name="str">文字列</param>
		/// <param name="val">値</param>
		/// <returns>変換に成功したらtrue、書式違いなどで変換できなかったらfalse</returns>
		public static bool TryParse<T>(string str, out T val)
		{
			try
			{
				System.Type type = typeof(T);
				if (type == typeof(string))
				{
					val = (T)(object)str;
				}
				else if (type.IsEnum)
				{
					val = (T)System.Enum.Parse(typeof(T), str);
				}
				else if (type == typeof(Color))
				{
					Color color = Color.white;
					bool ret = ColorUtil.TryParseColor(str, ref color);
					val = ret ? (T)(object)color : default(T);
					return ret;
				}
				else if( type == typeof(int) )
				{
					val = (T)(object)int.Parse(str);
				}
				else if (type == typeof(float))
				{
					val = (T)(object)WrapperUnityVersion.ParseFloatGlobal(str);
				}
				else if (type == typeof(double))
				{
					val = (T)(object)WrapperUnityVersion.ParseDoubleGlobal(str);
				}
				else if (type == typeof(bool))
				{
					val = (T)(object)bool.Parse(str);
				}
				else
				{
					System.ComponentModel.TypeConverter converter = System.ComponentModel.TypeDescriptor.GetConverter(type);
					val = (T)converter.ConvertFromString(str);
				}
				return true;
			}
			catch
			{
				val = default(T);
				return false;
			}
		}

		//コメントアウトされてる文字列を削除する
		public void EraseAllCommentOutStrings()
		{
			if (IsCommentOut)
			{
				//コメントアウトされている行を削除
				for (int i = 0; i < this.strings.Length; ++i)
				{
					this.strings[i] = "";
				}
				return;
			}
		}

		//指定の列をコメントアウトされた文字列として削除する
		public void EraseCommentOutColumn(int column)
		{
			if (strings.Length <= column ) return;
			if (string.IsNullOrEmpty(strings[column])) return;

			this.strings[column] = "";
		}


		/// <summary>
		/// 指定した列名のセルを型Tのカンマ区切り配列として値に変換
		/// </summary>
		/// <typeparam name="T">値の型</typeparam>
		/// <param name="columnName">列の名前</param>
		/// <returns>変換後の値</returns>
		public T[] ParseCellArray<T>(string columnName)
		{
			T[] ret;
			if (!TryParseCellArray(columnName, out ret))
			{
				Debug.LogError(ToErrorStringWithPraseColumnName(columnName));
			}
			return ret;
		}
		public T[] ParseCellArray<T>(int index)
		{
			T[] ret;
			if (!TryParseCellArray(index, out ret))
			{
				Debug.LogError(ToErrorStringWithPraseColumnIndex(index));
			}
			return ret;
		}

		/// <summary>
		/// 指定した列名のセルを型Tのカンマ区切り配列として値に変換
		/// 要素が空だった場合は、デフォルト値を返す
		/// </summary>
		/// <typeparam name="T">値の型</typeparam>
		/// <param name="columnName">列の名前</param>
		/// <param name="defaultVal">デフォルト値</param>
		/// <returns>変換後の結果</returns>
		public T[] ParseCellOptionalArray<T>(string columnName, T[] defaultVal)
		{
			T[] ret;
			return TryParseCellArray(columnName, out ret) ? ret : defaultVal;
		}

		public T[] ParseCellOptionalArray<T>(int index, T[] defaultVal)
		{
			T[] ret;
			return TryParseCellArray(index, out ret) ? ret : defaultVal;
		}

		/// <summary>
		/// 指定した列名のセルを型Tのカンマ区切り配列として値に変換を試みる。
		/// </summary>
		/// <typeparam name="T">値の型</typeparam>
		/// <param name="columnName">列の名前</param>
		/// <param name="val">変換後の結果</param>
		/// <returns>成功したらtrue。失敗したらfalse</returns>
		public bool TryParseCellArray<T>(string columnName, out T[] val)
		{
			int index;
			if (Grid.TryGetColumnIndex(columnName, out index))
			{
				return TryParseCellArray(index, out val);
			}
			else
			{
				val = null;
				return false;
			}
		}

		//指定した列インデックスのセルを型Tのカンマ区切り配列として値に変換
		public bool TryParseCellArray<T>(int index, out T[] val)
		{
			if (!IsEmptyCell(index))
			{
				if (TryParseArray<T>(strings[index], out val))
				{
					return true;
				}
				else
				{
					Debug.LogError(ToErrorStringWithPrase(strings[index], index));
					return false;
				}
			}
			else
			{
				val = null;
				return false;
			}
		}

		bool TryParseArray<T>(string str, out T[] val)
		{
			string[] strArray = str.Split(',');
			int count = strArray.Length;
			val = new T[count];
			for( int i = 0; i < count; ++i )
			{
				T v;
				if (!TryParse<T>(strArray[i].Trim(), out v))
				{
					return false;
				}
				else
				{
					val[i] = v;
				}
			}
			return true;
		}


		/// <summary>
		/// デバッグ文字列に変換
		/// </summary>
		/// <returns>デバッグ文字列</returns>
		internal string ToDebugString()
		{
			char separator = Grid.CsvSeparator;

			string textOutput = "";
			foreach (string str in strings)
			{
				textOutput += " " + str + separator;
			}
			return textOutput;
		}

		//デバッグ用の情報（マクロなどでシート名が変わっているとき対策）
		//シリアライズはしないのでエディタ上でのみ有効
		internal string DebugInfo 
		{ 
			get{ return debugInfo;} 
			set{ debugInfo = value;} 
		}


#if UNITY_EDITOR
		public UnityEngine.Object SourceAssetInEditor
		{
			get { return Grid.SourceAssetInEditor; }
		}
#else
		public UnityEngine.Object SourceAssetInEditor { get { return null;} }
#endif

#if UNITY_EDITOR
		[SerializeField]
		#else
		[NonSerialized]
		#endif
		string debugInfo;

		/// <summary>
		/// エラー用の文字列を取得
		/// </summary>
		/// <param name="msg">エラーメッセージ</param>
		/// <returns>エラー用のテキスト</returns>
		public string ToErrorString(string msg)
		{
			if (!msg.EndsWith("\n")) msg += "\n";

			//デバッグ用の行番号
			int lineNo = this.DebugIndex + 1;
			if (string.IsNullOrEmpty(this.DebugInfo))
			{
				string sheetName = Grid.SheetName;
				msg += sheetName + ":" + lineNo + " ";
			}
			else
			{
				msg += this.DebugInfo;
			}
			return msg
				+ ColorUtil.ToColorTagErrorMsg(ToDebugString()) + "\n"
				+ "<b>" + Grid.Name + "</b>" + "  : " + lineNo;
		}

		/// <summary>
		/// エラー用の文字列を取得
		/// </summary>
		/// <param name="msg">エラーメッセージ</param>
		/// <returns>エラー用のテキスト</returns>
		public string ToStringOfFileSheetLine()
		{
			int lineNo = this.DebugIndex + 1;
			return "<b>" + Grid.Name + "</b>" + "  : " + lineNo;
		}

		//列名指定パースエラー出力
		string ToErrorStringWithPraseColumnName(string columnName)
		{
			return ToErrorString( LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.StringGridRowPraseColumnName, columnName ) );
		}
		//列インデックス指定パースエラー出力
		string ToErrorStringWithPraseColumnIndex(int index)
		{
			return ToErrorString( LanguageErrorMsg.LocalizeTextFormat(ErrorMsg.StringGridRowPraseColumnIndex, index ) );
		}
		//パースエラー出力
		string ToErrorStringWithPrase(string column, int index)
		{
			return ToErrorString( LanguageErrorMsg.LocalizeTextFormat( ErrorMsg.StringGridRowPrase, index,column));
		}
	}
}
