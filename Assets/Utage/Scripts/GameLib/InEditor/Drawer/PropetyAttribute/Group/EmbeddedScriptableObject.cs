// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{
    //参照先のScriptableObjectを埋め込み表示するアトリビュート
    //インスペクターから直接ScriptableObjectを編集可能にするためもの
    public class EmbeddedScriptableObjectAttribute : PropertyAttribute
    {
        bool EditableReference { get; } //ScriptableObjectの参照先をインスペクターから編集可能にするか

        public EmbeddedScriptableObjectAttribute()
            : this(true)
        {
        }

        public EmbeddedScriptableObjectAttribute(bool editableReference)
        {
            EditableReference = editableReference;
        }

#if UNITY_EDITOR
        // [EmbeddedScriptableObjectAttribute]を表示するためのプロパティ拡張
        [CustomPropertyDrawer(typeof(EmbeddedScriptableObjectAttribute))]
        class Drawer : PropertyDrawerEx<EmbeddedScriptableObjectAttribute>
        {
            //高さが変わるので、overrideして計算
            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                float height = 0;
                if (Attribute.EditableReference)
                {
                    //参照先を変更編集可能な場合はいったん通常描画
                    height += EditorGUI.GetPropertyHeight(property);
                }
                else
                {
                    //変更不可能な場合は、ラベルのみ表示
                    height += EditorGUIUtility.singleLineHeight;
                    if (property.objectReferenceValue == null)
                    {
                        //エラーメッセージが表示される
                        height += EditorGUIUtility.singleLineHeight;
                    }
                }

                if (property.objectReferenceValue == null)
                {
                    //参照設定がまだなかったら描画終了
                    return height;
                }
                
                SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue);
                using var changeCheck = new EditorGUI.ChangeCheckScope();
                var iter = serializedObject.GetIterator();
                // 最初の一個は省略
                iter.NextVisible(true);
                // 残りのプロパティをすべて描画する
                while (iter.NextVisible(false))
                {
                    height += EditorGUI.GetPropertyHeight(iter);
                }
                return height;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                if (Attribute.EditableReference)
                {
                    //参照先を変更編集可能な場合はいったん通常描画
                    Helper.DrawPropertyFieldAutoRect(property, ref position);
                }
                else
                {
                    //変更不可能な場合は、ラベルのみ表示
                    Helper.DrawLabelFieldAutoRect(property.displayName, ref position);
                    if (property.objectReferenceValue == null)
                    {
                        string errorMsg = "Reference is null";
                        var styles = EditorStyles.label;
                        styles.richText = true;
                        Helper.DrawLabelFieldAutoRect( ColorUtil.ToColorTagErrorMsg(errorMsg), ref position, styles);
                    }
                }
                
                if( property.objectReferenceValue ==null)
                {
                    //参照設定がまだなかったら描画終了
                    return;
                }
                
                //参照先のScriptableObjectを展開して描画
                using (new EditorGUI.IndentLevelScope())
                {
                    SerializedObject serializedObject = new SerializedObject(property.objectReferenceValue);
                    using var changeCheck = new EditorGUI.ChangeCheckScope();
                    var iter = serializedObject.GetIterator();
                    // 最初の一個は省略
                    iter.NextVisible(true);

                    // 残りのプロパティをすべて描画する
                    while (iter.NextVisible(false))
                    {
                        Helper.DrawPropertyFieldAutoRect(iter, ref position);
                    }

                    if (changeCheck.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                    }
                }
            }

        }
#endif
    }
}
