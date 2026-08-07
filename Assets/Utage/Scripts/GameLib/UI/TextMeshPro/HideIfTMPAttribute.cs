// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{
    // [HideIfTMP]アトリビュート
    // TextMeshProを使用するコンポーネントの場合は非表示にする
    public class HideIfTMPAttribute : PropertyAttribute
    {
		public HideIfTMPAttribute()
        {
        }

#if UNITY_EDITOR
		// 表示するためのプロパティ拡張
		[CustomPropertyDrawer(typeof(HideIfTMPAttribute))]
		class Drawer : PropertyDrawerEx<HideIfTMPAttribute>
		{
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				if (!IsHide(property))
				{
					EditorGUI.PropertyField(position, property, label);
				}
			}

			bool IsHide(SerializedProperty property)
			{
				return (property.serializedObject.targetObject is IUsingTextMeshPro);
			}

			public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
			{
				if (IsHide(property))
				{
					return 0;
				}
				else
				{
					return base.GetPropertyHeight(property, label);
				}
			}
		}
#endif

    }

}
