// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using System;
using System.Text;
using UnityEditor;
#endif

namespace Utage
{
    // [HideIfTMP]アトリビュート
    // レガシーなTextを使用するコンポーネントの場合は非表示にする
    public class HideIfLegacyTextAttribute : PropertyAttribute
    {
		public HideIfLegacyTextAttribute()
        {
        }

#if UNITY_EDITOR
		// 表示するためのプロパティ拡張
		[CustomPropertyDrawer(typeof(HideIfLegacyTextAttribute))]
		class Drawer : PropertyDrawerEx<HideIfLegacyTextAttribute>
		{
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				if (!IsHide(property))
				{
					string displayName = StringUtil.RemoveSuffix(property.displayName,"TMP");
					EditorGUI.PropertyField(position, property, new GUIContent(displayName));
				}
			}

			bool IsHide(SerializedProperty property)
			{
				return (property.serializedObject.targetObject is not IUsingTextMeshPro);
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
