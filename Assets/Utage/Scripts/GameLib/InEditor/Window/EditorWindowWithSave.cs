// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UtageExtensions;

namespace Utage
{
	//[SerializeFiled]なフィールドを自動でGUI表示するエディタウィンドウ（セーブ機能あり）
	public abstract class EditorWindowWithSave : EditorWindowNoSave
	{
		protected abstract string SaveKey { get; }

		//セーブタイプ
		protected enum SaveType
		{
			EditorUserSettings,
			EditorPrefs,
		};

		//継承して、EditorPrefsに変えることも可能
		protected virtual SaveType EditorSaveType => SaveType.EditorUserSettings;

		protected virtual void Load()
		{
			string json = "";
			switch (EditorSaveType)
			{
				case SaveType.EditorPrefs:
					json = EditorPrefs.GetString(SaveKey, "");
					break;
				case SaveType.EditorUserSettings:
				default:
					json = EditorUserSettings.GetConfigValue(SaveKey);
					break;
			}
			if (!string.IsNullOrEmpty(json))
			{
				EditorJsonUtility.FromJsonOverwrite(json,this);
			}
		}

		/// エディタ上に保存してあるデータをセーブ
		protected virtual void Save()
		{
			string json = EditorJsonUtility.ToJson(this);
			switch (EditorSaveType)
			{
				case SaveType.EditorPrefs:
					EditorPrefs.SetString(SaveKey,json);
					break;
				case SaveType.EditorUserSettings:
				default:
					EditorUserSettings.SetConfigValue(SaveKey, json);
					break;
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			Load();
		}

		protected virtual void OnDisable()
		{
			Save();
		}

		//描画更新
		protected override void OnGUI()
		{
			if (Editor.DrawInspectorAllProperties())
			{
				Save();
			}
		}

	}
}
#endif
