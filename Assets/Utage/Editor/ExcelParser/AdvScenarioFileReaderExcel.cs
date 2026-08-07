// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura


namespace Utage.ExcelParser
{
	//エクセル形式のシナリオリーダー
	public class AdvScenarioFileReaderExcel : IAdvScenarioFileReader
	{
		AdvScenarioFileReaderSettingsExcel Settings { get; }

		public AdvScenarioFileReaderExcel(AdvScenarioFileReaderSettingsExcel settings)
		{
			Settings = settings;
		}

		public bool IsTargetFile(string path)
		{
			if (string.IsNullOrEmpty(path)) return false;
			if (!ExcelParser.IsExcelFile(path)) return false;

			var fileName = FilePathUtil.GetFileName(path);
			foreach (var prefix in Settings.IgnorePrefixes)
			{
				if (fileName.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
			}

			return true;
		}

		public bool TryReadFile(string path, out StringGridDictionary stringGridDictionary)
		{
			if (!IsTargetFile(path))
			{
				stringGridDictionary = null;
				return false;
			}

			stringGridDictionary = ExcelParser.Read(path, Settings.SheetCommentPrefix,
				Settings.ParseFormula, Settings.ParseNumeric, Settings.ParseHeader);
			// ExcelParser.Read はコメントシートを空グリッドとして残すため、RemoveSheets で除外する
			if (Settings.SheetCommentPrefix != '\0')
			{
				stringGridDictionary.RemoveSheets("^" + Settings.SheetCommentPrefix);
			}
			return true;
		}
	}
}
