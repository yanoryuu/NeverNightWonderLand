using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

/// <summary>ボス戦の決着の付け方。</summary>
public enum BossFightMode
{
    /// <summary>1 戦目: HP がしきい値 (半分) まで減ったら飛び去る (倒れない)。撤退 = 勝ち扱い。</summary>
    RetreatAtThreshold,

    /// <summary>2 戦目: HP がしきい値で第 2 形態へ移り、最後まで戦って倒す。</summary>
    FullFight,
}

/// <summary>
/// ボス「グラミネ第1のボス」のうごき (Docs/EnemySpec.md「ボス」節)。
/// EnemyController (HP/防御値/ブレイク/接触ダメージ/撃破) と同じ GameObject に付ける EnemyBehaviour。
///
/// 構え: 「浮遊」(遠距離 = ナイフ) と「地上」(近接・踏みつけ) を行き来する。ブレイク・拘束で行動が
/// 中断されると、浮遊中でも重力を戻して地面に落ちる (裁断のチャンス)。
///
/// 第1形態: 踏みつけ (地上) / ナイフ 3 本扇 (浮遊) / ステッキ横払い (地上) / 斜め急降下体当たり (浮遊→地上) / スピン (地上・近距離)。
/// HP がしきい値 (既定 50%) に達すると、モードに応じて
///   - <see cref="BossFightMode.RetreatAtThreshold"/>: 弾を消して飛び去る → EnemyController.Retreat (BossArena が勝ち扱い)
///   - <see cref="BossFightMode.FullFight"/>: 2 秒無敵・弾消し → 第2形態 (風船・爆弾 5 個を追加) → 倒す
/// しきい値は EnemyController.HpFloor で守るので、大技一発で飛ばされない。
///
/// 攻撃の判定は BossKit でプレイヤーとの重なりを見て与える。体当たり系 (急降下・踏みつけ) は
/// 本体の接触ダメージより先に判定するので、技ごとのダメージが入る。
/// 素材が来るまで見た目は変えない。予告 (光・影・範囲・印) だけ実行時の SpriteRenderer で出す。
///
/// Animator (EnemyAnimator 経由・任意。無いパラメータは無視される):
///   bool  Floating (浮遊の構え中) / Spinning (スピン回転中)
///   trigger Rise / Descend / Knife / Sweep / Dive / Spin / StompJump / StompLand / Balloon / Bomb / PhaseShift / Retreat
/// </summary>
public class GramineBossBehaviour : EnemyBehaviour
{
    private enum Stance { Floating, Ground }

    private enum Step
    {
        Idle,
        Rise, Descend,
        KnifeWindup,
        SweepWindup, SweepActive,
        DiveWindup, DiveDash,
        SpinWindup, Spinning,
        StompJump, StompTrack, StompLock, StompFall,
        BalloonCast, BombCast,
        Recover,
        PhaseShift, RetreatTaunt,
    }

    private enum AttackId { Stomp, Knife, Sweep, Dive, Spin, Balloon, Bomb, Count }

    #region Serialized Fields

    [Header("戦い方")]
    [Tooltip("1 戦目 = RetreatAtThreshold (HP しきい値で飛び去る = 勝ち扱い) / 2 戦目 = FullFight (第2形態まで戦って倒す)")]
    [SerializeField] private BossFightMode _mode = BossFightMode.RetreatAtThreshold;

    [Tooltip("形態遷移 / 撤退する HP の割合 (最大 HP に対して)")]
    [Range(0.05f, 0.95f)]
    [SerializeField] private float _phaseThreshold = 0.5f;

    [Tooltip("撤退 (1 戦目) の時に糸をドロップするか (撃破時と同じ数)")]
    [SerializeField] private bool _dropThreadsOnRetreat = true;

    [Header("場所")]
    [Tooltip("闘技場の範囲 (BossArena のトリガーなど)。左右の移動範囲・天井の高さに使う。未設定なら初期位置 ± _arenaHalfWidth")]
    [SerializeField] private Collider2D _arenaArea;

    [Tooltip("_arenaArea 未設定時の、初期位置からの左右の移動範囲 (units)")]
    [SerializeField] private float _arenaHalfWidth = 10f;

    [Tooltip("床とみなすレイヤー。空なら Ground")]
    [SerializeField] private LayerMask _groundMask;

    [Tooltip("浮遊の構えの高さ (床から体の下端まで, units)")]
    [SerializeField] private float _hoverHeight = 3.5f;

    [Tooltip("浮遊中の上下の揺れ幅 (units)")]
    [SerializeField] private float _bobAmplitude = 0.25f;

    [Tooltip("浮遊中の上下の揺れの速さ (rad/sec)")]
    [SerializeField] private float _bobSpeed = 2f;

    [Header("構え・移動")]
    [Tooltip("地上の構えでの重力スケール (浮遊中は 0)")]
    [SerializeField] private float _groundGravityScale = 4f;

    [Tooltip("浮遊中の移動の速さ (units/sec)")]
    [SerializeField] private float _floatMoveSpeed = 4f;

    [Tooltip("浮遊中にプレイヤーと取る水平距離 (units)")]
    [SerializeField] private float _floatKeepDistance = 5f;

    [Tooltip("地上で歩く速さ (units/sec)")]
    [SerializeField] private float _walkSpeed = 2.5f;

    [Tooltip("地上でプレイヤーにこれ以上近づかない水平距離 (units)")]
    [SerializeField] private float _walkStopDistance = 1.8f;

    [Tooltip("地上→浮遊 / 浮遊→地上 の上下移動の速さ (units/sec)")]
    [SerializeField] private float _riseSpeed = 6f;

    [Tooltip("浮遊の構えをこの時間 (sec) 続けたら地上へ降りる候補が出る")]
    [SerializeField] private float _floatStanceMinTime = 4f;

    [Tooltip("地上の構えをこの時間 (sec) 続けたら浮遊へ上がる候補が出る")]
    [SerializeField] private float _groundStanceMinTime = 4f;

    [Tooltip("攻撃と攻撃の間の待ち (sec, 第1形態)")]
    [SerializeField] private float _idleTimePhase1 = 0.9f;

    [Tooltip("攻撃と攻撃の間の待ち (sec, 第2形態)")]
    [SerializeField] private float _idleTimePhase2 = 0.6f;

    [Tooltip("ブレイク・拘束が明けてから次の行動までの待ち (sec)")]
    [SerializeField] private float _idleAfterInterrupt = 0.6f;

    [Tooltip("予告の光の色 (ナイフ・急降下・スピン)")]
    [SerializeField] private Color _glowColor = new Color(1f, 0.95f, 0.5f, 0.85f);

    [Tooltip("予告の範囲表示の色 (ステッキ横払い・踏みつけの影の固定時)")]
    [SerializeField] private Color _dangerColor = new Color(1f, 0.2f, 0.2f, 0.3f);

