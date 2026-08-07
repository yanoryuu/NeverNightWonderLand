// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{
	/// <summary>
	/// [Min]アトリビュート
	/// 入力可能な最小値を設定する
	/// </summary>
	public class MinAttribute : PropertyAttribute
	{
		public float Min { get; }
		public MinAttribute(float min)
		{
			Min = min;
		}
	}

#if UNITY_EDITOR

	[CustomPropertyDrawer(typeof(MinAttribute))]
	class MinDrawer : PropertyDrawerEx<MinAttribute>
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.propertyType == SerializedPropertyType.Float)
			{
				EditorGUI.PropertyField(position, property, label);
				if (property.floatValue < Attribute.Min)
				{
					property.floatValue = Attribute.Min;
				}
			}
			else if (property.propertyType == SerializedPropertyType.Integer)
			{
				EditorGUI.PropertyField(position, property, label);
				if (property.intValue < Attribute.Min)
				{
					property.intValue = (int)Attribute.Min;
				}
			}
			else
			{
				Debug.LogError(label + "cant use MinAttribute. Please us int or float type");
			}
		}
	}
#endif
}
