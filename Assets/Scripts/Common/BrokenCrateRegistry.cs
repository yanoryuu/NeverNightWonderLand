using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 壊した木箱の記録 (静的ハブ。<see cref="DefeatedEnemyRegistry"/> と同じ扱い)。
/// フラグを持たない <see cref="BreakableBox"/> (ふつうの木箱・コインの箱) は、壊すとここに記録され、
/// 部屋を出入りしても壊れたまま。拠点 (SavePoint) で休むか全リセット (StageLoader.LoadWithPlayerScene) で
/// クリアされて全部元に戻る (2026-10-04 PendingChanges 31: 木箱はセーブするとリセット)。
/// セーブファイルには保存しない: セーブ = 拠点で休んだ直後なので、復帰時は全部の箱が元どおりで正しい。
/// 箱の同定は「シーン名+オブジェクト名+初期位置」の ID (BreakableBox が生成する)。
/// </summary>
public static class BrokenCrateRegistry
{
    private static readonly HashSet<string> Broken = new();

    /// <summary>この ID の箱が壊れたままか。</summary>
    public static bool IsBroken(string id) =>
        !string.IsNullOrEmpty(id) && Broken.Contains(id);

    /// <summary>壊れたことを記録する (BreakableBox.Break から呼ばれる)。</summary>
    public static void MarkBroken(string id)
    {
        if (!string.IsNullOrEmpty(id))
            Broken.Add(id);
    }

    /// <summary>記録をクリアして箱を全部元に戻す (拠点で休んだ時・全リセット時)。</summary>
    public static void Clear() => Broken.Clear();

    /// <summary>記録している箱の数 (デバッグ・テスト用)。</summary>
    public static int Count => Broken.Count;

    // Enter Play Mode Settings で Domain Reload を切っていても、再生をまたいで持ち越さない
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Broken.Clear();
}
