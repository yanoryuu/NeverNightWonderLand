// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
	/// <summary>
	/// StringGridのキーバリュー
	/// </summary>
	[System.Serializable]
	public partial class StringGridDictionaryKeyValue : SerializableDictionaryKeyValue
	{
		/// <summary>
		/// 名前（キーと同じ）
		/// </summary>
		public string Name { get { return Key; } }

		/// <summary>
		/// シナリオを記述したStringGrid
		/// </summary>
		public StringGrid Grid { get { return grid; } }
		[SerializeField]
		StringGrid grid;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="value">値</param>
		public StringGridDictionaryKeyValue(string key, StringGrid grid)
		{
			InitKey(key);
			this.grid = grid;
		}
	}

	/// <summary>
	/// StringGridのDictionary
	/// </summary>
	[System.Serializable]
	public partial class StringGridDictionary : SerializableDictionary<StringGridDictionaryKeyValue>
	{
#if UNITY_EDITOR
		//エディタ実行中のエラー処理などのために、元のアセット（エクセルのアセットなど）を登録する
		public Object SourceAssetInEditor { get; private set; }

		//エディタ実行中のエラー処理などのために、元のアセット（エクセルのアセットなど）を登録する
		public void SetSourceAsset(Object asset)
		{
			this.SourceAssetInEditor = asset;
			foreach (var item in this.List)
			{
				item.Grid.SourceAssetInEditor = asset;
			}
		}
#else
		public Object SourceAssetInEditor => null;
#endif

		/// <summary>
		/// 要素（キーバリュー）の追加
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="value">値</param>
		public void Add(string key, StringGrid value)
		{
			Add(new StringGridDictionaryKeyValue(key, value));
		}
	
		/// <summary>
		/// 特定の名前のシートを消す
		/// </summary>
		public void RemoveSheets(string pattern)
		{
			List<string> removeList = new List<string>();
			foreach (string key in Keys)
			{
				if (System.Text.RegularExpressions.Regex.IsMatch(key, pattern))
				{
					removeList.Add(key);
				}
			}
			foreach (string key in removeList)
			{
				Remove(key);
			}
		}

		// コメントアウト処理
		public void EraseCommentOutStrings(string commentOutKey)
		{
			foreach (var item in this.List)
			{
				item.Grid.EraseCommentOutStrings();
			}
		}
	}
}
