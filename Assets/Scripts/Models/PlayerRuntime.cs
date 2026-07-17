using System;
using R3;

/// <summary>
/// 現在シーンに存在するプレイヤーへの実行時参照 (MonoBehaviour 非依存)。
/// GameLifetimeScope に Singleton 登録され、PlayerController が Awake/OnDestroy で
/// 登録・解除し、UI (Presenter) が購読する。未スポーン時やシーン遷移中は null。
/// </summary>
public sealed class PlayerRuntime : IDisposable
{
    private readonly ReactiveProperty<PlayerController> _current = new(null);
    private bool _disposed;

    /// <summary>現在のプレイヤーの購読用。</summary>
    public ReadOnlyReactiveProperty<PlayerController> Current => _current;

    public void Register(PlayerController player)
    {
        if (_disposed)
            return;

        _current.Value = player;
    }

    public void Unregister(PlayerController player)
    {
        // アプリ終了時はルートスコープ (本クラスを Dispose する) とプレイヤーの
        // 破棄順序が不定なので、破棄済みなら何もしない
        if (_disposed)
            return;

        if (_current.Value == player)
            _current.Value = null;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _current.Dispose();
    }
}
