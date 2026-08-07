using System;
using UnityEditor;
using UnityEngine;
using Utage;

/// <summary>
/// 会話シナリオ (Assets/Dialogue/Scenarios/*.tsv, *.xls) の再インポートメニュー。
/// TSV を編集したあとに実行して book アセットへ反映する。
/// テキスト検証はオフ (DisableTextValidate)、例外は握りつぶさずログに出す。
/// </summary>
public static class DialogueScenarioImporter
{
    [MenuItem("NeverNight/会話シナリオを再インポート")]
    public static void Reimport()
    {
        try
        {
            AssetDatabase.Refresh();

            var project = AdvScenarioDataBuilderWindow.ProjectData;
            if (project == null)
            {
                Debug.LogError("Utage のシナリオプロジェクトが未設定です。Tools > Utage > Scenario Data Builder で Dialogue.project を設定してください。");
                return;
            }

            var importer = new AdvScenarioImporterInEditor(project) { DisableTextValidate = true };
            importer.ImportAll();
            AssetDatabase.SaveAssets();
            Debug.Log($"シナリオを再インポートしました。ラベル: {string.Join(", ", DialogueLabelScanner.Scan(forceReload: true))}");
        }
        catch (Exception e)
        {
            Debug.LogError($"シナリオの再インポートで例外が発生しました。この内容を確認してください:\n{e}");
        }
    }
}
