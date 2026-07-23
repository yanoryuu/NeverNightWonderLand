using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 倒した敵の記録 (静的ハブ)。一度倒した敵は拠点で休むまで再出現しない。
/// ステージ遷移 (Additive 入替) をまたいで保持され、拠点 (HomeUIView) で休むか
/// 全リセット (StageLoader.LoadWithPlayerScene) でクリアされて復活する。
/// 敵の同定は「ステージ名+初期位置+名前」の ID で行う (EnemyController が生成する)。
/// </summary>
public static class DefeatedEnemyRegistry
{
    private static readonly HashSet<string> Defeated = new();

    /// <summary>この ID の敵が撃破済みか。</summary>
    public static bool IsDefeated(string id) =>
        !string.IsNullOrEmpty(id) && Defeated.Contains(id);

    /// <summary>撃破を記録する (EnemyController.Die から呼ばれる)。</summary>
    public static void MarkDefeated(string id)
    {
        if (!string.IsNullOrEmpty(id))
            Defeated.Add(id);
    }

    /// <summary>記録をクリアして敵を復活させる (拠点で休んだ時・全リセット時)。</summary>
    public static void Clear() => Defeated.Clear();

    // Enter Play Mode Settings で Domain Reload を切っていても、再生をまたいで持ち越さない
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Defeated.Clear();
}
