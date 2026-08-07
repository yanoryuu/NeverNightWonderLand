// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UtageExtensions;

namespace Utage
{
	//「Utage」のリソースコンバーター
	public class AdvResourcesConverter : EditorWindowWithSave
	{
		const string KeyScenario = "Scenario";
		const string KeyResouces = "Resouces";

		/// <summary>
		/// リソースのパス
		/// </summary>
		[SerializeField]
		Object resourcesDirectory;
		public Object ResourcesDirectory
		{
			get { return resourcesDirectory; }
			set { resourcesDirectory = value; }
		}

		/// シナリオファイルコンバート用のプロジェクトファイル
		[SerializeField]
		AdvScenarioDataProject projectSetting;
		public AdvScenarioDataProject ProjectSetting
		{
			get { return projectSetting; }
			set { projectSetting = value; }
		}

		//アセットバンドルのビルドをするか
		enum AssetBundleBuildMode
		{
			None,           //ビルドしない
			OnlyEditor,     //エディタ用のみビルドする
			AllPlatform,    //全プラットフォーム用のものをビルドする
		};
		[SerializeField]
		AssetBundleBuildMode buildMode = AssetBundleBuildMode.OnlyEditor;

		//アセットバンドルのリネーム法則
		public enum AssetBundleRenameType
		{
			None,           //名前を変えない
			Rename,         //リネームする
			OnlyNotNamed,   //まだ名前が設定されていないものだけリネームする
		};
		[SerializeField]
		AssetBundleRenameType renameType = AssetBundleRenameType.Rename;

		[SerializeField, EnumFlags]
		AssetBundleTargetFlags buildTargetFlags = AssetBundleTargetFlags.Windows;

		[SerializeField]
		BuildAssetBundleOptions buildOptions = BuildAssetBundleOptions.None;


		//****************出力設定****************//

		public enum OutputType
		{
			Default,
		};
		[SerializeField]
		OutputType outputType = OutputType.Default;

		//章別に分割するかどうか
		[SerializeField]
		bool separateChapter = false;

		[SerializeField]
		bool isOutputLog = true;

		/// <summary>
		/// サーバー用のリソースの出力先のパス
		/// </summary>
		[SerializeField, PathDialog(PathDialogAttribute.DialogType.Directory)]
		string outputPath = "";
		public string OutputPath
		{
			get { return outputPath; }
		}


		protected override string SaveKey => nameof(AdvResourcesConverter);

		protected override void OnGUI()
		{
			base.OnGUI();
/*			SerializedObjectHelper.SerializedObject.Update();
			OnDrawCustom(this.SerializedObjectHelper);
			bool ret = SerializedObjectHelper.SerializedObject.ApplyModifiedProperties();*/

			bool isEnable = (ResourcesDirectory != null || ProjectSetting != null) && !string.IsNullOrEmpty(OutputPath);
			EditorGUI.BeginDisabledGroup(!isEnable);
			bool isButton = GUILayout.Button("Convert", GUILayout.Width(180));
			EditorGUI.EndDisabledGroup();
			GUILayout.Space(8f);

			if (isButton)
			{
				Convert();
			}

			GUILayout.Space(EditorGUIUtility.singleLineHeight);
			EditorGUILayoutUtility.WebLinkHelpBox("Web Document", @"https://madnesslabo.net/utage/?page_id=9384");
		}
		
		//ファイルのコンバート
		void Convert()
		{
			try
			{
				AssetFileManager assetFileManager = WrapperFindObject.FindObjectOfType<AssetFileManager>();
				if (assetFileManager == null)
				{
					Debug.LogError("FileManager is not found in current scene");
					return;
				}
				//ファイルの入出力に使う
				FileIOManager fileIOManager = assetFileManager.FileIOManager;
				switch (outputType)
				{
					case OutputType.Default:
					default:
						//アセットバンドルをビルド
						BuildAssetBundles(fileIOManager);
						break;
				}
				AssetDatabase.Refresh();
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}


		//アセットバンドルのビルド
		void BuildAssetBundles(FileIOManager fileIOManager)
		{
			if (buildMode == AssetBundleBuildMode.None) return;

			//アセットバンドルをプラットフォーム別にビルド
			List<BuildTarget> buildTargets = new List<BuildTarget>();
			switch (buildMode)
			{
				case AssetBundleBuildMode.OnlyEditor://エディタ上のみ
					buildTargets.Add(AssetBundleHelper.BuildTargetFlagToBuildTarget(AssetBundleHelper.EditorAssetBundleTarget()));
					break;
				case AssetBundleBuildMode.AllPlatform://全プラットフォーム
					{
						buildTargets = AssetBundleHelper.BuildTargetFlagsToBuildTargetList(buildTargetFlags);
					}
					break;
				default:
					break;
			}

			MainAssetInfo inputDirAsset = new MainAssetInfo(this.ResourcesDirectory);
			List<MainAssetInfo> assets = GetAssetBudleList(inputDirAsset);
			RenameAssetBundles(inputDirAsset.AssetPath,assets);
			AssetBundleBuild[] builds = ToAssetBundleBuilds(assets);
			if (builds.Length <= 0) return;


			foreach (BuildTarget buildTarget in buildTargets)
			{
				string outputPath = FilePathUtil.Combine(OutputPath, AssetBundleHelper.BuildTargetToBuildTargetFlag(buildTarget).ToString());
				//出力先のディレクトリを作成
				if (!Directory.Exists(outputPath))
				{
					Directory.CreateDirectory(outputPath);
				}
				//アセットバンドルを作成
				AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(outputPath, builds, buildOptions, buildTarget);
				Debug.Log("BuildAssetBundles to " + buildTarget.ToString());
				if (isOutputLog)
				{
					WriteManifestLog(manifest, outputPath);
				}
			}
		}

		//アセットバンドルのリストを取得
		List<MainAssetInfo> GetAssetBudleList(MainAssetInfo inputDirAsset)
		{
			List<MainAssetInfo> assets = new List<MainAssetInfo>();

			//シナリオ用のアセットを取得
			assets.AddRange(MakeScenarioAssetBudleList());
			//指定ディレクトリ以下のアセットを全て取得
			assets.AddRange(MakeAssetBudleList(inputDirAsset));
			return assets;
		}

		//シナリオのアセットバンドルのリストを取得
		List<MainAssetInfo> MakeScenarioAssetBudleList()
		{
			List<MainAssetInfo> assets = new List<MainAssetInfo>();

			//章別に分割するかどうか
			if (separateChapter)
			{
				string[] pathList = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(ProjectSetting.Scenarios));
				foreach (string path in pathList)
				{
					MainAssetInfo assetInfo = new MainAssetInfo(path);
					if (assetInfo.Asset is AdvChapterData)
					{
						assets.Add(assetInfo);
					}
				}
			}
			else
			{
				assets.Add(new MainAssetInfo(ProjectSetting.Scenarios));
			}
			return assets;
		}

