using DG.Tweening;
using UnityEngine;

/// <summary>
/// 進行を妨げるリボン。対応する色のハサミ強化を持っていれば攻撃で切断できる。
/// 持っていない場合は切れず、必要な色を通知する。
/// 攻撃対象レイヤー (Ground) に置いて通行を塞ぐ。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Ribbon : MonoBehaviour, IDamageable
{
    [Tooltip("切断に必要なハサミ強化の色")]
    [SerializeField] private ScissorUpgrade _required = ScissorUpgrade.Yellow;

    [Tooltip("切断演出の時間 (sec)")]
    [SerializeField] private float _cutFadeTime = 0.3f;

    private bool _cut;
    private float _lastNotifyTime = -10f;

    public ScissorUpgrade Required => _required;

    public void TakeDamage(in DamageInfo info)
    {
        if (_cut || info.Source == null)
            return;

        var progression = info.Source.GetComponent<PlayerProgression>();
        if (progression == null)
            return;

        if (!progression.Has(_required))
        {
            // 連打で通知がスパムしないよう間隔を空ける
            if (Time.time - _lastNotifyTime > 1.5f)
            {
                Notifier.Notify($"このリボンは {_required.DisplayName()} がないと切れない…");
                _lastNotifyTime = Time.time;
            }
            return;
        }

        Cut();
    }

    private void Cut()
    {
        _cut = true;
        Notifier.Notify("リボンを切り裂いた!");

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        transform.DOScaleY(0f, _cutFadeTime)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }
}
