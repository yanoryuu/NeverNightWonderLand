using DG.Tweening;
using UnityEngine;

/// <summary>
/// 強化したハサミでのみ破壊できる壁。攻撃を受けた時、攻撃元 (DamageInfo.Source) の
/// PlayerProgression が指定の強化を持っていれば砕け、持っていなければヒントを通知して弾く。
/// 破壊状態は GameProgress で永続化される。Ground レイヤーに置くと攻撃対象になる。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class UpgradeWall : MonoBehaviour, IDamageable
{
    [Tooltip("破壊に必要なハサミ強化")]
    [SerializeField] private ScissorUpgrade _required = ScissorUpgrade.Red;

    [Tooltip("破壊状態の永続化フラグ (GameProgress)。例: CarouselBossWall")]
    [SerializeField] private string _flagId = "";

    [Tooltip("未強化で攻撃した時のヒント")]
    [SerializeField] private string _lockedMessage = "硬い壁だ。赤く強化した刃なら断ち切れそうだ";

    private SpriteRenderer _sprite;
    private bool _broken;
    private float _hintCooldown; // ヒント通知の連発防止

    private void Awake()
    {
        if (!string.IsNullOrEmpty(_flagId) && GameProgress.Has(_flagId))
        {
            Destroy(gameObject);
            return;
        }

        _sprite = GetComponent<SpriteRenderer>();
    }

    private void OnDestroy()
    {
        transform.DOKill(); // シーン遷移などで破壊演出の途中に消えても警告を出さない
    }

    private void Update()
    {
        if (_hintCooldown > 0f)
            _hintCooldown -= Time.deltaTime;
    }

    public void TakeDamage(in DamageInfo info)
    {
        if (_broken)
            return;

        var progression = info.Source != null ? info.Source.GetComponent<PlayerProgression>() : null;
        if (progression == null || !progression.Has(_required))
        {
            if (_hintCooldown <= 0f)
            {
                Notifier.Notify(_lockedMessage);
                _hintCooldown = 2f;
            }
            return;
        }

        Break(info.HitPoint);
    }

    private void Break(Vector2 hitPoint)
    {
        _broken = true;

        if (!string.IsNullOrEmpty(_flagId))
            GameProgress.Set(_flagId);

        Notifier.Notify($"{_required.DisplayName()}で壁を断ち切った!");
        DamageEvents.Raise(hitPoint, 1, DamageEvents.Kind.Hp);

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        transform.DOScale(Vector3.zero, 0.3f)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }
}
