using System;
using R3;
using VContainer.Unity;

/// <summary>
/// 素材「糸」所持数表示の Presenter (MonoBehaviour 非依存)。
/// PlayerItemInventoryModel.Thread を購読し、テキストを整形して View に反映する。
/// UILifetimeScope からエントリポイントとして起動される。
/// </summary>
public sealed class ThreadCountPresenter : IStartable, IDisposable
{
    private readonly PlayerItemInventoryModel _inventory;
    private readonly IThreadCountView _view;

    private IDisposable _subscription;

    public ThreadCountPresenter(PlayerItemInventoryModel inventory, IThreadCountView view)
    {
        _inventory = inventory;
        _view = view;
    }

    public void Start()
    {
        _subscription = _inventory.Thread.Subscribe(thread => _view.SetText($"糸 x{thread}"));
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
