#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Utage
{
    //宴のプロジェクトで共有するエディター設定のエディターウィンドウ
    public class UtageEditorProjectSettingsWindow : EditorSettingsWindow<UtageEditorProjectSettings>
    {
        [MenuItem("Tools/Utage/Editor Project Settings", priority = 700)]
        public static void ShowWindow()
        {
            // 新しいエディタウィンドウを開く
            GetWindow<UtageEditorProjectSettingsWindow>("Utage Editor Project Settings");
        }
        
        protected override UtageEditorProjectSettings GetSetting()
        {
            return UtageEditorProjectSettings.GetInstance();
        }

        protected override void OnGUI()
        {
            using (new EditorGuiLabelWidthScope(220))
            {
                base.OnGUI();
            }
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUILayoutUtility.WebLinkHelpBox("Web Document", @"https://madnesslabo.net/utage/?page_id=12234");
        }

    }
}
#endif
