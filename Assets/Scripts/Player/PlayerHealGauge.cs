using System;
using R3;
using UnityEngine;

/// <summary>
/// 回復ゲージ。攻撃を当てるとメモリが少しずつ蓄積され(<see cref="AddCharge"/>)、
/// 回復入力でメモリ1つを消費して HP を回復する(消費は <see cref="TryConsumePip"/>)。
/// 「リスクを取って攻めるほど回復源が増える」デザイン。1ゲージ = HealGaugeMax メモリ。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerHealGauge : MonoBehaviour
{
    private PlayerController _controller;

    private readonly ReactiveProperty<float> _charge = new(0f); // 蓄積中メモリの割合 (0..1)
    private readonly ReactiveProperty<int> _pips = new(0);      // 満タンのメモリ数

    /// <summary>蓄積中メモリの割合 (0..1)。HUD の部分蓄積表示用。</summary>
    public ReadOnlyReactiveProperty<float> Charge => _charge;

    /// <summary>使用可能なメモリ数。</summary>
    public ReadOnlyReactiveProperty<int> Pips => _pips;

    /// <summary>回復を実行した時に発火する (チュートリアルの達成判定用)。</summary>
    public event Action OnHealUsed;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
    }

    private void OnDestroy()
    {
        _charge.Dispose();
        _pips.Dispose();
    }

    /// <summary>攻撃ヒット1回分の蓄積を加算する (PlayerController.PerformAttackHit から呼ばれる)。</summary>
    public void AddCharge()
    {
        var consts = _controller.Consts;
        if (_pips.Value >= consts.HealGaugeMax)
            return;

        var charge = _charge.Value + consts.HealChargePerHit;
        var pips = _pips.Value;

        while (charge >= 1f && pips < consts.HealGaugeMax)
        {
            charge -= 1f;
            pips++;
        }

        // 全メモリが満タンなら端数は捨てる
        if (pips >= consts.HealGaugeMax)
            charge = 0f;

        _pips.Value = pips;
        _charge.Value = charge;
    }

    /// <summary>メモリを1消費する。消費できたら true (HealState.Enter から呼ばれる)。</summary>
    public bool TryConsumePip()
    {
        if (_pips.Value <= 0)
            return false;

        _pips.Value--;
        OnHealUsed?.Invoke();
        return true;
    }
}
