using System;
using R3;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// 装備中攻撃方法表示の Presenter (MonoBehaviour 非依存)。
/// PlayerAttackLoadoutModel の装備枠 (近接/特殊) を購読し、表示名を決めて View に反映する。
/// 初期値の反映ではフラッシュせず、入れ替え時のみフラッシュを指示する。
/// UILifetimeScope からエントリポイントとして起動される。
/// </summary>
public sealed class AttackLoadoutPresenter : IStartable, IDisposable
{
    private const string EmptyName = "---";

    private readonly PlayerAttackLoadoutModel _model;
    private readonly IAttackLoadoutView _view;

    private readonly CompositeDisposable _disposables = new();
    private bool _initialized;

    public AttackLoadoutPresenter(PlayerAttackLoadoutModel model, IAttackLoadoutView view)
    {
        _model = model;
        _view = view;
    }

    public void Start()
    {
        // Subscribe は購読時に現在値を即座に流すため、_initialized が立つ前の
        // 呼び出し (初期反映) ではフラッシュしない
        _model.Melee.Subscribe(_ => Refresh()).AddTo(_disposables);
        _model.Special.Subscribe(_ => Refresh()).AddTo(_disposables);
        _initialized = true;
    }

    private void Refresh()
    {
        var melee = _model.Melee.CurrentValue;
        var special = _model.Special.CurrentValue;

        _view.SetLoadout(
            melee != null ? melee.DisplayName : EmptyName,
            special != null ? special.DisplayName : EmptyName,
            special != null ? special.IconColor : Color.gray,
            flash: _initialized);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
