using System;
using R3;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// HP バーの Presenter (MonoBehaviour 非依存)。
/// PlayerHealthModel.Hp を購読し、割合に変換して View に反映する。
/// UILifetimeScope からエントリポイントとして起動される。
/// </summary>
public sealed class PlayerHpBarPresenter : IStartable, IDisposable
{
    private readonly PlayerHealthModel _model;
    private readonly IPlayerHpBarView _view;

    private IDisposable _subscription;

    public PlayerHpBarPresenter(PlayerHealthModel model, IPlayerHpBarView view)
    {
        _model = model;
        _view = view;
    }

    public void Start()
    {
        _subscription = _model.Hp.Subscribe(hp =>
            _view.SetRatio(Mathf.Clamp01((float)hp / _model.MaxHp)));
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
