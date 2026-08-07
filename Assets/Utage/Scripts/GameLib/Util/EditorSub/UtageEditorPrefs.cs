// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Utage
{

	public static class UtageEditorPrefs
	{
		public enum Key
		{
			CreateAdvEngineWindowFont,
			CreateAdvEngineWindowClickSe,
			CreateAdvEngineWindowTransitionFadeBg,
			CreateAdvEngineWindowMsgWindowSprite,
			CreateAdvEngineWindowIsEnableCloseButton,
			CreateAdvEngineWindowCloseButtonSprite,
			CreateAdvEngineWindowSelectionItemPrefab,
			CreateAdvEngineWindowIsEnableBackLog,
			CreateAdvEngineWindowBackLogButtonSprite,
			CreateAdvEngineWindowBackLogFilterSprite,
			CreateAdvEngineWindowBackLogScrollUpArrow,
			CreateAdvEngineWindowBackLogItemPrefab,
			CreateAdvEngineWindowBackLogCloseButtonSprite,


			AdvExcelEditorWindowExcelList,
			AdvExcelEditorWindowConvertPath,
			GameScreenWidth,
			GameScreenHeight,
			CustomProjectSetting,
			AdvScenarioProject,
			ScriptCleanerRoot,
			SelectableColorChanger,
		};

		static string ToKeyString( Key key )
		{
			return "Utage." + key.ToString();
		}

		/// <summary>
		/// エディター上のデータを全て消去
		/// </summary>
		public static void DeleteAll()
		{
			foreach (Key item in System.Enum.GetValues(typeof(Key)))
			{
				Delete( item );
			}
		}

		/// <summary>
		/// エディター上のデータを消去
		/// </summary>
		public static void Delete(Key key)
		{
			EditorPrefsUtil.Delete(ToKeyString(key));
		}


		/// <summary>
		/// エディター上の整数値セーブ
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="value">整数</param>
		public static void SaveInt(Key key, int value)
		{
			EditorPrefsUtil.SaveInt(ToKeyString(key), value);
		}

		/// <summary>
		/// エディター上の整数値ロード
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="defaultvale">デフォルト値</param>
		/// <returns>ロードした整数(キーがなれければdefaultValeを返す)</returns>
		public static int LoadInt(Key key, int defaultVale = 0)
		{
			return EditorPrefsUtil.LoadInt(ToKeyString(key), defaultVale);
		}

		/// <summary>
		/// エディター上のbool値セーブ
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="value">bool値</param>
		public static void SaveBool(Key key, bool value)
		{
			EditorPrefsUtil.SaveBool(ToKeyString(key), value);
		}

		/// <summary>
		/// エディター上のbool値ロード
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="defaultvale">デフォルト値</param>
		/// <returns>ロードした整数(キーがなれければdefaultValeを返す)</returns>
		public static bool LoadBool(Key key, bool defaultVale = false)
		{
			return EditorPrefsUtil.LoadBool(ToKeyString(key), defaultVale);
		}

		/// <summary>
		/// エディター上の整数値セーブ
		/// </summary>
		/// <param name="key">キー</param>
		/// <param name="value">文字列</param>
		public static void SaveString(Key key, string value)
		{
			EditorPrefsUtil.SaveString(ToKeyString(key), value);
		}

		/// <summary>
		/// エディター上の文字列ロード
		/// </summary>
		/// <param name="key">キー<</param>
		/// <param name="defaultvale">デフォルト値</param>
		/// <returns>ロードした文字列(キーがなれければdefaultValeを返す)</returns>
		public static string LoadString(Key key, string defaultVale = "" )
		{
			return EditorPrefsUtil.LoadString(ToKeyString(key), defaultVale);
		}

		/// <summary>
		/// GUIDのキーからプレハブのアセットをロード
		/// </summary>
		/// <param name="key">guidのキー<</param>
		/// <returns>ロードしたアセット</returns>
		public static T LoadPrefab<T>(Key key, string defaultPrefabPath = "" ) where T : Component
		{
			return EditorPrefsUtil.LoadPrefab<T>(ToKeyString(key), defaultPrefabPath );
		}

		/// <summary>
		/// GUIDのキーからアセットをロード
		/// </summary>
		/// <param name="key">guidのキー<</param>
		/// <returns>ロードしたアセット</returns>
		public static T LoadAsset<T>(Key key, string defaultAssetPath = "") where T : Object
		{
			return EditorPrefsUtil.LoadAsset<T>(ToKeyString(key), defaultAssetPath );
		}

		/// <summary>
		/// アセットのGUIDをセーブ
		/// </summary>
		/// <param name="key">guidのキー<</param>
		/// <returns>セーブルするアセット</returns>
		public static void SaveAsset(Key key, Object asset)
		{
			EditorPrefsUtil.SaveAsset(ToKeyString(key), asset);
		}

		/// <summary>
		/// GUIDのキーからアセットのリストをロード
		/// </summary>
		/// <param name="key">guidのキー<</param>
		/// <returns>ロードしたアセット</returns>
		public static List<T> LoadAssetList<T>(Key key) where T : Object
		{
			return EditorPrefsUtil.LoadAssetList<T>(ToKeyString(key));
		}

		/// <summary>
		/// アセットのGUIDのリストをセーブ
		/// </summary>
		/// <param name="key">guidのキー<</param>
		/// <returns>セーブルするアセット</returns>
		public static void SaveAssetList(Key key, List<Object> assetList)
		{
			EditorPrefsUtil.SaveAssetList(ToKeyString(key), assetList);
		}
	}
}
#endif
