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
	//[SerializeFiled]なフィールドを自動でGUI表示するエディタウィンドウ（セーブ機能なし）
	public abstract class EditorWindowNoSave : EditorWindow
	{
		protected Editor Editor { get; set; }

		protected virtual void OnEnable()
		{
			Editor = Editor.CreateEditor(this);
		}
		
		//描画更新
		protected virtual void OnGUI()
		{
			Editor.DrawInspectorAllProperties();
		}
	}
}
#endif
