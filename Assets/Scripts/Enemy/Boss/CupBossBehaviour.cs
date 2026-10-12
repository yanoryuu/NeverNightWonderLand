using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 中ボス「カップ」(A1_MB1, BossArena `A1_MidBossUpper`) の全体のうごき (Docs/EnemySpec.md「上の中ボス「カップ」」)。
/// ボスのルート (= 合体後のカップ, BossArena._boss) の EnemyController と同じ GameObject に付ける EnemyBehaviour。
///
/// 構成: ルート = 合体後のカップ (絵 cups, HP 100 / 防御値 15 / ブレイク 3 秒)。子 = 小さなカップ 3 体
/// (<see cref="CupMinionBehaviour"/>, それぞれ自分の EnemyController = HP 40 / 防御値 20)。
/// BossArena が撃破済みでルートを Destroy すると子も一緒に消える。撃破フラグはルートの OnDied だけで立つ
/// (小さなカップは HpFloor で決して死なない)。
///
/// 第1段階: ルートは眠っている (EnemyController 無効 = 行動・接触ダメージ・裁断の対象外、絵・当たり判定・HP 表示を隠す、物理停止)。
///   プレイヤーが闘技場に入ると (<see cref="Engaged"/>) 小さなカップが動き出す。ダウンしたカップは集合点へ飛んでいって待つ。
/// 合体: 3 体ともダウンして集合点に着いたら、寄り集まって積み重なり (_convergeTime)、
///   その場で _mergeHoldTime 待つ (無敵 + 軽い画面揺れ) → ルートを集合点へ移して起こす (合体後のカップが現れる)。
/// 第2段階: プレイヤーと距離 (約 5) を取って浮き、攻撃の間は 1.0 秒休む。攻撃 = 飛沫撒き / 斜め体当たり / 紅茶の雨 / 分離回転。
/// 撃破: 3 つのカップに割れて落ちながら消える (実行時の絵の破片。本体は EnemyController の縮小で消える)。
/// 予告 (光・床の印) は実行時の SpriteRenderer (BossKit) で出す。
/// </summary>
public class CupBossBehaviour : EnemyBehaviour
{
    private enum Phase { Minions, Converge, MergeHold, Merged }

    private enum Step
    {
        Idle,
        SplashWindup,
        DashWindup, Dash, DashDrift,
        RainRise, RainTilt, RainPour, RainEnd,
        SplitWindup, Split, Restack,
        Recover,
    }

    private enum AttackId { Splash, Dash, Rain, Split, Count }

    #region Serialized Fields

    [Header("部品")]
    [Tooltip("第1段階の小さなカップ 3 体 (子)。並び順 = 積み重ねの上から (黄 / ピンク / 水色)")]
    [SerializeField] private CupMinionBehaviour[] _minions = new CupMinionBehaviour[0];

    [Tooltip("合体後のカップの頭上の HP 表示 (StatusCanvas)。第1段階は隠す。未設定なら子の \"StatusCanvas\"")]
    [SerializeField] private GameObject _statusCanvas;

    [Header("場所")]
    [Tooltip("闘技場の範囲 (BossArena のトリガー)。戦闘開始の判定・移動範囲・紅茶の雨の列に使う。未設定なら初期位置 ± _arenaHalfSize")]
    [SerializeField] private Collider2D _arenaArea;

    [Tooltip("_arenaArea 未設定時の、初期位置からの範囲 (半分の幅・高さ, units)")]
    [SerializeField] private Vector2 _arenaHalfSize = new Vector2(12f, 8f);

    [Tooltip("床・壁とみなすレイヤー。空なら Ground")]
    [SerializeField] private LayerMask _groundMask;

    [Tooltip("ダウンしたカップが集まる点 (闘技場の中心からのずれ, units)")]
    [SerializeField] private Vector2 _gatherOffset = new Vector2(0f, 0.5f);

    [Tooltip("ダウンしたカップが待つ位置 (集合点から, カップの並び順に)")]
    [SerializeField] private Vector2[] _gatherSlots = { new Vector2(-1.8f, 0f), new Vector2(0f, 0.8f), new Vector2(1.8f, 0f) };

    [Header("合体")]
    [Tooltip("積み重なった時の各カップの位置 (ルートの中心から, 絵 cups の並びに合わせる。左向きの時)")]
    [SerializeField] private Vector2[] _stackOffsets = { new Vector2(0.02f, 0.68f), new Vector2(0.16f, -0.12f), new Vector2(0.28f, -0.78f) };

    [Tooltip("積み重なった時の小さなカップの大きさ (倍率)")]
    [SerializeField] private float _stackScale = 0.8f;

    [Tooltip("寄り集まって積み重なるまで (sec)")]
    [SerializeField] private float _convergeTime = 0.6f;

    [Tooltip("積み重なってから合体後のカップが現れるまで (sec, 無敵)")]
    [SerializeField] private float _mergeHoldTime = 1f;

    [Tooltip("合体の画面揺れ (強さ, sec)")]
    [SerializeField] private Vector2 _mergeShake = new Vector2(0.12f, 0.6f);

