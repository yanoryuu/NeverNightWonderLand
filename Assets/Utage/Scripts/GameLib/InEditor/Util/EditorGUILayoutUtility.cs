#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
    public static class EditorGUILayoutUtility
    {
        public static T ObjectField<T>(string label, T obj)
            where T : UnityEngine.Object
        {
            return EditorGUILayout.ObjectField(label, obj, typeof(T), false) as T;
        }

        public static void ObjectField<T>(string label, T obj, Action<T> onChanged)
            where T : UnityEngine.Object
        {
            var tmp = ObjectField(label, obj);
            if (tmp != obj)
            {
                onChanged(tmp);
            }
        }

        //Webリンクのヘルプボックス
        public static void WebLinkHelpBox(string message, string url, string linkLabel = "")
        {
            //一行追加することで、URLリンクテキストを最終行にダミー表示
            message += "\n";
            EditorGUILayout.HelpBox(message, MessageType.Info);
            GUILayout.Space(-EditorGUIUtility.singleLineHeight-4);
            GUILayout.BeginHorizontal();
            // 右に寄せるためのスペースを作成
            GUILayout.Space(38);
            WebLinkText(url, linkLabel);
            GUILayout.EndHorizontal();
        }

        //Webリンクのテキストボタン表示
        public static void WebLinkText(string url, string linkLabel = "")
        {
            if (linkLabel.IsNullOrEmpty())
            {
                linkLabel = url;
            }

            //マウスカーソルを変化
            if (GUILayout.Button(linkLabel, EditorStyles.linkLabel))
            {
                Application.OpenURL(url);
            }
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
        }
        
        //ディレクトリのパスと、選択ダイアログを開くUIを表示する
        public static string DirectoryPathFiled(string filePath, string label, string dialogTitle)
        {
            using (new GUILayout.HorizontalScope())
            {
                filePath = EditorGUILayout.TextField(label,filePath);
                if (GUILayout.Button("...", GUILayout.Width(20)))
                {
                    string dir = Path.GetDirectoryName(filePath);
                    string name = Path.GetFileNameWithoutExtension(filePath);
                    var path = EditorUtility.OpenFolderPanel(dialogTitle, dir, name);
                    if (!string.IsNullOrEmpty(path))
                    {
                        filePath = path;
                    }
                }
                return filePath;
            }
        }

    }
}
#endif
