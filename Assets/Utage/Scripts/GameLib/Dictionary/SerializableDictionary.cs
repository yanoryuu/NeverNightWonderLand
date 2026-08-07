// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{

	/// <summary>
	/// シリアライズ可能な自作Dictionary用のキーバリュー
	/// </summary>
	[System.Serializable]
	public abstract class SerializableDictionaryKeyValue
	{
		/// <summary>
		/// キー
		/// </summary>
		public string Key { get { return key; } }
		[SerializeField]
		string key;

		/// <summary>
		/// キーの初期化
		/// </summary>
		/// <param name="key"></param>
		public void InitKey(string key) { this.key = key; }
	}

	/// <summary>
	/// シリアライズ可能な自作Dictionary
	/// </summary>
	[System.Serializable]
	public class SerializableDictionary<T>
		where T : SerializableDictionaryKeyValue
	{
		/// <summary>
		/// データのリスト
		/// </summary>
		public List<T> List { get { return 	this.list ?? (list = new List<T>()); } }

		[SerializeField]
		List<T> list;

		/// <summary>
		/// データのdictionary
		/// </summary>
		protected Dictionary<string, T> dictionary = new Dictionary<string, T>();

		/// <summary>
		/// 要素数
		/// </summary>
		public int Count { get { InitDic(); return dictionary.Count; } }

		/// <summary>
		/// キーのリスト
		/// </summary>
		public Dictionary<string, T>.KeyCollection Keys { get { InitDic(); return dictionary.Keys; } }

		/// <summary>
		/// バリューのリスト
		/// </summary>
		public Dictionary<string, T>.ValueCollection Values { get { InitDic(); return dictionary.Values; } }

		/// <summary>
		/// 要素（キーバリュー）の追加
		/// </summary>
		/// <param name="val">要素（キーバリュー）</param>
		public void Add(T val)
		{
			if(dictionary.ContainsKey(val.Key))
			{
				Debug.LogError( "<color=red>" + val.Key + "</color>"+ "  is already contains" );
			}

			InitDic();

			dictionary.Add(val.Key, val);
			List.Add(val);
		}


		/// <summary>
		/// 要素（キーバリュー）の取得
		/// </summary>
		/// <param name="key">キー</param>
		/// <returns>要素（キーバリュー）</returns>
		public T GetValue(string key)
		{
			InitDic();
			return dictionary[key];
		}

		/// <summary>
		/// 要素（キーバリュー）の取得を試みる
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="val">要素（キーバリュー）</param>
		/// <returns>成否</returns>
		public bool TryGetValue(string key, out T val)
		{
			InitDic();
			return dictionary.TryGetValue(key, out val);
		}

		/// <summary>
		/// 要素（キーバリュー）の削除
		/// </summary>
		/// <param name="key">キー</param>
		/// <returns>成否</returns>
		public bool Remove(string key)
		{
			InitDic();

			bool ret = dictionary.Remove(key);
			if (ret)
			{
				List.RemoveAll(x => x.Key.CompareTo(key) == 0);
			}
			return ret;
		}

		/// <summary>
		/// 要素（キーバリュー）を全てクリア
		/// </summary>
		public void Clear()
		{
			dictionary.Clear();
			List.Clear();
		}

		/// <summary>
		/// 指定のキーの要素があるか
		/// </summary>
		/// <param name="key">キー</param>
		/// <returns>要素があればtrue。なかったらfalse</returns>
		public bool ContainsKey(string key)
		{
			InitDic();
			return dictionary.ContainsKey(key);
		}

		/// <summary>
		/// 指定の要素（キーバリュー）があるか
		/// </summary>
		/// <param name="val">要素（キーバリュー）</param>
		/// <returns>要素があればtrue。なかったらfalse</returns>
		public bool ContainsValue(T val)
		{
			InitDic();
			return dictionary.ContainsValue(val);
		}

		/// <summary>
		/// dictionaryを初期化。シリアライズ直後はdictionaryが空のため初期化する
		/// </summary>
		void InitDic()
		{
			if (dictionary.Count != 0) return;

			RefreshDictionary();
		}

		/// <summary>
		/// dictionaryを再構築
		/// </summary>
		public void RefreshDictionary()
		{
			dictionary.Clear();
			foreach (T item in List)
			{
				dictionary.Add(item.Key, item);
			}
		}

		/// <summary>
		/// リスト内の要素の順番を入れ替える
		/// </summary>
		/// <param name="index0">入れ替える用のインデックスその1</param>
		/// <param name="index1">入れ替える用のインデックスその2</param>
		public void Swap(int index0, int index1)
		{
			if (index0 < 0 || this.Count <= index0) return;
			if (index1 < 0 || this.Count <= index1) return;

			T tmp = List[index0];
			List[index0] = List[index1];
			List[index1] = tmp;
			RefreshDictionary();
		}
	}
}