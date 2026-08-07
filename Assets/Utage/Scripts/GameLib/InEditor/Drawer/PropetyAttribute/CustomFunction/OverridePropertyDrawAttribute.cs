// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Utage
{
    // <summary>
    // [OverridePropertyDraw]アトリビュート
    // プロパティの描画をコンポーネントやScriptableObjectの定義クラス内でオーバーライドする
    // </summary>
    public class OverridePropertyDrawAttribute : PropertyAttribute
    {
        GuiDrawerFunction Function { get; }
        public OverridePropertyDrawAttribute(string function, bool nested = false)
        {
            Function = new GuiDrawerFunction(function,nested);
        }


#if UNITY_EDITOR
        /// [OverridePropertyDraw]を表示するためのプロパティ拡張
        [CustomPropertyDrawer(typeof(OverridePropertyDrawAttribute))]
        class Drawer : PropertyDrawerEx<OverridePropertyDrawAttribute>
        {
            // Draw the property inside the given rect
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                object[] args = { position, property, label };
                //メソッド呼び出し
                Helper.CallFunction<List<string>>(property, Attribute.Function, args);
            }
        }
#endif
    }

}
