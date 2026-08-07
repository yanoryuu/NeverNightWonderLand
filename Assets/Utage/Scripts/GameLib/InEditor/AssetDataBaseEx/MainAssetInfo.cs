// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Utage
{

	/// <summary>
	/// アセットの情報
	/// </summary>
	public class MainAssetInfo : AssetInfo
	{
		//GUIDから作成
		public static MainAssetInfo CreateFromGUID(string guid)
		{
			return new MainAssetInfo(AssetDatabase.GUIDToAssetPath(guid));
		}

		//アセットパスから作成
		public MainAssetInfo(string assetPath)
		{
			Init(assetPath);
		}

		//Objectから作成
		public MainAssetInfo(Object asset)
			: base(asset)
		{
			Init(AssetDatabase.GetAssetPath(asset));
		}

		public override bool IsMainAsset{ get { return true; } } 

		//アセットパスから初期化
		protected void Init(string assetPath)
		{
			this.Guid = AssetDatabase.AssetPathToGUID( assetPath );
			if (string.IsNullOrEmpty(Guid))
			{
				Debug.LogError(assetPath);
			}
		}

		//GUID(ファイル移動などをしてもGUIDは一致するので、これが基準になる)
		public string Guid { get; private set; }
		//アセットのパス
		public string AssetPath { get { return AssetDatabase.GUIDToAssetPath(Guid); } }
		//アセットの名前
		public string AssetName { get { return Path.GetFileNameWithoutExtension(AssetPath); } }
		
		//デバイス内でのフルパス
		public string FullPath { get { return AssetPathToFullPath(AssetPath); } }
	
		//アイコンを取得します
		public Texture CachedIcon { get{ return AssetDatabase.GetCachedIcon(AssetPath); }}

		// .meta ファイルのパスを取得します
		public string TextMetaDataPath { get { return AssetDatabase.GetTextMetaFilePathFromAssetPath(AssetPath); }}

		public bool IsDirectory
		{
			get
			{
				return System.IO.Directory.Exists(AssetPath);
			}
		}

		//メインアセットオブジェクト
		public override Object Asset
		{
			get
			{
				return AssetDatabase.LoadMainAssetAtPath(AssetPath);
			}
		}

		// AssetImporter（アセットバンドルなどの取得に使う）を取得
		public AssetImporter AssetImporter
		{
			get
			{
				return AssetImporter.GetAtPath(AssetPath);
			}
		}

		//全てのサブアセット
		public List<SubAssetInfo> SubAssets
		{
			get
			{
				if (subAssetes == null)
				{
					subAssetes = new List<SubAssetInfo>();
					foreach (Object obj in AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetPath))
					{
						subAssetes.Add(new SubAssetInfo(obj,this));
					}
				}
				return subAssetes;
			}
		}
		List<SubAssetInfo> subAssetes;

		//フォルダの場合に、フォルダ以下にあるアセットを取得します
		public List<MainAssetInfo> GetAllChildren()
		{
			List<MainAssetInfo> list = new List<MainAssetInfo>();
			if (IsDirectory)
			{
				//重複を避けるためにHashSetを使う
				HashSet<string> guids = new HashSet<string>( AssetDatabase.FindAssets("", new[] { AssetPath } ));
				foreach (string guid in guids)
				{
					list.Add(MainAssetInfo.CreateFromGUID(guid));
				}
			}
			return list;
		}

		//フォルダの場合に、フォルダ以下にあるアセットを取得します
		public List<MainAssetInfo> GetAllChildren<T>() where T : UnityEngine.Object
		{
			List<MainAssetInfo> children = GetAllChildren();
			List<MainAssetInfo> list = new List<MainAssetInfo>();
			foreach (var child in children)
			{
				if (child.Asset is T)
				{
					list.Add(child);
				}
			}
			return list;
		}


		//依存関係にある全てのアセットを取得します
		public List<AssetInfo> Dependencies
		{
			get
			{
				List<AssetInfo> dependencies = new List<AssetInfo>();
				foreach (string path in AssetDatabase.GetDependencies(new[] { AssetPath }))
				{
					dependencies.Add(new MainAssetInfo(path));
				}
				return dependencies;
			}
		}

		//	Adds objectToAdd to an existing asset at path.
		public void AddObjectToAsset(UnityEngine.Object objectToAdd)
		{
			if (Asset is ScriptableObject)
			{
				AssetDatabase.AddObjectToAsset(objectToAdd, AssetPath);
				subAssetes = null;
			}
			else
			{
				Debug.LogError("AddObjectToAsset can use only ScriptableObject ");
			}
		}


		// アセットをコピーする
		public bool Copy(string newPath)
		{
			return AssetDatabase.CopyAsset(AssetPath, newPath);
		}

		// アセットを移動する
		public string Move(string newPath)
		{
			string errorMsg = AssetDatabase.ValidateMoveAsset(AssetPath, newPath);
			if (string.IsNullOrEmpty(errorMsg))
			{
				return AssetDatabase.MoveAsset(AssetPath, newPath);
			}
			else
			{
				Debug.LogError( errorMsg );
				return "";
			}
		}

		// アセットの名前を変える
		public string Rename(string newName)
		{
			return AssetDatabase.RenameAsset(AssetPath, newName);
		}

		// アセットを削除する
		public bool Delete()
		{
			return AssetDatabase.DeleteAsset(AssetPath);
		}

		//  再インポート（強制インポート）
		public void Reimport(ImportAssetOptions options = ImportAssetOptions.Default)
		{
			AssetDatabase.ImportAsset(AssetPath, options);
		}
	
		//  ダーティ設定（変更を告知）
		public void SetDirty()
		{
			EditorUtility.SetDirty(Asset);
		}


		public AssetBuildTimeStamp MakeBuildTimeStampAllChildren<T>() where T : UnityEngine.Object
		{
			AssetBuildTimeStamp timeStamp = new AssetBuildTimeStamp();
			List<MainAssetInfo> list = GetAllChildren<T>();
			List<Object> assets = new List<Object>();
			list.ForEach(x => assets.Add(x.Asset));
			timeStamp.MakeList(assets);
			return timeStamp;
		}
	}
}
#endif
