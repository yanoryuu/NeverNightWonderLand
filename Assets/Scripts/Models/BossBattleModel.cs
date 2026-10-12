using System;
using System.Collections.Generic;
using R3;

/// <summary>ボス HP バーの枠の種類 (Docs/UI/UIAssignment.md「HUD の HP バー」)。</summary>
public enum BossHpFrame
{
    /// <summary>1戦目 (B1 ボス部屋) の枠 (firsthp)。</summary>
    First,

    /// <summary>2戦目 (エリアボス) の枠 (lasthp)。</summary>
    Last,
}

/// <summary>
/// 戦闘中のボスへの実行時参照 (MonoBehaviour 非依存)。
/// GameLifetimeScope に Singleton 登録され、BossArena が戦闘開始/終了で登録・解除し、
/// HUD (BossHpBarPresenter) が購読して画面下のボス HP バーを出し入れする。戦闘中でなければ null。
/// </summary>
public sealed class BossBattleModel : IDisposable
{
    // 破棄済みのボスは Unity の == で null と等しくなり、null への切替が通知されなくなるので参照で比べる
    private readonly ReactiveProperty<EnemyController> _boss = new(null, ReferenceEqualityComparer<EnemyController>.Instance);
    private bool _disposed;

    /// <summary>戦闘中のボスの購読用。</summary>
    public ReadOnlyReactiveProperty<EnemyController> Boss => _boss;

    /// <summary>戦闘中のボスの HP バーの枠。Boss が変わる前に更新される。</summary>
    public BossHpFrame Frame { get; private set; }

    public void Begin(EnemyController boss, BossHpFrame frame)
    {
        if (_disposed)
            return;

        Frame = frame;
        _boss.Value = boss;
    }

    public void End(EnemyController boss)
    {
        // アプリ終了時はルートスコープと闘技場の破棄順序が不定なので、破棄済みなら何もしない
        if (_disposed)
            return;

        if (ReferenceEquals(_boss.Value, boss))
            _boss.Value = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _boss.Dispose();
    }
}

/// <summary>参照の一致だけで比べる比較子 (UnityEngine.Object の == の上書きを避ける)。</summary>
internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
{
    public static readonly ReferenceEqualityComparer<T> Instance = new();

    public bool Equals(T x, T y) => ReferenceEquals(x, y);

    public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
}
