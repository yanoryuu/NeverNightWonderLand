using UnityEngine;

/// <summary>
/// 動く足場 (整備用リフト・観覧車のゴンドラ・回る歯車)。Kinematic の Rigidbody2D を
/// 「目標位置への速度」で動かすので、乗ったプレイヤーは PlayerController の外力フック
/// (Kinematic な地面の接地点速度を上乗せ) で横にも縦にもずれずに運ばれる。
/// モード:
///   PingPong … 経由点 (_waypoints, 開始位置からのオフセット) を往復。各点で _waitTime 停止。
///   Loop     … 経由点を最後まで行ったら最初へ戻って周回。
///   Orbit    … 中心 (_orbitCenter か開始位置) の周りを半径 _radius で回る。
///              回転はしないので、ゴンドラは常に上向きのまま。
/// _spinSpeed を入れると足場自体が回転する (歯車の見た目用。乗る足場には使わない方が無難)。
/// _requiredFlag を設定すると、そのフラグが立つまで停止している (レバーで動き出すリフト)。
/// </summary>
[DefaultExecutionOrder(-100)] // プレイヤーより先に今ステップの速度を決める (運搬の1ステップ遅れを防ぐ)
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    public enum Mode
    {
        PingPong,
        Loop,
        Orbit,
    }

    [Tooltip("動き方")]
    [SerializeField] private Mode _mode = Mode.PingPong;

    [Header("PingPong / Loop")]
    [Tooltip("経由点 (開始位置からのオフセット, units)。先頭は通常 (0,0)")]
    [SerializeField] private Vector2[] _waypoints = { Vector2.zero, new Vector2(0f, 6f) };

    [Tooltip("移動速度 (units/sec)")]
    [SerializeField] private float _speed = 2f;

    [Tooltip("各経由点での停止時間 (sec)")]
    [SerializeField] private float _waitTime = 1f;

    [Tooltip("区間の始めと終わりを減速させる (リフトらしい動き)")]
    [SerializeField] private bool _easeInOut = true;

    [Header("Orbit")]
    [Tooltip("回転の中心。未設定なら開始位置を中心にする")]
    [SerializeField] private Transform _orbitCenter;

    [Tooltip("回転半径 (units)")]
    [SerializeField] private float _radius = 6f;

    [Tooltip("角速度 (deg/sec)。正で反時計回り")]
    [SerializeField] private float _angularSpeed = 15f;

    [Tooltip("開始角度 (deg)。0 = 中心の右、90 = 真上。複数ゴンドラはこれをずらす")]
    [SerializeField] private float _phaseDegrees;

    [Header("共通")]
    [Tooltip("足場自体の回転速度 (deg/sec)。0 なら回転しない (ゴンドラは 0)")]
    [SerializeField] private float _spinSpeed;

    [Tooltip("このフラグ (GameProgress) が立つまで停止する。空なら最初から動く")]
    [SerializeField] private string _requiredFlag = "";

    [Tooltip("動き出すまでの遅延 (sec)。複数のリフトの位相をずらす用")]
    [SerializeField] private float _startDelay;

    private Rigidbody2D _rb;
    private Vector2 _origin;      // 開始位置 (経由点の基準)
    private bool _running;
    private float _delayTimer;

    // PingPong / Loop の進行
    private int _segmentFrom;
    private int _direction = 1;
    private float _segmentT;
    private float _waitTimer;

    // Orbit の進行
    private float _angle;

    /// <summary>動いているか (フラグ待ち・遅延中は false)。</summary>
    public bool IsRunning => _running && _delayTimer <= 0f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.useFullKinematicContacts = false;
        _origin = _rb.position;
        _angle = _phaseDegrees;
        _delayTimer = _startDelay;

        if (_mode == Mode.Orbit)
        {
            // 置いた位置に関係なく、開始角の軌道上から始める
            var start = OrbitPosition(_angle);
            transform.position = start;
            _rb.position = start;
        }
        else if (_waypoints != null && _waypoints.Length > 0)
        {
            var start = _origin + _waypoints[0];
            transform.position = start;
            _rb.position = start;
        }
    }

    private void OnEnable()
    {
        GameProgress.Changed += OnFlagChanged;
        _running = string.IsNullOrEmpty(_requiredFlag) || GameProgress.Has(_requiredFlag);
    }

    private void OnDisable()
    {
        GameProgress.Changed -= OnFlagChanged;
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }
    }

    private void OnFlagChanged(string flag)
    {
        if (flag == _requiredFlag)
            _running = true;
    }

    private void FixedUpdate()
    {
        var dt = Time.fixedDeltaTime;

        if (!_running || _delayTimer > 0f)
        {
            if (_running)
                _delayTimer -= dt;

            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
            return;
        }

        var target = _mode == Mode.Orbit ? StepOrbit(dt) : StepWaypoints(dt);

        // 目標位置へちょうど届く速度を与える (MovePosition と違い、速度として読めるので乗り手を運べる)
        _rb.linearVelocity = (target - _rb.position) / dt;
        _rb.angularVelocity = _spinSpeed;
    }

    private Vector2 StepOrbit(float dt)
    {
        _angle += _angularSpeed * dt;
        if (_angle > 360f) _angle -= 360f;
        else if (_angle < -360f) _angle += 360f;
        return OrbitPosition(_angle);
    }

    private Vector2 OrbitPosition(float angleDeg)
    {
        var center = _orbitCenter != null ? (Vector2)_orbitCenter.position : _origin;
        var rad = angleDeg * Mathf.Deg2Rad;
        return center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _radius;
    }

    private Vector2 StepWaypoints(float dt)
    {
        if (_waypoints == null || _waypoints.Length < 2 || _speed <= 0f)
            return _rb.position;

        if (_waitTimer > 0f)
        {
            _waitTimer -= dt;
            return _origin + _waypoints[_segmentFrom];
        }

        var to = NextIndex(_segmentFrom);
        var a = _origin + _waypoints[_segmentFrom];
        var b = _origin + _waypoints[to];
        var length = Vector2.Distance(a, b);

        _segmentT += length > 0.0001f ? _speed * dt / length : 1f;
        if (_segmentT >= 1f)
        {
            // 経由点に到着: 次の区間へ (到着点で停止)
            _segmentT = 0f;
            _segmentFrom = to;
            _waitTimer = _waitTime;
            if (_mode == Mode.PingPong && (to == 0 || to == _waypoints.Length - 1))
                _direction = to == 0 ? 1 : -1;
            return b;
        }

        var t = _easeInOut ? Mathf.SmoothStep(0f, 1f, _segmentT) : _segmentT;
        return Vector2.Lerp(a, b, t);
    }

    private int NextIndex(int from)
    {
        if (_mode == Mode.Loop)
            return (from + 1) % _waypoints.Length;

        var next = from + _direction;
        if (next < 0 || next >= _waypoints.Length)
        {
            _direction = -_direction;
            next = from + _direction;
        }

        return next;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.6f, 0.9f);

        if (_mode == Mode.Orbit)
        {
            var center = _orbitCenter != null
                ? (Vector2)_orbitCenter.position
                : Application.isPlaying ? _origin : (Vector2)transform.position;
            const int steps = 48;
            for (var i = 0; i < steps; i++)
            {
                var a0 = i * Mathf.PI * 2f / steps;
                var a1 = (i + 1) * Mathf.PI * 2f / steps;
                Gizmos.DrawLine(center + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * _radius,
                    center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * _radius);
            }

            var rad = _phaseDegrees * Mathf.Deg2Rad;
            Gizmos.DrawWireSphere(center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _radius, 0.3f);
            return;
        }

        if (_waypoints == null)
            return;

        var origin = Application.isPlaying ? _origin : (Vector2)transform.position;
        for (var i = 0; i < _waypoints.Length; i++)
        {
            Gizmos.DrawWireSphere(origin + _waypoints[i], 0.25f);
            if (i + 1 < _waypoints.Length)
                Gizmos.DrawLine(origin + _waypoints[i], origin + _waypoints[i + 1]);
        }

        if (_mode == Mode.Loop && _waypoints.Length > 2)
            Gizmos.DrawLine(origin + _waypoints[^1], origin + _waypoints[0]);
    }
}
