using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 中ボスが守る破壊可能なスピーカー。破壊すると進行フラグが立ち、
/// ボス街道 (回転軸の内部) の入口を塞ぐ FlagDoor が1枚開く。
/// 破壊状態は GameProgress で永続化され、破壊済みなら出現しない。
/// HP/防御値どちらのダメージも合算して受ける (BreakableBox と同じ受け方)。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BreakableSpeaker : MonoBehaviour, IDamageable
{
    [Tooltip("破壊時に立てる進行フラグ (GameProgress)。例: SpeakerGate")]
    [SerializeField] private string _flagId = "";

    [Tooltip("耐久値")]
    [SerializeField] private int _maxHp = 3;

    [Tooltip("破壊時の通知メッセージ")]
    [SerializeField] private string _breakMessage = "スピーカーを破壊した! どこかで扉が開いた音がする";

    [Tooltip("被弾フラッシュの時間 (sec)")]
    [SerializeField] private float _hitFlashTime = 0.1f;

    private SpriteRenderer _sprite;
    private Color _baseColor;
    private int _hp;
    private float _flashTimer;
    private bool _broken;

    /// <summary>破壊された時に発火する。</summary>
    public event Action OnBroken;

    private void Awake()
    {
        // 破壊済みなら出現させない
        if (!string.IsNullOrEmpty(_flagId) && GameProgress.Has(_flagId))
        {
            Destroy(gameObject);
            return;
        }

        _sprite = GetComponent<SpriteRenderer>();
        _baseColor = _sprite.color;
        _hp = _maxHp;
    }

    private void OnDestroy()
    {
        transform.DOKill(); // シーン遷移などで破壊演出の途中に消えても警告を出さない
    }

    private void Update()
    {
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f && !_broken)
                _sprite.color = _baseColor;
        }
    }

    public void TakeDamage(in DamageInfo info)
    {
        if (_broken)
            return;

        var damage = info.HpDamage + info.GuardDamage;
        if (damage <= 0)
            return;

        var applied = Mathf.Min(_hp, damage);
        _hp -= applied;

        DamageEvents.Raise(info.HitPoint, applied, DamageEvents.Kind.Hp);

        _sprite.color = Color.white;
        _flashTimer = _hitFlashTime;

        if (_hp <= 0)
            Break();
    }

    private void Break()
    {
        _broken = true;

        if (!string.IsNullOrEmpty(_flagId))
            GameProgress.Set(_flagId);

        Notifier.Notify(_breakMessage);
        OnBroken?.Invoke();

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        transform.DOScale(Vector3.zero, 0.35f)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }
}