    [Tooltip("現れてから最初の攻撃までの追加の待ち (sec)")]
    [SerializeField] private float _appearDelay = 0.5f;

    [Header("第2段階の移動")]
    [Tooltip("浮遊の移動の速さ (units/sec)")]
    [SerializeField] private float _moveSpeed = 4f;

    [Tooltip("プレイヤーと取る距離 (units)")]
    [SerializeField] private float _keepDistance = 5f;

    [Tooltip("プレイヤーより最低これだけ上に浮く (units)")]
    [SerializeField] private float _keepHeight = 2f;

    [Tooltip("上下の揺れ幅 (units)")]
    [SerializeField] private float _bobAmplitude = 0.25f;

    [Tooltip("上下の揺れの速さ (rad/sec)")]
    [SerializeField] private float _bobSpeed = 2f;

    [Tooltip("攻撃と攻撃の間の休み (sec, 反撃のチャンス)")]
    [SerializeField] private float _restTime = 1f;

    [Tooltip("ブレイク・拘束が明けてから次の行動までの待ち (sec)")]
    [SerializeField] private float _idleAfterInterrupt = 0.6f;

    [Tooltip("予告の光の色")]
    [SerializeField] private Color _glowColor = new Color(1f, 0.92f, 0.6f, 0.8f);

    [Tooltip("床の予告の印の色 (紅茶の雨)")]
    [SerializeField] private Color _dangerColor = new Color(1f, 0.25f, 0.2f, 0.45f);

    [Tooltip("紅茶の滴の色")]
    [SerializeField] private Color _teaColor = new Color(0.5f, 0.2f, 0.13f, 1f);

    [Header("飛沫撒き")]
    [SerializeField] private int _splashDamage = 1;
    [Tooltip("光って予告する時間 (sec)")]
    [SerializeField] private float _splashWindup = 0.5f;
    [Tooltip("狙い = プレイヤーの足元と左右にこの距離 (units)")]
    [SerializeField] private float _splashSpread = 2.5f;
    [Tooltip("飛沫の重力 (units/sec²)")]
    [SerializeField] private float _splashGravity = 18f;
    [Tooltip("滞空時間の最小・最大 (sec)。遠いほど長い")]
    [SerializeField] private Vector2 _splashFlightTime = new Vector2(0.6f, 1.1f);
    [Tooltip("飛沫の半径 (units)")]
    [SerializeField] private float _splashRadius = 0.22f;
    [Tooltip("撒いた後の硬直 (sec)")]
    [SerializeField] private float _splashRecover = 0.3f;

    [Header("斜め体当たり")]
    [SerializeField] private int _dashDamage = 2;
    [Tooltip("後ろへ下がって予告する時間 (sec)")]
    [SerializeField] private float _dashWindup = 0.5f;
    [Tooltip("予告中に後ろへ下がる速さ (units/sec)")]
    [SerializeField] private float _dashBackOffSpeed = 2.5f;
    [SerializeField] private float _dashSpeed = 13f;
    [Tooltip("突進の最大時間 (sec)")]
    [SerializeField] private float _dashMaxTime = 0.7f;
    [Tooltip("突進後に減速して漂う時間 (sec)")]
    [SerializeField] private float _dashDriftTime = 0.8f;

    [Header("紅茶の雨")]
    [SerializeField] private int _rainDamage = 1;
    [Tooltip("再使用までの時間 (sec)")]
    [SerializeField] private float _rainCooldown = 8f;
    [Tooltip("上がる高さ (闘技場の天井からの距離, units)")]
    [SerializeField] private float _rainHoverBelowTop = 2.5f;
    [Tooltip("上がる速さ (units/sec)")]
    [SerializeField] private float _rainRiseSpeed = 8f;
    [Tooltip("上がるのを諦めるまで (sec, 足場に阻まれた時の保険)")]
    [SerializeField] private float _rainRiseMaxTime = 2f;
    [Tooltip("傾ける角度 (度)")]
    [SerializeField] private float _rainTiltAngle = 30f;
    [Tooltip("傾けるのにかける時間 (sec)")]
    [SerializeField] private float _rainTiltTime = 0.3f;
    [Tooltip("列の数 (闘技場の横幅を等分)。1 列は必ず落とさない")]
    [SerializeField] private int _rainColumns = 5;
    [Tooltip("列ごとの時間差 (sec)")]
    [SerializeField] private float _rainInterval = 0.25f;
    [Tooltip("床の予告から滴が出るまで (sec)")]
    [SerializeField] private float _rainTelegraph = 0.6f;
    [Tooltip("1 列の中の滴の間隔 (units)")]
    [SerializeField] private float _rainDropSpacing = 0.9f;
    [Tooltip("滴の初速 (下向き, units/sec)")]
    [SerializeField] private float _rainDropSpeed = 6f;
    [Tooltip("滴の重力 (units/sec²)")]
    [SerializeField] private float _rainGravity = 22f;
    [Tooltip("滴の半径 (units)")]
    [SerializeField] private float _rainDropRadius = 0.2f;
    [Tooltip("最後の滴を出してから傾きを戻すまで (sec)")]
    [SerializeField] private float _rainAfterTime = 0.5f;

