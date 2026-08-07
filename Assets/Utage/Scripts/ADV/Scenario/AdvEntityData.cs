// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Profiling;
using UtageExtensions;


namespace Utage
{

	/// <summary>
	/// マクロ定義の管理クラス
	/// </summary>
	[System.Serializable]
	public class AdvEntityData
	{
		[SerializeField]
		string[] originalStrings;

		public AdvEntityData(string[] originalStrings)
		{
			this.originalStrings = originalStrings;
		}

		//今のコマンドから、エンティティ処理したコマンドを作成
		public static AdvCommand CreateEntityCommand( AdvCommand original, AdvEngine engine, AdvScenarioPageData pageData)
		{
			StringGridRow row = new StringGridRow(original.RowData.Grid, original.RowData.RowIndex);
			row.DebugIndex = original.RowData.DebugIndex;

			string[] strings = original.EntityData.CreateCommandStrings(engine.Param.GetParameter);
			row.InitFromStringArray(strings);
			AdvCommand entityCommand = AdvCommandParser.CreateCommand(original.Id, row, engine.DataManager.SettingDataManager);
			if (entityCommand is IAdvInitOnCreateEntity)
			{
				(entityCommand as IAdvInitOnCreateEntity).InitOnCreateEntity(original);
			}
			return entityCommand;
		}

		//今のパラメーターを適用して、エンティティ処理した文字列を作成
		public string[] CreateCommandStrings(System.Func<string, object> GetParameter)
		{
			Profiler.BeginSample("EntityData To CommandData");
			string[] strings = new string[this.originalStrings.Length];
			for (int i = 0; i < strings.Length; ++i)
			{
				string str = strings[i] = this.originalStrings[i];
				if (str.Length <= 1) continue;
				if (str.IndexOf('&') < 0) continue;

				StringBuilder builder = new StringBuilder();
				int index = 0;
				while (index < str.Length)
				{
					if (str[index] == '&')
					{
						bool isEntity = false;
						int index2 = index + 1;
						while (index2 < str.Length)
						{
							if (index2 == str.Length - 1 || CheckEntitySeparator(str[index2 + 1]))
							{
								string key = str.Substring(index + 1, index2 - index);
								object param = GetParameter(key);
								if (param != null)
								{
									builder.Append(param.ToString());
									index = index2 + 1;
									isEntity = true;
								}
								break;
							}
							else
							{
								++index2;
							}
						}
						if (isEntity)
						{
							continue;
						}
					}

					builder.Append(str[index]);
					++index;
				}
				strings[i] = builder.ToString();
			}
			Profiler.EndSample();
			return strings;
		}


		//エンティティがあるかの簡易チェック
		public static bool ContainsEntitySimple(StringGridRow row)
		{
			for (int i = 0; i < row.Strings.Length; ++i)
			{
				int index = row.Strings[i].IndexOf('&');
				if (index < 0) continue;

				string str = row.Strings[i];
				if (index+1 < str.Length && str[index + 1] == '&')
				{
					continue;
				}
				return true;
			}
			return false;
		}

		//今のパラメーターを適用して、エンティティ処理した文字列を作成
		public static bool TryCreateEntityStrings(StringGridRow original, System.Func<string, object> GetParameter, out string[] strings)
		{
			bool succeed = false;
			strings = new string[original.Strings.Length];
			for (int i = 0; i < original.Strings.Length; ++i)
			{
				string str = strings[i] = original.Strings[i];
				if (str.Length <= 1) continue;
				if (str.IndexOf('&') < 0) continue;

				//WindowTypeとPageCtrlはエンティティによる変換を無視する
				int indexWindowType;
				if (original.Grid.TryGetColumnIndex(AdvColumnName.WindowType.QuickToString(), out indexWindowType))
				{
					if (i == indexWindowType)
					{
						Debug.LogError(" Can not use entity in " + AdvColumnName.WindowType.QuickToString());
						return false;
					}
				}
				int indexPageCtrl;
				if (original.Grid.TryGetColumnIndex(AdvColumnName.PageCtrl.QuickToString(), out indexPageCtrl))
				{
					if (i == indexPageCtrl)
					{
						Debug.LogError(" Can not use entity in " + AdvColumnName.PageCtrl.QuickToString());
						return false;
					}
				}

				StringBuilder builder = new StringBuilder();
				int index = 0;
				while (index < str.Length)
				{
					if (str[index] == '&')
					{
						bool isEntity = false;
						int index2 = index + 1;
						while (index2 < str.Length)
						{
							if (index2 == str.Length - 1 || CheckEntitySeparator(str[index2 + 1]))
							{
								string key = str.Substring(index + 1, index2 - index);
								object param = GetParameter(key);
								if (param != null)
								{
									builder.Append(param.ToString());
									index = index2 + 1;
									isEntity = true;
								}
								break;
							}
							else
							{
								++index2;
							}
						}
						if (isEntity)
						{
							succeed = true;
							continue;
						}
					}

					builder.Append(str[index]);
					++index;
				}
				strings[i] = builder.ToString();
			}
			return succeed;
		}

/*
		//今のコマンドから、エンティティ処理したコマンドを作成
		public static AdvCommand CreateEntityCommand( AdvCommand original, AdvEngine engine, AdvScenarioPageData pageData)
		{
			StringGridRow row;
			if (!TryParseRowDataEntity(original.EntityData, engine.Param.GetParameter, out row))
			{
				Debug.LogError("Enity Parse Error");
				return original;
			}

			AdvCommand entityCommand = AdvCommandParser.CreateCommand( original.Id, row, engine.DataManager.SettingDataManager);
			if (entityCommand is AdvCommandText)
			{
				AdvCommandText textCommand = entityCommand as AdvCommandText;
				textCommand.InitOnCreateEntity(original as AdvCommandText);
			}
			return entityCommand;
		}

		//今のコマンドから、エンティティ処理したデータを作成
		static public bool TryParseRowDataEntity(StringGridRow original, System.Func<string, object> GetParameter, out StringGridRow row)
		{
			Profiler.BeginSample("TryParseRowDataEntity");
			bool ret = false;
			row = original.Clone(original.Grid);

			List<int> ignoreIndex = AdvParser.CreateMacroOrEntityIgnoreIndexArray(original.Grid);
			for (int i = 0; i < row.Strings.Length; ++i)
			{
				string str = row.Strings[i];
				if (ignoreIndex.Contains(i)) continue;
				if (str.Length <= 1) continue;
				if (!str.Contains("&")) continue;


				StringBuilder builder = new StringBuilder();
				int index = 0;
				while (index < str.Length)
				{
					if (str[index] == '&')
					{
						bool isEntity = false;
						int index2 = index + 1;
						while (index2 < str.Length)
						{
							if (index2 == str.Length - 1 || CheckEntitySeparator(str[index2 + 1]))
							{
								string key = str.Substring(index + 1, index2 - index);
								object param = GetParameter(key);
								if (param != null)
								{
									builder.Append(param.ToString());
									index = index2 + 1;
									isEntity = true;
								}
								break;
							}
							else
							{
								++index2;
							}
						}
						if (isEntity)
						{
							ret = true;
							continue;
						}
					}

					builder.Append(str[index]);
					++index;
				}
				row.Strings[i] = builder.ToString();
			}
			Profiler.EndSample();
			return ret;
		}
*/
		static bool CheckEntitySeparator(char c)
		{
			switch (c)
			{
				case '[':
				case ']':
				case '.':
					return true;
				default:
					return ExpressionToken.CheckSeparator(c);
			}
		}

	}
}
