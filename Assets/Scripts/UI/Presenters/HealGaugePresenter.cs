using System;
using R3;
using VContainer.Unity;

/// <summary>
/// 回復ゲージの Presenter (MonoBehaviour 非依存)。
/// HealGaugeModel の Pips / Charge を購読して View に反映する。
/// UILifetimeScope からエントリポイントとして起動される。
/// </summary>
public sealed class HealGaugePresenter : IStartable, IDisposable
{
    private readonly HealGaugeModel _model;
    private readonly IHealGaugeView _view;

    private readonly CompositeDisposable _disposables = new();

    public HealGaugePresenter(HealGaugeModel model, IHealGaugeView view)
    {
        _model = model;
        _view = view;
    }

    public void Start()
    {
        _model.Pips.Subscribe(_ => Refresh()).AddTo(_disposables);
        _model.Charge.Subscribe(_ => Refresh()).AddTo(_disposables);
    }

    private void Refresh()
    {
        _view.SetGauge(_model.Pips.CurrentValue, _model.Charge.CurrentValue);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
