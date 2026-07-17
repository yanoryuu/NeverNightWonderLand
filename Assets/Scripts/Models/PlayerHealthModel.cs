using System;
using R3;
using UnityEngine;

/// <summary>
/// プレイヤー HP の Model (MonoBehaviour 非依存)。
/// GameLifetimeScope に Singleton 登録され、シーンをまたいで生存する。
/// 無敵時間・点滅・ステート遷移などフレーム依存の処理は PlayerHealth (アダプタ) 側が担う。
/// </summary>
public sealed class PlayerHealthModel : IDisposable
{
    // PlayerConsts 未設定時のフォールバック (アセットのデフォルト値と揃える)
    private const int DefaultMaxHp = 10;

    private readonly ReactiveProperty<int> _hp;

    /// <summary>現在 HP の購読用 (Presenter が Subscribe する)。</summary>
    public ReadOnlyReactiveProperty<int> Hp => _hp;

    public int MaxHp { get; }

    /// <summary>HP が尽きているか。</summary>
    public bool IsDepleted => _hp.Value <= 0;

    public PlayerHealthModel(PlayerConsts consts)
    {
        MaxHp = consts != null ? consts.MaxHp : DefaultMaxHp;
        _hp = new ReactiveProperty<int>(MaxHp);
    }

    /// <summary>スポーン時に満タンへ戻す (Model はシーンをまたぐため明示リセットが必要)。</summary>
    public void ResetForSpawn() => _hp.Value = MaxHp;

    /// <summary>ダメージを適用する (0 でクランプ)。被弾可否の判定は呼び出し側で行う。</summary>
    public void Damage(int amount)
    {
        if (amount <= 0)
            return;

        _hp.Value = Mathf.Max(0, _hp.Value - amount);
    }

    /// <summary>HP を回復する (最大値でクランプ)。</summary>
    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        _hp.Value = Mathf.Min(MaxHp, _hp.Value + amount);
    }

    public void Dispose() => _hp.Dispose();
}