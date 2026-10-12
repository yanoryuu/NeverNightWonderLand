using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 中ボス「歯車の重馬」(A1_MB2 = BossArena A1_MidBossLower) のうごき (Docs/EnemySpec.md「下の中ボス」)。
/// EnemyController (HP/防御値/ブレイク/接触ダメージ/撃破) と同じ GameObject に付ける EnemyBehaviour。
///
/// 特徴 = 素早い動きで翻弄する。攻撃の間も立ち止まらず、速さ 6 で駆けて位置を取り直す
/// (プレイヤーから 4〜7 離れた所へ。ときどきプレイヤーの脇を駆け抜けて反対側へ回る)。
/// 攻撃の間は 0.6〜0.9 秒、2 回攻撃するごとに 1.0 秒その場で息を整える (反撃のチャンス)。
///
/// 攻撃 (距離と再使用待ちから重み付きで選ぶ。同じ技の連続は重みを下げる):
///   突進 / 往復突進 / 飛び越し後ろ蹴り / フェイント突進 (近い時だけ) / 竿立ち踏みつけ (衝撃波 = <see cref="HorseBossShockwave"/>)
/// 攻撃の判定は BossKit でプレイヤーとの重なりを見て与える (本体の接触ダメージより先に判定するので技ごとのダメージが入る)。
///
/// 地面の判定は通常の地上の敵と同じ (重力 + EnemyController.Move)。床の大歯車のこぶの上もそのまま駆ける。
/// 壁は前方への横向きのレイで探し、ほぼ垂直な面だけを壁とみなす (こぶの斜面・動く足場は壁にしない)。
///
/// 見た目は bigHorse (子 Visual, 野良ウマと同じ Animator: 駆ける = Speed は EnemyAnimator が速度から自動で入れる)。
/// 竿立ち・後ろ蹴り・ふらつきは Visual の傾き (LateUpdate で Animator の後に上書き) で、予告は実行時の光・範囲表示で出す。
/// </summary>
public class HorseBossBehaviour : EnemyBehaviour
{
    private enum Step
    {
        Idle,          // 駆けて位置を取り直す
        Breather,      // 2 回攻撃ごとに息を整える (止まる)
        Recover,       // 技の後の硬直・ふらつき
        ChargePaw,     // 突進の前かき (予告)
        Charging,      // 突進中 (往復突進も)
        ShuttleTurn,   // 往復突進の折り返し
        LeapCrouch,    // 飛び越しの踏み切り (予告)
        Leaping,       // 飛び越し中
        LeapLanding,   // 着地待ち (予定より床が低かった時の保険)
        KickWindup,    // 後ろ蹴りの溜め
        KickActive,    // 後ろ蹴りの判定
        FeintHop,      // フェイントの跳び退き
        FeintDash,     // フェイントの短い突っ込み
        RearUp,        // 竿立ち (予告)
    }

    private enum AttackId { Charge, Shuttle, Leap, Feint, Stomp, Count }

    #region Serialized Fields

    [Header("場所")]
    [Tooltip("闘技場の範囲 (BossArena のトリガー)。駆け回る範囲・戦闘開始の判定に使う。未設定なら初期位置 ± _arenaHalfWidth")]
    [SerializeField] private Collider2D _arenaArea;

    [Tooltip("_arenaArea 未設定時の、初期位置からの左右の範囲 (units)")]
    [SerializeField] private float _arenaHalfWidth = 12f;

    [Tooltip("突進が壁を探しに行ける、闘技場の範囲の外側への余裕 (units)。入口のゲートが範囲の外にあっても壁まで走る")]
    [SerializeField] private float _wallSearchSlack = 5f;

    [Tooltip("床・壁とみなすレイヤー。空なら Ground")]
    [SerializeField] private LayerMask _groundMask;

    [Tooltip("傾ける見た目 (竿立ち・後ろ蹴り・ふらつき)。未設定なら子 Visual")]
    [SerializeField] private Transform _poseTarget;

    [Header("移動・間合い")]
    [Tooltip("位置を取り直す時に駆ける速さ (units/sec)")]
    [SerializeField] private float _gallopSpeed = 6f;

    [Tooltip("位置取り: プレイヤーから取る距離の最小 (units)")]
    [SerializeField] private float _keepDistanceMin = 4f;

    [Tooltip("位置取り: プレイヤーから取る距離の最大 (units)")]
    [SerializeField] private float _keepDistanceMax = 7f;

    [Tooltip("位置取りで、プレイヤーの脇を駆け抜けて反対側へ回る確率")]
    [Range(0f, 1f)]
    [SerializeField] private float _crossOverChance = 0.35f;

    [Tooltip("攻撃と攻撃の間の最短 (sec)")]
    [SerializeField] private float _idleTimeMin = 0.6f;

    [Tooltip("攻撃と攻撃の間の最長 (sec)")]
    [SerializeField] private float _idleTimeMax = 0.9f;

