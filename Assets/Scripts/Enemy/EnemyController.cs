using System;
using System.Collections.Generic;
using DG.Tweening;
using R3;
using UnityEngine;

/// <summary>
/// チュートリアル用の雑魚敵。防御値(白)と HP(赤)の二層構造を持ち、
/// 防御値を削り切ると一定時間ブレイク(スタン・無防備・被HPダメージ増)する。
/// 挙動は enum ベースの簡易ステート(Patrol/Hurt/Break/Dead)で管理する
/// (プレイヤー級のクラス FSM はボス実装時に検討)。
/// 攻撃はプレイヤーへの接触ダメージ。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyController : MonoBehaviour, IDamageable
{
    /// <summary>生存中の全敵。裁断可否判定 (CanFinisher) や HUD のプロンプト表示が参照する。</summary>
    public static readonly List<EnemyController> Active = new();

    /// <summary>撃破時の素材「糸」ドロップ通知 (位置, 数)。PlayerItemInventory が購読する。</summary>
    public static event Action<Vector2, int> ThreadDropped;

    private enum State
    {
        Patrol, // 巡回
        Hurt,   // 被弾硬直
        Break,  // ブレイク (スタン)
        Dead,   // 死亡 (演出中)
    }

    [Tooltip("挙動を定義する定数アセット")]
    [SerializeField] private EnemyConsts _consts;

    private Rigidbody2D _rb;
    private SpriteRenderer _sprite;
    private Collider2D _collider;
    private Color _baseColor;

    // 接触ダメージ用のプレイヤー参照 (物理衝突は無効なので重なり判定で行う)
    private PlayerHealth _playerHealth;
    private Collider2D _playerCollider;

    private State _state = State.Patrol;
    private float _stateTimer;   // Hurt / Break の残り時間
    private float _flashTimer;   // 被弾フラッシュの残り時間
    private Vector2 _patrolOrigin;
    private int _moveDir = 1;
    private bool _wasMovingLastPhysicsStep; // 壁詰まり検知用 (速度を与えたのに動けていない場合に反転)
    private Transform _playerTransform;     // 追跡 (俊敏型) 用。シーンに1人想定

    private ReactiveProperty<int> _hp;
    private ReactiveProperty<int> _guard;
    private readonly ReactiveProperty<bool> _isBroken = new(false);

    public ReadOnlyReactiveProperty<int> Hp => _hp;
    public ReadOnlyReactiveProperty<int> Guard => _guard;
    public ReadOnlyReactiveProperty<bool> IsBroken => _isBroken;
    public EnemyConsts Consts => _consts;
    public bool IsDead => _state == State.Dead;

    /// <summary>撃破時に発火する (チュートリアルの達成判定用)。</summary>
    public event Action<EnemyController> OnDied;

    #region Unity Callbacks

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sprite = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        _baseColor = _sprite.color;
        _rb.freezeRotation = true;

        // 壁ぎわで張り付かないよう、摩擦ゼロのマテリアルを適用する (移動は速度直接指定)
        _rb.sharedMaterial = new PhysicsMaterial2D("EnemyFrictionless")
        {
            friction = 0f,
            bounciness = 0f,
        };

        if (_consts == null)
        {
            Debug.LogError($"[{nameof(EnemyController)}] EnemyConsts が設定されていません。", this);
            enabled = false;
            return;
        }

        _hp = new ReactiveProperty<int>(_consts.MaxHp);
        _guard = new ReactiveProperty<int>(_consts.MaxGuard);
        _patrolOrigin = transform.position;
    }

    private void Start()
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            _playerTransform = player.transform;
            _playerHealth = player.GetComponent<PlayerHealth>();
            _playerCollider = player.GetComponent<Collider2D>();
        }
    }

    /// <summary>
    /// 糸で絡めて一定時間拘束する (アイテム「糸玉」用)。
    /// ブレイク中・死亡中は無効。拘束中は移動できない (被弾硬直の延長として扱う)。
    /// </summary>
    public void ApplyBind(float duration)
    {
        if (_state == State.Dead || _state == State.Break || duration <= 0f)
            return;

        _state = State.Hurt;
        _stateTimer = Mathf.Max(_stateTimer, duration);
        _wasMovingLastPhysicsStep = false;
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

        // 糸に絡まっている見た目 (紫)。拘束が明けるとフラッシュ処理が元の色へ戻す
        _sprite.color = new Color(0.75f, 0.55f, 1f);
        _flashTimer = duration;
    }

    /// <summary>
    /// 生成直後に敵タイプ (定数アセット) と色を差し替える (EnemySpawner 用)。
    /// Instantiate 直後 (Awake 実行後・Start 実行前) に呼ぶこと。
    /// </summary>
    public void ApplyProfile(EnemyConsts consts, Color tint)
    {
        if (consts != null)
        {
            _consts = consts;
            _hp.Value = consts.MaxHp;
            _guard.Value = consts.MaxGuard;
        }

        _sprite.color = tint;
        _baseColor = tint;
    }

    private void OnEnable()
    {
        if (_state != State.Dead)
            Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    private void OnDestroy()
    {
        _hp?.Dispose();
        _guard?.Dispose();
        _isBroken.Dispose();
    }

    private void Update()
    {
        // 被弾フラッシュの戻し
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f && _state != State.Dead)
                _sprite.color = _state == State.Break ? BreakTint() : _baseColor;
        }
    }

    private void FixedUpdate()
    {
        switch (_state)
        {
            case State.Patrol:
                UpdatePatrol();
                CheckContactDamage();
                break;

            case State.Hurt:
                _stateTimer -= Time.fixedDeltaTime;
                if (_stateTimer <= 0f)
                    _state = State.Patrol;
                CheckContactDamage();
                break;

            case State.Break:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                _stateTimer -= Time.fixedDeltaTime;
                if (_stateTimer <= 0f)
                    EndBreak();
                break;

            case State.Dead:
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                break;
        }
    }

    /// <summary>
    /// プレイヤーとの接触ダメージ。敵とプレイヤーは物理的には衝突しない (すり抜ける) ため、
    /// コライダーの重なりで判定する。重なったプレイヤーはダメージ+ノックバックを受ける。
    /// ブレイク・死亡中は無防備なので接触ダメージも出さない。
    /// </summary>
    private void CheckContactDamage()
    {
        if (_playerHealth == null || _playerCollider == null || _collider == null)
            return;

        if (!_collider.bounds.Intersects(_playerCollider.bounds))
            return;

        var info = new DamageInfo(_consts.ContactDamage, 0, transform.position, gameObject);
        _playerHealth.TakeDamage(info);
    }

    #endregion

    #region Movement

    private void UpdatePatrol()
    {
        var speed = _consts.MoveSpeed;

        // 俊敏型: 検知範囲内のプレイヤーを追跡する (巡回範囲は無視)
        var chasing = false;
        if (_playerTransform != null && _consts.ChaseRange > 0f
            && Vector2.Distance(transform.position, _playerTransform.position) <= _consts.ChaseRange)
        {
            chasing = true;
            speed = _consts.ChaseSpeed;
            _moveDir = _playerTransform.position.x >= transform.position.x ? 1 : -1;
        }

        if (!chasing)
        {
            // 巡回範囲の端まで来たら反転する
            var x = transform.position.x;
            if (_moveDir > 0 && x > _patrolOrigin.x + _consts.PatrolHalfWidth) _moveDir = -1;
            else if (_moveDir < 0 && x < _patrolOrigin.x - _consts.PatrolHalfWidth) _moveDir = 1;

            // 直前の物理ステップで速度を与えたのに動けていない = 壁に当たっているので反転する
            if (_wasMovingLastPhysicsStep && Mathf.Abs(_rb.linearVelocity.x) < 0.01f)
                _moveDir = -_moveDir;
        }

        _rb.linearVelocity = new Vector2(_moveDir * speed, _rb.linearVelocity.y);
        _sprite.flipX = _moveDir < 0;
        _wasMovingLastPhysicsStep = true;
    }

    #endregion

    #region Damage

    public void TakeDamage(in DamageInfo info)
    {
        if (_state == State.Dead)
            return;

        var broken = _isBroken.Value;

        // 裁断はブレイク中の敵にのみ有効
        if (info.IsFinisher && !broken)
            return;

        // 防御値(白)への適用。ブレイク中は防御値が無いので HP に直通
        var appliedGuard = 0;
        if (!broken && info.GuardDamage > 0 && _guard.Value > 0)
        {
            appliedGuard = Mathf.Min(_guard.Value, info.GuardDamage);
            _guard.Value -= appliedGuard;
        }

        // HP(赤)への適用。ブレイク中は倍率がかかる
        var hpDamage = info.HpDamage;
        if (broken)
            hpDamage = Mathf.RoundToInt(hpDamage * _consts.BreakHpDamageMultiplier);
        var appliedHp = Mathf.Min(_hp.Value, hpDamage);
        _hp.Value -= appliedHp;

        // ダメージ数値の通知 (崩し=白、HP=赤 で色分け表示される)
        DamageEvents.Raise(info.HitPoint, appliedGuard, DamageEvents.Kind.Guard);
        DamageEvents.Raise(info.HitPoint, appliedHp, DamageEvents.Kind.Hp);

        Flash();

        if (_hp.Value <= 0)
        {
            Die();
            return;
        }

        if (!broken && _guard.Value <= 0)
        {
            BeginBreak();
        }
        else if (!broken)
        {
            BeginHurt(info.HitPoint, info.KnockbackPower);
        }
        // ブレイク中の被弾はスタン継続 (硬直の上書きはしない)
    }

    private void BeginHurt(Vector2 hitPoint, float knockbackOverride = 0f)
    {
        _state = State.Hurt;
        _stateTimer = _consts.HitStunTime;
        _wasMovingLastPhysicsStep = false;

        var power = knockbackOverride > 0f ? knockbackOverride : _consts.KnockbackOnHit;
        var dir = transform.position.x >= hitPoint.x ? 1f : -1f;
        _rb.linearVelocity = new Vector2(dir * power, _rb.linearVelocity.y);
    }

    private void BeginBreak()
    {
        _state = State.Break;
        _stateTimer = _consts.BreakDuration;
        _wasMovingLastPhysicsStep = false;
        _isBroken.Value = true;
        _sprite.color = BreakTint();
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
    }

    private void EndBreak()
    {
        _state = State.Patrol;
        _isBroken.Value = false;
        _guard.Value = _consts.MaxGuard; // ブレイク明けで防御値は全回復
        _sprite.color = _baseColor;
    }

    private void Die()
    {
        _state = State.Dead;
        _isBroken.Value = false;
        Active.Remove(this);
        OnDied?.Invoke(this);

        // リザルト集計と素材「糸」のドロップ
        GameSession.EnemiesDefeated++;
        if (_consts.ThreadDrop > 0)
            ThreadDropped?.Invoke(transform.position, _consts.ThreadDrop);

        // 当たり判定を消して縮小フェードで消滅
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;
        _rb.simulated = false;

        transform.DOScale(Vector3.zero, _consts.DeathFadeTime)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }

    private void Flash()
    {
        _sprite.color = Color.white;
        _flashTimer = _consts.HitFlashTime;
    }

    /// <summary>ブレイク中であることを示す色 (仮素材の色分け)。</summary>
    private Color BreakTint() => new Color(1f, 0.85f, 0.3f);

    #endregion

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (_consts == null)
            return;

        // 巡回範囲
        var origin = Application.isPlaying ? _patrolOrigin : (Vector2)transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            origin + Vector2.left * _consts.PatrolHalfWidth,
            origin + Vector2.right * _consts.PatrolHalfWidth);
    }

    #endregion
}