    [Header("分離回転")]
    [SerializeField] private int _splitDamage = 1;
    [Tooltip("再使用までの時間 (sec)")]
    [SerializeField] private float _splitCooldown = 7f;
    [Tooltip("予告 (sec)")]
    [SerializeField] private float _splitWindup = 0.4f;
    [Tooltip("回っている時間 (sec)")]
    [SerializeField] private float _splitDuration = 3f;
    [Tooltip("回る半径 (units)")]
    [SerializeField] private float _orbitRadius = 2.2f;
    [Tooltip("1 周にかかる時間 (sec)")]
    [SerializeField] private float _orbitPeriod = 1.2f;
    [Tooltip("回りながらプレイヤーを追う速さ (units/sec)")]
    [SerializeField] private float _splitChaseSpeed = 1.8f;
    [Tooltip("回るカップの当たり判定の半径 (units)")]
    [SerializeField] private float _orbitHitRadius = 0.6f;
    [Tooltip("積み直すのにかける時間 (sec)")]
    [SerializeField] private float _restackTime = 0.3f;

    [Header("撃破")]
    [Tooltip("割れたカップが落ちながら消えるまで (sec)")]
    [SerializeField] private float _deathFallTime = 1.2f;
    [Tooltip("割れたカップが落ちる距離 (units)")]
    [SerializeField] private float _deathFallDistance = 5f;

    #endregion

    #region Fields

    private Rigidbody2D _rb;
    private Collider2D _col;
    private SpriteRenderer _sprite;
    private LayerMask _ground;

    private Phase _phase = Phase.Minions;
    private Step _step;
    private bool _engaged;
    private float _timer;
    private float _idleTimer;
    private float _time;
    private readonly float[] _cooldowns = new float[(int)AttackId.Count];
    private AttackId _lastAttack = AttackId.Count;

    // 合体の演出
    private Vector2 _gatherCenter;
    private bool _gatherFixed;
    private readonly List<Vector3> _poseFrom = new();  // x, y = 位置 / z = 傾き

    // 攻撃
    private SpriteRenderer _glow;
    private readonly List<SpriteRenderer> _telegraphs = new();
    private Vector2 _dashTarget;
    private Vector2 _dashDir;
    private int _rainNext;
    private int _rainSafe;
    private bool _rainFromLeft;
    private float _splitTime;
    private readonly List<AttackId> _candidates = new();

    #endregion

    #region Properties

    /// <summary>プレイヤーが闘技場に入って戦闘が始まったか (小さなカップはこれまで動かない)。</summary>
    public bool Engaged => _engaged;

    /// <summary>小さなカップ (並び順 = 積み重ねの上から)。</summary>
    public IReadOnlyList<CupMinionBehaviour> Minions => _minions;

    /// <summary>合体して第2段階に入ったか。</summary>
    public bool IsMerged => _phase == Phase.Merged;

    private Vector2 BodyCenter => _rb.position + (_col != null ? _col.offset : Vector2.zero);

    #endregion

    #region Unity Callbacks

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();
        _sprite = GetComponent<SpriteRenderer>();
        _ground = BossKit.GroundMaskOrDefault(_groundMask);
        if (_statusCanvas == null)
        {
            var canvas = transform.Find("StatusCanvas");
            if (canvas != null)
                _statusCanvas = canvas.gameObject;
        }

