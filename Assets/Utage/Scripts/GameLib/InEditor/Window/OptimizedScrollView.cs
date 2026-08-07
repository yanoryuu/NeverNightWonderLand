// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Utage
{
	//高速化したスクロール表示のためのクラス
	public class OptimizedScrollView
	{
		public string Name { get; protected set; }
		public bool EnableFoldout { get; set; }
		public bool Foldout { get { return this.foldout; } set { this.foldout = value; } }
		bool foldout;

		public CustomEditorWindow Window { get; protected set; }
		public float LineHeight { get; protected set; }
		public float CellHeight { get; protected set; }
		public float RepaintY { get; protected set; }

		public OptimizedScrollView( string name, CustomEditorWindow window, float lineHeight, float cellHeight )
		{
			this.Name = name;
			this.EnableFoldout = true;
			this.Foldout = true;

			this.Window = window;
			this.LineHeight = lineHeight;
			this.CellHeight = cellHeight;
		}

		public void OnGui(int itemCount, Action<int> OnDrawItem)
		{
			OnGui(itemCount, null, OnDrawItem);
		}
		public void OnGui(int itemCount, Action OnDrawHeader, Action<int> OnDrawItem)
		{
			if (EnableFoldout)
			{
				UtageEditorToolKit.FoldoutGroup(ref foldout, Name, () => OnGuiSub(itemCount, OnDrawHeader, OnDrawItem));
			}
			else
			{
				OnGuiSub(itemCount, OnDrawHeader, OnDrawItem);
			}
		}

		void OnGuiSub(int itemCount, Action OnDrawHeader, Action<int> OnDrawItem)
		{
			if (OnDrawHeader != null) OnDrawHeader();
			float currentY = RepaintY - Window.ScrollPosition.y;
			if (Event.current.type == EventType.Repaint)
			{
				RepaintY = GUILayoutUtility.GetLastRect().yMax;
			}
			GUILayout.BeginVertical();
			int index = 0;

			//スクロールがウィンドウの範囲内の描画処理
			while (index < itemCount)
			{
				if (currentY < -LineHeight * 2)
				{
					//スクロールがウィンドウの範囲内に来た
					EditorGUILayout.GetControlRect(false, CellHeight);
				}
				else if (currentY > Window.position.height + LineHeight * 2)
				{
					//スクロールのウィンドウの範囲外（下部）に来た
					EditorGUILayout.GetControlRect(false, CellHeight);
				}
				else
				{
					OnDrawItem(index);
				}
				currentY += LineHeight;
				++index;
			}

			GUILayout.EndVertical();
		}
	}
}
#endif
