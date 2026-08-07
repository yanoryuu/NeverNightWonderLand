// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UtageExtensions;


namespace Utage
{

	/// <summary>
	/// マクロのデータ
	/// </summary>
	public class AdvMacroData
	{
		//マクロ名
		public string Name { get; private set; }

		//マクロのヘッダ部分（マクロ名の行と同じで、引数が入る）
		public StringGridRow Header { get; private set; }

		//マクロ部分のデータ
		public List<StringGridRow> DataList { get; private set; }

		//構造化マクロ（マクロ引数をプロパティ参照可能にするための拡張）の際に使用する、プロパティ情報
		StructuredMacroParsedProperty StructuredMacroParsedProperty { get; }
		
		public AdvMacroData(string name, StringGridRow header, List<StringGridRow> dataList)
		{
			this.Name = name;
			this.Header = header;
			this.DataList = dataList;
			
			StructuredMacroParser structuredMacroParser = CustomProjectSetting.Instance.StructuredMacroParser;
			if (structuredMacroParser == null)
			{
				StructuredMacroParsedProperty = null;
			}
			else
			{
				StructuredMacroParsedProperty = new StructuredMacroParsedProperty(this,structuredMacroParser);
			}
		}

		//指定の行をマクロ展開
		public List<StringGridRow> MacroExpansion(StringGridRow args, string debugMsg)
		{
			//マクロ展開後の行リスト
			List<StringGridRow> list = new List<StringGridRow>();
			if (DataList.Count <= 0) return list;

			if (StructuredMacroParsedProperty != null)
			{
				StructuredMacroParsedProperty.StartRowParse();
			}

			//展開先の列数と同じ数のセル（文字列の配列）をもつ
			int maxStringCount = 0;
			foreach (var keyValue in args.Grid.ColumnIndexTbl)
			{
				maxStringCount = Mathf.Max(keyValue.Value, maxStringCount);
			}

			maxStringCount += 1;
			for (int i = 0; i < DataList.Count; ++i)
			{
				string[] strings = new string[maxStringCount];
				for (int index = 0; index < strings.Length; ++index)
				{
					strings[index] = "";
				}

				StringGridRow data = DataList[i];
				//展開先の列数と同じ数のセル（文字列の配列）をもつ
				foreach (var keyValue in args.Grid.ColumnIndexTbl)
				{
					string argKey = keyValue.Key;
					int argIndex = keyValue.Value;
					strings[argIndex] = ParseMacroArg(data.ParseCellOptional<string>(argKey, ""), args);
				}

				//展開先のシートの構造に合わせる
				//展開先シートを親Girdに持ち
				StringGridRow macroData = new StringGridRow(args.Grid, args.RowIndex);
				macroData.InitFromStringArray(strings);
				list.Add(macroData);

				//デバッグ情報の記録
				macroData.DebugInfo = debugMsg + " : " + (data.RowIndex + 1) + " ";
			}

			return list;
		}

		//マクロ引数展開
		string ParseMacroArg(string str, StringGridRow args)
		{
			//構造化マクロ引数のための拡張があれば取得しておく（ないならnullになる）
			StructuredMacroParser structuredMacroParser = CustomProjectSetting.Instance.StructuredMacroParser;
			int index = 0;
			string macroText = "";
			while (index < str.Length)
			{
				bool isFind = false;

				//先頭が%だったら引数として解析
				if (str[index] == '%')
				{
					foreach (string key in Header.Grid.ColumnIndexTbl.Keys)
					{
						if (key.Length <= 0) continue;

						//%以下の文字と一致している列名があるか検索
						for (int i = 0; i < key.Length; ++i)
						{
							if (key[i] != str[index + 1 + i])
							{
								break;
							}
							else if (i == key.Length - 1)
							{
								isFind = true;
							}
						}


						if (isFind)　//列名が一致したので、引数として展開
						{
							//構造化マクロ引数展開をするかチェック
							bool parseStructured = false;
							if (structuredMacroParser != null)
							{
								//構造化マクロ引数展開をした場合の解析
								var result = structuredMacroParser.TryParseStructuredMacroArguments(str, index, key, args, StructuredMacroParsedProperty);
								if (result.Success)
								{
									//引数を展開
									macroText += result.Expanded;
									//文字位置を進める
									index = result.NewIndex;
									parseStructured = true;
								}
							}

							if (!parseStructured)
							{
								//デフォルト引数を取得
								string def = Header.ParseCellOptional<string>(key, "");
								//引数を展開
								macroText += args.ParseCellOptional<string>(key, def);
								//文字位置を進める
								index += key.Length;
							}

							break;
						}
					}
				}

				//引数が見つからなかったので、そのまま文字を追加
				if (!isFind)
				{
					macroText += str[index];
				}

				++index;
			}
			return macroText;
		}
	}
}