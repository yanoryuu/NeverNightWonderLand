// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Object = UnityEngine.Object;

namespace Utage
{

	/// <summary>
	/// 便利クラス
	/// </summary>
	public class UtageEditorToolKit
	{
		//グループ表示の開始
		public static void BeginGroup(string label)
		{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Space(4f);
			GroupLabel(label);
			GUILayout.Space(4f);
			GUILayout.BeginHorizontal();
			GUILayout.Space(12f);
			EditorGUILayout.BeginVertical();
		}
		
		//グループ名の横に表示するプロパティの指定つき
		public static void BeginGroup(string label, SerializedProperty horizontalProperty )
		{
			EditorGUILayout.BeginVertical("box");
			GUILayout.Space(4f);
			GUILayout.BeginHorizontal();
			GroupLabel(label);
			EditorGUILayout.PropertyField(horizontalProperty, GUIContent.none);
			GUILayout.EndHorizontal();
			GUILayout.Space(4f);
			GUILayout.BeginHorizontal();
			GUILayout.Space(12f);
			EditorGUILayout.BeginVertical();
		}

		//グループ表示の終了
		public static void EndGroup()
		{
			EditorGUILayout.EndVertical();
			GUILayout.Space(4f);
			GUILayout.EndHorizontal();
			GUILayout.Space(4f);
			EditorGUILayout.EndVertical();
		}

		public static void GroupLabel(string label)
		{
			BoldLabel(label);
		}

		//太字のラベル表示
		public static void BoldLabel(string label, params GUILayoutOption[] options)
		{
			GUIStyle style = new GUIStyle("label");
			style.richText = true;
			GUILayout.Label("<b>" + label + "</b>", style, options);
		}

		[Obsolete("Getting serializedObject.FindProperty every time is a bad design that causes bugs in array display, so don't use this method.")]
		public static void PropertyField(SerializedObject serializedObject, string propertyPath, string label, params GUILayoutOption[] options)
		{
			SerializedProperty property = serializedObject.FindProperty(propertyPath);
			if (property == null)
			{
				Debug.LogError(propertyPath + " is Not Found");
			}
			else
			{
				EditorGUILayout.PropertyField(property, new GUIContent(label), options);
			}
		}

		[Obsolete("Getting serializedObject.FindProperty every time is a bad design.")]
		public static void PropertyField(SerializedObject serializedObject, string propertyPath, params GUILayoutOption[] options)
		{
			SerializedProperty property = serializedObject.FindProperty(propertyPath);
			if (property == null)
			{
				Debug.LogError(propertyPath + " is Not Found");
			}
			else
			{
				EditorGUILayout.PropertyField(property, GUIContent.none, options );
			}
		}

		[Obsolete("Use PropertyField instead of this method.")]
		public static void PropertyFieldArray(SerializedObject serializedObject, string propertyPath, string label, params GUILayoutOption[] options)
		{
			SerializedProperty property = serializedObject.FindProperty(propertyPath);
			if (property == null)
			{
				Debug.LogError(propertyPath + " is Not Found");
			}
			else
			{
#if UNITY_2020_2_OR_NEWER
				EditorGUILayout.PropertyField(property, new GUIContent(label), options);
#else
				EditorGUILayout.PropertyField(property, new GUIContent(label), true, options);
#endif
			}
		}
		public static T PrefabField<T>(string title, T currentPrefab) where T : Component
		{
			GameObject asset = (currentPrefab != null) ? currentPrefab.gameObject : null;
			EditorGUILayout.BeginHorizontal();

			GUILayout.Label(title);
			asset = EditorGUILayout.ObjectField(asset, typeof(GameObject), false) as GameObject;

			EditorGUILayout.EndHorizontal();

			T prefabComponent = (asset != null) ? asset.GetComponent<T>() : null;
			return prefabComponent;
		}

		//折りたたみ機能つきの描画
		public static void FoldoutGroup(ref bool foldOunt, string name, System.Action OnGui)
		{
			if (foldOunt = EditorGUILayout.Foldout(foldOunt, name))
			{
				EditorGUI.indentLevel++;
				OnGui();
				EditorGUI.indentLevel--;
			}
		}


		//インポート後のアセット（ScriptableObject）を取得。
		//既にあったらロード。なかったらCreate
		public static T GetImportedAssetCreateIfMissing<T>(string path) where T : ScriptableObject
		{
			var asset = AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
			if (asset == null)
			{
				asset = ScriptableObject.CreateInstance<T>() as T;
				AssetDatabase.CreateAsset(asset, path);
			}
			return asset;
		}

