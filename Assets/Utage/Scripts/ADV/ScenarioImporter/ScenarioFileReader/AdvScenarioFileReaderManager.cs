// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Utage
{
	//シナリオファイルの読み込みに共通のインターフェース
	//通常はエディタ上とランタイム両用可能にする
	public class AdvScenarioFileReaderManager
	{
		public IAdvScenarioDataProject Project { get; }
		public bool DebugLog { get; set; }
		List<string> TargetTypeFiles { get; }
		List<IAdvScenarioFileReader> ScenarioFileReaderList { get; }

		public AdvScenarioFileReaderManager(IAdvScenarioDataProject project,  List<string> allFiles)
		{
			Project = project;
			ScenarioFileReaderList = Project.CreateScenarioFileReaders(this).ToList();
			TargetTypeFiles = allFiles.Where(IsTargetTypeFile).ToList();
		}

		//指定のパス配列のうち、読み込み対象になっているものをリスト化して返す
		public List<string> ToTargetFiles(string[] files)
		{
			List<string> result = new ();
			foreach (var filePath in files)
			{
				if(TargetTypeFiles.Contains(filePath, System.StringComparer.OrdinalIgnoreCase))
				{
					result.Add(filePath);
				}
			}
			return result;
		}

		//読み込み対象のタイプのファイルか（対象のファイルタイプかを拡張子等で判定）
		public bool IsTargetTypeFile(string path)
		{
			foreach (var reader in ScenarioFileReaderList)
			{
				if (reader.IsTargetFile(path)) return true;
			}
			return false;
		}

		public bool TryReadFile(string path, out StringGridDictionary stringGridDictionary)
		{
			foreach (var reader in ScenarioFileReaderList)
			{
				if (reader.TryReadFile(path, out stringGridDictionary))
				{
					if(DebugLog) Debug.Log("Read File Succeeded: " + path);
					return true;
				}
			}
			if (DebugLog) Debug.LogError("Read File Failed: " + path);
			stringGridDictionary = null;
			return false;
		}

		public StringGridDictionary ReadAllFile()
		{
			StringGridDictionary result = new();
			foreach (var file in TargetTypeFiles)
			{
				if (TryReadFile(file, out StringGridDictionary stringGridDictionary))
				{
					foreach (var item in stringGridDictionary.List)
					{
						result.Add(item);
					}
				}
			}
			return result;
		}
	}
}
