using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NPOI.HSSF.UserModel;
using UnityEngine;

/// <summary>
/// Assets/Dialogue 内のシナリオファイル (.tsv / .csv / .xls) から
/// シナリオラベル (*ラベル名) を抽出する共有ユーティリティ。
/// デバッグパネルや会話テストシーンのラベル一覧に使う。
/// </summary>
public static class DialogueLabelScanner
{
    private const string ProjectDir = "Assets/Dialogue";

    private static string[] _cache;
    private static DateTime _cacheTime;

    // シナリオラベルの抽出対象外にするシート (設定用シートとマクロ定義)
    private static readonly HashSet<string> NonScenarioSheetNames = new HashSet<string>
    {
        "Character", "Texture", "Sound", "Param", "ParamTbl", "Layer",
        "Localize", "SceneGallery", "Animation", "EyeBlink", "LipSynch", "Macro", "Boot",
    };

    /// <summary>全シナリオラベルを返す (ファイル更新時刻でキャッシュ)。</summary>
    public static string[] Scan(bool forceReload = false)
    {
        if (!Directory.Exists(ProjectDir))
            return Array.Empty<string>();

        var files = EnumerateScenarioFiles();
        var newest = files.Count == 0 ? DateTime.MinValue : files.Max(File.GetLastWriteTimeUtc);
        if (!forceReload && _cache != null && newest == _cacheTime)
            return _cache;

        var labels = new List<string>();
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext == ".xls")
                CollectFromExcel(file, labels);
            else
                CollectFromSeparatedText(file, ext == ".tsv" ? '\t' : ',', labels);
        }

        _cache = labels.Distinct().ToArray();
        _cacheTime = newest;
        return _cache;
    }

    private static List<string> EnumerateScenarioFiles()
    {
        return Directory.GetFiles(ProjectDir, "*.*", SearchOption.AllDirectories)
            .Where(p =>
            {
                var ext = Path.GetExtension(p).ToLowerInvariant();
                return ext == ".xls" || ext == ".tsv" || ext == ".csv";
            })
            .Select(p => p.Replace('\\', '/'))
            .ToList();
    }

    /// <summary>TSV/CSV からラベルを抽出する。1ファイル=1シート (シート名=ファイル名)。</summary>
    private static void CollectFromSeparatedText(string path, char separator, List<string> labels)
    {
        try
        {
            var sheetName = Path.GetFileNameWithoutExtension(path);
            if (NonScenarioSheetNames.Contains(sheetName))
                return;

            // Excel 等で開かれていても読めるよう、書き込み共有を許可して開く
            string[] lines;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
                lines = reader.ReadToEnd().Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

            if (lines.Length == 0)
                return;

            int colCommand = Array.FindIndex(lines[0].Split(separator), c => c.Trim() == "Command");
            if (colCommand < 0)
                return;

            for (int i = 1; i < lines.Length; i++)
            {
                var cells = lines[i].Split(separator);
                if (colCommand >= cells.Length)
                    continue;

                var value = cells[colCommand].Trim();
                if (value.Length > 1 && value[0] == '*')
                    labels.Add(value.Substring(1));
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"シナリオファイルの読み込みに失敗しました: {path}\n{e.Message}");
        }
    }

    /// <summary>Excel (.xls) からラベルを抽出する。</summary>
    private static void CollectFromExcel(string path, List<string> labels)
    {
        try
        {
            // Excel で開かれていても読めるよう、書き込み共有を許可して開く
            HSSFWorkbook book;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                book = new HSSFWorkbook(stream);

            for (int i = 0; i < book.NumberOfSheets; i++)
            {
                var sheet = book.GetSheetAt(i);
                if (NonScenarioSheetNames.Contains(sheet.SheetName))
                    continue;

                var header = sheet.GetRow(0);
                if (header == null)
                    continue;

                int colCommand = -1;
                for (int c = 0; c < header.LastCellNum; c++)
                {
                    if (header.GetCell(c)?.ToString().Trim() == "Command")
                    {
                        colCommand = c;
                        break;
                    }
                }

                if (colCommand < 0)
                    continue;

                for (int r = 1; r <= sheet.LastRowNum; r++)
                {
                    var value = sheet.GetRow(r)?.GetCell(colCommand)?.ToString().Trim();
                    if (!string.IsNullOrEmpty(value) && value.Length > 1 && value[0] == '*')
                        labels.Add(value.Substring(1));
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"シナリオ Excel の読み込みに失敗しました: {path}\n{e.Message}");
        }
    }
}
