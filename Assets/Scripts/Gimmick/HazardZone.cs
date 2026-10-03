using UnityEngine;

/// <summary>
/// 触れるとダメージを受けるゾーン (トゲ・蒸気・穴・ピストンの押しつぶし面)。
/// 敵の接触ダメージと同じく PlayerHealth.TakeDamage を通すので、被弾後の無敵時間・
/// ノックバック (ゾーンの中心から離れる向き)・回避ダッシュ無効などは既存の挙動に従う。
/// 中にいる間は毎物理ステップ判定するので、無敵が切れれば再びダメージを受ける。
/// _cycle を有効にすると ON/OFF を時間で繰り返す (蒸気の噴出)。ON の間だけ _activeVisual を、
/// ON になる直前 _warningTime 秒は _warningVisual を表示する。
/// _returnToSafeGround を有効にすると、触れた時に直前の安全地点 (PlayerSafeGround) へ戻す
/// (トゲ床・奈落。ダメージを受けたかに関係なく戻す)。
/// </summary>
public class HazardZone : PlayerAreaZone
{
    [Tooltip("与えるダメージ (HP)")]
    [SerializeField] private int _damage = 1;

    [Tooltip("触れたら直前の安全地点へ戻す (トゲ床・奈落)")]
    [SerializeField] private bool _returnToSafeGround;

    [Tooltip("戻す時に暗転する (ScreenFader がある場合)")]
    [SerializeField] private bool _fadeOnReturn = true;

    [Header("ON/OFF の周期 (蒸気など)")]
    [Tooltip("時間で ON/OFF を繰り返す。無効なら常に ON")]
    [SerializeField] private bool _cycle;

    [Tooltip("ON の時間 (sec)")]
    [SerializeField] private float _onTime = 1.5f;

    [Tooltip("OFF の時間 (sec)")]
    [SerializeField] private float _offTime = 2f;

    [Tooltip("周期の開始オフセット (sec)。並んだ噴出口の位相をずらす用")]
    [SerializeField] private float _phaseOffset;

    [Tooltip("ON になる何秒前から予兆を出すか")]
    [SerializeField] private float _warningTime = 0.5f;

    [Tooltip("ON の間だけ表示するオブジェクト (蒸気の見た目など, 任意)")]
    [SerializeField] private GameObject _activeVisual;

    [Tooltip("ON になる直前に表示する予兆オブジェクト (任意)")]
    [SerializeField] private GameObject _warningVisual;

    private bool _hazardActive = true;
    private bool _externallyDisabled; // Piston などが SetHazardEnabled(false) で止めている
    private float _time;
    private bool _returning;

    /// <summary>現在ダメージ判定が有効か。</summary>
    public bool IsHazardActive => _hazardActive && !_externallyDisabled;

    /// <summary>外部 (Piston など) から判定の有効/無効を切り替える。周期とは AND で効く。</summary>
    public void SetHazardEnabled(bool value)
    {
        _externallyDisabled = !value;
    }

    protected override void Awake()
    {
        base.Awake();
        _time = _phaseOffset;
        UpdateCycle(0f);
    }

    private void Start()
    {
        // 戻し先の記録は落ちる前から必要なので、シーン開始時にプレイヤーへ記録係を付けておく
        if (_returnToSafeGround)
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null)
                EnsureSafeGround(player);
        }
    }

    private static PlayerSafeGround EnsureSafeGround(PlayerController player) =>
        player.TryGetComponent<PlayerSafeGround>(out var safe)
            ? safe
            : player.gameObject.AddComponent<PlayerSafeGround>();

    private void FixedUpdate()
    {
        UpdateCycle(Time.fixedDeltaTime);

        if (Player == null || !IsHazardActive || Player.IsDead)
            return;

        var health = Player.Health;
        if (health != null && _damage > 0)
        {
            var info = new DamageInfo(_damage, 0, ZoneCollider.bounds.center, gameObject);
            health.TakeDamage(info);
        }

        if (_returnToSafeGround && !Player.IsDead)
            ReturnToSafeGround(Player);
    }

    private void UpdateCycle(float dt)
    {
        if (!_cycle)
        {
            _hazardActive = true;
            SetVisuals(active: !_externallyDisabled, warning: false);
            return;
        }

        var period = Mathf.Max(0.01f, _onTime + _offTime);
        _time = Mathf.Repeat(_time + dt, period);

        // 周期: [0, on) ON → [on, period) OFF
        _hazardActive = _time < _onTime;
        SetVisuals(active: _hazardActive && !_externallyDisabled,
            warning: !_hazardActive && period - _time <= _warningTime);
    }

    private void SetVisuals(bool active, bool warning)
    {
        if (_activeVisual != null && _activeVisual.activeSelf != active)
            _activeVisual.SetActive(active);
        if (_warningVisual != null && _warningVisual.activeSelf != warning)
            _warningVisual.SetActive(warning);
    }

    private void ReturnToSafeGround(PlayerController player)
    {
        if (_returning)
            return;

        var safe = EnsureSafeGround(player);
        if (!safe.TryGetReturnPosition(out var position))
            return;

        var fader = _fadeOnReturn ? ScreenFader.Instance : null;
        if (fader == null)
        {
            Warp(player, position);
            return;
        }

        _returning = true;
        fader.FadeOut(() =>
        {
            _returning = false;
            if (player != null && !player.IsDead)
                Warp(player, position);
            fader.FadeIn();
        });
    }

    private static void Warp(PlayerController player, Vector2 position)
    {
        player.Teleport(position);

        var follow = FindAnyObjectByType<CameraFollow>();
        if (follow != null)
            follow.SnapToTarget();
    }
}
