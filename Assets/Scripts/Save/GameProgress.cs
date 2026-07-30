using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ゲーム進行フラグ (中ボス撃破・イベント達成など) の静的管理。
/// DefeatedEnemyRegistry と違い拠点で休んでもクリアされない永続進行で、
/// セーブデータ (SaveData.progressFlags) に保存される。
/// フラグ名の例: "MidBoss1" / "UpperBoss" / "LowerBoss"。
/// </summary>
public static class GameProgress
{
    private static readonly HashSet<string> Flags = new();

    /// <summary>フラグが新しく立った時に発火する (同シーン内の FlagDoor の即時開放などに使う)。</summary>
    public static event System.Action<string> Changed;

    public static bool Has(string flag) =>
        !string.IsNullOrEmpty(flag) && Flags.Contains(flag);

    public static void Set(string flag)
    {
        if (string.IsNullOrEmpty(flag) || !Flags.Add(flag))
            return;

        Changed?.Invoke(flag);
    }

    /// <summary>セーブ用に全フラグを集める。</summary>
    public static string[] Collect() => Flags.ToArray();

    /// <summary>セーブデータから復元する。</summary>
    public static void LoadFrom(SaveData data)
    {
        Flags.Clear();
        if (data?.progressFlags == null)
            return;

        foreach (var flag in data.progressFlags)
            Set(flag);
    }

    /// <summary>フラグを取り下げる (デバッグ用。イベントは発火しない)。</summary>
    public static void Unset(string flag)
    {
        if (!string.IsNullOrEmpty(flag))
            Flags.Remove(flag);
    }

    /// <summary>全消去 (ニューゲーム用)。</summary>
    public static void Clear() => Flags.Clear();
}
