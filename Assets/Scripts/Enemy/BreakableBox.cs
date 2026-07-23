using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 攻撃練習用の壊れる箱。防御値は持たず、HP/防御値どちらのダメージも合算して HP で受ける
/// (どのスタイルでも壊せるようにするため)。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BreakableBox : MonoBehaviour, IDamageable
{
    [Tooltip("耐久値。HPダメージ+防御値ダメージの合算で削られる")]
    [SerializeField] private int _maxHp = 1;

    [Tooltip("被弾フラッシュの時間 (sec)")]
    [SerializeField] private float _hitFlashTime = 0.1f;

    [Tooltip("破壊演出(縮小フェード)の時間 (sec)")]
    [SerializeField] private float _breakFadeTime = 0.3f;

    private SpriteRenderer _sprite;
    private Color _baseColor;
    private int _hp;
    private float _flashTimer;
    private bool _broken;

    /// <summary>破壊された時に発火する (チュートリアルの達成判定用)。</summary>
    public event Action OnBroken;

    private void Awake()
    {
        _sprite = GetComponent<SpriteRenderer>();
        _baseColor = _sprite.color;
        _hp = _maxHp;
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
        OnBroken?.Invoke();

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        transform.DOScale(Vector3.zero, _breakFadeTime)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }
}
