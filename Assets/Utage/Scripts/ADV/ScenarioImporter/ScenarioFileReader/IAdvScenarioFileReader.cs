// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Utage
{
	//シナリオファイルの読み込みに共通のインターフェース
	public interface IAdvScenarioFileReader
	{
		bool IsTargetFile(string path);
		bool TryReadFile(string path, out StringGridDictionary stringGridDictionary);
	}
}
