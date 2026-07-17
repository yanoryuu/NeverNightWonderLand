using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// HUD (UI) 用の子 LifetimeScope。
/// 親 (GameLifetimeScope) は VContainerSettings の RootLifetimeScope 経由で自動解決されるため、
/// UI を別シーンに分けて Additive でロードしても Model へそのまま接続できる。
/// 割り当てられた View を登録し、対応する Presenter をエントリポイントとして起動する。
/// View が未割り当ての項目はスキップされる (部分的な HUD でも動作する)。
/// </summary>
public class UILifetimeScope : LifetimeScope
{
    [Header("HUD Views (未割り当ての項目はスキップ)")]
    [SerializeField] private PlayerHpBarView _hpBarView;
    [SerializeField] private HealGaugeView _healGaugeView;
    [SerializeField] private AttackLoadoutView _attackLoadoutView;
    [SerializeField] private SpecialCooldownView _specialCooldownView;
    [SerializeField] private ItemSlotView _itemSlotView;
    [SerializeField] private ThreadCountView _threadCountView;
    [SerializeField] private FinisherPromptView _finisherPromptView;

    protected override void Configure(IContainerBuilder builder)
    {
        if (_hpBarView != null)
        {
            builder.RegisterComponent(_hpBarView).As<IPlayerHpBarView>();
            builder.RegisterEntryPoint<PlayerHpBarPresenter>();
        }

        if (_healGaugeView != null)
        {
            builder.RegisterComponent(_healGaugeView).As<IHealGaugeView>();
            builder.RegisterEntryPoint<HealGaugePresenter>();
        }

        if (_attackLoadoutView != null)
        {
            builder.RegisterComponent(_attackLoadoutView).As<IAttackLoadoutView>();
            builder.RegisterEntryPoint<AttackLoadoutPresenter>();
        }

        if (_specialCooldownView != null)
        {
            builder.RegisterComponent(_specialCooldownView).As<ISpecialCooldownView>();
            builder.RegisterEntryPoint<SpecialCooldownPresenter>();
        }

        if (_itemSlotView != null)
        {
            builder.RegisterComponent(_itemSlotView).As<IItemSlotView>();
            builder.RegisterEntryPoint<ItemSlotPresenter>();
        }

        if (_threadCountView != null)
        {
            builder.RegisterComponent(_threadCountView).As<IThreadCountView>();
            builder.RegisterEntryPoint<ThreadCountPresenter>();
        }

        if (_finisherPromptView != null)
        {
            builder.RegisterComponent(_finisherPromptView).As<IFinisherPromptView>();
            builder.RegisterEntryPoint<FinisherPromptPresenter>();
        }
    }
}