		//アセットバンドルのリストを取得
		List<MainAssetInfo> MakeAssetBudleList(MainAssetInfo inputDirAsset)
		{
			List<MainAssetInfo> assets = new List<MainAssetInfo>();

			//指定ディレクトリ以下のアセットを全て取得
			foreach (MainAssetInfo asset in inputDirAsset.GetAllChildren())
			{
				if (asset.IsDirectory) continue;
				if (IsIgnoreAssetBundle(asset)) continue;
				assets.Add(asset);
			}
			return assets;
		}

		//アセットバンドル化しないアセットを取得
		bool IsIgnoreAssetBundle(MainAssetInfo asset)
		{
			string path = asset.AssetPath;
			if (path.EndsWith("keep.keep"))
			{
				return true;
			}

			return false;
		}


		//アセットバンドル名のリネーム
		void RenameAssetBundles(string rootPath, List<MainAssetInfo> assets)
		{
			if (renameType == AssetBundleRenameType.None) return;

			foreach (MainAssetInfo asset in assets)
			{
				AssetImporter importer = asset.AssetImporter;
				if (importer == null)
				{
					Debug.LogError("Not Find Importer");
					continue;
				}

				if (renameType == AssetBundleRenameType.OnlyNotNamed
					&& !string.IsNullOrEmpty(importer.assetBundleName))
				{
					//まだ名前がついていないときにのみネーミング
					continue;
				}

				string assetBundleName = ToAssetBundleName(rootPath,asset.AssetPath);
				//強制的にリネーム
				if (importer.assetBundleName != assetBundleName)
				{
					importer.assetBundleName = assetBundleName;
					importer.SaveAndReimport();
				}
			}
		}

		//アセットバンドル名を取得
		string ToAssetBundleName(string rootPath, string assetPath)
		{
			string name;
			if (assetPath.StartsWith(rootPath))
			{
				name = assetPath.Substring(rootPath.Length+1);
			}
			else
			{
				name = FilePathUtil.GetFileName(assetPath);
			}
			return FilePathUtil.ChangeExtension(name,".asset");
		}

		//アセットバンドルリストを取得
		AssetBundleBuild[] ToAssetBundleBuilds(List<MainAssetInfo> assets)
		{
			List<AssetBundleBuild> list = new List<AssetBundleBuild>();
			foreach (MainAssetInfo asset in assets)
			{
				AssetImporter importer = asset.AssetImporter;
				if (importer == null)
				{
					Debug.LogError("Not Find Importer");
					continue;
				}
				AssetBundleBuild build = new AssetBundleBuild();
				build.assetBundleName = importer.assetBundleName;
				build.assetBundleVariant = importer.assetBundleVariant;
				build.assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(importer.assetBundleName);
				list.Add(build);
			}
			return list.ToArray();
		}

		//ログファイルを書き込む
		void WriteManifestLog(AssetBundleManifest manifest, string outputPath)
		{
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			foreach (string assetBundleName in manifest.GetAllAssetBundles())
			{
				builder.Append(assetBundleName);
				builder.AppendLine();

				Hash128 hash = manifest.GetAssetBundleHash(assetBundleName);
				builder.AppendFormat("  Hash128: {1}", assetBundleName, hash.ToString() );
				builder.AppendLine();


				builder.AppendLine();
			}
			string logFilePath = FilePathUtil.Combine(outputPath, Path.GetFileNameWithoutExtension(outputPath));
			logFilePath += ExtensionUtil.Log + ExtensionUtil.Txt;
			File.WriteAllText(logFilePath, builder.ToString());
		}


	}
}
