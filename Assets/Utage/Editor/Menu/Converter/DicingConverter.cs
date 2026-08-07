// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Utage
{

	//ダイシング素材のリソースコンバーター
	public class DicingConverter : CustomEditorWindow
	{
		[System.Serializable]
		class DicingFolderData
		{
			// リソースのパス
			[SerializeField]
			Object input;
			public Object InputFolder
			{
				get { return input; }
				set { input = value; }
			}

			// 出力先(SerializedObject)のパス
			[SerializeField]
			Object output1;
			public Object OuputFolder1
			{
				get { return output1; }
				set { output1 = value; }
			}

			/// 出力先のパス（Texture）のパス
			[SerializeField]
			Object output2;
			public Object OuputFolder2
			{
				get { return output2; }
				set { output2 = value; }
			}

			/// <summary>
			/// [DicingFolderData]表示のためのプロパティ描画
			/// </summary>
			[CustomPropertyDrawer(typeof(DicingFolderData))]
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
		}

		[System.Serializable]
		class DicingFolderDataList : ReorderableList<DicingFolderData>
		{
			/// <summary>
			/// プロパティ描画
			/// </summary>
			[CustomPropertyDrawer(typeof(DicingFolderDataList))]
			class DicingFolderDataListDrawer : ReorderableListDrawer { }

			internal bool IsEnableOutputResources
			{
				get
				{
					if (this.List == null) return false;
					return this.List.TrueForAll
						(x =>
						x.InputFolder != null && x.OuputFolder1 != null && x.OuputFolder2 != null);
				}
			}
			//ファイルのコンバート
			internal void Build(bool isRebuild)
			{
				this.List.ForEach(x => Build(isRebuild,x));
				AssetDatabase.SaveAssets();
			}

			//ファイルのコンバート
			void Build(bool isRebuild, DicingFolderData folderData)
			{
				MainAssetInfo inut = new MainAssetInfo(folderData.InputFolder);
				MainAssetInfo output1 = new MainAssetInfo(folderData.OuputFolder1);
				MainAssetInfo output2 = new MainAssetInfo(folderData.OuputFolder2);
				foreach (var child in inut.GetAllChildren())
				{
					//ディレクトリのみ検索
					if (!child.IsDirectory) continue;

					//子以下のフォルダは対象にしない
					if (FilePathUtil.GetDirectoryPath(child.AssetPath) != inut.AssetPath) continue;

					Build(child, output1, output2, isRebuild);
				}
				AssetDatabase.Refresh();
			}

			void Build(MainAssetInfo textureDir, MainAssetInfo output1, MainAssetInfo output2, bool isRebuild)
			{
				//スクリプタブルオブジェクトの取得（なければ作成）
				string name = textureDir.Asset.name;
				DicingTextures dicingAsset = UtageEditorToolKit.GetImportedAssetCreateIfMissing<DicingTextures>(FilePathUtil.Combine(output1.AssetPath, name + ".asset"));
				Object outPutDir = UtageEditorToolKit.GetFolderAssetCreateIfMissing(output2.AssetPath, name);
				//情報を設定
				dicingAsset.InputDir = textureDir.Asset;
				dicingAsset.OutputDir = outPutDir;
				if (dicingAsset.InputDir == null || dicingAsset.OutputDir == null)
				{
					Debug.LogError("Folder is not found");
					return;
				}
				//パッキング処理
				DicingTexturePacker.Pack(dicingAsset, isRebuild);
			}
		}

		[SerializeField]
		DicingFolderDataList folderDataList = new DicingFolderDataList();

		void OnEnable()
		{
			//スクロールを有効にする
			this.isEnableScroll = true;
		}

		protected override bool DrawProperties()
		{
			bool ret = base.DrawProperties();
			if (!ret)
			{
				EditorGUI.BeginDisabledGroup(!this.folderDataList.IsEnableOutputResources);
				if (GUILayout.Button("Build", GUILayout.Width(180)))
				{
					try
					{
						this.folderDataList.Build(false);
					}
					catch (System.Exception e)
					{
						Debug.LogException(e);
					}
				}
				GUILayout.Space(8f);

				if (GUILayout.Button("Rebuild", GUILayout.Width(180)))
				{
					try
					{
						this.folderDataList.Build(true);
					}
					catch (System.Exception e)
					{
						Debug.LogException(e);
					}
				}
				GUILayout.Space(8f);
				EditorGUI.EndDisabledGroup();

				GUILayout.Space(EditorGUIUtility.singleLineHeight);
				EditorGUILayoutUtility.WebLinkHelpBox("Web Document", @"https://madnesslabo.net/utage/?page_id=8849");
			}
			return ret;
		}

	}
}