    [Header("踏みつけ (地上)")]
    [SerializeField] private int _stompDamage = 3;
    [Tooltip("再使用までの時間 (sec)")]
    [SerializeField] private float _stompCooldown = 6f;
    [Tooltip("画面上へ跳ぶ速さ (units/sec)")]
    [SerializeField] private float _stompJumpSpeed = 28f;
    [Tooltip("影がプレイヤーを追う時間 (sec)")]
    [SerializeField] private float _stompTrackTime = 1.0f;
    [Tooltip("影が止まってから降ってくるまで (sec)")]
    [SerializeField] private float _stompLockTime = 0.3f;
    [Tooltip("影がプレイヤーを追う速さ (units/sec)")]
    [SerializeField] private float _stompShadowSpeed = 14f;
    [Tooltip("急降下の速さ (units/sec)")]
    [SerializeField] private float _stompFallSpeed = 32f;
    [Tooltip("着地の踏みつけ判定の幅 (units)")]
    [SerializeField] private float _stompHitWidth = 2.4f;
    [Tooltip("着地の踏みつけ判定の高さ (床から, units)")]
    [SerializeField] private float _stompHitHeight = 1.8f;
    [Tooltip("着地後の硬直 (sec, 大きな隙)")]
    [SerializeField] private float _stompRecoverTime = 1.5f;
    [Tooltip("画面の上端からさらに上へ隠れる距離 (units)")]
    [SerializeField] private float _stompOffscreenMargin = 2f;
    [Tooltip("影の色 (暗い床・明るいタイルのどちらでも見えるよう濃いめ)")]
    [SerializeField] private Color _shadowColor = new Color(0.08f, 0f, 0.06f, 0.75f);
    [Tooltip("着地の画面揺れ (強さ, sec)")]
    [SerializeField] private Vector2 _stompShake = new Vector2(0.3f, 0.3f);

    [Header("ナイフ投げ (浮遊)")]
    [Tooltip("ナイフの弾プレハブ (BossProjectile, 絵は右向き)。未設定なら実行時の仮の弾")]
    [SerializeField] private BossProjectile _knifePrefab;
    [SerializeField] private int _knifeDamage = 1;
    [SerializeField] private float _knifeCooldown = 2f;
    [Tooltip("光って予告する時間 (sec)")]
    [SerializeField] private float _knifeWindup = 0.3f;
    [Tooltip("本数")]
    [SerializeField] private int _knifeCount = 3;
    [Tooltip("扇の片側の角度 (度)。3 本なら -この角度 / 0 / +この角度")]
    [SerializeField] private float _knifeSpreadAngle = 15f;
    [SerializeField] private float _knifeSpeed = 14f;
    [SerializeField] private float _knifeLifetime = 3f;
    [Tooltip("投げた後の硬直 (sec)")]
    [SerializeField] private float _knifeRecover = 0.4f;
    [Tooltip("ナイフを出す手の位置 (体の中心から, 右向き基準)")]
    [SerializeField] private Vector2 _handOffset = new Vector2(0.6f, 0.2f);

    [Header("ステッキ横払い (地上)")]
    [SerializeField] private int _sweepDamage = 2;
    [SerializeField] private float _sweepCooldown = 2f;
    [Tooltip("予告 (sec)")]
    [SerializeField] private float _sweepWindup = 0.4f;
    [Tooltip("前方へ払う距離 (体の中心から, units)")]
    [SerializeField] private float _sweepRange = 2.5f;
    [Tooltip("払いの判定の高さ (units)")]
    [SerializeField] private float _sweepHeight = 1.6f;
    [Tooltip("払いの判定が出ている時間 (sec)")]
    [SerializeField] private float _sweepActiveTime = 0.15f;
    [SerializeField] private float _sweepRecover = 0.5f;
    [Tooltip("プレイヤーがこの水平距離以内なら選ぶ (units)")]
    [SerializeField] private float _sweepTriggerRange = 3.2f;

    [Header("斜め急降下体当たり (浮遊→地上)")]
    [SerializeField] private int _diveDamage = 2;
    [SerializeField] private float _diveCooldown = 3f;
    [Tooltip("予告 (sec)")]
    [SerializeField] private float _diveWindup = 0.4f;
    [SerializeField] private float _diveSpeed = 16f;
    [Tooltip("突っ込みの最大時間 (sec, 壁に阻まれた時の保険)")]
    [SerializeField] private float _diveMaxTime = 1.5f;
    [Tooltip("着地後の硬直 (sec)")]
    [SerializeField] private float _diveRecover = 0.5f;

    [Header("スピン (地上・近距離)")]
    [SerializeField] private int _spinDamage = 1;
    [SerializeField] private float _spinCooldown = 2.5f;
    [Tooltip("プレイヤーがこの距離以内に張り付いたら使う (units)")]
    [SerializeField] private float _spinTriggerRange = 2f;
    [Tooltip("予告 (sec)")]
    [SerializeField] private float _spinWindup = 0.3f;
    [Tooltip("回転している時間 (sec)")]
    [SerializeField] private float _spinDuration = 0.6f;
    [Tooltip("回転の判定の半径 (units)。当たるとノックバックで押し返される")]
    [SerializeField] private float _spinRadius = 1.6f;
    [SerializeField] private float _spinRecover = 0.4f;

    [Header("第2形態への遷移 (FullFight のみ)")]
    [Tooltip("遷移演出中の無敵時間 (sec)")]
    [SerializeField] private float _phaseShiftTime = 2f;
    [Tooltip("遷移時の画面揺れ (強さ, sec)")]
    [SerializeField] private Vector2 _phaseShiftShake = new Vector2(0.25f, 0.6f);
    [Tooltip("第2形態に入った瞬間に呼ばれる (専用の絵が来たら見た目の差し替えをここにつなぐ)")]
    [SerializeField] private UnityEvent _onPhase2Started = new();

    [Header("風船 (第2形態)")]
    [Tooltip("風船の弾プレハブ (BossProjectile, 回転しない設定)。未設定なら実行時の仮の弾")]
    [SerializeField] private BossProjectile _balloonPrefab;
    [SerializeField] private int _balloonDamage = 1;
    [SerializeField] private float _balloonCooldown = 6f;
    [Tooltip("呼び出しの構え (sec)")]
    [SerializeField] private float _balloonCastTime = 0.5f;
    [Tooltip("出る数の最小")]
    [SerializeField] private int _balloonCountMin = 6;
    [Tooltip("出る数の最大")]
    [SerializeField] private int _balloonCountMax = 8;
    [Tooltip("床の予告の時間 (sec)")]
    [SerializeField] private float _balloonTelegraph = 0.8f;
    [Tooltip("出現位置の区切り幅 (units)。闘技場の幅をこの幅で区切った枠から選ぶ")]
    [SerializeField] private float _balloonSlotWidth = 1.2f;
    [Tooltip("必ず残す通れる隙間の幅 (units)")]
    [SerializeField] private float _balloonGapWidth = 2.4f;
    [Tooltip("上昇の速さ (units/sec)")]
    [SerializeField] private float _balloonRiseSpeed = 4.5f;
    [Tooltip("風船の当たり判定の半径 (units)")]
    [SerializeField] private float _balloonRadius = 0.45f;
    [SerializeField] private float _balloonRecover = 0.6f;

