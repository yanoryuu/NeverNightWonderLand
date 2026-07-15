using R3;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD のプレイヤー HP バー(赤)。PlayerHealth.Hp を購読して fillAmount に反映する。
/// 見た目は Prefab 側 (Image) の差し替えで変更できる。
/// </summary>
public class PlayerHpBarView : MonoBehaviour
{
    [Tooltip("参照するプレイヤーの体力")]
    [SerializeField] private PlayerHealth _health;

    [Tooltip("残量を表すフィル画像 (Image Type = Filled)")]
    [SerializeField] private Image _fill;

    private System.IDisposable _subscription;

    private void Start()
    {
        if (_health == null || _fill == null)
        {
            Debug.LogError($"[{nameof(PlayerHpBarView)}] 参照が設定されていません。", this);
            return;
        }

        _subscription = _health.Hp.Subscribe(hp =>
            _fill.fillAmount = Mathf.Clamp01((float)hp / _health.MaxHp));
    }

    private void OnDestroy()
    {
        _subscription?.Dispose();
    }
}
