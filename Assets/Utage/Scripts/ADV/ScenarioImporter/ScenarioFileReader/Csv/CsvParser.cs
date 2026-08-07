// UTAGE: Unity Text Adventure Game Engine (c) Ryohei Tokimura

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Utage
{
    public class CsvParser
    {
        //エンコーディング
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        //区切り文字
        public char Delimiter { get; set; } = ',';

        //書き出し時にフィールド内の改行コードを\r\nに正規化するか
        public bool NormalizeNewLinesOnWrite { get; set; } = true;
        
        const char DoubleQuotes = '"';

        public StringGrid ReadFile(string path)
        {
            string sheet = FilePathUtil.GetFileNameWithoutExtension(path);
            StringGrid grid = new StringGrid(path, sheet, Delimiter == '\t' ? CsvType.Tsv : CsvType.Csv);
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs, Encoding);
            string text = reader.ReadToEnd();

            int lineNo = 0;
            List<List<string>> rows;
            try
            {
                rows = ParseRows(text);
            }
            catch (Exception e)
            {
                Debug.LogError($"Invalid CSV format. {path} {e.Message}");
                return null;
            }
            foreach (var row in rows)
            {
                try
                {
                    grid.AddRow(row);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Invalid CSV format. {path} Line:{lineNo} {e.Message}");
                    return null;
                }

                ++lineNo;
            }

            return grid;
        }

        // テキスト全体を文字単位でパースし、行（フィールドのリスト）のリストを返す
        List<List<string>> ParseRows(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;
            int i = 0;
            int currentLine = 1;
            int quoteStartLine = 0;

            while (i < text.Length)
            {
                char c = text[i];

                if (inQuotes)
                {
                    if (c == DoubleQuotes)
                    {
                        // 次の文字もダブルクォートならエスケープ（""→"）
                        if (i + 1 < text.Length && text[i + 1] == DoubleQuotes)
                        {
                            field.Append(DoubleQuotes);
                            i += 2;
                        }
                        else
                        {
                            // クォート終了
                            inQuotes = false;
                            ++i;
                        }
                    }
                    else
                    {
                        // クォート内ではそのまま追加（改行含む）
                        if (c == '\n') ++currentLine;
                        field.Append(c);
                        ++i;
                    }
                }
                else
                {
                    if (c == DoubleQuotes && field.Length == 0)
                    {
                        // フィールド先頭のクォートのみクォート開始として扱う（RFC 4180準拠）
                        inQuotes = true;
                        quoteStartLine = currentLine;
                        ++i;
                    }
                    else if (c == Delimiter)
                    {
                        // フィールド区切り
                        row.Add(field.ToString());
                        field.Clear();
                        ++i;
                    }
                    else if (c == '\r' || c == '\n')
                    {
                        // 行末：フィールドを確定して行を追加
                        row.Add(field.ToString());
                        field.Clear();
                        rows.Add(row);
                        row = new List<string>();
                        ++currentLine;

                        // \r\n を1つの改行として扱う
                        if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                        {
                            i += 2;
                        }
                        else
                        {
                            ++i;
                        }
                    }
                    else
                    {
                        field.Append(c);
                        ++i;
                    }
                }
            }

            if (inQuotes)
            {
                throw new Exception($"Double quotes are not closed. (line:{quoteStartLine})");
            }

            // 最後のフィールド・行を追加（末尾改行なしの場合）
            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            return rows;
        }

        public void WriteFile(string path, StringGrid grid)
        {
            FileIoUtil.CreateFilePathDirectoryIfNotExists(path);
            File.WriteAllText(path, ToText(grid), Encoding);
        }

        string ToText(StringGrid grid)
        {
            var builder = new StringBuilder();
            foreach (StringGridRow row in grid.Rows)
            {
                for (int i = 0; i < row.Strings.Length; ++i)
                {
                    var str = row.Strings[i];
                    if (NormalizeNewLinesOnWrite)
                    {
                        str = str.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
                    }
                    //必要な場合はエンクローズ処理をして書き込み
                    builder.Append(EncloseIfNeeded(str));
                    if (i < row.Strings.Length - 1)
                    {
                        //区切り文字を追加
                        builder.Append(Delimiter);
                    }
                }

                //改行
                builder.Append("\r\n");
            }

            return builder.ToString();
        }

        //エンクローズ処理
        string EncloseIfNeeded(string field)
        {
            (bool needEnclose, bool hasDoubleQuotes) = CheckEnclose(field);
            if (needEnclose)
            {
                if (hasDoubleQuotes)
                {
                    //ダブルクオートが含まれる場合は、二重にする
                    field = field.Replace("\"", "\"\"");
                }
                //ダブルクオートで囲む
                field = DoubleQuotes + field + DoubleQuotes;
            }
            return field;
        }

        //ダブルクオートで囲む必要があるかチェック
        //ダブルクオートが含まれていたら、needEncloseとhasDoubleQuotes両方ともtrue
        //区切り文字、改行文字が含まれていたら、needEncloseのみtrue
        (bool needEnclose, bool hasDoubleQuotes) CheckEnclose(string field)
        {
            bool needEnclose = false;
            foreach (var c in field)
            {
                if (c == DoubleQuotes)
                {
                    return (true, true);
                }

                if (!needEnclose)
                {
                    if (c == Delimiter || c == '\n' || c == '\r')
                    {
                        needEnclose = true;
                    }
                }
            }
            return (needEnclose, false);
        }
    }
}