        // 第1段階: 合体後のカップは眠らせる (行動・接触ダメージ・被弾・裁断の対象外、見た目と HP 表示も隠す)
        SetMergedActive(false);
    }

    private void Start()
    {
        if (Enemy == null || Enemy.Consts == null)
        {
            enabled = false;
            return;
        }

        Enemy.OnDied += HandleDied;
        _gatherCenter = ArenaBounds().center + (Vector3)_gatherOffset;

        // 小さなカップがいなければ (配置ミス) 最初から合体後で戦う
        if (_minions.Length == 0)
            Appear();
    }

    private void OnDestroy()
    {
        if (Enemy != null)
            Enemy.OnDied -= HandleDied;
        transform.DOKill();
        ClearTelegraphs();
    }

    /// <summary>第1段階と合体の演出 (ルートの EnemyController が無効の間) をここで進める。</summary>
    private void FixedUpdate()
    {
        if (_phase == Phase.Merged || Enemy == null || Enemy.IsDead)
            return;

        var dt = Time.fixedDeltaTime;
        _timer -= dt;
        switch (_phase)
        {
            case Phase.Minions:
                if (!_engaged && PlayerInArena())
                    _engaged = true;
                if (AllGathered())
                    BeginConverge();
                break;

            case Phase.Converge:
                TickConverge();
                if (_timer <= 0f)
                    BeginMergeHold();
                break;

            case Phase.MergeHold:
                // 積み重なったまま点滅 (無敵。小さなカップは当たり判定なし、ルートはまだ眠っている)
                var blink = Mathf.Repeat(_timer * 10f, 1f) < 0.5f;
                foreach (var m in _minions)
                    if (m != null) m.SetVisible(true, blink ? Color.white : new Color(1f, 0.9f, 0.7f));
                if (_timer <= 0f)
                    Appear();
                break;
        }
    }

    #endregion

    #region 第1段階・合体

    /// <summary>ダウンしたカップが待つ位置 (ワールド座標)。</summary>
    public Vector2 GatherPoint(CupMinionBehaviour minion)
    {
        var center = _gatherCenter;
        if (_rb != null && center == Vector2.zero)
            center = ArenaBounds().center + (Vector3)_gatherOffset;
        var index = System.Array.IndexOf(_minions, minion);
        var slot = index >= 0 && index < _gatherSlots.Length ? _gatherSlots[index] : Vector2.zero;
        return center + slot;
    }

    /// <summary>小さなカップがダウンした (今は集合点に着くのを待つだけ)。</summary>
    public void NotifyDown(CupMinionBehaviour minion)
    {
        // 集合点は最初の 1 体がダウンした時に決める: プレイヤーの真上あたり、画面の上端から 3 下 (画面の中で合体が見えるように)
        if (!_gatherFixed)
        {
            _gatherFixed = true;
            var b = ArenaBounds();
            var px = Player != null ? Player.position.x : b.center.x;
            _gatherCenter = ClampToArena(new Vector2(px, b.max.y - 3f), 2.5f);
        }

        _engaged = true;
    }

    private bool AllGathered()
    {
        if (_minions.Length == 0)
            return false;
        foreach (var m in _minions)
        {
            if (m != null && (!m.IsDown || !m.IsGathered))
                return false;
        }
        return true;
    }

    private void BeginConverge()
    {
        _phase = Phase.Converge;
        _timer = _convergeTime;
        _poseFrom.Clear();
        foreach (var m in _minions)
        {
            if (m == null)
            {
                _poseFrom.Add(Vector3.zero);
                continue;
            }
            m.BecomePuppet();
            var p = m.transform.position;
            _poseFrom.Add(new Vector3(p.x, p.y, m.transform.eulerAngles.z));
            m.Sprite.flipX = false; // 積み重ねの絵 (cups) と同じ向き
        }
    }

    private void TickConverge()
    {
        var t = 1f - Mathf.Clamp01(_timer / Mathf.Max(0.01f, _convergeTime));
        var e = t * t * (3f - 2f * t);
        for (var i = 0; i < _minions.Length; i++)
        {
            var m = _minions[i];
            if (m == null)
                continue;
            var from = _poseFrom[i];
            var to = _gatherCenter + StackOffset(i, false);
            var angle = Mathf.LerpAngle(from.z, 0f, e);
            m.SetPose(Vector2.Lerp(from, to, e), angle, Mathf.Lerp(1f, _stackScale, e));
            m.SetVisible(true, Color.Lerp(m.Sprite.color, Color.white, e));
        }
    }

    private void BeginMergeHold()
    {
        _phase = Phase.MergeHold;
        _timer = _mergeHoldTime;
        CameraShake.Shake(_mergeShake.x, _mergeShake.y);
    }

    /// <summary>合体後のカップが現れる: ルートを集合点へ移して起こし、小さなカップは隠す。</summary>
    private void Appear()
    {
        // ルートを動かすと子 (小さなカップ) も動くので、ワールド位置を戻す
        var positions = new List<Vector3>();
        foreach (var m in _minions)
            positions.Add(m != null ? m.transform.position : Vector3.zero);
        if (_minions.Length > 0)
        {
            transform.position = new Vector3(_gatherCenter.x, _gatherCenter.y, transform.position.z);
            _rb.position = _gatherCenter;
        }
        for (var i = 0; i < _minions.Length; i++)
        {
            if (_minions[i] == null)
                continue;
            _minions[i].BecomePuppet();
            _minions[i].transform.position = positions[i];
            _minions[i].SetVisible(false);
        }

        _phase = Phase.Merged;
        _engaged = true;
        SetMergedActive(true);
        transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.35f, 6, 0.6f);
        _step = Step.Idle;
        _idleTimer = _appearDelay + _restTime;
    }

    /// <summary>合体後のカップ (ルート) を起こす / 眠らせる。</summary>
    private void SetMergedActive(bool active)
    {
        if (_sprite != null)
            _sprite.enabled = active;
        if (_col != null)
            _col.enabled = active;
        if (_statusCanvas != null)
            _statusCanvas.SetActive(active);
        if (_rb != null)
        {
            _rb.gravityScale = 0f;
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated = active;
        }
        if (Enemy != null)
            Enemy.enabled = active;
    }

    /// <summary>
    /// プレイヤーが闘技場 (_arenaArea) に入ったか (一度入ったら以後ずっと戦う)。_arenaArea 未設定なら常に true。
    /// ルートの EnemyController は第1段階で眠っていてプレイヤーを探さないので、BossKit からプレイヤーを取る。
    /// </summary>
    private bool PlayerInArena()
    {
        if (_arenaArea == null)
            return true;
        var p = BossKit.PlayerCenter();
        if (p == null)
            return false;
        var b = _arenaArea.bounds;
        return p.Value.x >= b.min.x && p.Value.x <= b.max.x && p.Value.y >= b.min.y - 1f && p.Value.y <= b.max.y + 1f;
    }

    #endregion

    #region 場所

    /// <summary>
    /// 動ける範囲 = 闘技場。ただし上端はカメラに映っている範囲の上端 (- 0.2) までにする
    /// (A1_MB1 の闘技場は縦 32 あり、上の方で合体・雨をすると画面の外になるため。2026-10-12 プレイ確認)。
    /// </summary>
    private Bounds ArenaBounds()
    {
        var b = _arenaArea != null
            ? _arenaArea.bounds
            : new Bounds(HomePosition, new Vector3(_arenaHalfSize.x * 2f, _arenaHalfSize.y * 2f, 1f));
        var cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            var viewTop = cam.transform.position.y + cam.orthographicSize - 0.2f;
            if (viewTop < b.max.y && viewTop - b.min.y >= 6f)
                b.SetMinMax(b.min, new Vector3(b.max.x, viewTop, b.max.z));
        }
        return b;
    }

    /// <summary>闘技場の内側 (端から margin) に収める。</summary>
    public Vector2 ClampToArena(Vector2 p, float margin)
    {
        var b = ArenaBounds();
        return new Vector2(
            Mathf.Clamp(p.x, b.min.x + margin, Mathf.Max(b.min.x + margin, b.max.x - margin)),
            Mathf.Clamp(p.y, b.min.y + margin, Mathf.Max(b.min.y + margin, b.max.y - margin)));
    }

    /// <summary>積み重ねの i 番目のカップの位置 (ルートの中心から)。右向き (絵の反転中) なら左右を反転する。</summary>
    private Vector2 StackOffset(int i, bool flipped)
    {
        var o = i < _stackOffsets.Length ? _stackOffsets[i] : Vector2.zero;
        return flipped ? new Vector2(-o.x, o.y) : o;
    }

    private Vector2 PlayerPoint() =>
        BossKit.PlayerCenter() ?? (Player != null ? (Vector2)Player.position : BodyCenter + Vector2.left);

    /// <summary>プレイヤーの足元の高さ (当たり判定の下端)。</summary>
    private float PlayerFeetY()
    {
        if (BossKit.TryGetPlayer(out _, out var col))
            return col.bounds.min.y;
        return PlayerPoint().y - 0.6f;
    }

    #endregion

    #region EnemyBehaviour (第2段階)

    public override void PhysicsTick()
    {
        if (_phase != Phase.Merged)
            return;

        var dt = Time.fixedDeltaTime;
        _time += dt;
        _timer -= dt;
        for (var i = 0; i < _cooldowns.Length; i++)
            _cooldowns[i] -= dt;
        if (_glow != null)
            _glow.transform.position = BodyCenter;

        switch (_step)
        {
            case Step.Idle:
                Hover();
                FacePlayer();
                _idleTimer -= dt;
                if (_idleTimer <= 0f)
                    ChooseNext();
                break;

            case Step.SplashWindup:
                Hold();
                if (_timer <= 0f)
                {
                    ClearTelegraphs();
                    Splash();
                    EnterRecover(_splashRecover);
                }
                break;

            case Step.DashWindup:
                // 予告: 狙いと反対へ少し下がる
                _rb.linearVelocity = -_dashDir * _dashBackOffSpeed;
                if (_timer <= 0f)
                    BeginDash();
                break;

            case Step.Dash:
                TickDash();
                break;

            case Step.DashDrift:
                _rb.linearVelocity *= Mathf.Clamp01(1f - 4f * dt);
                if (_timer <= 0f)
                    EnterIdle();
                break;

            case Step.RainRise:
                TickRainRise(dt);
                break;

            case Step.RainTilt:
                Hold();
                if (_timer <= 0f)
                    BeginRainPour();
                break;

            case Step.RainPour:
                Hold();
                TickRainPour();
                break;

            case Step.RainEnd:
                Hold();
                if (_timer <= 0f)
                    EnterIdle();
                break;

            case Step.SplitWindup:
                Hold();
                if (_timer <= 0f)
                    BeginSplit();
                break;

            case Step.Split:
                TickSplit(dt);
                break;

            case Step.Restack:
                TickRestack();
                break;

            case Step.Recover:
                Hold();
                if (_timer <= 0f)
                    EnterIdle();
                break;
        }
    }

    public override void OnInterrupted()
    {
        // ブレイク・拘束: 予告を消し、分離中なら積み直し、傾きを戻してその場に止まる (床の印は予告どおりに降る)
        ClearTelegraphs();
        EndSplitImmediate();
        ResetTilt(0f);
        if (_rb != null)
            _rb.linearVelocity = Vector2.zero;
        _step = Step.Idle;
        _idleTimer = _idleAfterInterrupt;
    }

    private void HandleDied(EnemyController _)
    {
        ClearTelegraphs();
        BossHazard.ClearAll();
        transform.DOKill(true);
        BreakApart();
    }

    #endregion

    #region 移動・行動選択

    /// <summary>プレイヤーから距離を取って浮く (上下にふわふわ)。</summary>
    private void Hover()
    {
        var player = PlayerPoint();
        var pos = _rb.position;
        var away = pos - player;
        if (away.sqrMagnitude < 0.01f)
            away = Vector2.up;
        var target = player + away.normalized * _keepDistance;
        target.y = Mathf.Max(target.y, player.y + _keepHeight);
        target = ClampToArena(target, 1.5f);
        var velocity = Vector2.ClampMagnitude((target - pos) * 2f, _moveSpeed);
        velocity.y += Mathf.Cos(_time * _bobSpeed) * _bobAmplitude * _bobSpeed;
        _rb.linearVelocity = velocity;
    }

    /// <summary>その場で止まって (少しだけ揺れて) 構える。</summary>
    private void Hold()
    {
        _rb.linearVelocity = new Vector2(0f, Mathf.Cos(_time * _bobSpeed) * _bobAmplitude * 0.5f * _bobSpeed);
    }

    private void FacePlayer()
    {
        var dx = PlayerPoint().x - _rb.position.x;
        if (Mathf.Abs(dx) > 0.2f)
            Enemy.Face(Mathf.Sign(dx));
    }

    private void EnterIdle()
    {
        _step = Step.Idle;
        _idleTimer = _restTime;
    }

    private void EnterRecover(float time)
    {
        _step = Step.Recover;
        _timer = time;
    }

    /// <summary>次の攻撃を選ぶ (同じ攻撃は続けない。紅茶の雨・分離回転はクールダウン付き)。</summary>
    private void ChooseNext()
    {
        _candidates.Clear();
        for (var i = 0; i < (int)AttackId.Count; i++)
        {
            var id = (AttackId)i;
            if (id != _lastAttack && _cooldowns[i] <= 0f)
                _candidates.Add(id);
        }
        if (_candidates.Count == 0)
            _candidates.Add(_lastAttack == AttackId.Splash ? AttackId.Dash : AttackId.Splash);

        var pick = _candidates[Random.Range(0, _candidates.Count)];
        _lastAttack = pick;
        switch (pick)
        {
            case AttackId.Splash: BeginSplash(); break;
            case AttackId.Dash: BeginDashWindup(); break;
            case AttackId.Rain: BeginRain(); break;
            case AttackId.Split: BeginSplitWindup(); break;
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

    /// <summary>体が光る予告 (体に付いて動く、脈打つ光)。</summary>
    private void AddGlow(float size)
    {
        _glow = AddTelegraph("CupGlow", BossKit.CircleSprite, _glowColor, BodyCenter, new Vector2(size, size));
        _glow.sortingOrder = BossKit.MarkerSortingOrder + 5;
        _glow.transform.DOScale(new Vector3(size * 1.3f, size * 1.3f, 1f), 0.1f).SetLoops(-1, LoopType.Yoyo);
    }

    private void ClearTelegraphs()
    {
        foreach (var t in _telegraphs)
            BossKit.DestroyMarker(t);
        _telegraphs.Clear();
        _glow = null;
    }

    #endregion

    #region 飛沫撒き

    private void BeginSplash()
    {
        FacePlayer();
        _step = Step.SplashWindup;
        _timer = _splashWindup;
        AddGlow(1.6f);
    }

    /// <summary>プレイヤーの足元と左右 ±_splashSpread へ、放物線で飛沫を 3 つ撒く (斬ると消せる)。</summary>
    private void Splash()
    {
        var origin = BodyCenter + new Vector2(0f, 0.6f);
        var px = PlayerPoint().x;
        var feet = PlayerFeetY();
        for (var i = -1; i <= 1; i++)
        {
            var target = new Vector2(px + i * _splashSpread, feet + _splashRadius);
            var d = target - origin;
            var t = Mathf.Clamp(0.45f + d.magnitude * 0.06f, _splashFlightTime.x, _splashFlightTime.y);
            var velocity = new Vector2(d.x / t, (d.y + 0.5f * _splashGravity * t * t) / t);
            CupDroplet.Spawn(origin, velocity, _splashGravity, _splashRadius, _splashDamage, 4f, gameObject, _ground, true, _teaColor);
        }
    }

    #endregion

    #region 斜め体当たり

    private void BeginDashWindup()
    {
        // 狙い = 予告を始めた時のプレイヤー位置
        _dashTarget = PlayerPoint();
        var d = _dashTarget - BodyCenter;
        _dashDir = d.sqrMagnitude > 0.0001f ? d.normalized : Vector2.down;
        Enemy.Face(Mathf.Sign(_dashDir.x));
        _step = Step.DashWindup;
        _timer = _dashWindup;
        AddGlow(1.3f);
    }

    private void BeginDash()
    {
        ClearTelegraphs();
        var d = _dashTarget - BodyCenter;
        _dashDir = d.sqrMagnitude > 0.0001f ? d.normalized : _dashDir;
        _step = Step.Dash;
        _timer = _dashMaxTime;
    }

    private void TickDash()
    {
        _rb.linearVelocity = _dashDir * _dashSpeed;
        // 体当たりの判定 (接触ダメージより先に、技のダメージを入れる)
        if (_col != null && _col.enabled)
            BossKit.HitPlayerBox(_col.bounds.center, _col.bounds.size, _dashDamage, gameObject);

        var passed = Vector2.Dot(_dashTarget - BodyCenter, _dashDir) <= 0f;
        if (!passed && _timer > 0f)
            return;

        _step = Step.DashDrift;
        _timer = _dashDriftTime;
        _rb.linearVelocity = _dashDir * (_dashSpeed * 0.35f);
    }

    #endregion

    #region 紅茶の雨

    private void BeginRain()
    {
        _cooldowns[(int)AttackId.Rain] = _rainCooldown;
        _step = Step.RainRise;
        _timer = _rainRiseMaxTime;
    }

    private void TickRainRise(float dt)
    {
        var b = ArenaBounds();
        var target = new Vector2(b.center.x, b.max.y - _rainHoverBelowTop);
        var pos = _rb.position;
        var delta = target - pos;
        var step = _rainRiseSpeed * dt;
        _rb.linearVelocity = delta.magnitude <= step ? delta / dt : delta.normalized * _rainRiseSpeed;
        if (delta.magnitude > 0.1f && _timer > 0f)
            return;

        // 傾く: 端から落とす側へ注ぐように
        _rainFromLeft = Random.value < 0.5f;
        _rainSafe = Random.Range(0, Mathf.Max(1, _rainColumns));
        Enemy.Face(_rainFromLeft ? -1f : 1f);
        ResetTilt(_rainFromLeft ? _rainTiltAngle : -_rainTiltAngle, _rainTiltTime);
        _step = Step.RainTilt;
        _timer = _rainTiltTime;
    }

    private void BeginRainPour()
    {
        _step = Step.RainPour;
        _rainNext = 0;
        _timer = 0f;
    }

    /// <summary>端の列から順に _rainInterval おきで床に印を出し、_rainTelegraph 後に滴を落とす (逃げ場の列は飛ばす)。</summary>
    private void TickRainPour()
    {
        var columns = Mathf.Max(1, _rainColumns);
        while (_timer <= 0f && _rainNext < columns)
        {
            var column = _rainFromLeft ? _rainNext : columns - 1 - _rainNext;
            if (column != _rainSafe)
                TelegraphRainColumn(column, columns);
            _rainNext++;
            _timer += _rainInterval;
        }

        if (_rainNext < columns)
            return;

        // 最後の列の滴が出てから少し待って傾きを戻す
        _step = Step.RainEnd;
        _timer = _rainTelegraph + _rainAfterTime;
        DOVirtual.DelayedCall(_rainTelegraph, () =>
        {
            if (this != null && _step == Step.RainEnd)
                ResetTilt(0f, _rainTiltTime);
        }, false).SetLink(gameObject);
    }

    private void TelegraphRainColumn(int column, int columns)
    {
        var b = ArenaBounds();
        var width = b.size.x / columns;
        var x0 = b.min.x + column * width;
        var cx = x0 + width * 0.5f;
        var floor = BossKit.FindFloorY(cx, b.max.y - 0.5f, b.size.y + 6f, _ground, b.min.y);
        var top = b.max.y - 0.5f;
        BossTimedMarker.Create("TeaRainSpot", BossKit.SquareSprite, _dangerColor,
            new Vector2(cx, floor + 0.08f), new Vector2(width * 0.9f, 0.16f), _rainTelegraph,
            () => DropRainColumn(x0, width, top));
    }

    private void DropRainColumn(float x0, float width, float top)
    {
        if (this == null)
            return;
        var count = Mathf.Max(1, Mathf.RoundToInt(width / Mathf.Max(0.2f, _rainDropSpacing)));
        var spacing = width / count;
        for (var i = 0; i < count; i++)
        {
            var x = x0 + (i + 0.5f) * spacing;
            var y = top + Random.Range(0f, 0.8f);
            CupDroplet.Spawn(new Vector2(x, y), new Vector2(0f, -_rainDropSpeed), _rainGravity, _rainDropRadius,
                _rainDamage, 3f, gameObject, _ground, false, _teaColor);
        }
    }

    /// <summary>傾きを変える (time 0 なら即座に)。</summary>
    private void ResetTilt(float angle, float time = 0f)
    {
        // 登場の弾み (DOPunchScale) が途中なら終わらせて大きさを 1 に戻す
        transform.DOKill(true);
        if (time <= 0f)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            if (_rb != null)
                _rb.rotation = angle;
            return;
        }
        transform.DORotate(new Vector3(0f, 0f, angle), time).SetUpdate(UpdateType.Fixed);
    }

    #endregion

    #region 分離回転

    private void BeginSplitWindup()
    {
        _cooldowns[(int)AttackId.Split] = _splitCooldown;
        FacePlayer();
        _step = Step.SplitWindup;
        _timer = _splitWindup;
        AddGlow(2f);
    }

    /// <summary>積み重なったカップが 3 つに分かれる (本体の絵と当たり判定を隠し、小さなカップを回す)。</summary>
    private void BeginSplit()
    {
        ClearTelegraphs();
        _step = Step.Split;
        _timer = _splitDuration;
        _splitTime = 0f;
        _sprite.enabled = false;
        if (_col != null)
            _col.enabled = false;
        foreach (var m in _minions)
            if (m != null) m.SetVisible(true, Color.white);
    }

    private void TickSplit(float dt)
    {
        _splitTime += dt;
        // ゆっくりプレイヤーを追う
        var toPlayer = PlayerPoint() - _rb.position;
        var chase = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized * _splitChaseSpeed : Vector2.zero;
        _rb.linearVelocity = chase;

        var center = _rb.position;
        var count = Mathf.Max(1, _minions.Length);
        for (var i = 0; i < _minions.Length; i++)
        {
            var m = _minions[i];
            if (m == null)
                continue;
            var angle = _splitTime * Mathf.PI * 2f / Mathf.Max(0.1f, _orbitPeriod) + i * Mathf.PI * 2f / count;
            var pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _orbitRadius;
            m.SetPose(pos, Mathf.Sin(angle * 2f) * 15f, 1f);
            BossKit.HitPlayerCircle(pos, _orbitHitRadius, _splitDamage, gameObject);
        }

        if (_timer > 0f)
            return;

        _step = Step.Restack;
        _timer = _restackTime;
        _poseFrom.Clear();
        foreach (var m in _minions)
        {
            var p = m != null ? m.transform.position : Vector3.zero;
            _poseFrom.Add(new Vector3(p.x, p.y, m != null ? m.transform.eulerAngles.z : 0f));
        }
    }

    private void TickRestack()
    {
        Hold();
        var t = 1f - Mathf.Clamp01(_timer / Mathf.Max(0.01f, _restackTime));
        var flipped = _sprite.flipX;
        for (var i = 0; i < _minions.Length; i++)
        {
            var m = _minions[i];
            if (m == null)
                continue;
            m.Sprite.flipX = flipped;
            var to = _rb.position + StackOffset(i, flipped);
            m.SetPose(Vector2.Lerp(_poseFrom[i], to, t), Mathf.LerpAngle(_poseFrom[i].z, 0f, t), Mathf.Lerp(1f, _stackScale, t));
        }
        if (_timer > 0f)
            return;

        EndSplitImmediate();
        EnterIdle();
    }

    /// <summary>分離していたら即座に積み直す (小さなカップを隠し、本体の絵と当たり判定を戻す)。</summary>
    private void EndSplitImmediate()
    {
        if (_phase != Phase.Merged)
            return;
        foreach (var m in _minions)
            if (m != null) m.SetVisible(false);
        if (_sprite != null)
            _sprite.enabled = true;
        if (_col != null)
            _col.enabled = true;
    }

    #endregion

    #region 撃破

    /// <summary>撃破: 3 つのカップに割れて、落ちながら消える (実行時の絵の破片)。本体の絵はすぐ隠す。</summary>
    private void BreakApart()
    {
        var flipped = _sprite.flipX;
        var center = _rb.position;
        _sprite.enabled = false;
        for (var i = 0; i < _minions.Length; i++)
        {
            var m = _minions[i];
            if (m == null || m.Sprite == null)
                continue;
            m.SetVisible(false);

            var go = new GameObject("CupShard");
            go.transform.position = center + StackOffset(i, flipped);
            go.transform.localScale = new Vector3(_stackScale, _stackScale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = m.Sprite.sprite;
            sr.flipX = flipped;
            sr.sharedMaterial = m.Sprite.sharedMaterial;
            sr.sortingLayerID = _sprite.sortingLayerID;
            sr.sortingOrder = _sprite.sortingOrder + i;

            var side = (i - (_minions.Length - 1) * 0.5f) * 1.5f + Random.Range(-0.3f, 0.3f);
            var end = (Vector2)go.transform.position + new Vector2(side, -_deathFallDistance);
            var spin = (side >= 0f ? -1f : 1f) * Random.Range(180f, 360f);
            var seq = DOTween.Sequence().SetLink(go);
            seq.Join(go.transform.DOMove(end, _deathFallTime).SetEase(Ease.InQuad));
            seq.Join(go.transform.DORotate(new Vector3(0f, 0f, spin), _deathFallTime, RotateMode.FastBeyond360));
            seq.Join(sr.DOFade(0f, _deathFallTime).SetEase(Ease.InCubic));
            seq.OnComplete(() => Destroy(go));
        }
    }

    #endregion
}
