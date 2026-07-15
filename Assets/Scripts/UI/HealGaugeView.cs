using R3;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD の回復ゲージ(メモリ×3 + 部分蓄積)。PlayerHealGauge の Pips / Charge を購読し、
/// 満タンのメモリは fill=1、蓄積中のメモリは fill=Charge で表示する。
/// </summary>
public class HealGaugeView : MonoBehaviour
{
    [Tooltip("参照する回復ゲージ")]
    [SerializeField] private PlayerHealGauge _gauge;

    [Tooltip("メモリのフィル画像 (左から順、HealGaugeMax 個)")]
    [SerializeField] private Image[] _pipFills;

    private readonly CompositeDisposable _disposables = new();

    private void Start()
    {
        if (_gauge == null || _pipFills == null || _pipFills.Length == 0)
        {
            Debug.LogError($"[{nameof(HealGaugeView)}] 参照が設定されていません。", this);
            return;
        }

        _gauge.Pips.Subscribe(_ => Refresh()).AddTo(_disposables);
        _gauge.Charge.Subscribe(_ => Refresh()).AddTo(_disposables);
    }

    private void OnDestroy()
    {
        _disposables.Dispose();
    }

    private void Refresh()
    {
        var pips = _gauge.Pips.CurrentValue;
        var charge = _gauge.Charge.CurrentValue;

        for (var i = 0; i < _pipFills.Length; i++)
        {
            if (i < pips) _pipFills[i].fillAmount = 1f;
            else if (i == pips) _pipFills[i].fillAmount = charge;
            else _pipFills[i].fillAmount = 0f;
        }
    }
}
