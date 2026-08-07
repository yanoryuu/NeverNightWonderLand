// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{
    //SerializableなClassを折りたたまずに展開して表示するアトリビュート
    public class UnfoldedSerializableAttribute : PropertyAttribute
    {
#if UNITY_EDITOR
        // [UnfoldedSerializable]を表示するためのプロパティ拡張
        [CustomPropertyDrawer(typeof(UnfoldedSerializableAttribute))]
        class Drawer : PropertyDrawerEx<UnfoldedSerializableAttribute>
        {
            //高さが変わるので、overrideして計算
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                float height = EditorGUIUtility.singleLineHeight;
                //子要素の高さ
                height += Helper.GetPropertyHeightChildren(property);
                return height;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                //引数のpositionは、子要素含めた全体の高さを計算済みの矩形になっているので
                //描画要素ごとに矩形を計算しなおしていく
                float height = EditorGUIUtility.singleLineHeight;
                position.height = height;
                EditorGUI.LabelField(position, label);
                position.y += height; 
                
                //子要素の描画
                Helper.DrawChildrenWithIndent(position, property);
            }

        }
#endif
    }
}
