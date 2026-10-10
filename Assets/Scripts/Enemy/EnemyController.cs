using System;
using System.Collections.Generic;
using DG.Tweening;
using R3;
using UnityEngine;

/// <summary>
/// 敵のコア。防御値(白)と HP(赤)の二層構造を持ち、
/// 防御値を削り切ると一定時間ブレイク(スタン・無防備・被HPダメージ増)する。
/// 攻撃はプレイヤーへの接触ダメージ。
/// うごき (AI) は同じ GameObject の <see cref="EnemyBehaviour"/> に委譲し、
/// 行動可能な状態 (Acting) の間だけ Tick を呼ぶ。未装着なら標準の巡回
/// (<see cref="PatrolChaseBehaviour"/>) を自動で付ける。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyController : MonoBehaviour, IDamageable
{
    /// <summary>生存中の全敵。裁断可否判定 (CanFinisher) や HUD のプロンプト表示が参照する。</summary>
    public static readonly List<EnemyController> Active = new();

    /// <summary>
    /// 撃破時の素材「糸」ドロップ通知 (位置, 数, 敵の大きさ = scale, 散らす範囲 = <see cref="LootArea"/>)。
    /// ThreadDropSpawner (プレイヤー) が購読し、地面に散らばる粒にする (拾った時に糸が増える)。
    /// 範囲の幅が 0 なら普通の敵 (その場から小さく散る)、幅があればボス (範囲いっぱいに噴き出す)。
    /// </summary>
    public static event Action<Vector2, int, float, Rect> ThreadDropped;

    /// <summary>
    /// ボスの糸を散らす範囲 (ワールド座標の矩形。BossArena がトリガーの範囲を入れる)。幅 0 = 普通の敵。
    /// (2026-10-04 PendingChanges 25: ボスの糸は闘技場いっぱいにばらまく)
    /// </summary>
    public Rect LootArea { get; set; }

    private enum State
    {
        Acting, // 行動中 (EnemyBehaviour に委譲)
        Hurt,   // 被弾硬直 / 糸玉拘束
        Break,  // ブレイク (スタン)
        Dead,   // 死亡 (演出中)
    }

    [Tooltip("挙動を定義する定数アセット")]
    [SerializeField] private EnemyConsts _consts;

    [Tooltip("撃破時の糸の数の上書き (0 以下なら定数アセットの ThreadDrop)。ボス部屋のボスだけ増やす用 (2026-10-03 PendingChanges 18)")]
    [SerializeField] private int _threadDropOverride;

    [Tooltip("見た目の SpriteRenderer (色・向き・被弾フラッシュをかける先)。未設定なら自身の SpriteRenderer。" +
             "絵を子 (Visual) に置いてアニメーションさせる敵 (野良ウマ) 用")]
    [SerializeField] private SpriteRenderer _visual;

    [Tooltip("絵が左向きに描かれているか。true なら右へ進む時に反転する")]
    [SerializeField] private bool _visualFacesLeft;

    [Tooltip("被弾フラッシュの色 (白い絵は白では光って見えないので赤みを付ける)")]
    [SerializeField] private Color _hitFlashColor = Color.white;

    [Tooltip("スーパーアーマー。true なら通常の被弾で怯まない (硬直・ノックバック・行動中断なし)。" +
             "ブレイクと糸玉拘束は効く。ボス用")]
    [SerializeField] private bool _superArmor;

    private Rigidbody2D _rb;
    private SpriteRenderer _sprite;
    private Collider2D _collider;
    private Color _baseColor;

    // 接触ダメージ用のプレイヤー参照 (物理衝突は無効なので重なり判定で行う)
    private PlayerHealth _playerHealth;
    private Collider2D _playerCollider;

    private State _state = State.Acting;
    private float _stateTimer;   // Hurt / Break の残り時間
    private float _flashTimer;   // 被弾フラッシュの残り時間
    private bool _wasMovingLastPhysicsStep; // 壁詰まり検知用 (Move で速度を与えたのに動けていない場合)
    private Transform _playerTransform;     // 追跡・狙い撃ち用。シーンに1人想定
    private EnemyBehaviour _behaviour;      // うごき (AI)。行動中のみ Tick される
    private EnemyPerception _perception;    // 索敵 (任意)。無ければ距離判定にフォールバック
    private EnemyAnimator _animator;        // アニメーション制御 (任意)

    private ReactiveProperty<int> _hp;
    private ReactiveProperty<int> _guard;
    private readonly ReactiveProperty<bool> _isBroken = new(false);

    public ReadOnlyReactiveProperty<int> Hp => _hp;
    public ReadOnlyReactiveProperty<int> Guard => _guard;
    public ReadOnlyReactiveProperty<bool> IsBroken => _isBroken;
    public EnemyConsts Consts => _consts;

    /// <summary>撃破時に落とす糸の数 (上書きがあればそれ、無ければ定数アセットの値)。</summary>
    public int ThreadDrop => _threadDropOverride > 0 ? _threadDropOverride : _consts != null ? _consts.ThreadDrop : 0;
    public bool IsDead => _state == State.Dead;

    /// <summary>被弾硬直 (または糸玉拘束) 中か。EnemyAnimator が参照する。</summary>
    public bool IsHurt => _state == State.Hurt;

    /// <summary>アニメーション制御 (任意)。未装着なら null (うごき部品は null 条件演算子で使う)。</summary>
    public EnemyAnimator Animation => _animator;

    /// <summary>プレイヤーの Transform (EnemyBehaviour 用)。不在なら null。</summary>
    public Transform PlayerTransform => _playerTransform;

    /// <summary>
    /// プレイヤーを発見しているか (発見後フェーズ)。
    /// EnemyPerception があればそれに従い (ヒステリシス・記憶・視線)、
    /// 無ければ EnemyConsts.ChaseRange の単純な距離判定にフォールバックする。
    /// </summary>
    public bool IsPlayerDetected =>
        _perception != null
            ? _perception.IsAlert
            : _playerTransform != null && _consts != null && _consts.ChaseRange > 0f
              && Vector2.Distance(transform.position, _playerTransform.position) <= _consts.ChaseRange;

    /// <summary>撃破時に発火する (チュートリアルの達成判定用)。</summary>
    public event Action<EnemyController> OnDied;

    /// <summary>
    /// 撤退 (倒れずに飛び去る) 時に発火する。ボス 1 戦目の「HP 半分で飛び去る = 勝ち扱い」用。
    /// BossArena は OnDied と同様に扱う。
    /// </summary>
    public event Action<EnemyController> OnRetreated;

    /// <summary>
    /// 一時無敵。true の間は TakeDamage を無視する (ボスの形態遷移演出など)。接触ダメージは出る。
    /// </summary>
    public bool IsInvulnerable { get; set; }

    /// <summary>
    /// HP の下限。被ダメージでこれ未満には減らない (0 = 通常どおり倒れる)。
    /// ボスの「HP 半分で形態遷移/撤退」を一撃で飛ばされないようにする用。
    /// </summary>
    public int HpFloor { get; set; }

    private string _persistentId; // 撃破記録用 ID (ステージ名+初期位置+名前)
    private bool _trackDefeat;    // 撃破を記録するか (スポナー製は false)

    /// <summary>
    /// 撃破記録の対象から外す (スポナー製など、何度でも再出現させたい敵に使う)。
    /// </summary>
    public void SetDefeatTracking(bool enabled) => _trackDefeat = enabled;

    #region Unity Callbacks

    private void Awake()
    {
        // 撃破記録: 一度倒した敵は拠点で休むまで再出現しない (ステージ遷移をまたいで有効)。
        // ID は「ステージ名+初期位置+名前」で同定するため、シーンに手置きした敵が対象。
        // 実行時スポーン ("(Clone)") はスポナーが何度でも湧かせるため対象外
        _trackDefeat = !name.EndsWith("(Clone)");
        _persistentId = $"{gameObject.scene.name}:{name}:{transform.position.x:F1},{transform.position.y:F1}";
        if (_trackDefeat && DefeatedEnemyRegistry.IsDefeated(_persistentId))
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

        _rb = GetComponent<Rigidbody2D>();
        _sprite = _visual != null ? _visual : GetComponent<SpriteRenderer>();
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

        // うごき (AI)。未装着なら標準の巡回を付ける (既存プレハブ/シーン互換)
        _behaviour = GetComponent<EnemyBehaviour>();
        if (_behaviour == null)
            _behaviour = gameObject.AddComponent<PatrolChaseBehaviour>();

        // 索敵・アニメーション (どちらも任意)
        _perception = GetComponent<EnemyPerception>();
        _animator = GetComponent<EnemyAnimator>();
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
        _behaviour?.OnInterrupted();

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

        if (_state == State.Acting && _behaviour != null)
            _behaviour.LogicTick();
    }

    private void FixedUpdate()
    {
        switch (_state)
        {
            case State.Acting:
                _behaviour?.PhysicsTick();
                CheckContactDamage();
                break;

            case State.Hurt:
                _stateTimer -= Time.fixedDeltaTime;
                if (_stateTimer <= 0f)
                    _state = State.Acting;
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

    #region Movement (EnemyBehaviour 用ヘルパー)

    /// <summary>
    /// 水平移動する (速度を直接与える方式)。向きの反転と壁当たり検知
    /// (<see cref="HitWallLastStep"/>) が揃うため、EnemyBehaviour の移動はこれを使う。
    /// </summary>
    public void Move(float horizontalVelocity)
    {
        _rb.linearVelocity = new Vector2(horizontalVelocity, _rb.linearVelocity.y);
        if (Mathf.Abs(horizontalVelocity) > 0.01f)
            _sprite.flipX = (horizontalVelocity < 0f) != _visualFacesLeft;
        _wasMovingLastPhysicsStep = Mathf.Abs(horizontalVelocity) > 0.01f;
    }

    /// <summary>直前の物理ステップで動こうとしたのに動けていなかったか (壁当たり反転の判定用)。</summary>
    public bool HitWallLastStep => _wasMovingLastPhysicsStep && Mathf.Abs(_rb.linearVelocity.x) < 0.01f;

    #endregion

    #region Damage

    public void TakeDamage(in DamageInfo info)
    {
        if (_state == State.Dead || IsInvulnerable)
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
        var appliedHp = Mathf.Max(0, Mathf.Min(_hp.Value - Mathf.Max(0, HpFloor), hpDamage));
        _hp.Value -= appliedHp;

        // ダメージ数値の通知 (崩し=白、HP=赤 で色分け表示される)
        DamageEvents.Raise(info.HitPoint, appliedGuard, DamageEvents.Kind.Guard);
        DamageEvents.Raise(info.HitPoint, appliedHp, DamageEvents.Kind.Hp);

        Flash();

        var killed = _hp.Value <= 0;
        var breaking = !killed && !broken && _guard.Value <= 0;

        // プレイヤーの攻撃 (近接・突進・飛び道具・設置物) が当たったら一瞬止める。倒した・ブレイクさせた時は長め
        PlayerController attacker = null;
        if (info.Source != null && info.Source.TryGetComponent(out attacker))
        {
            GamePause.HitStop(info.IsFinisher ? _consts.FinisherHitStopTime
                : killed || breaking ? _consts.HeavyHitStopTime : _consts.HitStopTime);
        }

        // 裁断が決まった演出 (ここに来るのはブレイク中の敵に当たった時だけ)。倒したら斬撃線で真っ二つに割って見せる
        var sliced = false;
        if (info.IsFinisher)
        {
            sliced = FinisherEffect.Play(attacker, _sprite, info.HitPoint, killed,
                _consts.FinisherShakeAmplitude, _consts.FinisherShakeTime);
        }

        if (killed)
        {
            Die(sliced);
            return;
        }

        if (breaking)
        {
            BeginBreak();
        }
        else if (!broken && !_superArmor)
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
        _behaviour?.OnInterrupted();

        var power = knockbackOverride > 0f ? knockbackOverride : _consts.KnockbackOnHit;
        var dir = transform.position.x >= hitPoint.x ? 1f : -1f;
        _rb.linearVelocity = new Vector2(dir * power, _rb.linearVelocity.y);
    }

    private void BeginBreak()
    {
        _state = State.Break;
        _stateTimer = _consts.BreakDuration;
        _wasMovingLastPhysicsStep = false;
        _behaviour?.OnInterrupted();
        _isBroken.Value = true;
        _sprite.color = BreakTint();
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
    }

    private void EndBreak()
    {
        _state = State.Acting;
        _isBroken.Value = false;
        _guard.Value = _consts.MaxGuard; // ブレイク明けで防御値は全回復
        _sprite.color = _baseColor;
    }

    /// <param name="sliced">裁断で切り分けの片を出した (本体はすぐ隠して消す)。</param>
    private void Die(bool sliced = false)
    {
        _state = State.Dead;
        _isBroken.Value = false;
        Active.Remove(this);

        // 拠点で休むまで再出現しないよう記録する
        if (_trackDefeat)
            DefeatedEnemyRegistry.MarkDefeated(_persistentId);

        OnDied?.Invoke(this);
        _animator?.PlayDeath();

        // リザルト集計と素材「糸」のドロップ
        GameSession.EnemiesDefeated++;
        var threadDrop = ThreadDrop;
        if (threadDrop > 0)
            ThreadDropped?.Invoke(transform.position, threadDrop, Mathf.Abs(transform.lossyScale.y), LootArea);

        // 当たり判定を消して縮小フェードで消滅
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;
        _rb.simulated = false;

        if (sliced)
        {
            foreach (var r in GetComponentsInChildren<Renderer>())
                r.enabled = false;
            Destroy(gameObject, _consts.DeathFadeTime);
            return;
        }

        transform.DOScale(Vector3.zero, _consts.DeathFadeTime)
            .SetEase(Ease.InBack)
            .OnComplete(() => Destroy(gameObject));
    }

    private void Flash()
    {
        _sprite.color = _hitFlashColor;
        _flashTimer = _consts.HitFlashTime;
    }

    /// <summary>ブレイク中であることを示す色 (仮素材の色分け)。</summary>
    private Color BreakTint() => new Color(1f, 0.85f, 0.3f);

    #endregion

    #region ボス用 (撤退・硬直解除・向き)

    /// <summary>
    /// 被弾硬直・拘束・ブレイクを即座に解除して行動可能に戻す (ボスの形態遷移/撤退の開始用)。
    /// ブレイク中なら通常のブレイク明けと同じく防御値を全回復する。死亡中は何もしない。
    /// </summary>
    public void ClearStun()
    {
        if (_state == State.Break)
            EndBreak();
        else if (_state == State.Hurt)
            _state = State.Acting;
    }

    /// <summary>移動せずに向きだけ変える (direction の符号: 正 = 右)。</summary>
    public void Face(float direction)
    {
        if (Mathf.Abs(direction) > 0.01f && _sprite != null)
            _sprite.flipX = (direction < 0f) != _visualFacesLeft;
    }

    /// <summary>
    /// 倒れずに撤退する (勝ち扱い)。行動・当たり判定を止め、撃破記録と糸のドロップは撃破時と同様に行い、
    /// <see cref="OnRetreated"/> を発火する。本体は消さない (飛び去る演出と Destroy は呼び出し側が行う)。
    /// </summary>
    /// <param name="dropThreads">糸をドロップするか (撃破時と同じ数)</param>
    public void Retreat(bool dropThreads)
    {
        if (_state == State.Dead)
            return;

        _state = State.Dead;
        _isBroken.Value = false;
        Active.Remove(this);

        if (_trackDefeat)
            DefeatedEnemyRegistry.MarkDefeated(_persistentId);

        var threadDrop = ThreadDrop;
        if (dropThreads && threadDrop > 0)
            ThreadDropped?.Invoke(transform.position, threadDrop, Mathf.Abs(transform.lossyScale.y), LootArea);

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;
        _rb.simulated = false;

        OnRetreated?.Invoke(this);
    }

    #endregion

}
