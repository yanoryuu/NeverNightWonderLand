using UnityEngine;
using UnityEngine.UI;

/// <summary>回復ゲージの Passive View 抽象 (Presenter が参照する)。</summary>
public interface IHealGaugeView
{
    /// <summary>満タンのメモリ数と蓄積中メモリの割合 (0..1) を表示に反映する。</summary>
    void SetGauge(int fullPips, float charge);
}

/// <summary>
/// HUD の回復ゲージ(メモリ×3 + 部分蓄積)。表示のみを担い、
/// Pips / Charge の購読は HealGaugePresenter が行う。
/// 満タンのメモリは fill=1、蓄積中のメモリは fill=Charge で表示する。
/// </summary>
public class HealGaugeView : MonoBehaviour, IHealGaugeView
{
    [Tooltip("メモリのフィル画像 (左から順、HealGaugeMax 個)")]
    [SerializeField] private Image[] _pipFills;

    public void SetGauge(int fullPips, float charge)
    {
        if (_pipFills == null)
            return;

        for (var i = 0; i < _pipFills.Length; i++)
        {
            if (_pipFills[i] == null)
                continue;

            if (i < fullPips) _pipFills[i].fillAmount = 1f;
            else if (i == fullPips) _pipFills[i].fillAmount = charge;
            else _pipFills[i].fillAmount = 0f;
        }
    }
}