    [Tooltip("この回数攻撃するごとに息を整える")]
    [SerializeField] private int _attacksPerBreather = 2;

    [Tooltip("息を整える時間 (sec, その場で止まる = 反撃のチャンス)")]
    [SerializeField] private float _breatherTime = 1.0f;

    [Tooltip("戦闘開始 (プレイヤーが闘技場に入った) から最初の攻撃までの待ち (sec)")]
    [SerializeField] private float _firstAttackDelay = 1.0f;

    [Tooltip("ブレイク・拘束が明けてから次の攻撃までの待ち (sec)")]
    [SerializeField] private float _idleAfterInterrupt = 0.6f;

    [Header("予告の色")]
    [Tooltip("予告の光の色 (前かき・踏み切り・竿立ち)")]
    [SerializeField] private Color _glowColor = new Color(1f, 0.95f, 0.5f, 0.85f);

    [Tooltip("予告の範囲表示の色 (後ろ蹴り)")]
    [SerializeField] private Color _dangerColor = new Color(1f, 0.2f, 0.2f, 0.3f);

    [Header("突進")]
    [SerializeField] private int _chargeDamage = 2;
    [Tooltip("前かき (点滅の予告) の時間 (sec)")]
    [SerializeField] private float _chargePawTime = 0.4f;
    [Tooltip("突進の速さ (units/sec)")]
    [SerializeField] private float _chargeSpeed = 16f;
    [Tooltip("壁に当たった後のふらつき (sec)")]
    [SerializeField] private float _chargeStaggerTime = 0.5f;
    [Tooltip("突進の最長時間 (sec, 壁が見つからない時の保険)")]
    [SerializeField] private float _chargeMaxTime = 4f;
    [Tooltip("前方の壁までこれより近ければ突進を選ばない (units)")]
    [SerializeField] private float _chargeMinRunway = 3f;
    [SerializeField] private float _chargeCooldown = 1.5f;

    [Header("往復突進")]
    [Tooltip("折り返しにかける時間 (sec)")]
    [SerializeField] private float _shuttleTurnTime = 0.15f;
    [Tooltip("走る回数 (最初の突進を含む)")]
    [SerializeField] private int _shuttleRuns = 3;
    [Tooltip("最後の壁当たりの後のふらつき (sec, 大きな隙)")]
    [SerializeField] private float _shuttleStaggerTime = 1.2f;
    [SerializeField] private float _shuttleCooldown = 7f;

    [Header("飛び越し後ろ蹴り")]
    [SerializeField] private int _kickDamage = 2;
    [Tooltip("踏み切りの予告 (sec)")]
    [SerializeField] private float _leapCrouchTime = 0.25f;
    [Tooltip("跳んでいる時間 (sec)")]
    [SerializeField] private float _leapTime = 0.7f;
    [Tooltip("跳躍の頂点の高さ (踏み切りの高さから, units)")]
    [SerializeField] private float _leapApexHeight = 4f;
    [Tooltip("プレイヤーの向こう側に着地する距離 (プレイヤーの中心から, units)")]
    [SerializeField] private float _leapLandBeyond = 2.5f;
    [Tooltip("プレイヤーとの距離がこの範囲 (体の端から, units) の時に選ぶ")]
    [SerializeField] private Vector2 _leapRange = new Vector2(0.5f, 8f);
    [Tooltip("着地から蹴るまで (sec)")]
    [SerializeField] private float _kickWindup = 0.25f;
    [Tooltip("蹴りの判定が出ている時間 (sec)")]
    [SerializeField] private float _kickActiveTime = 0.15f;
    [Tooltip("蹴りの範囲 (体の後ろの端から後ろへ, units)")]
    [SerializeField] private float _kickRange = 2.0f;
    [Tooltip("蹴りの後の硬直 (sec)")]
    [SerializeField] private float _kickRecover = 0.4f;
    [SerializeField] private float _leapCooldown = 3.5f;

    [Header("フェイント突進 (近い時)")]
    [SerializeField] private int _feintDamage = 2;
    [Tooltip("プレイヤーがこの距離 (体の端から, units) 以内の時に選ぶ")]
    [SerializeField] private float _feintTriggerRange = 3f;
    [Tooltip("後ろへ跳び退く距離 (units)")]
    [SerializeField] private float _feintHopDistance = 3f;
    [Tooltip("跳び退きの時間 (sec)")]
    [SerializeField] private float _feintHopTime = 0.3f;
    [Tooltip("突っ込む速さ (units/sec)")]
    [SerializeField] private float _feintDashSpeed = 18f;
    [Tooltip("突っ込む距離 (units)")]
    [SerializeField] private float _feintDashDistance = 8f;
    [Tooltip("突っ込んだ後の硬直 (sec)")]
    [SerializeField] private float _feintRecover = 0.35f;
    [SerializeField] private float _feintCooldown = 2.5f;

