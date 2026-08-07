using UnityEngine;

namespace Utage
{
	//拡張packageを扱うための共通インターフェース
	public interface IExtensionPackage
	{
		//パッケージ名
		public string PackageName { get; }

		//最新バージョン
		public int Version { get; }
		
		//インポートされているバージョン
		public int ImportedVersion { get; }

		//パッケージのアセット
		public Object GetPackageAsset();

		//パッケージのインポートが必要かチェックする
		bool CheckNeedImportPackages();

		//パッケージのインポートを行う
		void ImportPackages();
	}
}
