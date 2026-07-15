using System.IO;
using UnityEngine;

/// <summary>
/// セーブデータの読み書き。Application.persistentDataPath/save.json に JSON で保存する。
/// スロットは1つ (企画書のセーブデータ選択は採用時に拡張)。
/// </summary>
public static class SaveSystem
{
    private static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static bool Exists() => File.Exists(FilePath);

    public static void Save(SaveData data)
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data, prettyPrint: true));
        }
        catch (IOException e)
        {
            Debug.LogError($"[SaveSystem] セーブに失敗しました: {e.Message}");
        }
    }

    public static SaveData TryLoad()
    {
        if (!Exists())
            return null;

        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SaveSystem] セーブデータの読込に失敗しました: {e.Message}");
            return null;
        }
    }

    public static void Delete()
    {
        if (Exists())
            File.Delete(FilePath);
    }
}
