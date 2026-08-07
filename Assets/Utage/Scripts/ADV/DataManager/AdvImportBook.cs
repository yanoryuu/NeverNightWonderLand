// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using UnityEngine;
using UnityEngine.Profiling;
using System.Collections;
using System.Collections.Generic;

namespace Utage
{

	/// <summary>
	/// インポートしたBook（エクセルファイル）のデータ
	/// マクロを適用済みの文字列グリッドを持つ
	/// </summary>
	[CreateAssetMenu(menuName = "Utage/Scenario/Book")]
	public class AdvImportBook : ScriptableObject
	{
		const int Version = 0;
		
		[SerializeField]
		int importVersion = 0;
		public bool CheckVersion()
		{
			return importVersion == Version;
		}

		public List<AdvImportScenarioSheet> ImportGridList { get { return importGridList; } }
		[SerializeField]
		List<AdvImportScenarioSheet> importGridList = new List<AdvImportScenarioSheet>();

		//起動時の初期化
		public void BootInit()
		{
			foreach (var sheet in ImportGridList)
			{
				sheet.InitLink();
			}
		}

#if UNITY_EDITOR
		public List<StringGrid> GridList { get { return gridList; } }
		[SerializeField]
		List<StringGrid> gridList = new List<StringGrid>();
#else
		public List<StringGrid> GridList { get { return gridList; } }
		[NonSerialized] List<StringGrid> gridList = new List<StringGrid>();
#endif
		public bool Reimport { get; set; }

		public void Clear()
		{
			Reimport = true;
			ImportGridList.Clear();
			GridList.Clear();
		}

		//未インポートのシナリオデータを追加
		public void AddSourceBook(StringGridDictionary book)
		{
			foreach (var sheet in book.List)
			{
				GridList.Add(sheet.Grid);
			}
		}

		//マクロを適用したインポートデータを作成
		public void MakeImportData(AdvSettingDataManager dataManager, AdvMacroManager macroManager)
		{
			Profiler.BeginSample("MakeImportData");			
			foreach (var grid in GridList)
			{
				string sheetName = grid.SheetName;
				if (sheetName.Contains("."))
				{
					Debug.LogErrorFormat("Don't use '.' to sheetname in  {0}", grid.Name);
				}
				if (AdvSheetParser.IsScenarioSheet(sheetName) && !AdvMacroManager.IsMacroName(sheetName) )
				{
					importGridList.Add(new AdvImportScenarioSheet(grid, dataManager, macroManager));
				}
			}
			Profiler.EndSample();
			Reimport = false;
		}

	}
}
