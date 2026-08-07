#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    //エディタ設定(EditorSettingsSingletonを継承するセーブ・ロード可能なオブジェクト)の、内容を表示するエディタウィンドウ
    public abstract class EditorSettingsWindow<T>
        : EditorWindow
        where T : EditorSettingsSingleton<T>
    {

        protected abstract T GetSetting();

        protected Editor SettingEditor
        {
            get
            {
                if (settingEditor == null)
                {
                    settingEditor = Editor.CreateEditor(GetSetting());
                }
                return settingEditor;
            }
            
        }
        Editor settingEditor;

        protected virtual void OnGUI()
        {
            T setting = GetSetting();
            if (setting == null)
            {
                Debug.LogError($"{nameof(T)} is missing");
                return;
            }

            if (SettingEditor.DrawInspectorAllProperties())
            {
                setting.OnSave();
            }
        }
    }
}
#endif
