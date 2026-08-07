#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Utage
{
    //宴のユーザーごとのエディター設定のエディターウィンドウ
    public class UtageEditorUserSettingsWindow : EditorSettingsWindow<UtageEditorUserSettings>
    {
        [MenuItem("Tools/Utage/Editor User Settings", priority = 700)]
        public static void ShowWindow()
        {
            // 新しいエディタウィンドウを開く
            GetWindow<UtageEditorUserSettingsWindow>("Utage Editor User Settings");
        }
        
        protected override UtageEditorUserSettings GetSetting()
        {
            return UtageEditorUserSettings.GetInstance();
        }

        protected override void OnGUI()
        {
            base.OnGUI();
            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUILayoutUtility.WebLinkHelpBox("Web Document", @"https://madnesslabo.net/utage/?page_id=12230");
        }
    }
}
#endif
