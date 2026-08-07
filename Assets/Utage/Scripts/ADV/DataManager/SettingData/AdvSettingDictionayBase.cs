// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{
	/// <summary>
	/// StringGridから作成するKeyValueデータ
	/// </summary>
	public abstract class AdvSettingDictinoayItemBase : IAdvSettingData
	{
		/// <summary>
		/// キー
		/// </summary>
		public string Key { get { return key; } }
		string key;

		/// <summary>
		/// キーの初期化
		/// </summary>
		/// <param name="key"></param>
		internal void InitKey(string key) { this.key = key; }

		/// <summary>
		/// 文字列グリッドの行データから、データを初期化
		/// </summary>
		/// <param name="row">初期化するための文字列グリッドの行データ</param>
		/// <returns>成否。空のデータの場合などはfalseが帰る</returns>
		internal bool InitFromStringGridRowMain(StringGridRow row)
		{
			this.RowData = row;
			return InitFromStringGridRow(row);
		}

		/// <summary>
		/// 文字列グリッドの行データから、データを初期化
		/// </summary>
		/// <param name="row">初期化するための文字列グリッドの行データ</param>
		/// <returns>成否。空のデータの場合などはfalseが帰る</returns>
		public abstract bool InitFromStringGridRow(StringGridRow row);

		//元となる行データ
		public StringGridRow RowData { get; protected set; }
	}

	/// <summary>
	/// 設定データの基本クラス
	/// </summary>
	public abstract class AdvSettingDataDictinoayBase<T> : AdvSettingBase
				where T : AdvSettingDictinoayItemBase, new()
	{
		public List<T> List { get; private set; }
		public Dictionary<string, T> Dictionary { get; private set; }
		public AdvSettingDataDictinoayBase()
		{
			Dictionary = new Dictionary<string, T>();
			List = new List<T>();
		}

		/// <summary>
		/// 文字列グリッドから、データ解析
		/// </summary>
		/// <param name="grid"></param>
		protected override void OnParseGrid(StringGrid grid)
		{
			T last = null;
			foreach (StringGridRow row in grid.Rows)
			{
				if (row.RowIndex < grid.DataTopRow) continue;			//データの行じゃない
				if (row.IsEmptyOrCommantOut) continue;					//データがない

				if (!TryParseContinues(last, row))
				{
					T data = ParseFromStringGridRow(last, row);
					if (data != null) last = data;
				}
			}
		}

		//連続するデータとして追加できる場合はする。基本はしない
		protected virtual bool TryParseContinues(T last, StringGridRow row)
		{
			if (last == null) return false;
			return false;
		}

		//連続するデータとして追加できる場合はする。基本はしない
		protected virtual T ParseFromStringGridRow(T last, StringGridRow row)
		{
			T data = new T();
			if (data.InitFromStringGridRowMain(row))
			{
				if (!Dictionary.ContainsKey(data.Key))
				{
					AddData(data);
					return data;
				}
				else
				{
					string errorMsg = "";
					errorMsg += row.ToErrorString(ColorUtil.ToColorTagErrorMsg(data.Key) + "  is already contains");
					Debug.LogError(errorMsg);
				}
			}
			return null;
		}

		protected void AddData(T data)
		{
			List.Add(data);
			Dictionary.Add(data.Key, data);
		}
	}
}
