// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace Utage
{

#if UNITY_EDITOR
	//アバター表示のスプライトを置いたフォルダデータ
	[System.Serializable]
	public class AvatorFolderData
	{
		[Folder]
		public Object folder;
		[StringPopupFunction("TagList")]
		public string tag;

		/// <summary>
		/// [AvatorFolderData]表示のためのプロパティ描画
		/// </summary>
		[CustomPropertyDrawer(typeof(AvatorFolderData))]
		public class Drawer : PropertyDrawerEx
		{
			protected override GuiDrawerHelper Helper { get; }

			public Drawer()
			{
				Helper = new GuiDrawerHelper<Drawer>(this);
			}
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				Helper.DrawHorizontalChildren(position, property, label);
			}
		}

		public List<T> GetAllAssets<T>() where T : Object
		{
			List<T> assets = new List<T>();
			if (folder == null) return assets;
			string assetPath = AssetDatabase.GetAssetPath(folder);

			//重複を避けるためにHashSetを使う
			HashSet<string> guids = new HashSet<string>(AssetDatabase.FindAssets("", new[] { assetPath }));
			foreach (string guid in guids)
			{
				var obj = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
				if (obj != null) assets.Add(obj);
			}
			return assets;
		}

		internal void ReimportResources(string spritePackingTag)
		{
			List<Texture> assets = GetAllAssets<Texture>();
			foreach (var asset in assets)
			{
				OverrideTextureImportSetting(AssetDatabase.GetAssetPath(asset), spritePackingTag);
			}
		}

		//元画像のテクスチャインポート設定を上書き
		static void OverrideTextureImportSetting(string path, string spritePackingTag)
		{
			var importer = AssetImporter.GetAtPath(path) as TextureImporter;

			bool hasChanged = TryOverrideTextureImportSetting(importer, spritePackingTag);
			if (hasChanged)
			{
				importer.SaveAndReimport();
			}
		}

		//元画像のテクスチャインポート設定を上書き
		static bool TryOverrideTextureImportSetting(TextureImporter importer, string spritePackingTag)
		{
			bool hasChanged = false;
			//スプライトに
			if (importer.textureType != TextureImporterType.Sprite)
			{
				importer.textureType = TextureImporterType.Sprite;
				hasChanged = true;
			}
#if !UNITY_2021_1_OR_NEWER
			//スプライトタグ
			if (importer.spritePackingTag != spritePackingTag)
			{
				importer.spritePackingTag = spritePackingTag;
				hasChanged = true;
			}
#endif
			if (importer.isReadable != false)
			{
				importer.isReadable = false;
				hasChanged = true;
			}
			//MipMapはオフに
			if (importer.mipmapEnabled != false)
			{
				importer.mipmapEnabled = false;
				hasChanged = true;
			}

#if UNITY_5_5_OR_NEWER
			//True Color
			if (importer.textureCompression != TextureImporterCompression.Uncompressed)
			{
				importer.textureCompression = TextureImporterCompression.Uncompressed;
				hasChanged = true;
			}
#else
			//True Color
			if (importer.textureFormat != TextureImporterFormat.AutomaticTruecolor)
			{
				importer.textureFormat = TextureImporterFormat.AutomaticTruecolor;
				hasChanged = true;
			}
#endif
			
#if false
			//テクスチャサイズの設定
			if (importer.maxTextureSize != 2048)
			{
				importer.maxTextureSize = 2048;
				hasChanged = true;
			}
#endif
			//Non Power of 2
			if (importer.npotScale != TextureImporterNPOTScale.None)
			{
				importer.npotScale = TextureImporterNPOTScale.None;
				hasChanged = true;
			}

			return hasChanged;
		}

	}

	[System.Serializable]
	public class AvatorFolderDataList : ReorderableList<AvatorFolderData>
	{
		/// <summary>
		/// プロパティ描画
		/// </summary>
		[CustomPropertyDrawer(typeof(AvatorFolderDataList))]
		class ResouceDataListDrawer : ReorderableListDrawer { }

		internal void ReimportResources(string spritePackingTag)
		{
			for (int i = 0; i < List.Count; ++i)
			{
				string info = string.Format("{0}/{1}", i + 1, List.Count);
				EditorUtility.DisplayProgressBar("Reimport Sprites", info, 1.0f * i / List.Count);
				List[i].ReimportResources(spritePackingTag);
			}
			EditorUtility.ClearProgressBar();
		}
	}
#endif
}