    [Header("爆弾投げ (第2形態)")]
    [Tooltip("爆弾プレハブ (BossBomb)。未設定なら実行時の仮の爆弾")]
    [SerializeField] private BossBomb _bombPrefab;
    [SerializeField] private int _bombDamage = 2;
    [SerializeField] private float _bombCooldown = 6f;
    [Tooltip("投げる構え (sec)")]
    [SerializeField] private float _bombCastTime = 0.4f;
    [SerializeField] private int _bombCount = 5;
    [Tooltip("プレイヤーの左右どこまで散らすか (units)")]
    [SerializeField] private float _bombSpread = 4.5f;
    [Tooltip("落下点どうしの最小間隔 (units)")]
    [SerializeField] private float _bombMinSpacing = 1.0f;
    [Tooltip("滞空時間 (sec)")]
    [SerializeField] private float _bombFlightTime = 0.8f;
    [Tooltip("放物線の高さ (units)")]
    [SerializeField] private float _bombArcHeight = 3f;
    [Tooltip("着弾から爆発まで (sec)")]
    [SerializeField] private float _bombFuseTime = 1.0f;
    [Tooltip("爆発半径 (units)")]
    [SerializeField] private float _bombRadius = 1.5f;
    [SerializeField] private float _bombRecover = 0.6f;

    [Header("撤退 (RetreatAtThreshold のみ)")]
    [Tooltip("飛び去る前に留まる時間 (sec, 無敵)")]
    [SerializeField] private float _retreatTauntTime = 0.8f;
    [Tooltip("飛び去る速さ (units/sec)")]
    [SerializeField] private float _retreatSpeed = 12f;
    [Tooltip("飛び去ってから消えるまで (sec)")]
    [SerializeField] private float _retreatFlyTime = 2.5f;
    [Tooltip("飛び去り始めた瞬間に呼ばれる (演出用)")]
    [SerializeField] private UnityEvent _onRetreatStarted = new();
    [Tooltip("撤退の瞬間に流れ弾 (ナイフ) で壊す対象。BreakableSpeaker なら ForceBreak (フラグ・通知・連動はプレイヤーが壊した時と同じ)、" +
             "それ以外の IDamageable なら大ダメージを与える。未設定、または壊れ済み (破壊済みフラグで消えている) なら何もしない")]
    [SerializeField] private Component _retreatBreakTarget;
    [Tooltip("流れ弾で壊した時の通知 (BreakableSpeaker のみ。空なら対象の既定の通知)")]
    [SerializeField] private string _retreatBreakMessage = "";
    [Tooltip("流れ弾の速さ (units/sec)。届くまで飛び去りを待つ")]
    [SerializeField] private float _strayShotSpeed = 24f;
    [Tooltip("流れ弾が当たった時の画面揺れ (強さ, 秒)")]
    [SerializeField] private Vector2 _strayShotShake = new Vector2(0.25f, 0.4f);

    [Header("会話 (宴, 空なら流さない)")]
    [Tooltip("プレイヤーが闘技場に入った時 (ゲートが閉まった直後) の会話ラベル。終わるまで攻撃しない")]
    [SerializeField] private string _introDialogueLabel = "";
    [Tooltip("戦闘前の会話を一度きりにするフラグ (GameProgress)。会話の後に立て、立っていれば流さない (死亡リトライで繰り返さない)")]
    [SerializeField] private string _introDialogueFlag = "";
    [Tooltip("撤退の会話ラベル (RetreatAtThreshold のみ)。流れ弾が届いた後に流し、終わってから飛び去る (= 勝ち扱い)")]
    [SerializeField] private string _retreatDialogueLabel = "";

    #endregion

    #region Fields

    private Rigidbody2D _rb;
    private Collider2D _col;
    private LayerMask _ground;

    private Stance _stance;
    private Step _step;
    private float _timer;        // 現在の Step の残り時間
    private float _idleTimer;    // Idle から次の行動を選ぶまで
    private float _stanceTimer;  // 今の構えを続けている時間
    private readonly float[] _cooldowns = new float[(int)AttackId.Count];

    private bool _phase2;
    private bool _engaged;       // プレイヤーが闘技場に入った (戦闘開始)
    private int _thresholdHp;
    private bool _thresholdHandled;
    private bool _retreatFlying;
    private Vector2 _retreatVelocity;
    private float _retreatTimer;
    private readonly DialogueStep _introDialogue = new();
    private readonly DialogueStep _retreatDialogue = new();

    private float _floorY;        // 闘技場の床の高さ (基準)
    private float _bottomOffset;  // transform.position.y - 体の下端
    private float _arenaMinX;
    private float _arenaMaxX;
    private float _arenaTopY;
    private float _facing = 1f;   // 攻撃の向き (+1 右 / -1 左)

    private Vector2 _diveTarget;
    private float _stompLandY;
    private Vector2 _sweepCenter;
    private Vector2 _sweepSize;
    private SpriteRenderer _shadow;
    private readonly List<SpriteRenderer> _telegraphs = new();
    private readonly List<int> _slotBuffer = new();
    private readonly List<float> _bombXs = new();
    private readonly List<(AttackId id, float weight)> _candidates = new();

    #endregion

    #region Properties

    /// <summary>第2形態に入っているか。</summary>
    public bool IsPhase2 => _phase2;

    /// <summary>浮遊の構えか。</summary>
    public bool IsFloating => _stance == Stance.Floating;

    /// <summary>
    /// 動きの合図 (Animator の trigger と同じ名前: Rise / Descend / Knife / Sweep / Dive / Spin / StompJump / StompFall /
    /// StompLand / Balloon / Bomb / PhaseShift / Retreat)。Spine の見た目 (GramineSpineVisual) が受け取る。
    /// </summary>
    public event System.Action<string> Cue;

    /// <summary>戦い方 (1 戦目/2 戦目) を実行時に切り替える (Start より前に呼ぶこと)。</summary>
    public BossFightMode Mode
    {
        get => _mode;
        set => _mode = value;
    }

    #endregion

