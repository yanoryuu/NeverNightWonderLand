// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Utage
{
	public static class LayerMaskEditor
	{
		//レイヤー名を追加
		public static bool TryAddLayerName(string layerName)
		{
			if (string.IsNullOrEmpty(layerName)) return false;

			SerializedObject tagManager = LoadTagManger();
			SerializedProperty layers = GetLayers(tagManager);

			//すでにレイヤー名があったら何もしない
			for (int i = 0; i < layers.arraySize; i++)
			{
				SerializedProperty it = layers.GetArrayElementAtIndex(i);
				if (it.stringValue == layerName)
				{
					return false;
				}
			}

			//ユーザー設定のレイヤー名が空欄だったら、そこに追加
			const int userLayerIndex = 8;
			for (int i = userLayerIndex; i < layers.arraySize; i++)
			{
				SerializedProperty it = layers.GetArrayElementAtIndex(i);
				if (string.IsNullOrEmpty(it.stringValue))
				{
					it.stringValue = layerName;
					break;
				}
			}
			tagManager.ApplyModifiedProperties();
			return true;
		}

		//レイヤー名一覧を取得
		public static List<string> GetAllLayerNames()
		{
			SerializedProperty layers = GetLayers();

			List<string> layerNames = new List<string>();
			for (int i = 0; i < layers.arraySize; i++)
			{
				SerializedProperty it = layers.GetArrayElementAtIndex(i);
				if (!string.IsNullOrEmpty(it.stringValue))
				{
					layerNames.Add(it.stringValue);
				}
			}
			return layerNames;
		}


		//レイヤー名があるかチェック
		public static bool ContainsInLayerNames(string layerName)
		{
			List<string> layerNames = GetAllLayerNames();
			return layerNames.Contains(layerName);
		}

		//タグマネージャーをロード
		internal static SerializedObject LoadTagManger()
		{
			return new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
		}
		//レイヤープロパティを取得
		internal static SerializedProperty GetLayers()
		{
			return GetLayers(LoadTagManger());
		}
		//レイヤープロパティを取得
		internal static SerializedProperty GetLayers(SerializedObject tagManager)
		{
			SerializedProperty layers = tagManager.FindProperty("layers");
			if (layers == null || !layers.isArray)
			{
				Debug.LogError("Layers is not found. Maybe Unity version error");
			}
			return layers;
		}
	}
}

#endif
