using R3;
using UnityEngine;
using VContainer;

/// <summary>
/// プレイヤーの体力 (MonoBehaviour アダプタ)。HP の実体は <see cref="PlayerHealthModel"/> が持ち、
/// 本クラスは <see cref="IDamageable"/> としての被弾受付・無敵時間 (点滅)・
/// <see cref="PlayerController"/> への HurtState / DeadState 遷移依頼を担う。
/// Model は PlayerLifetimeScope (プレハブ同梱) 経由で注入され、DI の無いシーンでは自前生成にフォールバックする。
/// </summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    // 無敵中の点滅間隔 (sec)
    private const float BlinkInterval = 0.1f;

    private PlayerController _controller;
    private SpriteRenderer _sprite;

    private PlayerHealthModel _model;
    private bool _ownsModel;
    private float _invincibleTimer;

    /// <summary>現在 HP の購読用 (HUD の Presenter が Subscribe する)。</summary>
    public ReadOnlyReactiveProperty<int> Hp => _model.Hp;

    public int MaxHp => _model.MaxHp;
    public bool IsInvincible => _invincibleTimer > 0f;

    [Inject]
    public void Construct(PlayerHealthModel model)
    {
        _model = model;
    }

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _sprite = GetComponent<SpriteRenderer>();

        if (_model == null)
        {
            _model = new PlayerHealthModel(_controller.Consts);
            _ownsModel = true;
        }

        // Model はシーンをまたいで生存するため、スポーン時に満タンへ戻す
        _model.ResetForSpawn();
    }

    private void OnDestroy()
    {
        if (_ownsModel)
            _model.Dispose();
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
        if (info.HpDamage <= 0 || IsInvincible || _controller.IsDead || _controller.IsDashInvulnerable
            || _controller.IsSkillInvulnerable || DebugCheats.Invincible)
            return;

        // パリィ受付中なら攻撃を無効化する (スキル)
        if (_controller.TryParry(info))
            return;

        _model.Damage(info.HpDamage);

        if (_model.IsDepleted)
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
        if (_controller.IsDead)
            return;

        _model.Heal(amount);
    }

    /// <summary>無敵時間を付与する (パリィ成功時など。既に長い無敵が残っていれば維持)。</summary>
    public void GrantInvincibility(float seconds)
    {
        _invincibleTimer = Mathf.Max(_invincibleTimer, seconds);
    }
}
