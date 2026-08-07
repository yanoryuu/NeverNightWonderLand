// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Profiling;


namespace Utage
{

	/// <summary>
	/// マクロ定義の管理クラス
	/// </summary>
	public class AdvMacroManager
	{
		//マクロシートから作った、マクロデータの一覧
		Dictionary<string, AdvMacroData> macroDataTbl = new Dictionary<string,AdvMacroData>();

		//マクロシートからマクロデータを作って追加。追加なければfalse
		public bool TryAddMacroData(string name, StringGrid grid)
		{
			if (!IsMacroName(name)) return false;

			int index = 0;
			while(index<grid.Rows.Count)
			{
				StringGridRow row = grid.Rows[index];
				++index;
				if (row.RowIndex < grid.DataTopRow) continue;			//データの行じゃない
				if (row.IsEmptyOrCommantOut) continue;								//データがない
				
				string 	macroName;
				if( TryParseMacoBegin(row, out macroName) )
				{
					List<StringGridRow> rowList = new List<StringGridRow>();
					while (index < grid.Rows.Count)
					{
						StringGridRow macroRow = grid.Rows[index];
						++index;
						if (macroRow.IsEmptyOrCommantOut) continue;						//データがない
						if (AdvParser.ParseCellOptional<string>(macroRow, AdvColumnName.Command, "") == "EndMacro")
						{
							break;
						}

						rowList.Add(macroRow);
					}

					if (macroDataTbl.ContainsKey(macroName))
					{
						Debug.LogError(row.ToErrorString(macroName + " is already contains "));
					}
					else
					{
						macroDataTbl.Add(macroName, new AdvMacroData(macroName, row, rowList));
					}
				}
			}

			return true;
		}

		//行のデータが、マクロ行だったら
		//出力行リストにマクロ展開して追加
		public bool TryMacroExpansion(StringGridRow row, List<StringGridRow> outputList, string debugMsg)
		{
			string commandName = AdvParser.ParseCellOptional<string>(row, AdvColumnName.Command,"");
			AdvMacroData data;
			if (!macroDataTbl.TryGetValue(commandName, out data))
			{
				//マクロ処理なし
				return false;
			}

			if (string.IsNullOrEmpty(debugMsg))
			{
				debugMsg = row.Grid.Name + ":" + (row.RowIndex + 1).ToString();
			}

			debugMsg += " -> MACRO " + data.Header.Grid.Name;
			//マクロ展開したデータを取得
			List<StringGridRow> macroRows = data.MacroExpansion(row, debugMsg);
			//マクロ展開した行を追加していく
			foreach ( StringGridRow macroRow in macroRows )
			{
				//マクロ内マクロ展開（再起呼び出し）
				if (TryMacroExpansion( macroRow, outputList, macroRow.DebugInfo) )
				{
					continue;
				}

				outputList.Add(macroRow);
			}
			return true;
		}

		const string SheetNamePattern = "Macro[0-9]";
		static readonly Regex SheetNameRegex = new Regex(SheetNamePattern, RegexOptions.IgnorePatternWhitespace);

		public static bool IsMacroName(string sheetName)
		{
			if (sheetName == "Macro") return true;
			Match match = SheetNameRegex.Match(sheetName);
			return match.Success;
		}


		bool TryParseMacoBegin(StringGridRow row, out string macroName)
		{
			return AdvCommandParser.TryParseScenarioLabel(row, AdvColumnName.Command, out macroName);
		}
	}
}