using R3;
using UnityEngine;

/// <summary>
/// プレイヤーの体力。<see cref="IDamageable"/> として敵の攻撃を受け、
/// 被弾時は無敵時間(点滅)を開始して <see cref="PlayerController"/> に HurtState / DeadState への遷移を依頼する。
/// </summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    // 無敵中の点滅間隔 (sec)
    private const float BlinkInterval = 0.1f;

    private PlayerController _controller;
    private SpriteRenderer _sprite;

    private ReactiveProperty<int> _hp;
    private float _invincibleTimer;

    /// <summary>現在 HP の購読用 (HUD が Subscribe する)。</summary>
    public ReadOnlyReactiveProperty<int> Hp => _hp;

    public int MaxHp => _controller.Consts.MaxHp;
    public bool IsInvincible => _invincibleTimer > 0f;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _sprite = GetComponent<SpriteRenderer>();
        _hp = new ReactiveProperty<int>(_controller.Consts.MaxHp);
    }

    private void OnDestroy()
    {
        _hp.Dispose();
    }

    private void Update()
    {
        if (_invincibleTimer <= 0f)
            return;

        _invincibleTimer -= Time.deltaTime;

        if (_invincibleTimer <= 0f)
        {
            _sprite.enabled = true;
        }
        else
        {
            // 一定間隔で表示/非表示を切り替えて点滅させる
            _sprite.enabled = (int)(_invincibleTimer / BlinkInterval) % 2 == 0;
        }
    }

    public void TakeDamage(in DamageInfo info)
    {
        // プレイヤーに防御値はないので HpDamage のみ扱う
        // 回避ダッシュ中は接触ダメージを受けない (布カッター突進は除く)
        if (info.HpDamage <= 0 || IsInvincible || _controller.IsDead || _controller.IsDashInvulnerable)
            return;

        _hp.Value = Mathf.Max(0, _hp.Value - info.HpDamage);

        if (_hp.Value <= 0)
        {
            _sprite.enabled = true;
            _controller.OnDied();
            return;
        }

        _invincibleTimer = _controller.Consts.InvincibleTime;
        _controller.OnDamaged(info);
    }

    /// <summary>HP を回復する (最大値でクランプ)。</summary>
    public void Heal(int amount)
    {
        if (amount <= 0 || _controller.IsDead)
            return;

        _hp.Value = Mathf.Min(MaxHp, _hp.Value + amount);
    }
}
