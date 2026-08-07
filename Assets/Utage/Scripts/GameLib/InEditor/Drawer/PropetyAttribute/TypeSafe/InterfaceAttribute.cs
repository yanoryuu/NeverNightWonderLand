// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace Utage
{
	/// <summary>
	/// [Interface]アトリビュート
	/// 指定のインターフェースを持つコンポーネントのみを登録する
	/// </summary>
	public class InterfaceAttribute : PropertyAttribute
	{
		public System.Type Type { get; }

		public InterfaceAttribute(System.Type type)
		{
			this.Type = type;
		}

#if UNITY_EDITOR
		// [Interface]表示のためのプロパティ描画
		[CustomPropertyDrawer(typeof(InterfaceAttribute))]
		class Drawer : PropertyDrawerEx<InterfaceAttribute>
		{
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				Object obj = EditorGUI.ObjectField(position, label, property.objectReferenceValue, typeof(GameObject),
					true);
				if (obj != property.objectReferenceValue)
				{
					if (obj != null)
					{
						GameObject go = obj as GameObject;
						if (go.GetComponent(Attribute.Type) == null)
						{
							//指定のInterfaceを持たないアセットは設定しない
							return;
						}
					}

					property.objectReferenceValue = obj;
				}
			}
		}
#endif
	}

}
