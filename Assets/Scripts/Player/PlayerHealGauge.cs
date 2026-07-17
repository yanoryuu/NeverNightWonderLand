using System;
using R3;
using UnityEngine;
using VContainer;

/// <summary>
/// 回復ゲージ (MonoBehaviour アダプタ)。ゲージの実体は <see cref="HealGaugeModel"/> が持ち、
/// 本クラスはゲームプレイ側 (PlayerController / HealState) との橋渡しを担う。
/// Model は PlayerLifetimeScope (プレハブ同梱) 経由で注入され、DI の無いシーンでは自前生成にフォールバックする。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerHealGauge : MonoBehaviour
{
    private HealGaugeModel _model;
    private bool _ownsModel;

    /// <summary>蓄積中メモリの割合 (0..1)。HUD の部分蓄積表示用。</summary>
    public ReadOnlyReactiveProperty<float> Charge => _model.Charge;

    /// <summary>使用可能なメモリ数。</summary>
    public ReadOnlyReactiveProperty<int> Pips => _model.Pips;

    /// <summary>回復を実行した時に発火する (チュートリアルの達成判定用)。</summary>
    public event Action OnHealUsed;

    [Inject]
    public void Construct(HealGaugeModel model)
    {
        _model = model;
    }

    private void Awake()
    {
        if (_model == null)
        {
            _model = new HealGaugeModel(GetComponent<PlayerController>().Consts);
            _ownsModel = true;
        }

        // Model はシーンをまたいで生存するため、スポーン時に空へ戻す
        _model.ResetForSpawn();
        _model.HealUsed += HandleHealUsed;
    }

    private void OnDestroy()
    {
        _model.HealUsed -= HandleHealUsed;
        if (_ownsModel)
            _model.Dispose();
    }

    private void HandleHealUsed() => OnHealUsed?.Invoke();

    /// <summary>
    /// 攻撃ヒット1回分の蓄積を加算する (PlayerController.PerformAttackHit や弾のヒット通知から呼ばれる)。
    /// multiplier は攻撃種別による倍率 (近接=1、特殊=0.5)。
    /// </summary>
    public void AddCharge(float multiplier = 1f) => _model.AddCharge(multiplier);

    /// <summary>メモリを1消費する。消費できたら true (HealState.Enter から呼ばれる)。</summary>
    public bool TryConsumePip() => _model.TryConsumePip();
}
