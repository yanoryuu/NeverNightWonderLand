// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Utage
{

	/// <summary>
	/// ファイル管理
	/// </summary>
	public partial class AssetFileManager : MonoBehaviour
	{
		//ファイル数
		static public int FileCount()
		{
			return instance == null ? 0 : GetInstance().fileTbl.Count;
		}

		//エディター上でビュワー描画として呼び出す
		static public void OnGuiViewerInEditor( int index )
		{
			GetInstance().OnGuiViewerInEditorSub(index);
		}

		//エディター上でビュワー描画として呼び出す
		void OnGuiViewerInEditorSub(int index)
		{
			int count = 0;
			foreach (var keyValue in fileTbl)
			{
				if (count == index)
				{
					OnGuiFile(keyValue.Key, keyValue.Value);
					break;
				}
				++count;
			}
		}

		GUILayoutOption paramWidth0 = GUILayout.Width(96);
//		GUILayoutOption paramWidth1 = GUILayout.Width(96);
		GUIStyle style = GUIStyle.none;

		void OnGuiFile(string name, AssetFileBase file)
		{
			Status status = GetFileStatus(file);
			EditorGUILayout.BeginHorizontal();
			{
				style.normal.textColor = GetStatusColor(status);
				style.richText = true;
				GUILayout.Label(status.ToString(), style, paramWidth0);

//				GUILayout.Label("" + file.Version, paramWidth0);
				GUILayout.Label(name);
			}
			EditorGUILayout.EndHorizontal();
		}

		public enum Status
		{
			WaitingToLoad,
			Loading,
			Using,
			Pooling,
			NotLoaded,
			NeedsToCache,
		}
		public Status GetFileStatus(AssetFileBase file)
		{
			if (loadingFileList.Contains(file)) return Status.Loading;
			if (loadWaitFileList.Contains(file)) return Status.WaitingToLoad;
			if (usingFileList.Contains (file)) 
			{
				return file.ReferenceCount > 0 ? Status.Using : Status.Pooling;
			}

			return Status.NotLoaded;
		}

		Color GetStatusColor(Status status)
		{
			switch (status)
			{
				case Status.WaitingToLoad:
					return ColorUtil.Magenta;
				case Status.Loading:
					return ColorUtil.Red;
				case Status.Using:
					return ColorUtil.Green;
				case Status.Pooling:
					return ColorUtil.Lime;
				case Status.NotLoaded:
				default:
					return ColorUtil.Grey;
			}
		}
	}
}
#endif