    [Header("竿立ち踏みつけ")]
    [SerializeField] private int _stompDamage = 1;
    [Tooltip("竿立ち (予告) の時間 (sec)")]
    [SerializeField] private float _rearTime = 0.5f;
    [Tooltip("竿立ちの傾き (度)")]
    [SerializeField] private float _rearAngle = 28f;
    [Tooltip("衝撃波の速さ (units/sec)")]
    [SerializeField] private float _shockwaveSpeed = 10f;
    [Tooltip("衝撃波の大きさ (幅, 高さ)。高さ 0.8 = 跳んでよけられる")]
    [SerializeField] private Vector2 _shockwaveSize = new Vector2(0.6f, 0.8f);
    [Tooltip("衝撃波の色")]
    [SerializeField] private Color _shockwaveColor = new Color(1f, 0.75f, 0.35f, 0.85f);
    [Tooltip("踏みつけの後の硬直 (sec)")]
    [SerializeField] private float _stompRecover = 0.6f;
    [Tooltip("踏みつけの画面揺れ (強さ, sec)")]
    [SerializeField] private Vector2 _stompShake = new Vector2(0.2f, 0.25f);
    [SerializeField] private float _stompCooldown = 4f;

    [Header("見た目")]
    [Tooltip("後ろ蹴りの傾き (度, 前が下がり後ろ足が上がる)")]
    [SerializeField] private float _kickAngle = 18f;
    [Tooltip("ふらつきの揺れ幅 (度)")]
    [SerializeField] private float _staggerWobble = 7f;
    [Tooltip("壁に当たった時の画面揺れ (強さ, sec)")]
    [SerializeField] private Vector2 _wallShake = new Vector2(0.15f, 0.2f);

    #endregion

    #region Fields

    private Rigidbody2D _rb;
    private Collider2D _col;
    private LayerMask _ground;
    private float _gravityScale;

    private Step _step = Step.Idle;
    private float _timer;          // 今の Step の残り時間
    private float _stepTime;       // 今の Step に入ってからの時間
    private float _idleTimer;      // Idle から次の攻撃まで
    private readonly float[] _cooldowns = new float[(int)AttackId.Count];
    private AttackId _lastAttack = AttackId.Count;
    private int _attacksSinceBreather;
    private bool _engaged;

    private float _arenaMinX;
    private float _arenaMaxX;
    private float _halfWidth;      // 体の半分の幅
    private float _facing = -1f;   // 攻撃の向き (+1 右 / -1 左)

    private float _repositionX;    // 駆けて向かう位置
    private bool _hasRepositionTarget;

    // 突進・往復突進
    private bool _shuttle;
    private int _runsLeft;
    private float _pawShuffle;

    // 飛び越し
    private Vector2 _leapFrom;
    private Vector2 _leapTo;
    private float _leapDir;

    // フェイント
    private float _dashStartX;

    // 見た目の傾き
    private float _poseAngle;
    private bool _poseApplied;
    private float _staggerTime;    // ふらつき中ならその長さ (0 = ふらついていない)

    private readonly List<SpriteRenderer> _telegraphs = new();
    private readonly List<(AttackId id, float weight)> _candidates = new();

    #endregion

