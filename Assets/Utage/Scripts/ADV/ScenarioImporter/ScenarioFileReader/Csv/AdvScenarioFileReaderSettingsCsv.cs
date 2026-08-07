using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utage
{
    [CreateAssetMenu(menuName = "Utage/ScenarioFileReader/Csv", fileName = "CsvFileReaderSettings")]
    public class AdvScenarioFileReaderSettingsCsv : ScenarioFileReaderSettings
    {
        [Serializable]
        public class FilePattern
        {
            public string ext;
            public char separator;
        }

        public List<FilePattern> FilePatternList { get; } = new()
        {
            new FilePattern() { ext = ".csv", separator = ',' },
            new FilePattern() { ext = ".tsv", separator = '\t' }
        };

        // シートのコメント記号（この文字で始まるファイル名はインポートから除外する）
        // CSVは1ファイル=1シートのため、ファイル名の先頭がこの文字ならシート全体のコメントアウトとして扱う。
        // Excel版（AdvScenarioFileReaderSettingsExcel.SheetCommentPrefix）と対称の仕様。
        // '\0' を設定するとコメントアウト除外を行わない
        [SerializeField] char sheetCommentPrefix = '#';

        public char SheetCommentPrefix
        {
            get => sheetCommentPrefix;
            set => sheetCommentPrefix = value;
        }

        public override IAdvScenarioFileReader CreateReader()
        {
            return new AdvScenarioFileReaderCsv(this);
        }
    }
}