		//インポート後のアセット（Object）を取得。
		//既にあったらロード。なかったらCreate
		public static T GetImportedAssetObjectCreateIfMissing<T>(string path) where T : Object, new()
		{
			var asset = AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
			if (asset == null)
			{
				asset = new T();
				AssetDatabase.CreateAsset(asset, path);
			}
			return asset;
		}

		//フォルダのアセットをロード。なかったらCreate
		public static Object GetFolderAssetCreateIfMissing(string parentFolder, string newFolderName)
		{
			string path = FilePathUtil.Combine(parentFolder, newFolderName);
			var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
			if (asset == null)
			{
				AssetDatabase.CreateFolder(parentFolder, newFolderName);
				return AssetDatabase.LoadAssetAtPath<Object>(path);
			}
			return asset;
		}

		public static T CreateNewUniqueAsset<T>() where T : ScriptableObject
		{
			string path = GetSelectedDirectory();
			string typeName = typeof(T).ToString();

			//ネームスペース対策
			if( typeName.Contains(".") )
			{
				int index = typeName.LastIndexOf('.') + 1;
				typeName = typeName.Substring( index, typeName.Length -index );
			}
			path += "/New " + typeName + ".asset";
			return CreateNewUniqueAsset<T>(path);
		}

		public static T CreateNewUniqueAsset<T>(string path) where T : ScriptableObject
		{
			path = AssetDatabase.GenerateUniqueAssetPath(path);
			T asset = ScriptableObject.CreateInstance<T>() as T;
			AssetDatabase.CreateAsset(asset, path);
			EditorUtility.SetDirty(asset);
			return asset;
		}

		//選択中のディレクトリ名
		public static string GetSelectedDirectory()
		{
			string path = "";
			foreach (var obj in Selection.objects)
			{
				path = AssetDatabase.GetAssetPath(obj);
				if (!string.IsNullOrEmpty(path) && !System.IO.Directory.Exists(path))
				{
					path = System.IO.Path.GetDirectoryName(path);
				}

				break;
			}

			if (string.IsNullOrEmpty(path))
			{
				return "Assets";
			}

			return path;
		}

		/// <summary>
		/// アセットリストからファイルパスのリストを取得
		/// </summary>
		/// <param name="assets">アセットのリスト</param>
		/// <returns>ファイルパスのリスト</returns>
		public static List<string> AssetsToPathList( List<Object> assets )
		{
			List<string> pathList = new List<string>();
			foreach (var asset in assets)
			{
				pathList.Add(AssetDatabase.GetAssetPath(asset));
			}
			return pathList;
		}

		/// <summary>
		/// アセットの拡張子をチェック
		/// </summary>
		/// <param name="asset">アセット</param>
		/// <param name="extensions">チェックする拡張子</param>
		/// <returns>指定の拡張子があればtrue。なければfalse</returns>
		public static bool CheckAssetExtension(Object asset, params string[] extensions )
		{
			string path = AssetDatabase.GetAssetPath(asset);
			string ext = System.IO.Path.GetExtension(path).ToLower();
			foreach( var extension in extensions )
			{
				if( ext == extension.ToLower() )
				{
					return true;
				}
			}
			return false;
		}

		//AssetDatabaseなどで使うAssets以下の相対パスを、System.IO系でも使えるフルパスに変換する
		public static string AssetPathToSystemIOFullPath(string assetPath)
		{
			return Application.dataPath.Remove( Application.dataPath.LastIndexOf("Assets")) + assetPath;
		}

		//System.IO系などで使うフルパスを、AssetDatabaseなどで使うAssets以下の相対パスに直す。
		public static string SystemIOFullPathToAssetPath(string fullPath)
		{
			string path= FileUtil.GetProjectRelativePath(fullPath.Replace(@"\", @"/"));
			//もともと相対パスなら空文字が返ってくる
			return string.IsNullOrEmpty(path) ? fullPath : path;
		}
		
		//すべてのウィンドウを取得
		public static List<EditorWindow> GetAllEditorWindow()
		{
			List<EditorWindow> allWindows = new List<EditorWindow>();
			foreach (EditorWindow window in Resources.FindObjectsOfTypeAll(typeof(EditorWindow)) as EditorWindow[])
			{
//				Debug.Log( window.title );
				allWindows.Add(window);
			}
			return allWindows;
		}
	}
}
#endif
