using System;
using R3;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// ボス HP バーの Presenter (MonoBehaviour 非依存)。
/// BossBattleModel.Boss を購読し、ボス戦の間だけ View を表示して、ボスの HP を割合で反映する。
/// UILifetimeScope からエントリポイントとして起動される。
/// </summary>
public sealed class BossHpBarPresenter : IStartable, IDisposable
{
    private readonly BossBattleModel _model;
    private readonly IBossHpBarView _view;

    private IDisposable _bossSubscription;
    private IDisposable _hpSubscription;

    public BossHpBarPresenter(BossBattleModel model, IBossHpBarView view)
    {
        _model = model;
        _view = view;
    }

    public void Start()
    {
        _bossSubscription = _model.Boss.Subscribe(OnBossChanged);
    }

    private void OnBossChanged(EnemyController boss)
    {
        _hpSubscription?.Dispose();
        _hpSubscription = null;

        if (boss == null || boss.Consts == null)
        {
            _view.Hide();
            return;
        }

        var maxHp = Mathf.Max(1, boss.Consts.MaxHp);
        _view.Show(_model.Frame, Mathf.Clamp01((float)boss.Hp.CurrentValue / maxHp));
        _hpSubscription = boss.Hp.Subscribe(hp => _view.SetRatio(Mathf.Clamp01((float)hp / maxHp)));
    }

    public void Dispose()
    {
        _hpSubscription?.Dispose();
        _bossSubscription?.Dispose();
    }
}
