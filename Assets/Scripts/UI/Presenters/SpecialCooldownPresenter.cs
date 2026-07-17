using System;
using R3;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// 特殊攻撃クールダウン円形表示の Presenter (MonoBehaviour 非依存)。
/// 装備中の特殊攻撃 (PlayerAttackLoadoutModel) を購読してアイコンを更新し、
/// クールダウンの残り割合は PlayerRuntime 経由で毎フレーム反映する。
/// UILifetimeScope からエントリポイントとして起動される。
/// </summary>
public sealed class SpecialCooldownPresenter : IStartable, ITickable, IDisposable
{
    private readonly PlayerAttackLoadoutModel _loadout;
    private readonly PlayerRuntime _playerRuntime;
    private readonly ISpecialCooldownView _view;

    private IDisposable _subscription;

    public SpecialCooldownPresenter(
        PlayerAttackLoadoutModel loadout, PlayerRuntime playerRuntime, ISpecialCooldownView view)
    {
        _loadout = loadout;
        _playerRuntime = playerRuntime;
        _view = view;
    }

    public void Start()
    {
        _subscription = _loadout.Special.Subscribe(special =>
            _view.SetIcon(special != null ? special.IconColor : Color.gray, special != null));
    }

    public void Tick()
    {
        var player = _playerRuntime.Current.CurrentValue;
        var ratio = player != null ? player.SpecialCooldownRatio : 0f;
        var hasSpecial = _loadout.Special.CurrentValue != null;

        _view.SetCooldown(ratio, ready: hasSpecial && ratio <= 0f);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }
}
