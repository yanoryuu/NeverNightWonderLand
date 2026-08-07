// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Utage
{
	public class CustomEditorWindow : EditorWindow
	{
		public SerializedObjectHelper SerializedObjectHelper
		{
			get
			{
				if (serializedObjectHelper == null)
				{
					serializedObjectHelper = new SerializedObjectHelper(this);
					OnSerializedObjectHelperInit();
				}
				return serializedObjectHelper;
			}
		}
		SerializedObjectHelper serializedObjectHelper;

		//スクロール
		public Vector2 ScrollPosition { get { return scrollPosition; } }
		[SerializeField, Hide]
		protected Vector2 scrollPosition;

		protected bool isEnableScroll;

		//描画更新
		protected virtual void OnGUI()
		{
			//スクロールするか
			if (isEnableScroll)
			{
				this.scrollPosition = EditorGUILayout.BeginScrollView(this.scrollPosition);
				OnGUISub();
				EditorGUILayout.EndScrollView();
			}
			else
			{
				OnGUISub();
			}
		}

		//描画更新
		protected virtual void OnGUISub()
		{
			if (DrawProperties())
			{
				Save();
			}
		}

		protected virtual bool DrawProperties()
		{
			return SerializedObjectHelper.OnGUI();
		}

		protected virtual string SaveKey()
		{
			return "Utage." + this.GetType().ToString();
		}

		//セーブタイプ
		protected enum SaveType
		{
			EditorUserSettings,
			EditorPrefs,
		};

		//継承して、EditorPrefsに変えることも可能
		protected virtual SaveType EditorSaveType
		{
			get { return SaveType.EditorUserSettings; }
		}

		/// <summary>
		/// エディタ上に保存してあるデータをロード
		/// </summary>
		protected virtual void Load()
		{
			string str = "";
			switch (EditorSaveType)
			{
				case SaveType.EditorPrefs:
					str = EditorPrefs.GetString(SaveKey(), "");
					break;
				case SaveType.EditorUserSettings:
				default:
					str = EditorUserSettings.GetConfigValue(SaveKey());
					break;
			}
			if (!string.IsNullOrEmpty(str))
			{
				BinaryUtil.BinaryReadFromString(str, reader => SerializedObjectHelper.ReadAllVisibleProperties(reader));
			}
		}

		//ロード後初期化
		protected virtual void OnSerializedObjectHelperInit()
		{
		}

		/// <summary>
		/// エディタ上に保存してあるデータをセーブ
		/// </summary>
		protected virtual void Save()
		{
			string str = BinaryUtil.BinaryWriteToString(writer => SerializedObjectHelper.WriteAllVisibleProperties(writer));
			switch (EditorSaveType)
			{
				case SaveType.EditorPrefs:
					EditorPrefs.SetString(SaveKey(),str);
					break;
				case SaveType.EditorUserSettings:
				default:
					EditorUserSettings.SetConfigValue(SaveKey(),str);
					break;
			}
		}

		protected virtual void OnFocus()
		{
			Load();
		}

		protected virtual void OnLostFocus()
		{
			Save();
		}

		protected virtual void OnDestroy()
		{
			Save();
		}


		protected GUIStyle BoxStyle
		{
			get
			{
				if(!isInitBoxStyle)
				{
					boxStyle = new GUIStyle(GUI.skin.box);
					isInitBoxStyle = true;
				}
				return boxStyle;
			}
		}
		bool isInitBoxStyle = false;
		GUIStyle boxStyle;

		protected GUIStyle WindowStyle
		{
			get
			{
				if (!isInitWindowStyle)
				{
					windowStyle = new GUIStyle(GUI.skin.window);
					isInitWindowStyle = true;
				}
				return windowStyle;
			}
		}
		bool isInitWindowStyle = false;
		GUIStyle windowStyle;
	}
}
#endif
