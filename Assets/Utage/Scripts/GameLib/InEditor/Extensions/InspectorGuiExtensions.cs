#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace UtageExtensions
{
    //インスペクター表示の際の拡張メソッド
    public static class InspectorGuiExtensions
    {
        //指定のserializedObjectの全てのプロパティのインスペクター表示する
        public static bool DrawInspectorAllProperties(this SerializedObject serializedObject)
        {
            using var changeCheck = new EditorGUI.ChangeCheckScope();
            serializedObject.UpdateIfRequiredOrScript();
            SerializedProperty iterator = serializedObject.GetIterator();
            for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                // スクリプトは表示しない
                if ("m_Script" == iterator.propertyPath) continue;
                // シリアライズデータモードは表示しない(UiToolKitがらみで、Unity2022から内部状態が表示されてしまうのを避けるため)
                if ("m_SerializedDataModeController" == iterator.propertyPath) continue;

                EditorGUILayout.PropertyField(iterator, true);
            }

            serializedObject.ApplyModifiedProperties();
            return changeCheck.changed;
        }

        //指定のeditorの全てのプロパティのインスペクター表示する
        //editor.OnInspectorGUIと違い、インスペクター表示をCustomEditorで拡張している場合、そのGUIは呼ばれない。拡張前のデフォルトのGUIが呼ばれる
        //editor.DrawDefaultInspectorと違い、Script部分の表示は省略される
        public static bool DrawInspectorAllProperties(this Editor editor)
        {
            return editor.serializedObject.DrawInspectorAllProperties();
        }

        //指定のeditorのインスペクター表示
        //drawCustom=trueの場合、CustomEditorで拡張している場合、そのGUIを呼ぶ
        //includeScript=trueの場合、Script部分も表示する
        public static void DrawInspector(this Editor editor, bool includeScript, bool drawCustom)
        {
            if (drawCustom)
            {
                editor.OnInspectorGUI();
            }
            else if (includeScript)
            {
                editor.DrawDefaultInspector();
            }
            else
            {
                editor.DrawInspectorAllProperties();
            }
        }
    }
}
#endif
