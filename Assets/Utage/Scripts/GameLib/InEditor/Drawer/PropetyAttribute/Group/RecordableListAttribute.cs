// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Utage
{
	//Unityの配列表示がReorderableListじゃなかったころのアトリビュート
	//互換性維持のために残してるだけで基本的には使わない
	[System.Serializable]
	public class ReorderableList<T>
	{
		public List<T> List { get { return list; } }
		[SerializeField]
		List<T> list = new List<T>();

#if UNITY_EDITOR
		public class ReorderableListDrawer : PropertyDrawerEx
		{
			protected override GuiDrawerHelper Helper { get; }

			public ReorderableListDrawer()
			{
				Helper = new GuiDrawerHelper<ReorderableListDrawer>(this);
			}

			SerializedProperty listProperty;
			ReorderableList reorderableList;

			public override void OnGUI(Rect rect, SerializedProperty serializedProperty, GUIContent label)
			{
				if (TryInitIfMissing(serializedProperty, label))
				{
					float height = 0f;
					for (var i = 0; i < listProperty.arraySize; i++)
					{
						height = Mathf.Max(height, EditorGUI.GetPropertyHeight(listProperty.GetArrayElementAtIndex(i)));
					}
					reorderableList.elementHeight = height;
					reorderableList.DoList(rect);
				}
				else
				{
					base.OnGUI(rect, serializedProperty, label);
				}
			}

			public override float GetPropertyHeight(SerializedProperty serializedProperty, GUIContent label)
			{
				if (TryInitIfMissing(serializedProperty, label))
				{
					return reorderableList.GetHeight();
				}
				else
				{
					return base.GetPropertyHeight(serializedProperty, label);
				}
			}

			bool TryInitIfMissing(SerializedProperty serializedProperty, GUIContent label)
			{
				if (reorderableList == null)
				{
					listProperty = serializedProperty.FindPropertyRelative("list");
					if (listProperty == null)
					{
						Debug.LogError("list is not found");
					}
					else
					{
						string headerText = label.text;
						reorderableList = new ReorderableList(listProperty.serializedObject, listProperty );
						reorderableList.drawHeaderCallback = (rect) => {
							EditorGUI.LabelField(rect, headerText);
						};

						reorderableList.drawElementCallback = (rect, index, isActive, isFocused) => {
							var property = listProperty.GetArrayElementAtIndex(index);
							EditorGUI.indentLevel++;
							EditorGUI.PropertyField(rect, property, true);
							EditorGUI.indentLevel--;
						};
					}
				}
				return reorderableList != null;
			}

		}
#endif
	}
}
