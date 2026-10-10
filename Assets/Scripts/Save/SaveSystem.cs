using System;
using System.IO;
using UnityEngine;

/// <summary>
/// セーブデータの読み書き。Application.persistentDataPath/save_1.json 〜 save_3.json に JSON で保存する (3 スロット)。
/// 番号を取らない API (<see cref="Exists()"/> / <see cref="Save(SaveData)"/> / <see cref="TryLoad()"/>) は
/// 現在のスロット <see cref="ActiveSlot"/> (はじめから/つづきから/セーブで選んだスロット) に対して働く
/// (ゲームオーバーのリトライなど、スロットを意識しない呼び出し側はそのまま使える)。
/// 旧形式の単一ファイル save.json は、初回アクセス時にスロット 1 が空なら save_1.json へ移す (章・場所・日付を補う)。
/// </summary>
public static class SaveSystem
{
    public const int SlotCount = 3;

    private const string LegacyFileName = "save.json";

    private static bool _migrated;

    /// <summary>現在のスロット (0 始まり)。はじめから/つづきから/セーブで選んだスロットになる。</summary>
    public static int ActiveSlot { get; set; }

    private static string Dir => Application.persistentDataPath;

    /// <summary>スロットのファイルパス (slot は 0 始まり、ファイル名は 1 始まり)。</summary>
    public static string PathOf(int slot) => Path.Combine(Dir, $"save_{slot + 1}.json");

    #region 現在のスロット

    public static bool Exists() => Exists(ActiveSlot);

    public static void Save(SaveData data) => Save(ActiveSlot, data);

    public static SaveData TryLoad() => TryLoad(ActiveSlot);

    public static void Delete() => Delete(ActiveSlot);

    #endregion

    #region スロット指定

    public static bool Exists(int slot)
    {
        MigrateLegacy();
        return IsValidSlot(slot) && File.Exists(PathOf(slot));
    }

    /// <summary>どれか 1 つでもセーブがあるか (タイトルの「つづきから」の有効判定)。</summary>
    public static bool AnyExists()
    {
        for (var i = 0; i < SlotCount; i++)
        {
            if (Exists(i))
                return true;
        }

        return false;
    }

    public static void Save(int slot, SaveData data)
    {
        if (!IsValidSlot(slot) || data == null)
            return;

        MigrateLegacy();
        try
        {
            File.WriteAllText(PathOf(slot), JsonUtility.ToJson(data, prettyPrint: true));
        }
        catch (IOException e)
        {
            Debug.LogError($"[SaveSystem] セーブに失敗しました (スロット {slot + 1}): {e.Message}");
        }
    }

    public static SaveData TryLoad(int slot)
    {
        if (!Exists(slot))
            return null;

        try
        {
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(PathOf(slot)));
            FillDisplayInfo(data);
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] セーブデータの読込に失敗しました (スロット {slot + 1}): {e.Message}");
            return null;
        }
    }

    public static void Delete(int slot)
    {
        if (Exists(slot))
            File.Delete(PathOf(slot));
    }

    /// <summary>
    /// セーブ日時。データに記録が無い旧形式はファイルの更新日時で代用する。読めなければ null。
    /// </summary>
    public static DateTime? SavedAt(int slot, SaveData data)
    {
        if (data != null && !string.IsNullOrEmpty(data.savedAt)
            && DateTime.TryParse(data.savedAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            return parsed;

        return Exists(slot) ? File.GetLastWriteTime(PathOf(slot)) : null;
    }

    #endregion

    private static bool IsValidSlot(int slot) => slot >= 0 && slot < SlotCount;

    /// <summary>
    /// 一覧表示用の章・場所を補う (旧形式のセーブは空、または章が「第n章」だけ)。
    /// 章: エリアのシーン (A{n}_) は現在の章名に揃える。それ以外は対応表 (StageNameCatalog) から。
    /// 場所: 空なら対応表の部屋名。対応表にも無ければ空のまま (シーン名は出さない)。
    /// 変更したら true。
    /// </summary>
    public static bool FillDisplayInfo(SaveData data)
    {
        if (data == null || string.IsNullOrEmpty(data.sceneName))
            return false;

        var changed = false;
        var entry = StageNameCatalog.Find(data.sceneName);

        var chapter = StageInfo.ChapterFromSceneName(data.sceneName);
        if (string.IsNullOrEmpty(chapter) && entry != null)
            chapter = entry.ChapterName;
        if (!string.IsNullOrEmpty(chapter) && data.chapterName != chapter)
        {
            data.chapterName = chapter;
            changed = true;
        }

        if (string.IsNullOrEmpty(data.placeName) && entry != null && !string.IsNullOrEmpty(entry.PlaceName))
        {
            data.placeName = entry.PlaceName;
            changed = true;
        }

        return changed;
    }

    /// <summary>旧形式 save.json をスロット 1 へ移す (スロット 1 が空の時だけ。1 回だけ試す)。</summary>
    private static void MigrateLegacy()
    {
        if (_migrated)
            return;

        _migrated = true;
        var legacy = Path.Combine(Dir, LegacyFileName);
        var slot1 = PathOf(0);
        if (!File.Exists(legacy) || File.Exists(slot1))
            return;

        try
        {
            // 旧形式には一覧用の章・場所・日付が無いので、移す時に補って書き直す
            var savedAt = File.GetLastWriteTime(legacy);
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(legacy));
            if (data != null)
            {
                FillDisplayInfo(data);
                if (string.IsNullOrEmpty(data.savedAt))
                    data.savedAt = savedAt.ToString("o");
                File.WriteAllText(slot1, JsonUtility.ToJson(data, prettyPrint: true));
                File.Delete(legacy);
            }
            else
            {
                File.Move(legacy, slot1);
            }

            Debug.Log("[SaveSystem] 旧形式のセーブ (save.json) をスロット 1 (save_1.json) へ移しました。");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] 旧形式のセーブの移行に失敗しました: {e.Message}");
        }
    }
}
