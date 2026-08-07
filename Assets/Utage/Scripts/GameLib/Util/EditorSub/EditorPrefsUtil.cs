// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace Utage
{
	public static class EditorPrefsUtil
	{
		/// <summary>
		/// エディターの値を消去
		/// </summary>
		/// <param name="key">キー</param>
		public static void Delete(string key)
		{
			EditorPrefs.DeleteKey(ToBase64(key));
		}

		/// <summary>
		/// エディター上の整数値セーブ
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="value">整数</param>
		public static void SaveInt(string key, int value)
		{
			EditorPrefs.SetInt(ToBase64(key), value);
		}

		/// <summary>
		/// エディター上の文字列ロード
		/// </summary>
		/// <param name="defaultvale">デフォルト値</param>
		/// <returns>ロードした整数(キーがなれければdefaultValeを返す)</returns>
		public static int LoadInt(string key, int defaltValue = 0 )
		{
			string str = ToBase64(key);
			if (EditorPrefs.HasKey(str))
			{
				return EditorPrefs.GetInt(str);
			}
			else
			{
				return defaltValue;
			}
		}


		/// <summary>
		/// エディター上のbool値セーブ
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="value">bool値</param>
		public static void SaveBool(string key, bool value)
		{
			EditorPrefs.SetBool(ToBase64(key), value);
		}

		/// <summary>
		/// エディター上のbool値ロード
		/// </summary>
		/// <param name="defaultvale">デフォルト値</param>
		/// <returns>ロードした整数(キーがなれければdefaultValeを返す)</returns>
		public static bool LoadBool(string key, bool defaltValue = false)
		{
			string str = ToBase64(key);
			if (EditorPrefs.HasKey(str))
			{
				return EditorPrefs.GetBool(str);
			}
			else
			{
				return defaltValue;
			}
		}

		/// <summary>
		/// エディター上の文字列セーブ
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="value">文字列</param>
		public static void SaveString(string key, string value)
		{
			EditorPrefs.SetString(ToBase64(key), ToBase64(value));
		}

		/// <summary>
		/// エディター上の文字列ロード
		/// </summary>
		/// <param name="defaultvale">デフォルト値</param>
		/// <returns>ロードした文字列(キーがなれければdefaultValeを返す)</returns>
		public static string LoadString(string key, string defaultVale = "" )
		{
			string str = ToBase64(key);
			if (EditorPrefs.HasKey(str))
			{
				return FromBase64(EditorPrefs.GetString(str));
			}
			else
			{
				return defaultVale;
			}
		}

		/// <summary>
		/// GUIDのキーからアセットをロード
		/// </summary>
		/// <param name="key">guidのキー</param>
		/// <returns>ロードしたアセット</returns>
		public static T LoadAsset<T>(string keyGuid, string defaultPrefabPath = "") where T : Object
		{
			string str = ToBase64(keyGuid);
			if (EditorPrefs.HasKey(str))
			{
				string guid = FromBase64(EditorPrefs.GetString(str));
				return LoadAssetFromGuid<T>(guid);
			}
			else
			{
				if (string.IsNullOrEmpty(defaultPrefabPath)) return null;

				T asset = AssetDatabase.LoadAssetAtPath(defaultPrefabPath, typeof(T)) as T;
				return asset;
			}
		}

		/// <summary>
		/// GUIDからアセットをロード
		/// </summary>
		/// <param name="key">guidのキー</param>
		/// <returns>ロードしたアセット</returns>
		public static T LoadAssetFromGuid<T>(string guid) where T : Object
		{
			if (string.IsNullOrEmpty(guid)) return null;

			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(path)) return null;

			T asset = AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
			return asset;
		}

		/// <summary>
		/// GUIDのキーからプレハブのアセットをロード
		/// </summary>
		/// <param name="key">guidのキー</param>
		/// <returns>ロードしたアセット</returns>
		public static T LoadPrefab<T>(string keyGuid, string defaultPrefabPath = "") where T : Component
		{
			GameObject go = LoadAsset<GameObject>(keyGuid, defaultPrefabPath);
			if (go == null) return null;

			return go.GetComponent<T>();
		}

		/// <summary>
		/// アセットのGUIDをセーブ
		/// </summary>
		/// <param name="key">guidのキー</param>
		/// <returns>セーブルするアセット</returns>
		public static void SaveAsset(string keyGuid, Object asset)
		{
			string guid = GetGuid(asset);
			SaveString(keyGuid, guid);
		}

		/// <summary>
		/// アセットのリストをセーブ
		/// </summary>
		/// <param name="key">guidのキー</param>
		/// <returns>セーブルするアセット</returns>
		public static void SaveAssetList(string key, List<Object> assetList)
		{
			try
			{
				string encode;
				using (MemoryStream stream = new MemoryStream())
				{
					//バイナリ化
					using (BinaryWriter writer = new BinaryWriter(stream))
					{
						writer.Write(assetList.Count);
						foreach (Object asset in assetList)
						{
							string guid = GetGuid(asset);
							writer.Write(guid);
						}
					}
					encode = System.Convert.ToBase64String(stream.ToArray());					
				}
				SaveString(key, encode);
			}
			catch (System.Exception e)
			{
				Debug.LogError("Failed Save " + key + "   " + e.ToString() );
			}
		}

		/// <summary>
		/// アセットのリスとをロード
		/// </summary>
		/// <param name="key">guidのキー</param>
		/// <returns>セーブルするアセット</returns>
		public static List<T> LoadAssetList<T>(string key) where T : Object
		{
			try
			{
				string encode = LoadString(key);
				if (string.IsNullOrEmpty(encode)) return new List<T>();

				//バイナリ
				byte[] buffer = System.Convert.FromBase64String( encode );

				//ロード
				List<T> assetList = new List<T>();
				using (MemoryStream stream = new MemoryStream(buffer))
				{
					using (BinaryReader reader = new BinaryReader(stream))
					{
						int num = reader.ReadInt32();
						for( int i = 0; i < num; ++i)
						{
							string guid = reader.ReadString();
							assetList.Add(LoadAssetFromGuid<T>(guid));
						}
					}
				}
				return assetList;
			}
			catch (System.Exception e)
			{
				Debug.LogError("Failed Load " + key + "   " + e.ToString());
				return new List<T>();
			}
		}

		/// <summary>
		/// アセットのGUIDを取得
		/// </summary>
		/// <param name="key">guidのキー<</param>
		/// <returns>セーブルするアセット</returns>
		public static string GetGuid(Object asset)
		{
			string path = AssetDatabase.GetAssetPath(asset);
			string guid = AssetDatabase.AssetPathToGUID(path);
			return guid;
		}


		static string ToBase64(string s)
		{
			return System.Convert.ToBase64String(System.Text.UTF8Encoding.UTF8.GetBytes(s));
		}

		static string FromBase64(string s)
		{
			return System.Text.UTF8Encoding.UTF8.GetString(System.Convert.FromBase64String(s));
		}
	}
}
#endif
