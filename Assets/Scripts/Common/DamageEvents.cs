using System;
using UnityEngine;

/// <summary>
/// ダメージ発生をワールド全体へ通知する静的イベントハブ。
/// 敵・箱などのダメージ適用側が Raise し、DamageFloaterSpawner(数値表示)が購読する。
/// 対象ごとの配線を不要にするための仕組みで、購読側は OnDestroy で必ず解除すること。
/// </summary>
public static class DamageEvents
{
    /// <summary>ダメージの種別。フローターの色分けに使う。</summary>
    public enum Kind
    {
        /// <summary>HP(赤ゲージ)へのダメージ。</summary>
        Hp,

        /// <summary>防御値(白ゲージ)へのダメージ(崩し)。</summary>
        Guard,
    }

    /// <summary>(ワールド座標, 適用量, 種別)</summary>
    public static event Action<Vector2, int, Kind> OnDamageApplied;

    public static void Raise(Vector2 position, int amount, Kind kind)
    {
        if (amount <= 0)
            return;

        OnDamageApplied?.Invoke(position, amount, kind);
    }
}
