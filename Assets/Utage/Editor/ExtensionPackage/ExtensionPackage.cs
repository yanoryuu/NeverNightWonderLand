using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Utage;
using UtageExtensions;

namespace Utage
{
	//拡張packageを扱うための基本クラス
	public class ExtensionPackage : IExtensionPackage
	{
		//パッケージのGUID
		protected string PackageGuid { get; }

		//パッケージ名
		public virtual string PackageName { get; }

		//セーブデータ
		protected ExtensionPackageManager.PackageSaveData SaveData { get; }

		bool DebugLog { get; } = ExtensionPackageManager.Instance.DebugLog;

		//最新バージョン
		public int Version { get; }

		Object PackageAsset { get; set; }

		//インポートされているバージョン
		public int ImportedVersion
		{
			get => SaveData.ImportedVersion;
			protected set
			{
				if (ImportedVersion == value) return;
				SaveData.ImportedVersion = value;
			}
		}

		public ExtensionPackage(string guid, int version)
		{
			PackageGuid = guid;
			Version = version;
			var path = AssetDatabase.GUIDToAssetPath(PackageGuid);
			PackageName = path.IsNullOrEmpty() ? "" : Path.GetFileNameWithoutExtension(path);
			SaveData = ExtensionPackageManager.Instance.GetPackageSaveDataCreateIfMissing(PackageGuid);
		}

		//パッケージのアセット
		public Object GetPackageAsset()
		{
			if (PackageAsset == null)
			{
				PackageAsset = AssetDataBaseEx.LoadAssetByGuid<Object>(PackageGuid);
			}
			return PackageAsset;
		}

		//パッケージのインポートが必要かチェックする
		public bool CheckNeedImportPackages()
		{
			return !CheckVersion();
		}
		
		bool CheckVersion()
		{
			return Version == ImportedVersion;
		} 
		
		void InitializeImportEvents()
		{
			FinalizeImportEvents();
			AssetDatabase.importPackageStarted += OnImportPackageStarted;
			AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
			AssetDatabase.importPackageFailed += OnImportPackageFailed;
			AssetDatabase.importPackageCancelled += OnImportPackageCancelled;
		}

		void FinalizeImportEvents()
		{
			if (DebugLog) Debug.Log("FinalizeImportEvents");
			//インポート後の後処理
			AssetDatabase.importPackageStarted -= OnImportPackageStarted;
			AssetDatabase.importPackageCompleted -= OnImportPackageCompleted;
			AssetDatabase.importPackageFailed -= OnImportPackageFailed;
			AssetDatabase.importPackageCancelled -= OnImportPackageCancelled;
		}

		public void ImportPackages()
		{
			InitializeImportEvents();
			AssetDataBaseEx.ImportPackageByGuid(PackageGuid,false);
		}

		void OnImportPackageStarted(string packageName)
		{
			if (DebugLog) Debug.Log("Import Started " + packageName);
			if (PackageName != packageName) return;
		}

		void OnImportPackageCancelled(string packageName)
		{
			if (DebugLog) Debug.Log("Import Cancelled " + packageName);
			if(PackageName!=packageName) return;
			FinalizeImportEvents();
		}

		void OnImportPackageFailed(string packageName, string error)
		{
			if (DebugLog) Debug.LogError("Import Failed " + packageName + " " + error);
			if (PackageName != packageName) return;
			FinalizeImportEvents();
		}

		void OnImportPackageCompleted(string packageName)
		{
			if (DebugLog) Debug.Log("Import Completed " + packageName);
			if (PackageName != packageName) return;
			this.ImportedVersion = Version;
			FinalizeImportEvents();
		}
	}
}
