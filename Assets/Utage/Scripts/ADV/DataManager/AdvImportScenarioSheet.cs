// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Profiling;

namespace Utage
{
	/// <summary>
	/// マクロ処理済みのStringGrid
	/// </summary>
	[System.Serializable]
	public class AdvImportScenarioSheet : StringGrid
	{
		//マクロ処理前のデータ
		public class StringGridRowMacroed
		{
			public StringGridRow row = null;
			public AdvEntityData entityData = null;

			public StringGridRowMacroed(StringGridRow row)
			{
				this.row = row;
			}
			public StringGridRowMacroed(StringGridRow row, AdvEntityData entityData)
			{
				this.row = row;
				this.entityData = entityData;
			}
		}

		[SerializeField]
		List<int> entityIndexTbl = new List<int>();

		[SerializeField]
		List<AdvEntityData> entityDataList = new List<AdvEntityData>();

		Dictionary<int, int> EntityIndexDictionary
		{
			get
			{
				if (entityIndexDictionary == null)
				{
					entityIndexDictionary = new Dictionary<int, int>();
					for (var i = 0; i < entityIndexTbl.Count; i++)
					{
						entityIndexDictionary.Add(entityIndexTbl[i],i);
					}
				}
				return entityIndexDictionary;
			}
		}
		Dictionary<int, int> entityIndexDictionary = null;

		//インポート用の文字列グリッドを作成
		public AdvImportScenarioSheet(StringGrid original, AdvSettingDataManager dataManager, AdvMacroManager macroManager)
			: base(original.Name, original.SheetName, original.Type)
		{
			//ヘッダー（最初の一行目のはず）は、オリジナルを使う
			this.headerRow = original.HeaderRow;
			for (int i = 0; i < original.DataTopRow; ++i)
			{
				this.AddRow(original.Rows[i].Strings);
			}

			//マクロ展開
			List<StringGridRow> rowList = new List<StringGridRow>();
			foreach (StringGridRow row in original.Rows)
			{
				if (row.RowIndex < original.DataTopRow) continue;       //データの行じゃない
				if (row.IsEmptyOrCommantOut) continue;                  //データがない

				//マクロ展開
				bool isMacro = macroManager.TryMacroExpansion(row, rowList, "");
				if (!isMacro)
				{
					//マクロ展開がないので、普通に行を追加
					rowList.Add(row);
				}
			}

			//データ部分はマクロ展開済みのもの
			foreach (var row in rowList)
			{
				StringGridRow data;

				string[] strings;
				//マクロの場合はエンティテイ処理のチェック
				//将来的にはマクロ展開抜きでいけそう
				if (AdvEntityData.ContainsEntitySimple(row))
				{
					//エンティティ処理がある場合はそれをリストにいれてインデックスを
					string[] entityStrings;
					if (AdvEntityData.TryCreateEntityStrings(row, dataManager.DefaultParam.GetParameter, out entityStrings))
					{
						AdvEntityData entityData = new AdvEntityData(row.Strings);
						strings = entityStrings;
						entityDataList.Add(entityData);
						entityIndexTbl.Add(this.Rows.Count);
					}
					else
					{
						strings = row.Strings;
					}
				}
				else
				{
					strings = row.Strings;
				}
				data = this.AddRow(strings);
				data.DebugIndex = row.DebugIndex;
				data.DebugInfo = row.DebugInfo;
			}

#if UNITY_EDITOR
			this.SourceAssetInEditor = original.SourceAssetInEditor;
#endif
			this.InitLink();
		}

		public List<AdvCommand> CreateCommandList(AdvSettingDataManager dataManager)
		{
			List<AdvCommand> commandList = new List<AdvCommand>();
			foreach (StringGridRow row in Rows)
			{
				if (row.RowIndex < DataTopRow) continue;           //データの行じゃない
				if (row.IsEmptyOrCommantOut) continue;             //データがない

				Profiler.BeginSample("コマンド作成");

				AdvCommand command = AdvCommandParser.CreateCommand(row, dataManager);
				Profiler.EndSample();

				//エンティティ処理がある
				Profiler.BeginSample("GetEntityIndex");
				int entityIndex = GetEntityIndexQuick(row.RowIndex);
/*				if (entityIndex != GetEntityIndex(row.RowIndex))
				{
					Debug.LogError("GetEntityIndex Error!!");
				}
*/
				Profiler.EndSample();
				if (entityIndex >= 0)
				{
					command.EntityData = entityDataList[entityIndex];
				}
				if (null != command) commandList.Add(command);
			}
			return commandList;
		}
/*
		int GetEntityIndex(int index)
		{
			for (int i = 0; i < entityIndexTbl.Count; ++i)
			{
				if (entityIndexTbl[i] == index)
				{
					return i;
				}
			} 
			return -1;
		}
*/
		int GetEntityIndexQuick(int index)
		{
			if( EntityIndexDictionary.TryGetValue(index, out int entityIndex) )
			{
				return entityIndex;
			} 
			return -1;
		}
	}
}