    #region Unity Callbacks

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _ground = BossKit.GroundMaskOrDefault(_groundMask);
    }

    private void Start()
    {
        if (Enemy == null || Enemy.Consts == null || _col == null)
        {
            enabled = false;
            return;
        }

        // 倒れた時は場に残った弾・爆弾・風船の予告を消す (撃破の後に爆発してプレイヤーが被弾しないように)
        Enemy.OnDied += HandleDied;

        var bounds = _col.bounds;
        _bottomOffset = transform.position.y - bounds.min.y;
        _floorY = BossKit.FindFloorY(bounds.center.x, bounds.center.y, 50f, _ground, bounds.min.y);

        // 闘技場の範囲
        if (_arenaArea != null)
        {
            var a = _arenaArea.bounds;
            _arenaMinX = a.min.x;
            _arenaMaxX = a.max.x;
            _arenaTopY = a.max.y;
        }
        else
        {
            _arenaMinX = HomePosition.x - _arenaHalfWidth;
            _arenaMaxX = HomePosition.x + _arenaHalfWidth;
            _arenaTopY = _floorY + 10f;
        }

        // しきい値の HP を下限にして、大技一発で形態遷移/撤退を飛ばされないようにする
        var maxHp = Enemy.Consts.MaxHp;
        _thresholdHp = Mathf.Clamp(Mathf.CeilToInt(maxHp * _phaseThreshold), 1, Mathf.Max(1, maxHp - 1));
        Enemy.HpFloor = _thresholdHp;

        // 初期の構え: 床に足が着いていれば地上、そうでなければ浮遊
        SetStance(bounds.min.y - _floorY < 0.3f ? Stance.Ground : Stance.Floating);
        _step = Step.Idle;
        _idleTimer = 1.5f;
    }

    private void Update()
    {
        if (_retreatFlying)
        {
            UpdateRetreatFlight(Time.deltaTime);
            return;
        }

        if (_thresholdHandled || Enemy == null || Enemy.IsDead || Enemy.Hp == null)
            return;

        // HP しきい値。TakeDamage の途中 (購読コールバック) ではなくここで処理する (ブレイク処理と競合させない)
        if (Enemy.Hp.CurrentValue > _thresholdHp)
            return;

        _thresholdHandled = true;
        if (_mode == BossFightMode.RetreatAtThreshold)
            BeginRetreat();
        else
            BeginPhaseShift();
    }

    private void OnDestroy()
    {
        if (Enemy != null)
            Enemy.OnDied -= HandleDied;
        ClearTelegraphs();
        BossKit.DestroyMarker(_shadow);
    }

    private void HandleDied(EnemyController _)
    {
        ClearTelegraphs();
        BossKit.DestroyMarker(_shadow);
        _shadow = null;
        BossHazard.ClearAll();
    }

    #endregion

    #region EnemyBehaviour

    public override void PhysicsTick()
    {
        var dt = Time.fixedDeltaTime;
        for (var i = 0; i < _cooldowns.Length; i++)
            _cooldowns[i] -= dt;
        _stanceTimer += dt;
        _timer -= dt;

        switch (_step)
        {
            case Step.Idle: TickIdle(dt); break;
            case Step.Rise: TickRise(dt); break;
            case Step.Descend: TickDescend(); break;

            case Step.KnifeWindup:
                Hold();
                if (_timer <= 0f)
                {
                    ClearTelegraphs();
                    FireKnives();
                    EnterRecover(_knifeRecover);
                }
                break;

            case Step.SweepWindup:
                Hold();
                if (_timer <= 0f)
                {
                    // 払いの瞬間は範囲表示を濃くする
                    foreach (var t in _telegraphs)
                        if (t != null) t.color = new Color(_dangerColor.r, _dangerColor.g, _dangerColor.b, Mathf.Min(1f, _dangerColor.a * 2.5f));
                    _step = Step.SweepActive;
                    _timer = _sweepActiveTime;
                }
                break;

            case Step.SweepActive:
                Hold();
                BossKit.HitPlayerBox(_sweepCenter, _sweepSize, _sweepDamage, gameObject);
                if (_timer <= 0f)
                {
                    ClearTelegraphs();
                    EnterRecover(_sweepRecover);
                }
                break;

            case Step.DiveWindup:
                Hold();
                if (_timer <= 0f)
                    BeginDiveDash();
                break;

            case Step.DiveDash: TickDiveDash(dt); break;

            case Step.SpinWindup:
                Hold();
                if (_timer <= 0f)
                {
                    ClearTelegraphs();
                    _step = Step.Spinning;
                    _timer = _spinDuration;
                    Animation?.SetBool("Spinning", true);
                }
                break;

            case Step.Spinning:
                Hold();
                BossKit.HitPlayerCircle(_col.bounds.center, _spinRadius, _spinDamage, gameObject);
                if (_timer <= 0f)
                {
                    Animation?.SetBool("Spinning", false);
                    EnterRecover(_spinRecover);
                }
                break;

            case Step.StompJump: TickStompJump(dt); break;
            case Step.StompTrack: TickStompTrack(dt); break;
            case Step.StompLock:
                if (_timer <= 0f)
                    BeginStompFall();
                break;
            case Step.StompFall: TickStompFall(dt); break;

            case Step.BalloonCast:
                Hold();
                if (_timer <= 0f)
                {
                    SpawnBalloons();
                    EnterRecover(_balloonRecover);
                }
                break;

            case Step.BombCast:
                Hold();
                if (_timer <= 0f)
                {
                    ThrowBombs();
                    EnterRecover(_bombRecover);
                }
                break;

            case Step.Recover:
                Hold();
                if (_timer <= 0f)
                    EnterIdle();
                break;

            case Step.PhaseShift:
                Hold();
                if (_timer <= 0f)
                    EndPhaseShift();
                break;

            case Step.RetreatTaunt:
                Hold();
                // 流れ弾が届いて (_timer) → スピーカーの破壊演出 (スロー・カメラの寄せ) が終わって → 撤退の会話が終わってから飛び去る
                // (演出のスロー中に会話のポーズが重ならないよう、会話は演出の後に始める)
                if (_timer <= 0f && !SpeakerBreakEffect.IsPlaying && DialogueDone(_retreatDialogueLabel, _retreatDialogue))
                    StartRetreatFlight();
                break;
        }
    }

    public override void OnInterrupted()
    {
        // ブレイク・拘束: 予告を消し、浮遊中でも重力を戻して地面に落とす (裁断のチャンス)
        ClearTelegraphs();
        BossKit.DestroyMarker(_shadow);
        _shadow = null;
        Animation?.SetBool("Spinning", false);
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.linearVelocity = new Vector2(0f, Mathf.Min(0f, _rb.linearVelocity.y));
        SetStance(Stance.Ground);

        // 形態遷移・撤退の演出中なら、明けた後にそのまま続ける
        if (_step == Step.PhaseShift || _step == Step.RetreatTaunt)
            return;

        _step = Step.Idle;
        _idleTimer = _idleAfterInterrupt;
    }

    #endregion

    #region Idle / 行動選択

    private void TickIdle(float dt)
    {
        if (_stance == Stance.Floating)
            HoverTowards(PreferredFloatX());
        else
            WalkTowardsPlayer();

        FacePlayer();

        // プレイヤーが闘技場に入るまでは攻撃しない (入口の手前でナイフを受けて隣の部屋へ押し戻されないように)
        if (!PlayerInArena())
        {
            _idleTimer = Mathf.Max(_idleTimer, _idleTimePhase1);
            return;
        }

        _idleTimer -= dt;
        if (_idleTimer <= 0f)
            ChooseNext();
    }

    /// <summary>
    /// プレイヤーが一度でも闘技場 (_arenaArea) に入ったか (入った後は外れても戦い続ける。闘技場の端の段に逃げて
    /// 攻撃が止まらないように)。_arenaArea 未設定なら常に true。
    /// </summary>
    private bool PlayerInArena()
    {
        if (_engaged || _arenaArea == null || Player == null)
            return true;
        var b = _arenaArea.bounds;
        var p = Player.position;
        var inside = p.x >= b.min.x && p.x <= b.max.x && p.y >= b.min.y - 1f && p.y <= b.max.y + 1f;
        if (!inside && _introDialogue.State == DialogueState.None)
            return false;

        // 戦闘前の会話 (一度きり: _introDialogueFlag が立っていれば流さない)。終わるまで攻撃しない
        if (_introDialogue.State == DialogueState.None
            && (string.IsNullOrEmpty(_introDialogueLabel)
                || (!string.IsNullOrEmpty(_introDialogueFlag) && GameProgress.Has(_introDialogueFlag))))
            _introDialogue.State = DialogueState.Done;
        if (!DialogueDone(_introDialogueLabel, _introDialogue))
            return false;

        if (!string.IsNullOrEmpty(_introDialogueFlag))
            GameProgress.Set(_introDialogueFlag);
        _engaged = true;
        return true;
    }

    private enum DialogueState { None, Playing, Done }

    /// <summary>会話 1 回分の進み具合 (終わりのコールバックから書き換えるのでクラス)。</summary>
    private sealed class DialogueStep
    {
        public DialogueState State;
    }

    /// <summary>
    /// 会話 (宴) を 1 回だけ流し、終わったら true。ラベルが空なら即 true。
    /// 別の会話の再生中・ロード中は次の呼び出しで再挑戦する (Play が無視されて終わりのコールバックが来ない、を避ける)。
    /// 会話中は GamePause (timeScale 0) で FixedUpdate が止まるので、ボスの行動は会話の終わりまで進まない。
    /// </summary>
    private static bool DialogueDone(string label, DialogueStep step)
    {
        if (step.State == DialogueState.Done || string.IsNullOrEmpty(label))
        {
            step.State = DialogueState.Done;
            return true;
        }
        if (step.State == DialogueState.Playing || DialogueService.IsBusy)
            return false;

        step.State = DialogueState.Playing;
        DialogueService.Play(label, () => step.State = DialogueState.Done);
        return false;
    }

    private void EnterIdle()
    {
        _step = Step.Idle;
        _idleTimer = _phase2 ? _idleTimePhase2 : _idleTimePhase1;
    }

    private void EnterRecover(float time)
    {
        _step = Step.Recover;
        _timer = time;
    }

    /// <summary>今の構えと距離・クールダウンから次の行動を重み付きで選ぶ。</summary>
    private void ChooseNext()
    {
        if (Player == null)
        {
            _idleTimer = 0.5f;
            return;
        }

        var toPlayer = (Vector2)Player.position - (Vector2)_col.bounds.center;
        var dist = toPlayer.magnitude;
        var dx = Mathf.Abs(toPlayer.x);

        _candidates.Clear();
        if (_stance == Stance.Ground)
        {
            // 張り付かれたら最優先でスピン
            if (dist <= _spinTriggerRange && Ready(AttackId.Spin))
            {
                BeginSpin();
                return;
            }

            if (Ready(AttackId.Sweep) && dx <= _sweepTriggerRange)
                _candidates.Add((AttackId.Sweep, 4f));
            if (Ready(AttackId.Stomp))
                _candidates.Add((AttackId.Stomp, dx > _sweepTriggerRange ? 3f : 1.5f));
            AddPhase2Candidates();

            var wantRise = _stanceTimer >= _groundStanceMinTime;
            if (wantRise)
                _candidates.Add((AttackId.Count, 3f)); // Count = 構えの切り替え (浮遊へ)
        }
        else
        {
            if (Ready(AttackId.Knife))
                _candidates.Add((AttackId.Knife, 3f));
            AddPhase2Candidates();

            if (_stanceTimer >= _floatStanceMinTime)
            {
                // 斜め急降下で地上へ。使えなければ普通に降りる
                _candidates.Add(Ready(AttackId.Dive) && toPlayer.y < -0.5f
                    ? (AttackId.Dive, 4f)
                    : (AttackId.Count, 2f));
            }
        }

        if (_candidates.Count == 0)
        {
            _idleTimer = 0.3f;
            return;
        }

        var total = 0f;
        foreach (var c in _candidates)
            total += c.weight;
        var roll = Random.value * total;
        var chosen = _candidates[_candidates.Count - 1].id;
        foreach (var c in _candidates)
        {
            roll -= c.weight;
            if (roll <= 0f)
            {
                chosen = c.id;
                break;
            }
        }

        switch (chosen)
        {
            case AttackId.Stomp: BeginStomp(); break;
            case AttackId.Knife: BeginKnife(); break;
            case AttackId.Sweep: BeginSweep(); break;
            case AttackId.Dive: BeginDive(); break;
            case AttackId.Spin: BeginSpin(); break;
            case AttackId.Balloon: BeginBalloon(); break;
            case AttackId.Bomb: BeginBomb(); break;
            default:
                if (_stance == Stance.Ground) BeginRise();
                else BeginDescend();
                break;
        }
    }

    private void AddPhase2Candidates()
    {
        if (!_phase2)
            return;
        if (Ready(AttackId.Balloon))
            _candidates.Add((AttackId.Balloon, 2f));
        if (Ready(AttackId.Bomb))
            _candidates.Add((AttackId.Bomb, 2f));
    }

    private bool Ready(AttackId id) => _cooldowns[(int)id] <= 0f;

    private void StartCooldown(AttackId id, float time) => _cooldowns[(int)id] = time;

    #endregion

    #region 構え・移動

    private void SetStance(Stance stance)
    {
        _stance = stance;
        _stanceTimer = 0f;
        _rb.gravityScale = stance == Stance.Floating ? 0f : _groundGravityScale;
        Animation?.SetBool("Floating", stance == Stance.Floating);
    }

    /// <summary>浮遊の構えの高さ (transform.position.y)。</summary>
    private float HoverY(float x) => FloorYAt(x) + _hoverHeight + _bottomOffset;

    /// <summary>地上に立った時の高さ (transform.position.y)。</summary>
    private float GroundY(float x) => FloorYAt(x) + _bottomOffset;

    /// <summary>x の床の高さ。闘技場の床の少し上から真下へ探す (見つからなければ基準の床)。</summary>
    private float FloorYAt(float x) => BossKit.FindFloorY(x, _floorY + 3f, 8f, _ground, _floorY);

    private float ClampX(float x, float margin = 1f) =>
        Mathf.Clamp(x, _arenaMinX + margin, Mathf.Max(_arenaMinX + margin, _arenaMaxX - margin));

    /// <summary>浮遊中に取りたい水平位置 (プレイヤーから一定距離、今いる側を保つ)。</summary>
    private float PreferredFloatX()
    {
        if (Player == null)
            return transform.position.x;

        var px = Player.position.x;
        var side = transform.position.x >= px ? 1f : -1f;
        var x = ClampX(px + side * _floatKeepDistance);
        // 壁ぎわで近づきすぎるなら反対側へ回る
        if (Mathf.Abs(x - px) < _floatKeepDistance * 0.5f)
            x = ClampX(px - side * _floatKeepDistance);
        return x;
    }

    private void HoverTowards(float targetX)
    {
        var pos = _rb.position;
        var target = new Vector2(targetX, HoverY(targetX) + Mathf.Sin(Time.time * _bobSpeed) * _bobAmplitude);
        _rb.linearVelocity = Vector2.ClampMagnitude((target - pos) * 3f, _floatMoveSpeed);
    }

    private void WalkTowardsPlayer()
    {
        if (Player == null)
        {
            Enemy.Move(0f);
            return;
        }

        var dx = Player.position.x - transform.position.x;
        var dir = Mathf.Sign(dx);
        var x = transform.position.x;
        var blocked = (dir > 0f && x >= _arenaMaxX - 1f) || (dir < 0f && x <= _arenaMinX + 1f);
        Enemy.Move(Mathf.Abs(dx) > _walkStopDistance && !blocked ? dir * _walkSpeed : 0f);
    }

    /// <summary>その場に留まる (浮遊中は止まる、地上は水平だけ止めて重力に任せる)。</summary>
    private void Hold()
    {
        if (_rb.bodyType != RigidbodyType2D.Dynamic)
            return;
        if (_stance == Stance.Floating)
            _rb.linearVelocity = Vector2.zero;
        else
            Enemy.Move(0f);
    }

    private void FacePlayer()
    {
        if (Player == null)
            return;
        var dx = Player.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.05f)
        {
            _facing = Mathf.Sign(dx);
            Enemy.Face(_facing);
        }
    }

    private bool IsGrounded()
    {
        var b = _col.bounds;
        return Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 0.15f, _ground).collider != null
               && _rb.linearVelocity.y <= 0.05f;
    }

    private void BeginRise()
    {
        _step = Step.Rise;
        _timer = 2f;
        _rb.gravityScale = 0f;
        PlayCue("Rise");
    }

    private void TickRise(float dt)
    {
        var pos = _rb.position;
        var targetY = HoverY(pos.x);
        var vy = Mathf.Clamp((targetY - pos.y) / dt, -_riseSpeed, _riseSpeed);
        _rb.linearVelocity = new Vector2(0f, vy);
        if (Mathf.Abs(targetY - pos.y) < 0.05f || _timer <= 0f)
        {
            _rb.linearVelocity = Vector2.zero;
            SetStance(Stance.Floating);
            EnterIdle();
        }
    }

    private void BeginDescend()
    {
        _step = Step.Descend;
        _timer = 2f;
        _rb.gravityScale = _groundGravityScale;
        _rb.linearVelocity = Vector2.zero;
        PlayCue("Descend");
    }

    private void TickDescend()
    {
        Enemy.Move(0f);
        if (IsGrounded() || _timer <= 0f)
        {
            SetStance(Stance.Ground);
            EnterIdle();
        }
    }

    #endregion

    #region 予告表示

    private SpriteRenderer AddTelegraph(string name, Sprite sprite, Color color, Vector2 position, Vector2 size)
    {
        var sr = BossKit.CreateMarker(name, sprite, color, position, size);
        _telegraphs.Add(sr);
        return sr;
    }

    /// <summary>体が光る予告 (手元に脈打つ光)。</summary>
    private void AddGlow(Vector2 position, float size)
    {
        var sr = AddTelegraph("BossGlow", BossKit.CircleSprite, _glowColor, position, new Vector2(size, size));
        sr.sortingOrder = BossKit.MarkerSortingOrder + 5;
        sr.transform.DOScale(new Vector3(size * 1.35f, size * 1.35f, 1f), 0.1f).SetLoops(-1, LoopType.Yoyo);
    }

    private void ClearTelegraphs()
    {
        foreach (var t in _telegraphs)
            BossKit.DestroyMarker(t);
        _telegraphs.Clear();
    }

    private Vector2 HandPosition() =>
        (Vector2)_col.bounds.center + new Vector2(_handOffset.x * _facing, _handOffset.y);

    #endregion

    #region ナイフ投げ (浮遊)

    private void BeginKnife()
    {
        FacePlayer();
        StartCooldown(AttackId.Knife, _knifeCooldown);
        _step = Step.KnifeWindup;
        _timer = _knifeWindup;
        AddGlow(HandPosition(), 0.7f);
        PlayCue("Knife");
    }

    private void FireKnives()
    {
        FacePlayer();
        var origin = HandPosition();
        var target = BossKit.PlayerCenter() ?? origin + new Vector2(_facing, 0f);
        var aim = (target - origin).sqrMagnitude > 0.0001f ? (target - origin).normalized : new Vector2(_facing, 0f);

        var count = Mathf.Max(1, _knifeCount);
        for (var i = 0; i < count; i++)
        {
            var angle = count == 1 ? 0f : Mathf.Lerp(-_knifeSpreadAngle, _knifeSpreadAngle, i / (float)(count - 1));
            var dir = (Vector2)(Quaternion.Euler(0f, 0f, angle) * aim);
            var knife = _knifePrefab != null
                ? Instantiate(_knifePrefab, origin, Quaternion.identity)
                : BossProjectile.CreateFallback("BossKnife", BossKit.SquareSprite, new Color(0.85f, 0.85f, 0.95f, 1f),
                    new Vector2(0.6f, 0.14f), 0.2f, true);
            knife.transform.position = origin;
            knife.Launch(dir * _knifeSpeed, 0f, _knifeDamage, _knifeLifetime, gameObject, true, _ground);
        }
    }

    #endregion

    #region ステッキ横払い (地上)

    private void BeginSweep()
    {
        FacePlayer();
        StartCooldown(AttackId.Sweep, _sweepCooldown);
        _step = Step.SweepWindup;
        _timer = _sweepWindup;

        var center = (Vector2)_col.bounds.center;
        _sweepCenter = center + new Vector2(_facing * _sweepRange * 0.5f, 0f);
        _sweepSize = new Vector2(_sweepRange, _sweepHeight);
        AddTelegraph("SweepRange", BossKit.SquareSprite, _dangerColor, _sweepCenter, _sweepSize);
        PlayCue("Sweep");
    }

    #endregion

    #region 斜め急降下体当たり (浮遊→地上)

    private void BeginDive()
    {
        FacePlayer();
        StartCooldown(AttackId.Dive, _diveCooldown);
        _step = Step.DiveWindup;
        _timer = _diveWindup;
        AddGlow(_col.bounds.center, 1.4f);
        PlayCue("Dive");
    }

    private void BeginDiveDash()
    {
        ClearTelegraphs();
        var px = Player != null ? Player.position.x : transform.position.x + _facing * 3f;
        var x = ClampX(px);
        _diveTarget = new Vector2(x, GroundY(x));
        _step = Step.DiveDash;
        _timer = _diveMaxTime;
        _rb.gravityScale = 0f;
    }

    private void TickDiveDash(float dt)
    {
        var pos = _rb.position;
        var delta = _diveTarget - pos;
        var step = _diveSpeed * dt;
        _rb.linearVelocity = delta.magnitude <= step ? delta / dt : delta.normalized * _diveSpeed;
        if (Mathf.Abs(delta.x) > 0.05f)
            Enemy.Face(Mathf.Sign(delta.x));

        // 体当たりの判定 (接触ダメージより先に、技のダメージを入れる)
        BossKit.HitPlayerBox(_col.bounds.center, _col.bounds.size, _diveDamage, gameObject);

        var arrived = delta.magnitude <= 0.05f || pos.y <= _diveTarget.y + 0.02f;
        var landedEarly = _timer < _diveMaxTime - 0.1f && IsGroundedIgnoringVelocity();
        if (!arrived && !landedEarly && _timer > 0f)
            return;

        _rb.linearVelocity = Vector2.zero;
        SetStance(Stance.Ground);
        CameraShake.Shake(_stompShake.x * 0.5f, _stompShake.y * 0.6f);
        EnterRecover(_diveRecover);
    }

    private bool IsGroundedIgnoringVelocity()
    {
        var b = _col.bounds;
        return Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 0.15f, _ground).collider != null;
    }

    #endregion

    #region スピン (地上・近距離)

    private void BeginSpin()
    {
        FacePlayer();
        StartCooldown(AttackId.Spin, _spinCooldown);
        _step = Step.SpinWindup;
        _timer = _spinWindup;
        AddGlow(_col.bounds.center, 1.2f);
        PlayCue("Spin");
    }

    #endregion

    #region 踏みつけ (地上)

    private void BeginStomp()
    {
        StartCooldown(AttackId.Stomp, _stompCooldown);
        _step = Step.StompJump;
        _rb.bodyType = RigidbodyType2D.Kinematic; // 天井をすり抜けて画面外へ
        _rb.linearVelocity = Vector2.zero;
        PlayCue("StompJump");
    }

    /// <summary>画面外 (カメラの上端と闘技場の天井の高い方のさらに上) の高さ。</summary>
    private float OffscreenY()
    {
        var top = _arenaTopY;
        var cam = Camera.main;
        if (cam != null && cam.orthographic)
            top = Mathf.Max(top, cam.transform.position.y + cam.orthographicSize);
        return top + _stompOffscreenMargin + _col.bounds.size.y + _bottomOffset;
    }

    private void TickStompJump(float dt)
    {
        var pos = _rb.position;
        var topY = OffscreenY();
        pos.y = Mathf.Min(topY, pos.y + _stompJumpSpeed * dt);
        _rb.MovePosition(pos);
        if (pos.y < topY)
            return;

        // 画面外に消えたら、プレイヤーの足元に影を出して追わせる
        var px = Player != null ? ClampX(Player.position.x) : pos.x;
        _shadow = BossKit.CreateMarker("StompShadow", BossKit.CircleSprite, _shadowColor,
            new Vector2(px, FloorYAt(px)), new Vector2(_stompHitWidth, _stompHitWidth * 0.25f));
        _step = Step.StompTrack;
        _timer = _stompTrackTime;
    }

    private void TickStompTrack(float dt)
    {
        if (_shadow != null && Player != null)
        {
            var x = Mathf.MoveTowards(_shadow.transform.position.x, ClampX(Player.position.x), _stompShadowSpeed * dt);
            _shadow.transform.position = new Vector3(x, FloorYAt(x), 0f);
        }

        if (_timer > 0f)
            return;

        // 固定: 影を危険色にして降ってくる場所を確定させる
        if (_shadow != null)
            _shadow.color = new Color(_dangerColor.r, _dangerColor.g * 0.5f, _dangerColor.b * 0.5f, Mathf.Max(_shadowColor.a, 0.6f));
        _step = Step.StompLock;
        _timer = _stompLockTime;
    }

    private void BeginStompFall()
    {
        var x = _shadow != null ? _shadow.transform.position.x : _rb.position.x;
        _stompLandY = GroundY(x);
        var start = new Vector2(x, OffscreenY());
        _rb.position = start;
        transform.position = start;
        _step = Step.StompFall;
        PlayCue("StompFall");
    }

    /// <summary>動きの合図: Animator の trigger (あれば) と <see cref="Cue"/> の購読者 (Spine の見た目) に送る。</summary>
    private void PlayCue(string name)
    {
        Animation?.SetTrigger(name);
        Cue?.Invoke(name);
    }

    private void TickStompFall(float dt)
    {
        var pos = _rb.position;
        pos.y = Mathf.Max(_stompLandY, pos.y - _stompFallSpeed * dt);
        _rb.MovePosition(pos);

        // 落ちてくる体に当たっても踏みつけのダメージ
        BossKit.HitPlayerBox(_col.bounds.center, _col.bounds.size, _stompDamage, gameObject);

        if (pos.y > _stompLandY)
            return;

        // 着地: 足元の範囲に踏みつけ判定、画面揺れ、大きな隙
        var floor = _stompLandY - _bottomOffset;
        BossKit.HitPlayerBox(new Vector2(pos.x, floor + _stompHitHeight * 0.5f),
            new Vector2(_stompHitWidth, _stompHitHeight), _stompDamage, gameObject);
        BossKit.DestroyMarker(_shadow);
        _shadow = null;
        _rb.bodyType = RigidbodyType2D.Dynamic;
        _rb.linearVelocity = Vector2.zero;
        SetStance(Stance.Ground);
        CameraShake.Shake(_stompShake.x, _stompShake.y);
        PlayCue("StompLand");
        EnterRecover(_stompRecoverTime);
    }

    #endregion

    #region 風船 (第2形態)

    private void BeginBalloon()
    {
        StartCooldown(AttackId.Balloon, _balloonCooldown);
        _step = Step.BalloonCast;
        _timer = _balloonCastTime;
        PlayCue("Balloon");
    }

    /// <summary>
    /// 闘技場の幅を区切った枠から 6〜8 か所を選んで予告し、0.8 秒後に風船を上昇させる。
    /// 連続した枠を「隙間」として必ず空け、通り抜けられる場所を残す。
    /// </summary>
    private void SpawnBalloons()
    {
        var min = _arenaMinX + 0.5f;
        var max = _arenaMaxX - 0.5f;
        var slots = Mathf.Max(2, Mathf.FloorToInt((max - min) / Mathf.Max(0.3f, _balloonSlotWidth)));
        var slotWidth = (max - min) / slots;

        // 隙間の幅には風船の直径ぶんを足す (隣の風船がはみ出しても通れるように)
        var gapSlots = Mathf.Clamp(Mathf.CeilToInt((_balloonGapWidth + _balloonRadius * 2f) / slotWidth), 1, slots - 1);
        var gapStart = Random.Range(0, slots - gapSlots + 1);

        _slotBuffer.Clear();
        for (var i = 0; i < slots; i++)
        {
            if (i < gapStart || i >= gapStart + gapSlots)
                _slotBuffer.Add(i);
        }
        for (var i = _slotBuffer.Count - 1; i > 0; i--)
        {
            var j = Random.Range(0, i + 1);
            (_slotBuffer[i], _slotBuffer[j]) = (_slotBuffer[j], _slotBuffer[i]);
        }

        var count = Mathf.Min(_slotBuffer.Count, Random.Range(_balloonCountMin, _balloonCountMax + 1));
        var telegraphColor = new Color(_dangerColor.r, _dangerColor.g, _dangerColor.b, Mathf.Max(0.45f, _dangerColor.a));
        for (var i = 0; i < count; i++)
        {
            var x = min + (_slotBuffer[i] + 0.5f) * slotWidth;
            var floor = FloorYAt(x);
            var spawn = new Vector2(x, floor + _balloonRadius);
            var lifetime = Mathf.Max(0.5f, (OffscreenY() - spawn.y) / Mathf.Max(0.1f, _balloonRiseSpeed));
            BossTimedMarker.Create("BalloonSpot", BossKit.SquareSprite, telegraphColor,
                new Vector2(x, floor + 0.08f), new Vector2(slotWidth * 0.8f, 0.16f), _balloonTelegraph,
                () => LaunchBalloon(spawn, lifetime));
        }
    }

    private void LaunchBalloon(Vector2 spawn, float lifetime)
    {
        if (this == null)
            return;

        var balloon = _balloonPrefab != null
            ? Instantiate(_balloonPrefab, spawn, Quaternion.identity)
            : BossProjectile.CreateFallback("BossBalloon", BossKit.CircleSprite, new Color(1f, 0.45f, 0.6f, 1f),
                new Vector2(_balloonRadius * 2f, _balloonRadius * 2.3f), _balloonRadius, false);
        balloon.transform.position = spawn;
        balloon.Launch(new Vector2(0f, _balloonRiseSpeed), 0f, _balloonDamage, lifetime, gameObject, false, _ground);
    }

    #endregion

    #region 爆弾投げ (第2形態)

    private void BeginBomb()
    {
        FacePlayer();
        StartCooldown(AttackId.Bomb, _bombCooldown);
        _step = Step.BombCast;
        _timer = _bombCastTime;
        PlayCue("Bomb");
    }

    /// <summary>プレイヤーの周りに散らして爆弾を投げる (落下点どうしは最小間隔を空ける)。</summary>
    private void ThrowBombs()
    {
        var px = Player != null ? Player.position.x : transform.position.x;
        var origin = HandPosition();

        _bombXs.Clear();
        for (var i = 0; i < _bombCount; i++)
        {
            var x = ClampX(px + Random.Range(-_bombSpread, _bombSpread), 0.5f);
            for (var attempt = 0; attempt < 8 && TooClose(x); attempt++)
                x = ClampX(px + Random.Range(-_bombSpread, _bombSpread), 0.5f);
            _bombXs.Add(x);
        }

        for (var i = 0; i < _bombXs.Count; i++)
        {
            var x = _bombXs[i];
            var bomb = _bombPrefab != null
                ? Instantiate(_bombPrefab, origin, Quaternion.identity)
                : BossBomb.CreateFallback(0.5f);
            var landing = new Vector2(x, FloorYAt(x) + 0.25f);
            // 少しずつ時間差を付けて、同時に爆発しないようにする
            bomb.Launch(origin, landing, _bombFlightTime + i * 0.06f, _bombArcHeight, _bombFuseTime,
                _bombRadius, _bombDamage, gameObject);
        }
    }

    private bool TooClose(float x)
    {
        foreach (var other in _bombXs)
        {
            if (Mathf.Abs(other - x) < _bombMinSpacing)
                return true;
        }
        return false;
    }

    #endregion

    #region 第2形態への遷移 (FullFight)

    private void BeginPhaseShift()
    {
        CancelForCutscene();
        Enemy.IsInvulnerable = true;
        BossHazard.ClearAll(); // 画面内の弾は消す
        _step = Step.PhaseShift;
        _timer = _phaseShiftTime;
        CameraShake.Shake(_phaseShiftShake.x, _phaseShiftShake.y);
        PlayCue("PhaseShift");
    }

    private void EndPhaseShift()
    {
        _phase2 = true;
        Enemy.HpFloor = 0; // ここからは倒せる
        Enemy.IsInvulnerable = false;
        OnPhase2Started();
        EnterIdle();
    }

    /// <summary>
    /// 第2形態に入った時の演出フック。専用の絵が来るまで見た目は変えない
    /// (差し替えは継承か Inspector の _onPhase2Started で行う)。
    /// </summary>
    protected virtual void OnPhase2Started() => _onPhase2Started?.Invoke();

    #endregion

    #region 撤退 (RetreatAtThreshold)

    private void BeginRetreat()
    {
        CancelForCutscene();
        Enemy.IsInvulnerable = true;
        BossHazard.ClearAll();
        _step = Step.RetreatTaunt;
        _timer = _retreatTauntTime;
        PlayCue("Retreat");

        // 流れ弾が対象を壊してから飛び去る (届くまで留まる)
        var travel = FireStrayShot();
        if (travel > 0f)
            _timer = Mathf.Max(_retreatTauntTime, travel + 0.4f);
    }

    /// <summary>
    /// 撤退の瞬間の流れ弾。_retreatBreakTarget へナイフを 1 本飛ばし、届いたら壊す。
    /// 戻り値 = 届くまでの秒数 (撃たなかったら 0)。
    /// </summary>
    private float FireStrayShot()
    {
        if (_retreatBreakTarget == null || !(_retreatBreakTarget is BreakableSpeaker || _retreatBreakTarget is IDamageable))
            return 0f;
        if (_retreatBreakTarget is BreakableSpeaker brokenSpeaker && brokenSpeaker.IsBroken)
            return 0f; // 既に壊れている (破壊済みフラグ) なら撃たない

        var targetCol = _retreatBreakTarget.GetComponentInChildren<Collider2D>();
        var targetPoint = targetCol != null && targetCol.enabled
            ? (Vector2)targetCol.bounds.center
            : (Vector2)_retreatBreakTarget.transform.position;
        var origin = HandPosition();
        var speed = Mathf.Max(1f, _strayShotSpeed);
        var travel = Vector2.Distance(origin, targetPoint) / speed;

        var knife = _knifePrefab != null
            ? Instantiate(_knifePrefab, origin, Quaternion.identity)
            : BossProjectile.CreateFallback("BossStrayKnife", BossKit.SquareSprite, new Color(0.85f, 0.85f, 0.95f, 1f),
                new Vector2(0.6f, 0.14f), 0.2f, true);
        knife.transform.position = origin;
        knife.LaunchAt(targetPoint, speed, travel + 1f, gameObject, BreakRetreatTarget);
        return travel;
    }

    private void BreakRetreatTarget()
    {
        if (_retreatBreakTarget == null)
            return;

        var broke = false;
        if (_retreatBreakTarget is BreakableSpeaker speaker)
        {
            broke = speaker.ForceBreak(_retreatBreakMessage);
        }
        else if (_retreatBreakTarget is IDamageable damageable)
        {
            damageable.TakeDamage(new DamageInfo(9999, 9999, _retreatBreakTarget.transform.position, gameObject));
            broke = true;
        }

        if (broke)
            CameraShake.Shake(_strayShotShake.x, _strayShotShake.y);
    }

    private void StartRetreatFlight()
    {
        // 勝ち扱い (BossArena が OnRetreated でフラグ記録・扉開放)。当たり判定はここで消える
        _onRetreatStarted?.Invoke();
        Enemy.Retreat(_dropThreadsOnRetreat);

        var away = Player != null && Player.position.x > transform.position.x ? -1f : 1f;
        _retreatVelocity = new Vector2(away * 0.6f, 1f).normalized * _retreatSpeed;
        _retreatTimer = _retreatFlyTime;
        _retreatFlying = true;
        Enemy.Face(away);
    }

    private void UpdateRetreatFlight(float dt)
    {
        transform.position += (Vector3)(_retreatVelocity * dt);
        _retreatVelocity *= 1f + dt; // だんだん速く
        _retreatTimer -= dt;
        if (_retreatTimer <= 0f)
            Destroy(gameObject);
    }

    #endregion

    /// <summary>形態遷移・撤退の演出に入る前に、ブレイク・攻撃・物理の状態を片付ける。</summary>
    private void CancelForCutscene()
    {
        Enemy.ClearStun();
        ClearTelegraphs();
        BossKit.DestroyMarker(_shadow);
        _shadow = null;
        Animation?.SetBool("Spinning", false);

        // 踏みつけで画面外にいた時は元の高さに戻す
        if (_rb.bodyType != RigidbodyType2D.Dynamic)
        {
            _rb.bodyType = RigidbodyType2D.Dynamic;
            var x = _rb.position.x;
            _rb.position = new Vector2(x, GroundY(x));
            transform.position = _rb.position;
            SetStance(Stance.Ground);
        }
        _rb.linearVelocity = Vector2.zero;
    }
}
