// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace Utage
{
    /// <summary>
    /// [PathDialog]アトリビュート
    /// パス用の文字列を、選択ダイアログ用のボタンつきで表示する
    /// </summary>
    public class PathDialogAttribute : PropertyAttribute
    {
        public enum DialogType
        {
            Directory,
            File,
            SaveFile,
        };

        public DialogType Type { get; }
        public string Extension { get; }
        public string DefaultName { get; }

        public PathDialogAttribute(DialogType type)
            : this(type, "", "")
        {
        }

        public PathDialogAttribute(DialogType type, string extension)
            : this(type, extension, "")
        {
        }

        public PathDialogAttribute(DialogType type, string extension, string defaultName)
        {
            this.Type = type;
            this.Extension = extension;
            this.DefaultName = defaultName;
        }

#if UNITY_EDITOR

        // [PathDialog]表示のためのプロパティ描画
        [CustomPropertyDrawer(typeof(PathDialogAttribute))]
        public class Drawer : PropertyDrawerEx<PathDialogAttribute>
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                const float buttonWide = 64.0f;
                const float space = 8.0f;
                position.width -= buttonWide + space;
                EditorGUI.PropertyField(position, property, new GUIContent(property.displayName));
                position.x = position.xMax + space;
                position.width = buttonWide;
                if (GUI.Button(position, "Select"))
                {
                    string path = property.stringValue;
                    string dir = string.IsNullOrEmpty(path) ? "" : Path.GetDirectoryName(path);
                    string name = string.IsNullOrEmpty(path) ? "" : Path.GetFileName(path);
                    switch (Attribute.Type)
                    {
                        case PathDialogAttribute.DialogType.Directory:
                            path = EditorUtility.OpenFolderPanel("Select Directory", dir, name);
                            break;
                        case PathDialogAttribute.DialogType.File:
                            path = EditorUtility.OpenFilePanel("Select File", dir, Attribute.Extension);
                            break;
                        case PathDialogAttribute.DialogType.SaveFile:
                            path = EditorUtility.SaveFilePanel("Save File", dir, Attribute.DefaultName, Attribute.Extension);
                            break;
                        default:
                            Debug.LogError("Unknown Type");
                            break;
                    }

                    if (!string.IsNullOrEmpty(path))
                    {
                        property.stringValue = path;
                        //ダイアログ呼び出しの後は、ApplyModifiedPropertiesを呼ばないと、値が反映されない
                        property.serializedObject.ApplyModifiedProperties();
                    }
                    //ダイアログ呼び出しの後は、GUIUtility.ExitGUIしないとエラーになる
                    GUIUtility.ExitGUI();
                }
            }
        }

#endif
    }
}