    #region Unity Callbacks

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _ground = BossKit.GroundMaskOrDefault(_groundMask);
        if (_poseTarget == null)
            _poseTarget = transform.Find("Visual");
    }

    private void Start()
    {
        if (Enemy == null || Enemy.Consts == null || _col == null)
        {
            enabled = false;
            return;
        }

        Enemy.OnDied += HandleDied;
        _gravityScale = _rb.gravityScale > 0f ? _rb.gravityScale : 3f;
        _halfWidth = _col.bounds.extents.x;

        if (_arenaArea != null)
        {
            var a = _arenaArea.bounds;
            _arenaMinX = a.min.x;
            _arenaMaxX = a.max.x;
        }
        else
        {
            _arenaMinX = HomePosition.x - _arenaHalfWidth;
            _arenaMaxX = HomePosition.x + _arenaHalfWidth;
        }

        IgnoreOneWayPlatforms();

        _step = Step.Idle;
        _idleTimer = _firstAttackDelay;
    }

    /// <summary>
    /// すり抜け床 (配管・鉄骨) には乗らない: プレイヤーの逃げ場なので、跳び越しの着地や駆け足で上に乗らないようにする
    /// (2026-10-12 プレイ確認で配管の上で蹴り・息継ぎをしていたため)。床の大歯車の踏み板 (剛体付き) はそのまま。
    /// </summary>
    private void IgnoreOneWayPlatforms()
    {
        foreach (var effector in FindObjectsByType<PlatformEffector2D>(FindObjectsSortMode.None))
        {
            if (!effector.useOneWay)
                continue;
            foreach (var c in effector.GetComponents<Collider2D>())
            {
                if (c.usedByEffector && c.attachedRigidbody == null)
                    Physics2D.IgnoreCollision(_col, c, true);
            }
        }
    }

    /// <summary>すり抜け床 (PlatformEffector2D の当たり。剛体付きの動く足場は除く) か。</summary>
    private static bool IsOneWay(Collider2D c) => c.usedByEffector && c.attachedRigidbody == null;

    private void LateUpdate()
    {
        // 見た目の傾き: Animator (弾み・駆け足が Visual の位置と回転を書く) の後に上書きする
        if (_poseTarget == null || Enemy == null || Enemy.IsDead)
            return;

        var angle = _poseAngle;
        if (_staggerTime > 0f && _step == Step.Recover)
            angle += Mathf.Sin(_stepTime * 22f) * _staggerWobble * Mathf.Clamp01(_timer / Mathf.Max(0.01f, _staggerTime));

        if (Mathf.Abs(angle) > 0.01f)
        {
            _poseTarget.localRotation = Quaternion.Euler(0f, 0f, angle);
            _poseApplied = true;
        }
        else if (_poseApplied)
        {
            _poseTarget.localRotation = Quaternion.identity;
            _poseApplied = false;
        }
    }

    private void OnDestroy()
    {
        if (Enemy != null)
            Enemy.OnDied -= HandleDied;
        ClearTelegraphs();
    }

    private void HandleDied(EnemyController _)
    {
        ClearTelegraphs();
        BossHazard.ClearAll();
        _poseAngle = 0f;
        _staggerTime = 0f;
        _rb.gravityScale = _gravityScale;
    }

    #endregion

    #region EnemyBehaviour

    public override void PhysicsTick()
    {
        var dt = Time.fixedDeltaTime;
        for (var i = 0; i < _cooldowns.Length; i++)
            _cooldowns[i] -= dt;
        _timer -= dt;
        _stepTime += dt;

        switch (_step)
        {
            case Step.Idle: TickIdle(dt); break;

            case Step.Breather:
                Hold();
                FacePlayer();
                if (_timer <= 0f)
                    EnterIdle();
                break;

            case Step.Recover:
                Hold();
                if (_timer <= 0f)
                {
                    _staggerTime = 0f;
                    EnterIdle();
                }
                break;

            case Step.ChargePaw: TickChargePaw(); break;
            case Step.Charging: TickCharging(); break;

            case Step.ShuttleTurn:
                Hold();
                if (_timer <= 0f)
                    BeginRun();
                break;

            case Step.LeapCrouch:
                Hold();
                if (_timer <= 0f)
                    BeginLeap();
                break;

            case Step.Leaping: TickLeaping(dt); break;

            case Step.LeapLanding:
                Enemy.Move(0f);
                if (IsGrounded() || _timer <= 0f)
                    BeginKickWindup();
                break;

            case Step.KickWindup:
                Hold();
                Enemy.Face(_facing);
                _poseAngle = -_facing * _kickAngle * 0.35f * Mathf.Clamp01(_stepTime / Mathf.Max(0.01f, _kickWindup));
                if (_timer <= 0f)
                    BeginKickActive();
                break;

            case Step.KickActive:
                Hold();
                Enemy.Face(_facing);
                BossKit.HitPlayerBox(KickBoxCenter(), KickBoxSize(), _kickDamage, gameObject);
                if (_timer <= 0f)
                {
                    ClearTelegraphs();
                    _poseAngle = 0f;
                    EnterRecover(_kickRecover);
                }
                break;

            case Step.FeintHop: TickFeintHop(); break;
            case Step.FeintDash: TickFeintDash(); break;

            case Step.RearUp:
                Hold();
                Enemy.Face(_facing);
                _poseAngle = _facing * _rearAngle * Mathf.Clamp01(_stepTime / Mathf.Max(0.01f, _rearTime * 0.6f));
                if (_timer <= 0f)
                    Stomp();
                break;
        }
    }

    public override void OnInterrupted()
    {
        // ブレイク・拘束: 予告と傾きを消し、跳躍中でも重力を戻して地面に落とす (裁断のチャンス)
        ClearTelegraphs();
        _poseAngle = 0f;
        _staggerTime = 0f;
        _rb.gravityScale = _gravityScale;
        _rb.linearVelocity = new Vector2(0f, Mathf.Min(0f, _rb.linearVelocity.y));
        _hasRepositionTarget = false;
        SetStep(Step.Idle, 0f);
        _idleTimer = _idleAfterInterrupt;
    }

    #endregion

    #region Idle / 行動選択

    private void SetStep(Step step, float time)
    {
        _step = step;
        _timer = time;
        _stepTime = 0f;
    }

    private void EnterIdle()
    {
        _poseAngle = 0f;
        if (_attacksSinceBreather >= Mathf.Max(1, _attacksPerBreather))
        {
            // 2 回攻撃したら、その場で息を整える (反撃のチャンス)
            _attacksSinceBreather = 0;
            SetStep(Step.Breather, _breatherTime);
            return;
        }

        SetStep(Step.Idle, 0f);
        _idleTimer = Random.Range(_idleTimeMin, _idleTimeMax);
        _hasRepositionTarget = false;
    }

    private void EnterRecover(float time, bool stagger = false)
    {
        SetStep(Step.Recover, time);
        _staggerTime = stagger ? time : 0f;
    }

    private void TickIdle(float dt)
    {
        // プレイヤーが闘技場に入るまでは向きだけ合わせて待つ
        if (!PlayerInArena())
        {
            Hold();
            FacePlayer();
            _idleTimer = Mathf.Max(_idleTimer, _firstAttackDelay);
            return;
        }

        Gallop();

        _idleTimer -= dt;
        if (_idleTimer <= 0f)
            ChooseNext();
    }

    /// <summary>
    /// プレイヤーが一度でも闘技場 (_arenaArea) に入ったか (入った後は外れても戦い続ける)。未設定なら常に true。
    /// </summary>
    private bool PlayerInArena()
    {
        if (_engaged || _arenaArea == null)
            return true;
        if (Player == null)
            return false;
        var b = _arenaArea.bounds;
        var p = Player.position;
        _engaged = p.x >= b.min.x && p.x <= b.max.x && p.y >= b.min.y - 1f && p.y <= b.max.y + 1f;
        return _engaged;
    }

    /// <summary>攻撃の合間: 立ち止まらずに速さ 6 で駆けて、プレイヤーとの間合いを取り直す。</summary>
    private void Gallop()
    {
        var x = transform.position.x;
        if (!_hasRepositionTarget || Mathf.Abs(_repositionX - x) < 0.3f)
            PickRepositionTarget(_hasRepositionTarget);

        var dx = _repositionX - x;
        var dir = Mathf.Sign(dx);
        // 壁に詰まったら次の位置へ
        if (Enemy.HitWallLastStep)
            PickRepositionTarget(true);
        Enemy.Move(dir * _gallopSpeed);
    }

    /// <summary>
    /// 駆けて向かう位置を決める。プレイヤーから _keepDistanceMin〜Max 離れた所 (基本は今いる側、ときどき反対側へ駆け抜ける)。
    /// reached = 前の目標に着いた (着いたら同じ側で少し離れた所へ、止まらないように)。
    /// </summary>
    private void PickRepositionTarget(bool reached)
    {
        var x = transform.position.x;
        var px = Player != null ? Player.position.x : HomePosition.x;
        var side = x >= px ? 1f : -1f;
        if (!reached && Random.value < _crossOverChance)
            side = -side;

        var dist = Random.Range(_keepDistanceMin, _keepDistanceMax);
        var target = ClampMove(px + side * dist);
        // 壁ぎわで十分離れられない側なら反対側へ
        if (Mathf.Abs(target - px) < _keepDistanceMin * 0.6f)
            target = ClampMove(px - side * dist);
        // 目標が今の位置に近すぎる (止まって見える) なら少しずらす
        if (Mathf.Abs(target - x) < 1.5f)
            target = ClampMove(x + (x >= (_arenaMinX + _arenaMaxX) * 0.5f ? -1f : 1f) * Random.Range(2f, 3f));

        _repositionX = target;
        _hasRepositionTarget = true;
    }

    /// <summary>駆け回る範囲 (闘技場の範囲から体の半分 + 少し内側)。</summary>
    private float ClampMove(float x)
    {
        var margin = _halfWidth + 0.5f;
        return Mathf.Clamp(x, _arenaMinX + margin, Mathf.Max(_arenaMinX + margin, _arenaMaxX - margin));
    }

    /// <summary>距離・再使用待ち・前の技から次の攻撃を重み付きで選ぶ。</summary>
    private void ChooseNext()
    {
        if (Player == null)
        {
            _idleTimer = 0.5f;
            return;
        }

        var x = transform.position.x;
        var px = Player.position.x;
        var gap = Mathf.Max(0f, Mathf.Abs(px - x) - _halfWidth);   // 体の端からプレイヤーまで
        var toward = px >= x ? 1f : -1f;

        _candidates.Clear();
        if (gap <= _feintTriggerRange && Ready(AttackId.Feint))
            AddCandidate(AttackId.Feint, 3f);
        if (Ready(AttackId.Charge) && RunwayAhead(toward) >= _chargeMinRunway)
            AddCandidate(AttackId.Charge, gap > _feintTriggerRange ? 3f : 1f);
        if (Ready(AttackId.Shuttle) && RunwayAhead(toward) >= _chargeMinRunway && gap > _feintTriggerRange)
            AddCandidate(AttackId.Shuttle, 2f);
        if (Ready(AttackId.Leap) && gap >= _leapRange.x && gap <= _leapRange.y && CanLeapOver(px, toward))
            AddCandidate(AttackId.Leap, 2.5f);
        if (Ready(AttackId.Stomp))
            AddCandidate(AttackId.Stomp, gap <= _feintTriggerRange ? 2f : 1.5f);

        if (_candidates.Count == 0)
        {
            // 何も使えない (壁ぎわで助走が取れない等) → 駆け続けて少し後に選び直す
            _idleTimer = 0.25f;
            PickRepositionTarget(false);
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

        _lastAttack = chosen;
        _attacksSinceBreather++;
        _facing = toward;
        Enemy.Face(_facing);
        switch (chosen)
        {
            case AttackId.Charge: BeginCharge(false); break;
            case AttackId.Shuttle: BeginCharge(true); break;
            case AttackId.Leap: BeginLeapCrouch(); break;
            case AttackId.Feint: BeginFeint(); break;
            default: BeginRearUp(); break;
        }
    }

    /// <summary>候補に足す。直前と同じ技は重みを下げる (同じ技ばかりにならないように)。</summary>
    private void AddCandidate(AttackId id, float weight) =>
        _candidates.Add((id, id == _lastAttack ? weight * 0.3f : weight));

    private bool Ready(AttackId id) => _cooldowns[(int)id] <= 0f;

    private void StartCooldown(AttackId id, float time) => _cooldowns[(int)id] = time;

    #endregion

    #region 移動・判定の小道具

    /// <summary>その場に留まる (水平だけ止めて重力に任せる)。</summary>
    private void Hold() => Enemy.Move(0f);

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
        if (_rb.linearVelocity.y > 0.05f)
            return false;
        foreach (var hit in Physics2D.RaycastAll(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 0.2f, _ground))
        {
            if (hit.collider != null && !hit.collider.isTrigger && !IsOneWay(hit.collider))
                return true;
        }
        return false;
    }

    /// <summary>
    /// x の床の高さ (体の少し上から真下へ。見つからなければ今の足元)。
    /// すり抜け床 (配管・鉄骨の PlatformEffector2D) は床にしない (跳び越しの着地点を配管の上にしない)。
    /// </summary>
    private float FloorYAt(float x)
    {
        var b = _col.bounds;
        var hits = Physics2D.RaycastAll(new Vector2(x, b.max.y + 3f), Vector2.down, b.size.y + 12f, _ground);
        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger || hit.collider.usedByEffector)
                continue;
            return hit.point.y;
        }
        return b.min.y;
    }

    /// <summary>
    /// 前方 (dir) の壁が distance 以内にあるか。ほぼ垂直な面 (法線が横向き) だけを壁とし、
    /// 床の大歯車のこぶの斜面や動く足場 (剛体付き) は壁にしない。
    /// </summary>
    private bool WallAhead(float dir, float distance)
    {
        return WallDistance(dir, _halfWidth + distance) >= 0f;
    }

    /// <summary>
    /// 体の中心から dir へ maxDistance 以内の壁までの距離 (体の中心から)。無ければ -1。
    /// 壁 = ほぼ垂直な面。すり抜け床・トリガー・動く足場 (剛体付き = 床の大歯車の踏み板) は除く。
    /// </summary>
    private float WallDistance(float dir, float maxDistance)
    {
        var hits = Physics2D.RaycastAll(_col.bounds.center, new Vector2(dir, 0f), maxDistance, _ground);
        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider.isTrigger || hit.collider.usedByEffector)
                continue;
            if (hit.collider.attachedRigidbody != null && hit.collider.attachedRigidbody.bodyType != RigidbodyType2D.Static)
                continue;
            if (Mathf.Abs(hit.normal.x) >= 0.8f)
                return hit.distance;
        }
        return -1f;
    }

    /// <summary>前方 (dir) にどれだけ走れるか (闘技場の範囲 + 余裕 まで。途中の壁 = 端の段差・ゲートはレイで確かめる)。</summary>
    private float RunwayAhead(float dir)
    {
        var x = transform.position.x;
        var limit = dir > 0f ? _arenaMaxX + _wallSearchSlack : _arenaMinX - _wallSearchSlack;
        var runway = Mathf.Max(0f, Mathf.Abs(limit - x) - _halfWidth);
        var wall = WallDistance(dir, _halfWidth + runway);
        return wall >= 0f ? Mathf.Max(0f, wall - _halfWidth) : runway;
    }

    private void ClearTelegraphs()
    {
        foreach (var t in _telegraphs)
            BossKit.DestroyMarker(t);
        _telegraphs.Clear();
    }

    /// <summary>脈打つ光の予告。</summary>
    private SpriteRenderer AddGlow(Vector2 position, float size)
    {
        var sr = BossKit.CreateMarker("HorseGlow", BossKit.CircleSprite, _glowColor, position, new Vector2(size, size));
        sr.sortingOrder = BossKit.MarkerSortingOrder + 5;
        sr.transform.DOScale(new Vector3(size * 1.35f, size * 1.35f, 1f), 0.1f).SetLoops(-1, LoopType.Yoyo);
        _telegraphs.Add(sr);
        return sr;
    }

    /// <summary>前足 (向いている側の足元) の位置。</summary>
    private Vector2 FrontHoof()
    {
        var b = _col.bounds;
        return new Vector2(b.center.x + _facing * _halfWidth * 0.7f, b.min.y + 0.2f);
    }

    /// <summary>突進中の体当たり (技のダメージ)。</summary>
    private void HitWithBody(int damage)
    {
        var b = _col.bounds;
        BossKit.HitPlayerBox(b.center, b.size, damage, gameObject);
    }

    #endregion

    #region 突進・往復突進

    private void BeginCharge(bool shuttle)
    {
        _shuttle = shuttle;
        _runsLeft = shuttle ? Mathf.Max(1, _shuttleRuns) : 1;
        StartCooldown(AttackId.Charge, _chargeCooldown);
        if (shuttle)
            StartCooldown(AttackId.Shuttle, _shuttleCooldown);
        SetStep(Step.ChargePaw, _chargePawTime);
        _pawShuffle = 0f;
        AddGlow(FrontHoof(), shuttle ? 1.3f : 1f);
    }

    private void TickChargePaw()
    {
        // 前かき: その場で小刻みに足踏みしながら光る (向きは保つ)
        _pawShuffle += Time.fixedDeltaTime;
        Enemy.Move(Mathf.Repeat(_pawShuffle * 10f, 1f) < 0.5f ? 0.8f : -0.8f);
        Enemy.Face(_facing);
        foreach (var t in _telegraphs)
            if (t != null) t.transform.position = FrontHoof();

        if (_timer <= 0f)
        {
            ClearTelegraphs();
            BeginRun();
        }
    }

    private void BeginRun()
    {
        _runsLeft--;
        SetStep(Step.Charging, _chargeMaxTime);
        Enemy.Face(_facing);
    }

    private void TickCharging()
    {
        Enemy.Move(_facing * _chargeSpeed);
        HitWithBody(_chargeDamage);

        var x = transform.position.x;
        var pastLimit = _facing > 0f ? x >= _arenaMaxX + _wallSearchSlack - _halfWidth : x <= _arenaMinX - _wallSearchSlack + _halfWidth;
        // 壁 (前方のレイ) / 動けなくなった (段差に詰まった) / 範囲の外 / 時間切れ
        var hitWall = WallAhead(_facing, _chargeSpeed * Time.fixedDeltaTime + 0.1f)
                      || (_stepTime > 0.15f && Enemy.HitWallLastStep)
                      || pastLimit || _timer <= 0f;
        if (!hitWall)
            return;

        Enemy.Move(0f);
        CameraShake.Shake(_wallShake.x, _wallShake.y);
        if (_runsLeft > 0)
        {
            // 往復突進: 0.15 秒で折り返して走り直す
            _facing = -_facing;
            SetStep(Step.ShuttleTurn, _shuttleTurnTime);
            Enemy.Face(_facing);
            return;
        }

        EnterRecover(_shuttle ? _shuttleStaggerTime : _chargeStaggerTime, true);
    }

    #endregion

    #region 飛び越し後ろ蹴り

    /// <summary>プレイヤー (px) の向こう側 (dir) に着地できるか。</summary>
    private bool CanLeapOver(float px, float dir)
    {
        var land = ClampMove(px + dir * _leapLandBeyond);
        return (land - px) * dir >= _halfWidth + 0.6f;
    }

    private void BeginLeapCrouch()
    {
        StartCooldown(AttackId.Leap, _leapCooldown);
        SetStep(Step.LeapCrouch, _leapCrouchTime);
        _poseAngle = 0f;
        AddGlow((Vector2)_col.bounds.center + new Vector2(0f, -_col.bounds.extents.y * 0.6f), 1.1f);
    }

    private void BeginLeap()
    {
        ClearTelegraphs();
        // 踏み切りの瞬間のプレイヤーの位置で着地点を決める
        var px = Player != null ? Player.position.x : transform.position.x + _facing * 3f;
        _leapDir = px >= transform.position.x ? 1f : -1f;
        _facing = _leapDir;
        var landX = ClampMove(px + _leapDir * _leapLandBeyond);
        var bottomOffset = transform.position.y - _col.bounds.min.y;
        _leapFrom = _rb.position;
        _leapTo = new Vector2(landX, FloorYAt(landX) + bottomOffset);
        _rb.gravityScale = 0f;
        SetStep(Step.Leaping, _leapTime);
        Enemy.Face(_facing);
    }

    private void TickLeaping(float dt)
    {
        var s = Mathf.Clamp01(_stepTime / Mathf.Max(0.01f, _leapTime));
        var next = LeapPoint(Mathf.Clamp01((_stepTime + dt) / Mathf.Max(0.01f, _leapTime)));
        _rb.linearVelocity = (next - _rb.position) / dt;
        Enemy.Face(_facing);
        // 跳んでいる間は体を少し前へ傾ける (上り) → 戻す (下り)
        _poseAngle = _facing * 12f * Mathf.Cos(s * Mathf.PI);

        if (s < 1f)
            return;

        _rb.gravityScale = _gravityScale;
        _rb.linearVelocity = new Vector2(0f, Mathf.Min(0f, _rb.linearVelocity.y));
        _poseAngle = 0f;
        SetStep(Step.LeapLanding, 0.6f);
    }

    /// <summary>跳躍の軌道 (s = 0..1)。頂点は踏み切りから _leapApexHeight だけ上。</summary>
    private Vector2 LeapPoint(float s)
    {
        var x = Mathf.Lerp(_leapFrom.x, _leapTo.x, s);
        var baseY = Mathf.Lerp(_leapFrom.y, _leapTo.y, s);
        var arc = _leapApexHeight + Mathf.Max(0f, _leapTo.y - _leapFrom.y) * 0.5f;
        return new Vector2(x, baseY + 4f * arc * s * (1f - s));
    }

    private void BeginKickWindup()
    {
        // 着地: プレイヤーは背中側。前 (進んだ向き) を向いたまま後ろ足で蹴る
        _facing = _leapDir;
        Enemy.Face(_facing);
        SetStep(Step.KickWindup, _kickWindup);
        var sr = BossKit.CreateMarker("HorseKickZone", BossKit.SquareSprite, _dangerColor, KickBoxCenter(), KickBoxSize());
        _telegraphs.Add(sr);
    }

    private void BeginKickActive()
    {
        SetStep(Step.KickActive, _kickActiveTime);
        _poseAngle = -_facing * _kickAngle;
        foreach (var t in _telegraphs)
            if (t != null) t.color = new Color(_dangerColor.r, _dangerColor.g, _dangerColor.b, Mathf.Min(1f, _dangerColor.a * 2.5f));
    }

    private Vector2 KickBoxCenter()
    {
        var b = _col.bounds;
        return new Vector2(b.center.x - _facing * (_halfWidth + _kickRange * 0.5f), b.center.y);
    }

    private Vector2 KickBoxSize() => new Vector2(_kickRange, _col.bounds.size.y);

    #endregion

    #region フェイント突進

    private void BeginFeint()
    {
        StartCooldown(AttackId.Feint, _feintCooldown);
        SetStep(Step.FeintHop, _feintHopTime);
        // 後ろへ小さく跳ぶ (_feintHopTime で着地する初速)
        var g = Mathf.Abs(Physics2D.gravity.y) * _gravityScale;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, g * _feintHopTime * 0.5f);
    }

    private void TickFeintHop()
    {
        // 跳び退く間もプレイヤーの方を向いたまま (Move は進む向きへ振り向くので、向きを戻す)
        var back = -_facing;
        var blocked = WallAhead(back, 0.2f);
        Enemy.Move(blocked ? 0f : back * _feintHopDistance / Mathf.Max(0.01f, _feintHopTime));
        Enemy.Face(_facing);
        if (_timer > 0f)
            return;

        // すぐに短く突っ込む
        FacePlayer();
        _dashStartX = transform.position.x;
        SetStep(Step.FeintDash, _feintDashDistance / Mathf.Max(0.01f, _feintDashSpeed) + 0.3f);
    }

    private void TickFeintDash()
    {
        Enemy.Move(_facing * _feintDashSpeed);
        HitWithBody(_feintDamage);

        var traveled = Mathf.Abs(transform.position.x - _dashStartX);
        var stop = traveled >= _feintDashDistance
                   || WallAhead(_facing, _feintDashSpeed * Time.fixedDeltaTime + 0.1f)
                   || (_stepTime > 0.1f && Enemy.HitWallLastStep)
                   || _timer <= 0f;
        if (!stop)
            return;

        Enemy.Move(0f);
        EnterRecover(_feintRecover);
    }

    #endregion

    #region 竿立ち踏みつけ

    private void BeginRearUp()
    {
        StartCooldown(AttackId.Stomp, _stompCooldown);
        SetStep(Step.RearUp, _rearTime);
        AddGlow(FrontHoof() + new Vector2(0f, _col.bounds.size.y * 0.6f), 1.1f);
    }

    private void Stomp()
    {
        ClearTelegraphs();
        _poseAngle = 0f;
        CameraShake.Shake(_stompShake.x, _stompShake.y);

        // 床を伝う衝撃波を左右へ 1 つずつ (足元から)
        var b = _col.bounds;
        var minX = _arenaMinX - _wallSearchSlack;
        var maxX = _arenaMaxX + _wallSearchSlack;
        foreach (var dir in new[] { -1f, 1f })
        {
            var x = b.center.x + dir * (_halfWidth * 0.8f);
            HorseBossShockwave.Create(gameObject, _stompDamage, x, FloorYAt(x), dir, _shockwaveSpeed,
                _shockwaveSize, minX, maxX, _ground, _shockwaveColor);
        }

        EnterRecover(_stompRecover);
    }

    #endregion
}
