#if UNITY_EDITOR

using System;
using UnityEditor;

namespace Utage
{
    //GUIラベルの幅（EditorGUIUtility.labelWidth）を一時的に変化させるためのスコープ
    public class EditorGuiLabelWidthScope : IDisposable
    {
        float DefaultWidth { get; }

        public EditorGuiLabelWidthScope(int labelWidth)
        {
            DefaultWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = labelWidth;
        }

        public void Dispose()
        {
            EditorGUIUtility.labelWidth = DefaultWidth;
        }
    }
}
#endif
