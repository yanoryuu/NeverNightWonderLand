// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Utage
{

	//テンプレートをコピーして、新しいプロジェクトのアセットを作る処理
	public class AdvProjectCreatorAssets
	{
		protected AdvProjectCreator Creator { get; }

		public Dictionary<Object, Object> CloneAssetPair { get; protected set; }

		public AdvProjectCreatorAssets(AdvProjectCreator creator)
		{
			Creator = creator;
		}

		Stopwatch Stopwatch { get; set; }

		//新たなプロジェクトを作成
		//テンプレートをコピー
		public virtual void Create()
		{
			Stopwatch = new Stopwatch();
			Stopwatch.Start();
//			Debug.Log($"Start {Stopwatch.Elapsed}");
			
			Profiler.BeginSample("CopyTemplate");

			//テンプレートをフォルダごとコピペ
			AssetDatabase.CopyAsset(Creator.TemplateFolderPath, Creator.GetProjectRelativeDir());

//			Debug.Log($"Complete CopyTemplate {Stopwatch.Elapsed}");

			//アセットの再設定
			Profiler.BeginSample($"{nameof(RebuildAssets)}");
			RebuildAssets();
			Profiler.EndSample();

//			Debug.Log($"Complete RebuildAssets {Stopwatch.Elapsed}");

			//リネーム
			Profiler.BeginSample($"{nameof(Rename)}");
			Rename();
			Profiler.EndSample();

//			Debug.Log($"Complete Rename {Stopwatch.Elapsed}");

			//アセットのセーブ
			AssetDatabase.SaveAssets();
			//いったんアセットをリフレッシュ
			AssetDatabase.Refresh();

			Profiler.EndSample();
		}

		void Rename()
		{
			//リフレッシュ必須
			AssetDatabase.Refresh();
			//アセットのセーブ
			AssetDatabase.SaveAssets();

			//アセットの編集開始
			AssetDatabase.StartAssetEditing();

			//「Template」と同じファイル名をリネーム
			foreach (string filePath in System.IO.Directory.GetFiles(Creator.NewProjectDir, "*",
				         SearchOption.AllDirectories))
			{
				//メタファイルは対象外
				if (Path.GetExtension(filePath) == ".meta") continue;
				string fileName = Path.GetFileNameWithoutExtension(filePath);
				if (fileName.Contains(Creator.TemplateFolderName))
				{
					string src = UtageEditorToolKit.SystemIOFullPathToAssetPath(filePath);
					string dst = fileName.Replace(Creator.TemplateFolderName, Creator.ProjectName);
					string error = AssetDatabase.RenameAsset(src, dst);
					if (!string.IsNullOrEmpty(error))
					{
						Debug.LogError($"Rename error {src} to {dst}\n{error}");
					}
					else
					{
//						Debug.Log($"Rename {src} to {dst}");
					}
				}
			}

//			Debug.Log($"Complete Rename TemplateFolder {Stopwatch.Elapsed}");

			//「TemplateFolder」というフォルダ名をリネーム
			foreach (string dirPath in System.IO.Directory.GetDirectories(Creator.NewProjectDir, "*",
				         SearchOption.AllDirectories))
			{
				if (Path.GetFileName(dirPath) == Creator.TemplateFolderName)
				{
					string src = UtageEditorToolKit.SystemIOFullPathToAssetPath(dirPath);
					string dst = Creator.ProjectName;
					string error = AssetDatabase.RenameAsset(src, dst);
					if (!string.IsNullOrEmpty(error))
					{
						Debug.LogError($"Rename error {src} to {dst}\n{error}");
					}
				}
			}

//			Debug.Log($"Complete Rename TemplateFolder {Stopwatch.Elapsed}");

			//アセットの編集終了
			AssetDatabase.StopAssetEditing();
		}

		//アセットの再設定
		void RebuildAssets()
		{
			//いったんアセットをリフレッシュ
			AssetDatabase.Refresh();
			//アセットの編集開始
			AssetDatabase.StartAssetEditing();

			Debug.Log("RebuildAssets･･･");
			
			Profiler.BeginSample($"{nameof(FindCloneAssets)}");
			CloneAssetPair = FindCloneAssets();
			Profiler.EndSample();

			Profiler.BeginSample($"{nameof(ReplaceCloneInSelf)}");
			ReplaceCloneInSelf(CloneAssetPair);
			Profiler.EndSample();

			Debug.Log("...End RebuildAssets");
			//アセットの編集終了
			AssetDatabase.StopAssetEditing();
		}

		class AssetInfo
		{
			string RelativePath { get; }

			public MainAssetInfo MainAssetInfo { get; }

			public AssetInfo(string assetPath, string rootPath)
			{
				MainAssetInfo = new MainAssetInfo(assetPath);
				assetPath = FilePathUtil.Format(assetPath);
				rootPath = FilePathUtil.Format(rootPath);
				if (!assetPath.StartsWith(rootPath))
				{
					Debug.LogError($"{assetPath} is not rootPath {rootPath}");
				}

				RelativePath = assetPath.Replace(rootPath, "");
			}

			public bool IsClone(AssetInfo assetInfo)
			{
				return RelativePath == assetInfo.RelativePath;
			}
		}

		//シーン内で、クローンしたアセットに置き換えるためのDictionaryを作成
		//元のアセットをキーとし、クローンしたアセットをValueとする
		Dictionary<Object, Object> FindCloneAssets()
		{
			Dictionary<Object, Object> cloneAssetPair = new Dictionary<Object, Object>();

			
			var oldDir = Creator.TemplateFolderPath;
			var newDir = Creator.GetProjectRelativeDir();
			List<AssetInfo> oldAssetInfos = AssetDataBaseEx.GetAllAssetPaths(oldDir).Select(assetPath => new AssetInfo(assetPath, oldDir)).ToList();
			List<AssetInfo> newAssetInfos = AssetDataBaseEx.GetAllAssetPaths(newDir).Select(assetPath => new AssetInfo(assetPath, newDir)).ToList();

			foreach (var oldMainAsset in oldAssetInfos)
			{
				var mainAsset = oldMainAsset.MainAssetInfo.Asset;
				var newMainAsset = newAssetInfos.Find(x => x.IsClone(oldMainAsset));
				if (newMainAsset == null)
				{
					Debug.LogError($"{oldMainAsset.MainAssetInfo.AssetPath} is not found clone asset under {newDir}", mainAsset);
				}
				else
				{
					var asset = newMainAsset.MainAssetInfo.Asset;
					if (asset == null)
					{
						Debug.LogError($"{newMainAsset.MainAssetInfo.AssetPath} is null asset under {newDir}");
					}
					cloneAssetPair.Add(mainAsset, asset);

					var oldSubAssets = oldMainAsset.MainAssetInfo.SubAssets;
					var newSubAssets = newMainAsset.MainAssetInfo.SubAssets;
					if (oldSubAssets.Count!= newSubAssets.Count)
					{
						Debug.LogError(
							$"{oldMainAsset.MainAssetInfo.AssetPath} subAsset count {oldSubAssets.Count} is not equal clone asset under {newDir}",
							mainAsset);
						break;
					}
					for(int i = 0; i < oldSubAssets.Count; ++i)
					{
						var oldSubAsset = oldSubAssets[i].Asset;
						var newSubAsset = newSubAssets[i].Asset;
						if (oldSubAsset.name != newSubAsset.name || oldSubAsset.GetType() != newSubAsset.GetType())
						{
							Debug.LogError(
								$"{oldSubAsset} subAsset [{i}] is not equal clone asset under {newDir}",
								oldSubAsset);
							continue;
						}
						cloneAssetPair.Add(oldSubAsset, newSubAsset);
					}
				}
			}
			return cloneAssetPair;
		}

		//クローンしたプレハブやScriptableObject内にテンプレートアセットへのリンクがあったら、クローンアセットのものに変える
		void ReplaceCloneInSelf(Dictionary<Object, Object> cloneAssetPair)
		{
			//Valuesはクローンしたアセット
			foreach (Object clone in cloneAssetPair.Values)
			{
				Profiler.BeginSample($"{nameof(DependencyReplacer)}.Replace");
				var replacer = new DependencyReplacer(clone, false,cloneAssetPair);
				replacer.Replace();
				Profiler.EndSample();
			}
		}
	}
}
