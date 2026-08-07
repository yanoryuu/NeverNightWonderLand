// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{
	//拡張packageの管理を行う
	//URP用のアセットなどのプロジェクト設定やUnityのバージョン違いによって追加で必要になる（デフォルトでは認識しない）アセットをインポートする
	[FilePath("ProjectSettings/UtageExtensionPackageManager.asset", FilePathAttribute.Location.ProjectFolder)]
	public class ExtensionPackageManager : ScriptableSingleton<ExtensionPackageManager>
	{
		//シングルトンでアクセスする
		public static ExtensionPackageManager Instance => instance;

		//各種の拡張機能のパッケージ情報のリスト
		public List<IExtensionPackage> Packages { get; }= new ();

		//パッケージのセーブデータ
		[Serializable]
		public class PackageSaveData
		{
			public string Guid
			{
				get => guid;
				set => guid = value;
			}

			[SerializeField] string guid;

			public int ImportedVersion
			{
				get => importedVersion;
				set => importedVersion = value;
			}

			[SerializeField] int importedVersion;
		}
		[SerializeField] List<PackageSaveData> packageSaveDataList = new();
		public List<PackageSaveData> PackageSaveDataList => packageSaveDataList;

		public bool DebugLog { get; } = false;

		//追加パッケージ必要な各拡張からパッケージ情報を追加する
		public void AddPackage(IExtensionPackage importer)
		{
			if(DebugLog) Debug.Log($"AddPackage {importer.PackageName}");
			Packages.Add(importer);
		}
		
		//パッケージのインポートが必要かチェックする
		public bool IsNeedImportPackages()
		{
			return Packages.Exists(x => x.CheckNeedImportPackages());
		}
		
		//必要なパッケージのみインポートを行う	
		public void ImportPackages()
		{			
			foreach (var importer in Packages)
			{
				if (importer.CheckNeedImportPackages())
				{
					importer.ImportPackages();
				}
			}
		}
		
		//バージョンチェック等を無視して強制インポートする
		public void ForceImportPackages()
		{
			foreach (var importer in Packages)
			{
				importer.ImportPackages();
			}
		}

		//パッケージのセーブデータをクリア
		public void ClearPackageSaveData()
		{
			//データリストをクリアするのではなく、インポートバージョンを0にする
			PackageSaveDataList.ForEach(x => x.ImportedVersion = 0);
		}

		//指定のGUIDのパッケージデータを取得する
		public PackageSaveData GetPackageSaveDataCreateIfMissing(string packageGuid)
		{
			var data = PackageSaveDataList.Find(x => x.Guid == packageGuid);
			if (data != null) return data;

			data = new () { Guid = packageGuid };
			PackageSaveDataList.Add(data);
			return data;
		}

		void OnDisable() => this.Save();
		public void Save() => this.Save(true);
	}
}
