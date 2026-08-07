using System;
using UnityEngine;

namespace Utage
{
	//UnitCloudBuildのバージョンなどを確認するマニフェストファイル
	[Serializable]
	public class CloudBuildManifest
	{
		public static string VersionText()
		{
			var manifest = Instance();
			if (manifest == null) return "Not Unity Cloud Build";

			return string.Format("{0} #{1}  UTC: {2}", manifest.CloudBuildTargetName, manifest.BuildNumber, manifest.BuildStartTime);
		}
		public static CloudBuildManifest Instance()
		{
			if (instance != null) return instance;

			var json = Resources.Load<TextAsset>("UnityCloudBuildManifest.json");
			if (json == null)
			{
				return null;
			}
			instance = JsonUtility.FromJson<CloudBuildManifest>(json.text);
			return instance;
		}

		[StaticField] static CloudBuildManifest instance;


		//Unity Cloud Build によってビルドされた commit、または changelist
		[SerializeField]
		string scmCommitId = "";
		public string ScmCommitId { get { return scmCommitId; } }

		//ビルドされたブランチ名
		[SerializeField]
		string scmBranch = "";
		public string ScmBranch { get { return scmBranch; } }

		//このビルドに関連する Unity Cloud Build 番号
		[SerializeField]
		string buildNumber = "";
		public string BuildNumber { get { return buildNumber; } }

		//ビルドプロセスが始まったときの UTC timestamp
		[SerializeField]
		string buildStartTime = "";
		public string BuildStartTime { get { return buildStartTime; } }

		//Unity Cloud Build プロジェクト識別子
		[SerializeField]
		string projectId = "";
		public string ProjectId { get { return projectId; } }

		//(iOS/Androidのみ) bundleIdentifier は Unity Cloud Build 内で設定されます
		[SerializeField]
		string bundleId = "";
		public string BundleId { get { return bundleId; } }

		//Unity Cloud Build がビルド作成に使用した Unity のバージョン
		[SerializeField]
		string unityVersion = "";
		public string UnityVersion { get { return unityVersion; } }

		//(iOS のみ) ビルドに使用される XCode のバージョン
		[SerializeField]
		string xcodeVersion = "";
		public string XCodeVersion { get { return xcodeVersion; } }

		//ビルドされたプロジェクトビルドターゲットの名前。
		//現在は、プラットフォームに呼応していて、"default-web” “default-ios” “default-android" のいずれか。
		[SerializeField]
		string cloudBuildTargetName = "";
		public string CloudBuildTargetName { get { return cloudBuildTargetName; } }
	}
}
