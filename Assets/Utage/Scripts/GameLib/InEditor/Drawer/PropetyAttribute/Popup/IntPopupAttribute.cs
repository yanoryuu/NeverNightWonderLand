// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{
    /// <summary>
    /// [IntPopup]アトリビュート
    /// 指定の整数リストから一つを選択するポップアップを表示する
    /// </summary>
    public class IntPopupAttribute : PropertyAttribute
    {
        public List<int> PopupList { get; } = new ();

        public IntPopupAttribute(params int[] args)
        {
			PopupList.AddRange(args);
        }

#if UNITY_EDITOR
        // [IntPopup]を表示するためのプロパティ拡張
        [CustomPropertyDrawer(typeof(IntPopupAttribute))]
        class Drawer : PropertyDrawerEx<IntPopupAttribute>
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                List<int> popupList = Attribute.PopupList;
                if (popupList == null || popupList.Count <= 0)
                {
                    EditorGUI.PropertyField(position, property, label);
                }
                else
                {
                    int old = property.intValue;
                    int index = popupList.FindIndex(x => x == old);
                    if (index < 0) index = 0;
                    index = EditorGUI.Popup(position, label.text, index, ToStringArray(popupList));
                    property.intValue = popupList[index];
                }
            }

            string[] ToStringArray(List<int> list)
            {
                string[] array = new string[list.Count];
                for (int i = 0; i < list.Count; ++i)
                {
                    array[i] = list[i].ToString();
                }

                return array;
            }
        }
#endif
    }
}
