using System;
using R3;

/// <summary>
/// 回復ゲージの Model (MonoBehaviour 非依存)。
/// 攻撃を当てるとメモリが少しずつ蓄積され (<see cref="AddCharge"/>)、
/// 回復入力でメモリ1つを消費する (<see cref="TryConsumePip"/>)。
/// 「リスクを取って攻めるほど回復源が増える」デザイン。1ゲージ = GaugeMax メモリ。
/// </summary>
public sealed class HealGaugeModel : IDisposable
{
    // PlayerConsts 未設定時のフォールバック (アセットのデフォルト値と揃える)
    private const int DefaultGaugeMax = 3;
    private const float DefaultChargePerHit = 0.25f;

    private readonly float _chargePerHit;

    private readonly ReactiveProperty<float> _charge = new(0f); // 蓄積中メモリの割合 (0..1)
    private readonly ReactiveProperty<int> _pips = new(0);      // 満タンのメモリ数

    /// <summary>蓄積中メモリの割合 (0..1)。HUD の部分蓄積表示用。</summary>
    public ReadOnlyReactiveProperty<float> Charge => _charge;

    /// <summary>使用可能なメモリ数。</summary>
    public ReadOnlyReactiveProperty<int> Pips => _pips;

    /// <summary>メモリの最大数。</summary>
    public int GaugeMax { get; }

    /// <summary>回復を実行した時に発火する (チュートリアルの達成判定用)。</summary>
    public event Action HealUsed;

    public HealGaugeModel(PlayerConsts consts)
    {
        GaugeMax = consts != null ? consts.HealGaugeMax : DefaultGaugeMax;
        _chargePerHit = consts != null ? consts.HealChargePerHit : DefaultChargePerHit;
    }

    /// <summary>スポーン時に空へ戻す (Model はシーンをまたぐため明示リセットが必要)。</summary>
    public void ResetForSpawn()
    {
        _pips.Value = 0;
        _charge.Value = 0f;
    }

    /// <summary>
    /// 攻撃ヒット1回分の蓄積を加算する。
    /// multiplier は攻撃種別による倍率 (近接=1、特殊=0.5 など)。
    /// </summary>
    public void AddCharge(float multiplier = 1f)
    {
        if (multiplier <= 0f || _pips.Value >= GaugeMax)
            return;

        var charge = _charge.Value + _chargePerHit * multiplier;
        var pips = _pips.Value;

        while (charge >= 1f && pips < GaugeMax)
        {
            charge -= 1f;
            pips++;
        }

        // 全メモリが満タンなら端数は捨てる
        if (pips >= GaugeMax)
            charge = 0f;

        _pips.Value = pips;
        _charge.Value = charge;
    }

    /// <summary>メモリを1消費する。消費できたら true。</summary>
    public bool TryConsumePip()
    {
        if (_pips.Value <= 0)
            return false;

        _pips.Value--;
        HealUsed?.Invoke();
        return true;
    }

    public void Dispose()
    {
        _charge.Dispose();
        _pips.Dispose();
    }
}
